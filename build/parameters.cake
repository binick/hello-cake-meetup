#load "./paths.cake"
#load "./version.cake"
#load "./utils.cake"

public class BuildParameters
{
    public string Target { get; private set; }
    public string Configuration { get; private set; }
    public string Framework { get; private set; }

    public string DotNetVersion { get; private set; }
    public int CoverageThreshold { get; private set; }

    public string NetStandard10Version { get; private set; }     = "netstandard1.0";
    public string NetStandard11Version { get; private set; }     = "netstandard1.1";
    public string NetStandard12Version { get; private set; }     = "netstandard1.2";
    public string NetStandard13Version { get; private set; }     = "netstandard1.3";
    public string NetStandard14Version { get; private set; }     = "netstandard1.4";
    public string NetStandard15Version { get; private set; }     = "netstandard1.5";
    public string NetStandard16Version { get; private set; }     = "netstandard1.6";
    public string NetStandard20Version { get; private set; }     = "netstandard2.0";
    public string NetCore10Version { get; private set; }         = "netcoreapp1.0";
    public string NetCore11Version { get; private set; }         = "netcoreapp1.1";
    public string NetCore20Version { get; private set; }         = "netcoreapp2.0";
    public string NetCore21Version { get; private set; }         = "netcoreapp2.1";
    public string NetCore22Version { get; private set; }         = "netcoreapp2.2";
    public string NetFx11Version { get; private set; }           = "net11";
    public string NetFx20Version { get; private set; }           = "net20";
    public string NetFx35Version { get; private set; }           = "net35";
    public string NetFx40Version { get; private set; }           = "net40";
    public string NetFx403Version { get; private set; }          = "net403";
    public string NetFx45Version { get; private set; }           = "net45";
    public string NetFx451Version { get; private set; }          = "net451";
    public string NetFx452Version { get; private set; }          = "net452";
    public string NetFx46Version { get; private set; }           = "net46";
    public string NetFx461Version { get; private set; }          = "net461";
    public string NetFx462Version { get; private set; }          = "net462";
    public string NetFx47Version { get; private set; }           = "net47";
    public string NetFx471Version { get; private set; }          = "net471";
    public string NetFx472Version { get; private set; }          = "net472";
    
    public bool EnabledUnitTests { get; private set; }
    public bool EnabledPublishNuget { get; private set; }

    public bool IsRunningOnUnix { get; private set; }
    public bool IsRunningOnWindows { get; private set; }
    public bool IsRunningOnLinux { get; private set; }
    public bool IsRunningOnMacOS { get; private set; }

    public bool IsLocalBuild { get; private set; }
    public bool IsRunningOnAzurePipeline { get; private set; }

    public bool IsMainRepo { get; private set; }
    public bool IsMainBranch { get; private set; }
    public bool IsTagged { get; private set; }
    public bool IsPullRequest { get; private set; }

    public DotNetCoreMSBuildSettings MSBuildSettings { get; private set; }
    public BuildPaths Paths { get; private set; }
    public BuildVersion Version { get; private set; }

    public bool IsStableRelease() => !IsLocalBuild && IsMainRepo && IsMainBranch && !IsPullRequest && IsTagged;
    public bool IsPreRelease() => !IsLocalBuild && IsMainRepo && IsMainBranch && !IsPullRequest && !IsTagged;

    public Dictionary<string, object> ProcessVariables { get; private set; }

    /*
     * Get build parameters.
     */
    public static BuildParameters GetParameters(ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var buildSystem = context.BuildSystem();

        return new BuildParameters
        {
            Target           = context.Argument("target", "Default"),
            Configuration    = context.Argument("configuration", "Release"),
            Framework        = context.Argument("framework", "netstandard2.0"),

            EnabledUnitTests      = IsEnabled(context, "ENABLED_UNIT_TESTS"),
            EnabledPublishNuget   = IsEnabled(context, "ENABLED_PUBLISH_NUGET"),

            IsRunningOnUnix      = context.IsRunningOnUnix(),
            IsRunningOnWindows   = context.IsRunningOnWindows(),
            IsRunningOnLinux     = context.Environment.Platform.Family == PlatformFamily.Linux,
            IsRunningOnMacOS     = context.Environment.Platform.Family == PlatformFamily.OSX,

            IsLocalBuild                = buildSystem.IsLocalBuild,
            IsRunningOnAzurePipeline    = buildSystem.IsRunningOnAzurePipelinesHosted,

            IsMainRepo      = IsOnMainRepo(context),
            IsMainBranch    = IsOnMainBranch(context),
            IsPullRequest   = IsPullRequestBuild(context),
            IsTagged        = IsBuildTagged(context),

            ProcessVariables = new Dictionary<string, object>()
        };
    }

    /*
     * Initialize build context.
     */
    public void Setup(ICakeContext context, GitVersion gitVersion, int lineCoverageThreshold)
    {
        Version = BuildVersion.Calculate(context, this, gitVersion);
        CoverageThreshold = lineCoverageThreshold;
        MSBuildSettings = GetMsBuildSettings(context, Version);

        Paths = BuildPaths.GetPaths(context, Configuration, Version);
    }

    private DotNetCoreMSBuildSettings GetMsBuildSettings(ICakeContext context, BuildVersion version)
    {
        var msBuildSettings = new DotNetCoreMSBuildSettings()
                                .WithProperty("Version", version.SemVersion)
                                .WithProperty("AssemblyVersion", version.Version)
                                .WithProperty("PackageVersion", version.SemVersion)
                                .WithProperty("FileVersion", version.Version);

        if(!IsRunningOnWindows)
        {
            var frameworkPathOverride = new FilePath(typeof(object).Assembly.Location).GetDirectory().FullPath + "/";

            // Use FrameworkPathOverride when not running on Windows.
            context.Information("Build will use FrameworkPathOverride={0} since not building on Windows.", frameworkPathOverride);
            msBuildSettings.WithProperty("FrameworkPathOverride", frameworkPathOverride);
        }

        return msBuildSettings;
    }

    private static bool IsOnMainRepo(ICakeContext context)
    {
        var buildSystem = context.BuildSystem();
        string repositoryName = null;
        if (buildSystem.IsRunningOnAzurePipelinesHosted)
            repositoryName = buildSystem.TFBuild.Environment.Repository.RepoName;

        if(!string.IsNullOrWhiteSpace(repositoryName))
            context.Information("Repository Name: {0}", repositoryName);

        return !string.IsNullOrWhiteSpace(repositoryName) && StringComparer.OrdinalIgnoreCase.Equals("reactconsulting/hello-cake", repositoryName);
    }

    private static bool IsOnMainBranch(ICakeContext context)
    {
        var buildSystem = context.BuildSystem();
        string repositoryBranch = null;
        if (buildSystem.IsRunningOnAzurePipelinesHosted)
            repositoryBranch = buildSystem.TFBuild.Environment.Repository.Branch;

        if(!string.IsNullOrWhiteSpace(repositoryBranch))
            context.Information("Repository Branch: {0}", repositoryBranch);

        return !string.IsNullOrWhiteSpace(repositoryBranch) && StringComparer.OrdinalIgnoreCase.Equals("master", repositoryBranch);
    }

    private static bool IsPullRequestBuild(ICakeContext context)
    {
        var buildSystem = context.BuildSystem();
        if (buildSystem.IsRunningOnAzurePipelinesHosted)
        {
            var value = context.EnvironmentVariable("SYSTEM_PULLREQUEST_ISFORK");
            return !string.IsNullOrWhiteSpace(value) && !string.Equals(value, false.ToString(), StringComparison.InvariantCultureIgnoreCase);
        }
        return false;
    }

    private static bool IsBuildTagged(ICakeContext context)
    {
        var gitPath = context.Tools.Resolve(context.IsRunningOnWindows() ? "git.exe" : "git");
        context.StartProcess(gitPath, new ProcessSettings { Arguments = "rev-parse --verify HEAD", RedirectStandardOutput = true }, out var sha);
        context.StartProcess(gitPath, new ProcessSettings { Arguments = "tag --points-at " + sha.Single(), RedirectStandardOutput = true }, out var redirectedOutput);

        return redirectedOutput.Any();
    }

    private static bool IsEnabled(ICakeContext context, string envVar, bool nullOrEmptyAsEnabled = true)
    {
        var value = context.EnvironmentVariable(envVar);
        return string.IsNullOrWhiteSpace(value) ? nullOrEmptyAsEnabled : bool.Parse(value);
    }
}
