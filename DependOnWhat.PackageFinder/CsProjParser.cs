using System.Xml.Linq;

namespace DependOnWhat.PackageFinder;

internal static class CsProjParser
{
    internal static IEnumerable<Dependency> GetAllDependencies(string filePath)
    {
        var all = new HashSet<Dependency>();
        var project = File.ReadAllText(filePath);
        var document = XDocument.Parse(project);

        var dependencies = document.Descendants("PackageReference");

        foreach (var package in dependencies)
        {
            var dependency = Extract(package);

            if (dependency != null)
            {
                all.Add(dependency);
            }
        }

        dependencies = document.Descendants("DotNetCliToolReference");

        foreach (var package in dependencies)
        {
            var dependency = Extract(package);

            if (dependency != null)
            {
                all.Add(dependency);
            }
        }

        return all;
    }

    private static Dependency? Extract(XElement element)
    {
        var name = element.Attribute("Include")?.Value.Trim();
        var version = element.Attribute("Version")?.Value.Trim();

        if (name != null && version != null)
        {
            return new Dependency(name, version);
        }

        return null;
    }
}