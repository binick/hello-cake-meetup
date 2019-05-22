#load "./version.cake"

public class BuildPaths
{
    public BuildFiles Files { get; private set; }
    public BuildDirectories Directories { get; private set; }

    public static BuildPaths GetPaths(ICakeContext context, string configuration, BuildVersion version)
    {
        if (context == null)
            throw new ArgumentNullException(nameof(context));
        if (string.IsNullOrWhiteSpace(configuration))
            throw new ArgumentNullException(nameof(configuration));
        if (version == null)
            throw new ArgumentNullException(nameof(version));

        var semVersion = version.SemVersion;
        var artifactsDir = (DirectoryPath)(context.Directory("./.artifacts") + context.Directory("v" + semVersion));
        var binArtifactsDir = artifactsDir.Combine("bin");
        var artifactsOutputDir = artifactsDir.Combine("output");
        var licensePath = $"{artifactsOutputDir.FullPath}/LICENSE.txt";
        var releaseNotesPath = $"{artifactsOutputDir.FullPath}/release-notes.md";

        var testResultOutputDir = artifactsDir.Combine("test-results");
        var testCoverageOutputDir = artifactsDir.Combine("code-coverage");
        var testConverageOutputResultsDir = testCoverageOutputDir.Combine("results");
        
        var zipArtifactPath = $"{artifactsOutputDir.FullPath}/HelloCake.zip";

        // Directories
        var buildDirectories = new BuildDirectories(artifactsDir, binArtifactsDir, artifactsOutputDir, testResultOutputDir, testCoverageOutputDir, testConverageOutputResultsDir);

        // Files
        var buildFiles = new BuildFiles(context, licensePath, releaseNotesPath, zipArtifactPath);

        return new BuildPaths
        {
            Files = buildFiles,
            Directories = buildDirectories
        };
    }
}

public class BuildFiles
{
    public FilePath License { get; private set; }
    public FilePath ReleaseNotes { get; private set; }
    public FilePath ZipArtifact { get; private set; }
    
    public BuildFiles(ICakeContext context, FilePath license, FilePath releaseNotes, FilePath zipArtifact)
    {
        License = license;
        ReleaseNotes = releaseNotes;
        ZipArtifact = zipArtifact;
    }
}

public class BuildDirectories
{
    public DirectoryPath Artifacts { get; private set; }
    public DirectoryPath ArtifactsBin { get; private set; }
    public DirectoryPath ArtifactsOutput {get; private set; }
    public DirectoryPath TestResultOutput { get; private set; }
    public DirectoryPath TestCoverageOutput { get; private set; }
    public DirectoryPath TestCoverageOutputResults { get; private set; } 
    public ICollection<DirectoryPath> ToClean { get; private set; }

    public BuildDirectories(DirectoryPath artifacts, DirectoryPath artifactsBin, DirectoryPath artifactsOutput, DirectoryPath testResultOutput
        , DirectoryPath testCoverageOutput, DirectoryPath testCoverageOutputResults)
    {
        Artifacts = artifacts;
        ArtifactsBin = artifactsBin;
        ArtifactsOutput = artifactsOutput;
        TestResultOutput = testResultOutput;
        TestCoverageOutput = testCoverageOutput;
        TestCoverageOutputResults = testCoverageOutputResults;
        ToClean = new[]
        {
            Artifacts,
            ArtifactsBin,
            ArtifactsOutput,
            TestResultOutput,
            TestCoverageOutput,
            TestCoverageOutputResults
        };
    }
}
