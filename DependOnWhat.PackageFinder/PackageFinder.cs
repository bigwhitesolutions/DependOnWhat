namespace DependOnWhat.PackageFinder;

public static class PackageFinder
{
    public static IEnumerable<Dependency> GetPackages(string csprojLocation) => CsProjParser.GetAllDependencies(csprojLocation).DistinctBy(x => x.Name);

    public static IEnumerable<Dependency> GetPackages(string[] csprojLocations)
    {
        List<Dependency> allDependancies = new();

        foreach (var csprojLocation in csprojLocations)
        {
            allDependancies.AddRange(CsProjParser.GetAllDependencies(csprojLocation));
        }
        return allDependancies.DistinctBy(x => x.Name);
    }
}