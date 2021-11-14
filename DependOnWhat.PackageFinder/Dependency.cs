namespace DependOnWhat.PackageFinder;
public record Dependency : IEquatable<Dependency>
{
    public Dependency(string name, string current)
    {
        Name = name;
        Version = current;
    }

    public string Name { get; private set; }
    public string Version { get; private set; }
}