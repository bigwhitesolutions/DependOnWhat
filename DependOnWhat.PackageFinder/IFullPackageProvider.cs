
namespace DependOnWhat.PackageFinder
{
    public interface IFullPackageProvider
    {
        Task<List<AnalyzedProject>> GetProjects();
    }
}