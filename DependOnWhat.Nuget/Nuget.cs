namespace DependOnWhat.Nuget;

using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System.Linq;

public static class Nuget
{
    private static readonly ILogger logger = NullLogger.Instance;
    private static readonly SourceCacheContext cache = new();
    private static readonly SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");

    public static async Task<Dictionary<string, IEnumerable<NuGetVersion>>> GetVersionsForPackages(IEnumerable<string> names)
    {
        var tasks = names.Select(x => GetVersionsForPackage(x));
        var results = await Task.WhenAll(tasks);
        return results.ToDictionary(x => x.Key, x => x.Value);
    }

    private static async Task<KeyValuePair<string, IEnumerable<NuGetVersion>>> GetVersionsForPackage(string name)
    {
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>();

        var versions = await resource.GetAllVersionsAsync(
              name,
              cache,
              logger,
              CancellationToken.None
              );
        return KeyValuePair.Create(name, versions);
    }

    public static NuGetVersion? Latest(this IEnumerable<NuGetVersion> nugetVersions)
    {
        var latestList = nugetVersions.Where(x => x.IsPrerelease == false);

        if (latestList.Any())
        {
            return latestList.OrderByDescending(x => x.Version).First();
        }

        return nugetVersions.Any() ? nugetVersions.Where(x => x.IsPrerelease == true).OrderByDescending(x => x.Version).First() : null;
    }

    public enum Operator
    {
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
    }

    public static IEnumerable<NuGetVersion> Filter(this Dictionary<string, IEnumerable<NuGetVersion>> nugetVersions, string dependancyName, string? versionsToIgnore = null)
    {
        if (string.IsNullOrWhiteSpace(versionsToIgnore))
        {
            return nugetVersions[dependancyName];
        }
        var (op, version) = ParseVersion(versionsToIgnore);
        return Filter(nugetVersions[dependancyName], op, version);
    }

    private static IEnumerable<NuGetVersion> Filter(this IEnumerable<NuGetVersion> nugetVersions, Operator @operator, Version version)
    {
        return @operator switch
        {
            Operator.GreaterThan => nugetVersions.Where(x => x.Version > version),
            Operator.GreaterThanOrEqual => nugetVersions.Where(x => x.Version >= version),
            Operator.LessThan => nugetVersions.Where(x => x.Version < version),
            Operator.LessThanOrEqual => nugetVersions.Where(x => x.Version <= version),
            _ => nugetVersions,
        };
    }

    private static (Operator @operator, Version version) ParseVersion(string ignore)
    {
        var split = ignore.Split(' ');
        var op = split[0] switch
        {
            "<" => Operator.GreaterThan,
            "<=" => Operator.GreaterThanOrEqual,
            ">" => Operator.LessThan,
            ">=" => Operator.LessThanOrEqual,
            _ => throw new InvalidOperationException("Can't parse filter")
        };
        var version = Version.Parse(split[1]);
        return (op, version);
    }
}