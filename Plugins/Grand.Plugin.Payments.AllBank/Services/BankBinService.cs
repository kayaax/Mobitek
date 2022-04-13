using Grand.Domain;
using Grand.Domain.Data;
using Grand.Plugin.Payments.AllBank.Domain;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Plugin.Payments.AllBank.Services
{
    public class BankBinService : IBankBinService
    {
        private readonly IRepository<OmniBankBin> _bankBinRepository;

        public BankBinService(IRepository<OmniBankBin> bankBinRepository)
        {
            _bankBinRepository = bankBinRepository;
        }

        public async Task InsertBankBin(OmniBankBin bankBin)
        {
            if (bankBin == null)
                throw new ArgumentNullException(nameof(bankBin));
            await _bankBinRepository.InsertAsync(bankBin);
        }

        public async Task InsertBankBin(IEnumerable<OmniBankBin> bankBins)
        {
            if (bankBins == null)
                throw new ArgumentNullException(nameof(bankBins));
            foreach (var bankBin in bankBins)
            {
                await InsertBankBin(bankBin);
            }

        }
        public async Task UpdateBankBin(OmniBankBin bankBin)
        {
            if (bankBin == null)
                throw new ArgumentNullException(nameof(bankBin));
            await _bankBinRepository.UpdateAsync(bankBin);
        }

        public async Task UpdateBankBin(IEnumerable<OmniBankBin> bankBins)
        {
            if (bankBins == null)
                throw new ArgumentNullException(nameof(bankBins));
            foreach (var bankBin in bankBins)
            {
                await UpdateBankBin(bankBin);
            }
        }

        public async Task DeleteBankBin(OmniBankBin bankBin)
        {
            if (bankBin == null)
                throw new ArgumentNullException(nameof(bankBin));
            await _bankBinRepository.DeleteAsync(bankBin);
        }

        public async Task DeleteBankBin(IEnumerable<OmniBankBin> bankBins)
        {
            if (bankBins == null)
                throw new ArgumentNullException(nameof(bankBins));
            foreach (var bankBin in bankBins)
            {
                await DeleteBankBin(bankBin);
            }
        }

        public async Task<OmniBankBin> GetBankBinId(string id)
        {
            return await _bankBinRepository.GetByIdAsync(id);
        }

        public async Task<OmniBankBin> GetBankBin(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return await Task.FromResult<OmniBankBin>(null);
            prefix = prefix.Trim();
            var bin = _bankBinRepository.Table.Where(b => b.BinNumber.Contains(prefix));
            var result = bin.FirstOrDefaultAsync();
            return await result;
        }

        public async Task<IList<OmniBankBin>> GetBankBinList()
        {
            var query = from bank in _bankBinRepository.Table
                        orderby bank.BankCode
                        select bank;
            return await query.ToListAsync();

        }

        public async Task<IPagedList<OmniBankBin>> GetBankBinPageList(string bankName = null, int pageIndex = 0, int pageSize = Int32.MaxValue)
        {
            var builder = Builders<OmniBankBin>.Filter;
            var filter = builder.Where(b => true);
            if (!String.IsNullOrEmpty(bankName))
                filter = filter & builder.Where(b => b.BankName.Contains(bankName));
            var builderSort = Builders<OmniBankBin>.Sort.Descending(x => x.Id);
            var query = _bankBinRepository.Collection;
            var bankBinList = await PagedList<OmniBankBin>.Create(query, filter, builderSort, pageIndex, pageSize);
            return bankBinList;
        }
    }
}
