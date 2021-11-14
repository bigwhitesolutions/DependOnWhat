using YamlDotNet.Serialization;

namespace DependOnWhat.Dependabot;

public record Dependabot
{
    public long Version { get; set; }

    public List<Update> Updates { get; set; } = new List<Update>();
}

public class Update
{
    [YamlMember(Alias = "package-ecosystem", ApplyNamingConventions = false)]
    public string? PackageEcosystems { get; set; }

    [YamlMember(Alias = "versioning-strategy", ApplyNamingConventions = false)]
    public string? VersioningStrategy { get; set; }

    public string? Directory { get; set; }

    [YamlMember(Alias = "target-branch", ApplyNamingConventions = false)]
    public string? TargetBranch { get; set; }

    [YamlMember(Alias = "open-pull-requests-limit", ApplyNamingConventions = false)]
    public long OpenPullRequestsLimit { get; set; }

    public long Milestone { get; set; }

    [YamlMember(Alias = "ignore", ApplyNamingConventions = false)]
    public List<Ignore> Ignores { get; set; } = new List<Ignore>();
}

public class Ignore
{
    [YamlMember(Alias = "dependency-name", ApplyNamingConventions = false)]
    public string? DependencyName { get; set; }

    public List<string> Versions { get; set; } = new List<string>();

    public IEnumerable<(string, Version)> ParsedVersions()
    {
        foreach (var version in Versions)
        {
            var split = version.Split(' ');

            yield return new(split[0], Version.Parse(split[1]));
        }
    }
}