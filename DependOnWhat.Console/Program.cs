using Spectre.Console;
using DependOnWhat.Dependabot;
using DependOnWhat.PackageFinder;
using Microsoft.Extensions.DependencyInjection;
using DependOnWhat.Nuget;

List<AnalyzedProject>? AnalyzedProjects = null;
using (var services = new ServiceCollection().AddPackageFinderServices().BuildServiceProvider())
{
    var packageProvider = services.GetService<IFullPackageProvider>()!;
    AnalyzedProjects = await packageProvider.GetProjects();
}

var dependabotFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "dependabot.yml", SearchOption.AllDirectories);
var dependabotConfig = DependabotParser.ParseYaml(File.ReadAllText(dependabotFiles.First()));

var allDependancies = AnalyzedProjects.SelectMany(project => project.TargetFrameworks.First().Dependencies);

var table = new Table();
table.AddColumns("Name", "Current", "latest");

foreach (var dependancy in allDependancies.OrderBy(x => x.Name).DistinctBy(x => new { x.Name, x.ResolvedVersion } ))
{
    var versionsToIgnore = dependabotConfig.GetVersionsToIgnore(dependancy.Name);
    var resolved = dependancy.ResolvedVersion;
    var latest = dependancy.AllVersions.Filter(versionsToIgnore.FirstOrDefault()).Latest();

    var func = (string _) => new Markup("");

    if (resolved == null)
    {
        func = (string s) => new Markup($"[yellow]{s}[/]");
    }
    else if (resolved != latest)
    {
        func = (string s) => new Markup($"[red]{s}[/]");
    }
    else
    {
        continue;
    }
    table.AddRow(func(dependancy.Name), func(resolved?.OriginalVersion ?? ""), func(latest?.OriginalVersion ?? ""));
}

AnsiConsole.Write(table);

Console.ResetColor();
Console.ReadLine();