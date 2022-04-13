using Grand.Domain;
using Grand.Plugin.Payments.AllBank.Domain;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Plugin.Payments.AllBank.Services
{
    public interface IBankBinService
    {

        Task InsertBankBin(OmniBankBin bankBin);
        Task InsertBankBin(IEnumerable<OmniBankBin> bankBins);
        Task UpdateBankBin(OmniBankBin bankBin);
        Task UpdateBankBin(IEnumerable<OmniBankBin> bankBins);
        Task DeleteBankBin(OmniBankBin bankBin);
        Task DeleteBankBin(IEnumerable<OmniBankBin> bankBins);
        Task<OmniBankBin> GetBankBinId(string id);
        Task<OmniBankBin> GetBankBin(string prefix);
        Task<IList<OmniBankBin>> GetBankBinList();
        Task<IPagedList<OmniBankBin>> GetBankBinPageList(string bankId = null, int pageIndex = 0, int pageSize = int.MaxValue);

    }
}