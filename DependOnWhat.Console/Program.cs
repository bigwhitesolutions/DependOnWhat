using Spectre.Console;
using DependOnWhat.Dependabot;
using DependOnWhat.PackageFinder;
using DependOnWhat.Nuget;

var allDependancies = PackageFinder.GetPackages(Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj", SearchOption.AllDirectories));

var dependabotConfig = DependabotParser.ParseYaml(File.ReadAllText(Directory.GetFiles(Directory.GetCurrentDirectory(), ".dependabot", SearchOption.AllDirectories).Single()));

var nugetVersions = await Nuget.GetVersionsForPackages(allDependancies.Select(x => x.Name));

var table = new Table();
table.AddColumns("Name", "Current", "latest");

foreach (var dependancy in allDependancies.OrderBy(x => x.Name))
{
    var versionsToIgnore = dependabotConfig.GetVersionsToIgnore(dependancy.Name);
    var latest = nugetVersions.Filter(dependancy.Name, versionsToIgnore.FirstOrDefault()).Latest();

    var func = (string _) => new Markup("");

    if (dependancy.Version == null)
    {
        func = (string s) => new Markup($"[yellow]{s}[/]");
    }
    else if (dependancy.Version != latest?.OriginalVersion)
    {
        func = (string s) => new Markup($"[red]{s}[/]");
    }
    else
    {
        continue;
    }
    table.AddRow(func(dependancy.Name), func(dependancy.Version ?? ""), func(latest?.OriginalVersion ?? ""));
}

AnsiConsole.Write(table);

Console.ResetColor();
Console.ReadLine();