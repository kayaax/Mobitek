using Grand.Domain;
using Grand.Domain.Data;
using Grand.Plugin.Payments.AllBank.Domain;
using Microsoft.CodeAnalysis;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Plugin.Payments.AllBank.Services
{
    public class BankPosService : IBankPosService
    {
        private readonly IRepository<OmniBankPos> _bankPosRepository;
        private readonly IRepository<OmniBankInstallmentCategory> _bankInstallmentCategory;
        public BankPosService(IRepository<OmniBankPos> bankPosRepository,

            IRepository<OmniBankInstallmentCategory> bankInstallmentCategory = null)
        {
            _bankPosRepository = bankPosRepository;
            _bankInstallmentCategory = bankInstallmentCategory;
        }

        public async Task InsertBankPos(OmniBankPos bankPos)
        {
            if (bankPos == null)
                throw new ArgumentNullException(nameof(bankPos));
            await _bankPosRepository.InsertAsync(bankPos);
        }

        public async Task UpdateBankPos(OmniBankPos bankPos)
        {
            if (bankPos == null)
                throw new ArgumentNullException(nameof(bankPos));
            var oldBankPos = await _bankPosRepository.GetByIdAsync(bankPos.Id);
            var builder = Builders<OmniBankPos>.Filter;
            var filter = builder.Eq(x => x.Id, bankPos.Id);
            var update = Builders<OmniBankPos>.Update
                .Set(x => x.IsActive, bankPos.IsActive)
                .Set(x => x.BankTypeId, bankPos.BankTypeId)
                .Set(x => x.Primary, bankPos.Primary)
                .Set(x => x.PrimaryBank, bankPos.PrimaryBank)
                .Set(x => x.PictureId, bankPos.PictureId)
                .CurrentDate("UpdatedOnUtc");
            await _bankPosRepository.Collection.UpdateOneAsync(filter, update);


        }

        public async Task DeleteBankPos(OmniBankPos bankPos)
        {
            if (bankPos == null)
                throw new ArgumentNullException(nameof(bankPos));
            await _bankPosRepository.DeleteAsync(bankPos);
        }

        public async Task<OmniBankPos> GetBankPosId(string id)
        {
            return await _bankPosRepository.GetByIdAsync(id);
        }

        public async Task<IList<OmniBankPos>> GetDefaultBankPos()
        {
            var bankPos = from pos in _bankPosRepository.Table
                          where pos.IsActive &
                                pos.PrimaryBank &
                                !pos.Primary
                          select pos;
            return await bankPos.ToListAsync();
        }


        public async Task<OmniBankPos> GetDefaultBankPosPrimary(bool primary)
        {
            var bankPos = from pos in _bankPosRepository.Table
                          where pos.IsActive &
                                pos.PrimaryBank
                          select pos;
            if (primary)
                bankPos = bankPos.Where(x => x.Primary);
            return await bankPos.FirstOrDefaultAsync();
        }

        public async Task<OmniBankPos> GetDefaultBankPosName(string bankName)
        {
            var bankPos = from pos in _bankPosRepository.Table
                          where pos.Name.Contains(bankName) &
                                pos.IsActive
                          select pos;
            return await bankPos.FirstOrDefaultAsync();
        }

        public async Task<IList<OmniBankPos>> GetBankPosList()
        {
            var query = from bank in _bankPosRepository.Table
                        orderby bank.Name
                        select bank;
            return await query.ToListAsync();
        }

        public async Task<IPagedList<OmniBankPos>> GetBankPosPageList(string bankId = null, int bankTypeId = 0,
            bool showHidden = true, int pageIndex = 0, int pageSize = 2147483647)
        {
            var builder = Builders<OmniBankPos>.Filter;
            var filter = builder.Where(b => true);
            filter &= builder.Where(x => x.IsActive);
            if (!string.IsNullOrEmpty(bankId))
            {
                filter &= builder.Where(b => b.Id == bankId);
            }

            if (bankTypeId > 0)
            {
                filter &= builder.Where(b => b.BankTypeId == bankTypeId);
            }
            if (!showHidden)
                filter &= builder.Where(b => !b.IsActive);
            var builderSort = Builders<OmniBankPos>.Sort.Descending(x => x.Id);
            var query = _bankPosRepository.Collection;
            var bankBinList = await PagedList<OmniBankPos>.Create(query, filter, builderSort, pageIndex, pageSize);
            return bankBinList;
        }

        public async Task<IPagedList<OmniBankPos>> GetBankPosPageList(string bankId = null, int bankTypeId = 0, int pageIndex = 0, int pageSize = Int32.MaxValue)
        {
            var builder = Builders<OmniBankPos>.Filter;
            var filter = builder.Where(b => true);
            if (!string.IsNullOrEmpty(bankId))
            {
                filter = filter & builder.Where(b => b.Id == bankId);
            }
            if (bankTypeId > 0)
            {
                filter = filter & builder.Where(b => b.BankTypeId == bankTypeId);
            }
            var builderSort = Builders<OmniBankPos>.Sort.Descending(x => x.Id);
            var query = _bankPosRepository.Collection;
            var bankBinList = await PagedList<OmniBankPos>.Create(query, filter, builderSort, pageIndex, pageSize);
            return bankBinList;
        }

        public async Task InsertBankPosInstallment(BankInstallment bankInstallment)
        {
            if (bankInstallment == null)
                throw new ArgumentNullException(nameof(bankInstallment));
            var updateBuilder = Builders<OmniBankPos>.Update;
            var update = updateBuilder.AddToSet(b => b.BankInstallments, bankInstallment);
            await _bankPosRepository.Collection.UpdateOneAsync(new BsonDocument("_id", bankInstallment.BankPosId),
                update);
        }

        public async Task UpdateBankPosInstallment(BankInstallment bankInstallment)
        {
            if (bankInstallment == null)
                throw new ArgumentNullException(nameof(bankInstallment));
            var builder = Builders<OmniBankPos>.Filter;
            var filter = builder.Eq(x => x.Id, bankInstallment.BankPosId);
            filter = filter & builder.Where(x => x.BankInstallments.Any(y => y.Id == bankInstallment.Id));
            var update = Builders<OmniBankPos>.Update
                .Set(x => x.BankInstallments.ElementAt(-1).BankId, bankInstallment.BankId)
                .Set(x => x.BankInstallments.ElementAt(-1).NumberOfInstallment, bankInstallment.NumberOfInstallment)
                .Set(x => x.BankInstallments.ElementAt(-1).Percentage, bankInstallment.Percentage);

            await _bankPosRepository.Collection.UpdateManyAsync(filter, update);
        }

        public async Task DeleteBankPosInstallment(BankInstallment bankInstallment)
        {
            if (bankInstallment == null)
                throw new ArgumentNullException(nameof(bankInstallment));
            var updateBuilder = Builders<OmniBankPos>.Update;
            var update = updateBuilder.Pull(p => p.BankInstallments, bankInstallment);
            await _bankPosRepository.Collection.UpdateOneAsync(new BsonDocument("_id", bankInstallment.BankPosId),
                update);
        }
        public async Task InsertBankInstallmentCategory(OmniBankInstallmentCategory bankInstallmentCategory)
        {
            if (bankInstallmentCategory == null)
                throw new ArgumentNullException(nameof(bankInstallmentCategory));
            await _bankInstallmentCategory.InsertAsync(bankInstallmentCategory);
        }

        public async Task UpdateBankInstallmentCategory(OmniBankInstallmentCategory bankInstallmentCategory)
        {
            if (bankInstallmentCategory == null)
                throw new ArgumentNullException(nameof(bankInstallmentCategory));
            await _bankInstallmentCategory.UpdateAsync(bankInstallmentCategory);
        }

        public async Task DeleteBankInstallmentCategory(OmniBankInstallmentCategory bankInstallmentCategory)
        {
            if (bankInstallmentCategory == null)
                throw new ArgumentNullException(nameof(bankInstallmentCategory));
            await _bankInstallmentCategory.DeleteAsync(bankInstallmentCategory);
        }

        public async Task<OmniBankInstallmentCategory> GetBankInstallmentCategoryId(string id)
        {
            return await _bankInstallmentCategory.GetByIdAsync(id);
        }

        public async Task<OmniBankInstallmentCategory> GetBankInstallmentCategoryName(string bankInstallmentCategoryName)
        {
            var query = from b in _bankInstallmentCategory.Table
                        where b.Name.Contains(bankInstallmentCategoryName)
                        select b;
            return await query.FirstOrDefaultAsync();
        }

        public async Task<IPagedList<OmniBankInstallmentCategory>> GetBankInstallmentCategoryList(int pageIndex = 0,
            int pageSize = int.MaxValue)
        {
            var builder = Builders<OmniBankInstallmentCategory>.Filter;
            var filter = builder.Where(b => true);
            var builderSort = Builders<OmniBankInstallmentCategory>.Sort.Ascending(b => b.Name);
            var query = _bankInstallmentCategory.Collection;

            return await PagedList<OmniBankInstallmentCategory>.Create(query, filter, builderSort, pageIndex, pageSize);
        }
    }
}
