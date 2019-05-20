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
 * Variables
 */
bool publishingError = false;
DirectoryPath artifactDir = (DirectoryPath)Directory("./.artifacts");

/*
 * Tasks
 */
Task("Clean")   
    .Does(() => 
    {
        CleanDirectory(artifactDir.FullPath);

        CleanDirectories($"./**/bin/{configuration}");
        CleanDirectories("./**/obj");
    });

Task("Restore")
    .IsDependentOn("Clean")
    .Does(() => 
    {
        foreach (var project in GetFiles("./**/*.csproj"))
        {
            DotNetCoreRestore(project.FullPath);      
        }
    });

Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        DotNetCoreBuildSettings settings = new DotNetCoreBuildSettings
        {
            NoRestore = true,
            Configuration = configuration
        };

        foreach (var project in GetFiles("./src/**/*.csproj"))
        {
            DotNetCoreBuild(project.FullPath, settings);     
        }
    });

Task("Test")
    .IsDependentOn("Build")
    .Does(() => 
    {    
        DotNetCoreTestSettings settings = new DotNetCoreTestSettings
        {
            Configuration = configuration
        };

        foreach (var project in GetFiles("./test/**/*.csproj"))
        {   
            DotNetCoreTest(project.FullPath, settings);
        }
    });

Task("Pack")
    .IsDependentOn("Test")
    .Does(() => 
    {    
        DotNetCorePackSettings settings = new DotNetCorePackSettings
        {
            NoBuild = true,
            Configuration = configuration
        };

        foreach (var project in GetFiles("./src/**/*.csproj"))
        {   
            DotNetCorePack(project.FullPath, settings);
        }
    });

Task("Copy")
    .IsDependentOn("Pack") 
    .Does(() =>
    {
        CopyFiles(GetFiles("./src/**/*.nupkg"), artifactDir.FullPath);
    });

Task("Default")
    .IsDependentOn("Copy")
    .Does(() =>
    {

    });

/*
 * Execution
 */
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
RunTarget(target);
