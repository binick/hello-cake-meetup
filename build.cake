/*
 * Install addins.
 */
#addin "nuget:?package=Cake.Coverlet&version=2.2.1"
#addin "nuget:?package=Cake.Json&version=3.0.1"
#addin "nuget:?package=Newtonsoft.Json&version=11.0.2"
#addin "nuget:?package=Cake.Gitter&version=0.11.0"

/*
 * Install tools.
 */
#tool "nuget:?package=GitReleaseNotes&version=0.7.1"
#tool "nuget:?package=GitVersion.CommandLine&version=4.0.0"
#tool "nuget:?package=ReportGenerator&version=4.1.5"
#tool "nuget:?package=xunit.runner.console&version=2.4.1"

/*
 * Load other scripts.
 */
#load "./build/parameters.cake"
#load "./build/utils.cake"

/*
 * Variables
 */
bool publishingError = false;

/*
 * Setup
 */
Setup<BuildParameters>(context =>
{
    var parameters = BuildParameters.GetParameters(Context);
    var gitVersion = GetVersion(parameters);
    parameters.Setup(context, gitVersion, 1);

    if (parameters.IsMainBranch && (context.Log.Verbosity != Verbosity.Diagnostic)) {
        Information("Increasing verbosity to diagnostic.");
        context.Log.Verbosity = Verbosity.Diagnostic;
    }

    Information("Building of Hello Cake ({0}) with dotnet version {1}", parameters.Configuration, GetDotnetVersion());

    Information("Build version : Version {0}, SemVersion {1}, NuGetVersion: {2}",
        parameters.Version.Version, parameters.Version.SemVersion, parameters.Version.NuGetVersion);

    Information("Repository info : IsMainRepo {0}, IsMainBranch {1}, IsTagged: {2}, IsPullRequest: {3}",
        parameters.IsMainRepo, parameters.IsMainBranch, parameters.IsTagged, parameters.IsPullRequest);

    return parameters;
});

/*
 * Teardown
 */
Teardown<BuildParameters>((context, parameters) =>
{
    if(context.Successful)
    {
        Information("Finished running tasks. Thanks for your patience :D");
    }
    else
    {
        Error("Something wrong! :|");
        Error(context.ThrownException.Message);
    }
});

/*
 * Tasks
 */
Task("Clean")   
    .Does<BuildParameters>((parameters) => 
    {
        CleanDirectories(parameters.Paths.Directories.ToClean);

        CleanDirectories($"./**/bin/{parameters.Configuration}");
        CleanDirectories("./**/obj");
    });

Task("Build")
    .IsDependentOn("Clean")
    .Does<BuildParameters>((parameters) =>
    {
        foreach (var project in GetFiles("./src/**/*.csproj"))
        {
            Build(project, parameters.Configuration, parameters.MSBuildSettings);        
        }
    });

Task("Test")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.EnabledUnitTests, "Unit tests were disabled.")
    .IsDependentOn("Build")
    .OnError<BuildParameters>((exception, parameters) => {
        parameters.ProcessVariables.Add("IsTestsFailed", true);
    })
    .Does<BuildParameters>((parameters) => 
    {
        var settings = new DotNetCoreTestSettings 
        {
            Configuration = parameters.Configuration,
            NoBuild = false
        };

        var timestamp = $"{DateTime.UtcNow:dd-MM-yyyy-HH-mm-ss-FFF}";

        var coverletSettings = new CoverletSettings 
        {
            CollectCoverage = true,
            CoverletOutputDirectory = parameters.Paths.Directories.TestCoverageOutput,
            CoverletOutputName = $"results.{timestamp}.xml",
            Exclude = new List<string>() { "[xunit.*]*", "[*.Specs?]*" }
        };

        var projects = GetFiles("./test/**/*.csproj");

        if (projects.Count > 1)
            coverletSettings.MergeWithFile = $"{coverletSettings.CoverletOutputDirectory.FullPath}/{coverletSettings.CoverletOutputName}";

        var i = 1;
        foreach (var project in projects)
        {   
            if (i++ == projects.Count)
                coverletSettings.CoverletOutputFormat = CoverletOutputFormat.cobertura;

            var projectName = project.GetFilenameWithoutExtension();
            Information("Run specs for {0}", projectName);

            settings.ArgumentCustomization = args => args
                .Append("--logger").AppendQuoted($"trx;LogFileName={MakeAbsolute(parameters.Paths.Directories.TestResultOutput).FullPath}/{projectName}_{timestamp}.trx");

            DotNetCoreTest(project.FullPath, settings, coverletSettings);
        }
    });

Task("Coverage-Report")
    .WithCriteria<BuildParameters>((context, parameters) => GetFiles($"{parameters.Paths.Directories.TestCoverageOutput.FullPath}/**/*.xml").Count != 0)
    .Does<BuildParameters>((parameters) => 
    {
        var settings = new ReportGeneratorSettings
        {
            ReportTypes = { ReportGeneratorReportType.HtmlInline }
        };

        if (parameters.IsRunningOnAzurePipeline)
            settings.ReportTypes.Add(ReportGeneratorReportType.HtmlInline_AzurePipelines);

        ReportGenerator(GetFiles($"{parameters.Paths.Directories.TestCoverageOutput.FullPath}/**/*.xml"), parameters.Paths.Directories.TestCoverageOutputResults, settings);
    });

Task("Copy-Files")
    .Does<BuildParameters>((parameters) => 
    {
        Information("Copy static files to artifacts"); 
        CopyFileToDirectory("./LICENSE", parameters.Paths.Directories.Artifacts);

        foreach (var project in GetFiles("./src/**/*.csproj"))
        {
            var settings = new DotNetCorePackSettings 
            {
                NoBuild = true,
                NoRestore = true,
                Configuration = parameters.Configuration,
                OutputDirectory = parameters.Paths.Directories.ArtifactsOutput,
                MSBuildSettings = parameters.MSBuildSettings
            };

            Information("Run pack for {0} to {1}", project.GetFilenameWithoutExtension(), settings.OutputDirectory); 
            DotNetCorePack(project.FullPath, settings);
        }
    });

Task("Release-Notes")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnWindows,  "Release notes are generated only on Windows agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsRunningOnAzurePipeline, "Release notes are generated only on release agents.")
    .WithCriteria<BuildParameters>((context, parameters) => parameters.IsStableRelease(),   "Release notes are generated only for stable releases.")
    .Does<BuildParameters>((parameters) => 
    {
        GetReleaseNotes(parameters.Paths.Files.ReleaseNotes);

        if (string.IsNullOrEmpty(System.IO.File.ReadAllText(parameters.Paths.Files.ReleaseNotes.FullPath)))
            System.IO.File.WriteAllText(parameters.Paths.Files.ReleaseNotes.FullPath, "No issues closed since last release");
    });

Task("Copy")
    .IsDependentOn("Test")
    .IsDependentOn("Coverage-Report")
    .IsDependentOn("Copy-Files")    
    .Does(() =>
    {

    });

Task("Pack")
    .IsDependentOn("Copy")
    .IsDependentOn("Release-Notes")
    .Does(() =>
    {

    });

Task("Default")
    .IsDependentOn("Pack")
    .Does(() =>
    {

    });

/*
 * Execution
 */
var target = Argument("target", "Default");
RunTarget(target);
