using System.IO.Abstractions;
using System.Reflection;
using DotNetOutdated.Core.Models;
using DotNetOutdated.Core.Services;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using NuGet.Versioning;
using DotNetOutdated.Core;
using NuGet.Credentials;
using System.Collections.Concurrent;

namespace DependOnWhat.PackageFinder;
public static class PackageFinderExtensions
{
    public static IServiceCollection AddPackageFinderServices(this IServiceCollection services)
    {
        return services
               .AddSingleton(PhysicalConsole.Singleton)
               .AddSingleton<IFileSystem, FileSystem>()
               .AddSingleton<IProjectDiscoveryService, ProjectDiscoveryService>()
               .AddSingleton<IProjectAnalysisService, ProjectAnalysisService>()
               .AddSingleton<IDotNetRunner, DotNetRunner>()
               .AddSingleton<IDependencyGraphService, DependencyGraphService>()
               .AddSingleton<IDotNetRestoreService, DotNetRestoreService>()
               .AddSingleton<INuGetPackageInfoService, NuGetPackageInfoService>()
               .AddSingleton<INuGetPackageResolutionService, NuGetPackageResolutionService>()
               .AddSingleton<IFullPackageProvider, FullPackageProvider>()
               .AddSingleton<INuGetPackageInfoService, NuGetPackageInfoService>();
    }
}
public class FullPackageProvider : IFullPackageProvider
{
    private readonly IFileSystem _fileSystem;
    private readonly INuGetPackageResolutionService _nugetService;
    private readonly IProjectAnalysisService _projectAnalysisService;
    private readonly IProjectDiscoveryService _projectDiscoveryService;
    private readonly INuGetPackageInfoService _nuGetPackageInfoService;

    private const bool Recursive = false;
    private const bool Transitive = false;
    private const int TransitiveDepth = 1;
    private const bool IncludeAutoReferences = true;
    private const VersionLock Versionlock = VersionLock.None;
    private const PrereleaseReporting Prerelease = PrereleaseReporting.Auto;
    private const int OlderThanDays = 0;

    public FullPackageProvider(IFileSystem fileSystem, INuGetPackageResolutionService nugetService, IProjectAnalysisService projectAnalysisService,
        IProjectDiscoveryService projectDiscoveryService, INuGetPackageInfoService nuGetPackageInfoService)
    {
        _fileSystem = fileSystem;
        _nugetService = nugetService;
        _projectAnalysisService = projectAnalysisService;
        _projectDiscoveryService = projectDiscoveryService;
        _nuGetPackageInfoService = nuGetPackageInfoService;
    }


    public async Task<List<AnalyzedProject>> GetProjects()
    {
        var Path = _fileSystem.Directory.GetCurrentDirectory();

        DefaultCredentialServiceUtility.SetupDefaultCredentialService(new NuGet.Common.NullLogger(), true);

        var projectPaths = _projectDiscoveryService.DiscoverProjects(Path, Recursive);

        var projects = projectPaths.SelectMany(path => _projectAnalysisService.AnalyzeProject(path, false, Transitive, TransitiveDepth)).ToList();

        // Analyze the dependencies
        var outdatedProjects = await AnalyzeDependencies(projects);

        return outdatedProjects;
    }

    private async Task<List<AnalyzedProject>> AnalyzeDependencies(List<Project> projects)
    {
        var outdatedProjects = new ConcurrentBag<AnalyzedProject>();

        var tasks = new Task[projects.Count];

        for (var index = 0; index < projects.Count; index++)
        {
            var project = projects[index];
            tasks[index] = AddOutdatedProjectsIfNeeded(project, outdatedProjects);
        }

        await Task.WhenAll(tasks);


        return outdatedProjects.ToList();
    }

    private async Task AddOutdatedProjectsIfNeeded(Project project, ConcurrentBag<AnalyzedProject> outdatedProjects)
    {
        var outdatedFrameworks = new ConcurrentBag<AnalyzedTargetFramework>();

        var tasks = new Task[project.TargetFrameworks.Count];

        // Process each target framework with its related dependencies
        for (var index = 0; index < project.TargetFrameworks.Count; index++)
        {
            var targetFramework = project.TargetFrameworks[index];
            tasks[index] = AddOutdatedFrameworkIfNeeded(targetFramework, project, outdatedFrameworks);
        }

        await Task.WhenAll(tasks);

        if (!outdatedFrameworks.IsEmpty)
        {
            outdatedProjects.Add(new AnalyzedProject(project.Name, project.FilePath, outdatedFrameworks));
        }
    }

    private async Task AddOutdatedFrameworkIfNeeded(TargetFramework targetFramework, Project project, ConcurrentBag<AnalyzedTargetFramework> outdatedFrameworks)
    {
        var outdatedDependencies = new ConcurrentBag<AnalyzedDependency>();

        var deps = targetFramework.Dependencies.Where(d => IncludeAutoReferences || d.IsAutoReferenced == false);

        var dependencies = deps.OrderBy(dependency => dependency.IsTransitive)
            .ThenBy(dependency => dependency.Name)
            .ToList();

        var tasks = new Task[dependencies.Count];

        for (var index = 0; index < dependencies.Count; index++)
        {
            var dependency = dependencies[index];

            tasks[index] = this.AddOutdatedDependencyIfNeeded(project, targetFramework, dependency, outdatedDependencies);
        }

        await Task.WhenAll(tasks);

        if (!outdatedDependencies.IsEmpty)
        {
            outdatedFrameworks.Add(new AnalyzedTargetFramework(targetFramework.Name, outdatedDependencies));
        }
    }

    private async Task AddOutdatedDependencyIfNeeded(Project project, TargetFramework targetFramework, Dependency dependency, ConcurrentBag<AnalyzedDependency> outdatedDependencies)
    {
        var referencedVersion = dependency.ResolvedVersion;
        NuGetVersion? latestVersion = null;

        if (referencedVersion != null)
        {
            latestVersion = await _nugetService.ResolvePackageVersions(dependency.Name, referencedVersion, project.Sources, dependency.VersionRange,
                Versionlock, Prerelease, targetFramework.Name, project.FilePath, dependency.IsDevelopmentDependency, OlderThanDays);
        }

        if (referencedVersion == null || latestVersion == null || referencedVersion != latestVersion)
        {
            var allversions = await  _nuGetPackageInfoService.GetAllVersions(dependency.Name, project.Sources,true,targetFramework.Name,project.FilePath,dependency.IsDevelopmentDependency);
            // special case when there is version installed which is not older than "OlderThan" days makes "latestVersion" to be null
            if (OlderThanDays > 0 && latestVersion == null)
            {
                var absoluteLatestVersion = await _nugetService.ResolvePackageVersions(dependency.Name, referencedVersion, project.Sources, dependency.VersionRange,
                    Versionlock, Prerelease, targetFramework.Name, project.FilePath, dependency.IsDevelopmentDependency);

                if (absoluteLatestVersion == null || referencedVersion > absoluteLatestVersion)
                {
                    outdatedDependencies.Add(new AnalyzedDependency(dependency, latestVersion, allversions));
                }
            }
            else if (latestVersion != null)
            {
                outdatedDependencies.Add(new AnalyzedDependency(dependency, latestVersion, allversions));
            }
        }
    }
}
