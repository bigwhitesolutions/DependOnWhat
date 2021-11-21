using DotNetOutdated.Core.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NuGet.Frameworks;
using NuGet.Versioning;

namespace DependOnWhat.PackageFinder;

[JsonObject(MemberSerialization.OptIn)]
public class AnalyzedProject
{
    [JsonProperty(Order = 2)]
    public IReadOnlyList<AnalyzedTargetFramework> TargetFrameworks { get; }

    [JsonProperty(Order = 0)]
    public string Name { get; set; }

    [JsonProperty(Order = 1)]
    public string FilePath { get; set; }

    public AnalyzedProject(string name, string filePath, IEnumerable<AnalyzedTargetFramework> targetFrameworks)
    {
        Name = name;
        FilePath = filePath;
        TargetFrameworks = new List<AnalyzedTargetFramework>(targetFrameworks);
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class AnalyzedTargetFramework
{
    [JsonProperty(Order = 1)]
    public IReadOnlyList<AnalyzedDependency> Dependencies { get; }

    [JsonProperty(Order = 0)]
    public NuGetFramework Name { get; set; }

    public AnalyzedTargetFramework(NuGetFramework name, IEnumerable<AnalyzedDependency> dependencies)
    {
        Name = name;
        Dependencies = new List<AnalyzedDependency>(dependencies);
    }
}

[JsonObject(MemberSerialization.OptIn)]
public class AnalyzedDependency
{
    private readonly Dependency _dependency;

    public string Description
    {
        get
        {
            var description = Name;

            if (IsAutoReferenced)
                description += " [A]";
            else if (IsTransitive)
                description += " [T]";

            return description;
        }
    }

    public bool IsAutoReferenced => _dependency.IsAutoReferenced;

    public bool IsTransitive => _dependency.IsTransitive;

    [JsonProperty(Order = 0)]
    public string Name => _dependency.Name;

    [JsonProperty(Order = 1)]
    public NuGetVersion ResolvedVersion => _dependency.ResolvedVersion;

    [JsonProperty(Order = 2)]
    public NuGetVersion? LatestVersion { get; set; }

    [JsonProperty(Order = 3)]
    [JsonConverter(typeof(StringEnumConverter))]
    public DependencyUpgradeSeverity UpgradeSeverity
    {
        get
        {
            if (LatestVersion == null || ResolvedVersion == null)
                return DependencyUpgradeSeverity.Unknown;

            if (LatestVersion.Major > ResolvedVersion.Major || ResolvedVersion.IsPrerelease)
                return DependencyUpgradeSeverity.Major;
            if (LatestVersion.Minor > ResolvedVersion.Minor)
                return DependencyUpgradeSeverity.Minor;
            if (LatestVersion.Patch > ResolvedVersion.Patch || LatestVersion.Revision > ResolvedVersion.Revision)
                return DependencyUpgradeSeverity.Patch;

            return DependencyUpgradeSeverity.None;
        }
    }

    [JsonProperty(Order = 4)]
    public IEnumerable<NuGetVersion> AllVersions { get; }

    public AnalyzedDependency(Dependency dependency, NuGetVersion latestVersion, IEnumerable<NuGetVersion> allVersions)
    { 
        _dependency = dependency;

        LatestVersion = latestVersion;
        AllVersions = allVersions;
    }

    public enum DependencyUpgradeSeverity
    {
        None,
        Patch,
        Minor,
        Major,
        Unknown
    }
}
