/*
 * Install addins
 */
#addin nuget:?package=Cake.Coverlet&version=2.3.4
#addin nuget:?package=Cake.MicrosoftTeams&version=0.9.0
#addin nuget:?package=Cake.WebDeploy&version=0.3.4

/*
 * Install tools
 */
#tool nuget:?package=xunit.runner.console&version=2.4.1
#tool nuget:?package=ReportGenerator&version=4.2.19
#tool nuget:?package=coverlet.console&version=1.5.3

/*
 * Load scripts
 */
#load "build/microsoft-teams.cake"

/*
 * Variables.
 */
var isTestFailed = false;
var isDeployFailed = false;
Exception exception = null;
var artifactRoot = Directory("./.artifacts").Path;
var testResult = artifactRoot.Combine(Directory("test-results"));
var output = artifactRoot.Combine(Directory("output"));
var binary = artifactRoot.Combine(Directory("bin"));
IEnumerable<string> runtimes = new string[] {};

/*
 * Setup
 */
Setup(context => {
    runtimes = XmlPeek(
        "./Directory.Build.props",
        "Project/PropertyGroup/RuntimeIdentifier").Split(';');
});

Teardown(context => {
    MicrosoftTeamsMessageCard card = null;
    if (context.Successful 
        && !isTestFailed
        && !isDeployFailed) 
    {
        Information("Good job!");
        card = GetMsTeamsCard(
            "Build with Cake",
            "Process is complete.",
            exception);
    }  
    else
    {
        Information("Bad job!");
        if (isTestFailed && !isDeployFailed)
            card = GetMsTeamsCard(
                "Build with Cake",
                "Build is complete.",
                exception);
        else
            card = GetMsTeamsCard(
                "Deploy with Cake",
                "Deploy is complete.",
                exception);
    }
    
    if (BuildSystem.IsRunningOnAzurePipelines 
        || BuildSystem.IsRunningOnAzurePipelinesHosted)
        SendMessage(
            EnvironmentVariable("MICROSFT_TEAMS_WEBHOOK"),
            card);
});

/*
 * Tasks
 */
Task("Clean")   
    .Does(() => 
    {
        CleanDirectories(new string[] 
        { 
            artifactRoot.FullPath,
            testResult.FullPath,
            output.FullPath,
            binary.FullPath
        });
        CleanDirectories($"./**/bin/{configuration}");
        CleanDirectories("./**/obj");
    });

Task("Test")
    .IsDependentOn("Clean")
    .ReportError((ex) => {
        isTestFailed = true;
        exception = ex;
    })
    .ContinueOnError()
    .Does(() => 
    {  
        var settings = new DotNetCoreTestSettings 
        {
            Configuration = configuration
        };

        var timestamp = $"{DateTime.UtcNow:dd-MM-yyyy-HH-mm-ss-FFF}";

        var coverletSettings = new CoverletSettings 
        {
            CollectCoverage = true,
            CoverletOutputDirectory = testResult,
            CoverletOutputName = $"coverage.{timestamp}.xml",
            Exclude = { "[xunit.*]*", "[*.Tests?]*" }
        };

        var projects = GetFiles("./**/*.Tests.csproj");

        if (projects.Count > 1)
            coverletSettings.MergeWithFile = $"{coverletSettings.CoverletOutputDirectory.FullPath}/{coverletSettings.CoverletOutputName}";

        var i = 1;
        foreach (var project in projects)
        {   
            if (i++ == projects.Count)
                coverletSettings.CoverletOutputFormat = CoverletOutputFormat.cobertura;

            var projectName = project.GetFilenameWithoutExtension();

            settings.ArgumentCustomization = args => args
                .Append("--logger")
                .AppendQuoted($"trx;LogFileName={MakeAbsolute(testResult).FullPath}/{projectName}_{timestamp}.trx");

            Information("Execute tests for {0}", projectName);
            DotNetCoreTest(
                project.FullPath,
                settings,
                coverletSettings);
        }

        Information("Generate code coverage reports");
        ReportGenerator(
            GetFiles($"{testResult}/**/coverage.*"),
            testResult.Combine("coverage"),
            new ReportGeneratorSettings
            {
                ReportTypes = { ReportGeneratorReportType.HtmlInline_AzurePipelines }
            });
    });

Task("Publish")
    .IsDependentOn("Test")
    .Does(() => 
    {
        foreach (var project in GetFiles("./**/*.csproj").Where(p => !p.GetFilenameWithoutExtension().ToString().Contains("Tests")))
            foreach (var runtime in runtimes) 
            {
                Information("Publish {0} for {1}", project.GetFilenameWithoutExtension(), runtime);
                DotNetCorePublish(
                    project.FullPath,
                    new DotNetCorePublishSettings
                    {
                        Configuration = configuration,
                        Runtime = runtime,
                        SelfContained = true,
                        OutputDirectory = $"{binary}/{runtime}/{project.GetFilenameWithoutExtension()}"
                    });
            }
    });
    
Task("Copy")
    .IsDependentOn("Publish")  
    .Does(() =>
    {
        CopyFiles(GetFiles($"{testResult}/**"), output);
        CopyFile(File("LICENSE"), $"{output}/LICENSE.txt");
        CopyFile(File("README.md"), $"{output}/README.md");
        CopyFile(File("README.md"), $"{output}/README.md");
        foreach (var runtimeBuildPath in GetSubDirectories(binary))
        {
            var runtime = runtimeBuildPath.Segments.Last();
            CreateDirectory($"{output}/{runtime}");
            foreach (var project in GetSubDirectories($"{runtimeBuildPath}").Where(d => !d.Segments.Last().Contains("Tests")))
            {
                Information("Generate zip {0} for {1}", project.Segments.Last(), runtime);
                Zip(project, $"{output}/{runtime}/{project.Segments.Last()}.zip");
            }
        }
    });

Task("Build")
    .IsDependentOn("Copy")
    .Does(() => 
    {
    });

Task("Deploy-WebSite")
    .IsDependentOn("Build")
    .ReportError((ex) => {
        isDeployFailed = true;
        exception = ex;
    })
    .ContinueOnError()
    .Does(() =>
    {
        CopyFile("./deploy/hello_cake.web.config", EnvironmentVariable("DEPLOYMENT_SOURCE"));
        DeployWebsite(new DeploySettings
        {
            SourcePath = EnvironmentVariable("DEPLOYMENT_SOURCE"),
            SiteName = EnvironmentVariable("DEPLOYMENT_SITE"),
            ComputerName = EnvironmentVariable("DEPLOYMENT_URL"),
            Username = EnvironmentVariable("DEPLOYMENT_USERNAME"),
            Password = EnvironmentVariable("DEPLOYMENT_PASSWORD")
        });
    });

Task("Deploy")
    .IsDependentOn("Deploy-WebSite")
    .Does(() => 
    {
    });
    

Task("Default")
    .IsDependentOn("Build")
    .Does(() =>
    {
    });

/*
 * Execution
 */
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

RunTarget(target);
