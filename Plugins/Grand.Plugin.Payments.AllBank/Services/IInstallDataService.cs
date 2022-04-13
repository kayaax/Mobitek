using System.Threading.Tasks;

namespace Grand.Plugin.Payments.AllBank.Services
{
    public interface IInstallDataService
    {
        Task InstallData();
        Task UninstallData();
    }
}
