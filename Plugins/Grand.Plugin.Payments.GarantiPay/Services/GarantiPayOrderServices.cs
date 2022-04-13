using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Domain;
using Grand.Domain.Data;
using Grand.Plugin.Payments.GarantiPay.Domain;
using Grand.Services.Common;
using Grand.Services.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Grand.Plugin.Payments.GarantiPay.Services
{
    public class GarantiPayOrderServices : IGarantiPayOrderServices
    {
        private readonly IRepository<OmniGarantiPayOrder> _bankOrderRepository;
        private readonly IRepository<OmniGarantiPayCategory> _garantiPayCategoryRepository;
        private readonly IRepository<OmniGarantiPayPos> _garantiPayPosRepository;
        private readonly ISettingService _settingService;
        private readonly IGenericAttributeService _genericAttributeService;

        public GarantiPayOrderServices(IRepository<OmniGarantiPayOrder> bankOrderRepository,
            IRepository<OmniGarantiPayCategory> garantiPayCategoryRepository,
            IRepository<OmniGarantiPayPos> garantiPayPosRepository, ISettingService settingService, IGenericAttributeService genericAttributeService)
        {
            _bankOrderRepository = bankOrderRepository;
            _garantiPayCategoryRepository = garantiPayCategoryRepository;
            _garantiPayPosRepository = garantiPayPosRepository;
            _settingService = settingService;
            _genericAttributeService = genericAttributeService;
        }
#pragma warning disable S4457 // Parameter validation in "async"/"await" methods should be wrapped
        public async Task InsertOmniGarantiPayOrder(OmniGarantiPayOrder bankOrder)
#pragma warning restore S4457 // Parameter validation in "async"/"await" methods should be wrapped
        {
            if (bankOrder == null)
                throw new ArgumentNullException(nameof(bankOrder));
            await _bankOrderRepository.InsertAsync(bankOrder);
        }

        public async Task UpdateOmniGarantiPayOrder(OmniGarantiPayOrder bankOrder)
        {
            if (bankOrder == null)
                throw new ArgumentNullException(nameof(bankOrder));
            await _bankOrderRepository.UpdateAsync(bankOrder);
        }

        public async Task DeleteOmniGarantiPayOrder(OmniGarantiPayOrder bankOrder)
        {
            if (bankOrder == null)
                throw new ArgumentNullException(nameof(bankOrder));
            await _bankOrderRepository.DeleteAsync(bankOrder);
        }

        public async Task<OmniGarantiPayOrder> GetOmniGarantiPayOrderId(string id)
        {
            return await _bankOrderRepository.GetByIdAsync(id);
        }

        public async Task<List<OmniGarantiPayOrder>> GetOmniGarantiPayCustomerId(Guid customerGuid)
        {
            var query = from b in _bankOrderRepository.Table
                        where b.CustomerId == customerGuid.ToString("N")
                        orderby b.CustomerId
                        select b;

            var bankOrder = await query.ToListAsync();
            return bankOrder;
        }

        public async Task<OmniGarantiPayOrder> GetOmniGarantiPayPaymentSession(Guid paymentInfoSession)
        {
            var query = from b in _bankOrderRepository.Table
                        where b.PaymentInfoSession == paymentInfoSession
                        orderby b.CreateDate descending
                        select b;

            var bankOrder = await query.FirstOrDefaultAsync();
            return bankOrder;
        }

        public async Task<OmniGarantiPayOrder> GetOmniGarantiPayGuidId(Guid orderGuid)
        {
            var query = from b in _bankOrderRepository.Table
                        where b.OrderGuid == orderGuid
                        orderby b.CustomerId
                        select b;

            var bankOrder = await query.FirstOrDefaultAsync();
            return bankOrder;
        }

        public async Task<OmniGarantiPayOrder> GetOmniGarantiPayNumber(string orderNumber)
        {
            var query = from b in _bankOrderRepository.Table
                        where b.OrderNumber == orderNumber
                        orderby b.CustomerId
                        select b;

            var bankOrder = await query.FirstOrDefaultAsync();
            return bankOrder;
        }

        public async Task<OmniGarantiPayOrder> GetOmniGarantiPayTokenId(string token)
        {
            var query = from b in _bankOrderRepository.Table
                        where b.Token.Contains(token)
                        orderby b.CustomerId
                        select b;
            var bankOrder = await query.FirstOrDefaultAsync();
            return bankOrder;
        }

        public async Task<IList<OmniGarantiPayOrder>> GetOmniGarantiPayList()
        {
            var query = from b in _bankOrderRepository.Table
                        orderby b.CustomerId
                        select b;
            var bankOrders = await query.ToListAsync();
            return bankOrders;
        }

        public async Task<IPagedList<OmniGarantiPayOrder>> GetOmniGarantiPayOrderPageList(string customerId = null, int pageIndex = 0, int pageSize = Int32.MaxValue)
        {
            var builder = Builders<OmniGarantiPayOrder>.Filter;
            var filter = builder.Where(b => true);
            if (!String.IsNullOrEmpty(customerId))
                filter = filter & builder.Where(b => b.CustomerId.Contains(customerId));
            var builderSort = Builders<OmniGarantiPayOrder>.Sort.Descending(x => x.Id);
            var query = _bankOrderRepository.Collection;
            var bankOrderList = await PagedList<OmniGarantiPayOrder>.Create(query, filter, builderSort, pageIndex, pageSize);
            return bankOrderList;
        }

        public async Task InsertGarantiPayPos(OmniGarantiPayPos bankPos)
        {
            if (bankPos == null)
                throw new ArgumentNullException(nameof(bankPos));
            await _garantiPayPosRepository.InsertAsync(bankPos);
        }

        public async Task<IPagedList<OmniGarantiPayPos>> GetGarantiPayPosPageList(string bankId = null,  int pageIndex = 0, int pageSize = Int32.MaxValue)
        {
            var builder = Builders<OmniGarantiPayPos>.Filter;
            var filter = builder.Where(b => true);
            filter &= builder.Where(x => x.IsActive);
            if (!string.IsNullOrEmpty(bankId))
            {
                filter &= builder.Where(b => b.Id == bankId);
            }
            var builderSort = Builders<OmniGarantiPayPos>.Sort.Descending(x => x.Id);
            var query = _garantiPayPosRepository.Collection;
            var bankPosList = await PagedList<OmniGarantiPayPos>.Create(query, filter, builderSort, pageIndex, pageSize);
            return bankPosList;
        }

        public async Task<IList<OmniGarantiPayPos>> GetGarantiPayPosList()
        {
            var bankPos = from pos in _garantiPayPosRepository.Table
                where pos.IsActive
                select pos;
            return await bankPos.ToListAsync();
        }
        public async Task<OmniGarantiPayPos> GetGarantiPayPosId(string id)
        {
            return await _garantiPayPosRepository.GetByIdAsync(id);
        }
        public async Task UpdateGarantiPayPos(OmniGarantiPayPos bankPos)
        {
            if (bankPos == null)
                throw new ArgumentNullException(nameof(bankPos));
            var oldBankPos = await _garantiPayPosRepository.GetByIdAsync(bankPos.Id);
            var builder = Builders<OmniGarantiPayPos>.Filter;
            var filter = builder.Eq(x => x.Id, bankPos.Id);
            var update = Builders<OmniGarantiPayPos>.Update
                .Set(x => x.IsActive, bankPos.IsActive)
                .CurrentDate("UpdatedOnUtc");
            await _garantiPayPosRepository.Collection.UpdateOneAsync(filter, update);


        }
        public async Task InsertGarantiPayPosInstallment(BankInstallment bankInstallment)
        {
            if (bankInstallment == null)
                throw new ArgumentNullException(nameof(bankInstallment));
            var updateBuilder = Builders<OmniGarantiPayPos>.Update;
            var update = updateBuilder.AddToSet(b => b.BankInstallments, bankInstallment);
            await _garantiPayPosRepository.Collection.UpdateOneAsync(new BsonDocument("_id", bankInstallment.OmniGarantiPayPosId),
                update);
        }

        public async Task UpdateGarantiPayPosInstallment(BankInstallment bankInstallment)
        {
            if (bankInstallment == null)
                throw new ArgumentNullException(nameof(bankInstallment));
            var builder = Builders<OmniGarantiPayPos>.Filter;
            var filter = builder.Eq(x => x.Id, bankInstallment.OmniGarantiPayPosId);
            filter = filter & builder.Where(x => x.BankInstallments.Any(y => y.Id == bankInstallment.Id));
            var update = Builders<OmniGarantiPayPos>.Update
                .Set(x => x.BankInstallments.ElementAt(-1).OmniGarantiPayPosId, bankInstallment.OmniGarantiPayPosId)
                .Set(x => x.BankInstallments.ElementAt(-1).NumberOfInstallment, bankInstallment.NumberOfInstallment)
                .Set(x => x.BankInstallments.ElementAt(-1).Percentage, bankInstallment.Percentage);

            await _garantiPayPosRepository.Collection.UpdateManyAsync(filter, update);
        }

        public async Task DeleteGarantiPayPosInstallment(BankInstallment bankInstallment)
        {
            if (bankInstallment == null)
                throw new ArgumentNullException(nameof(bankInstallment));
            var updateBuilder = Builders<OmniGarantiPayPos>.Update;
            var update = updateBuilder.Pull(p => p.BankInstallments, bankInstallment);
            await _garantiPayPosRepository.Collection.UpdateOneAsync(new BsonDocument("_id", bankInstallment.OmniGarantiPayPosId),
                update);
        }

        public async Task InsertGarantiPayInstallmentCategory(OmniGarantiPayCategory bankInstallmentCategory)
        {
            if (bankInstallmentCategory == null)
                throw new ArgumentNullException(nameof(bankInstallmentCategory));
            await _garantiPayCategoryRepository.InsertAsync(bankInstallmentCategory);
        }

        public async Task UpdateGarantiPayInstallmentCategory(OmniGarantiPayCategory bankInstallmentCategory)
        {
            if (bankInstallmentCategory == null)
                throw new ArgumentNullException(nameof(bankInstallmentCategory));
            await _garantiPayCategoryRepository.UpdateAsync(bankInstallmentCategory);
        }

        public async Task DeleteGarantiPayInstallmentCategory(OmniGarantiPayCategory bankInstallmentCategory)
        {
            if (bankInstallmentCategory == null)
                throw new ArgumentNullException(nameof(bankInstallmentCategory));
            await _garantiPayCategoryRepository.DeleteAsync(bankInstallmentCategory);
        }

        public async Task<OmniGarantiPayCategory> GetGarantiPayInstallmentCategoryId(string id)
        {
            return await _garantiPayCategoryRepository.GetByIdAsync(id);
        }

        public async Task<OmniGarantiPayCategory> GetGarantiPayInstallmentCategoryName(string bankInstallmentCategoryName)
        {
            var query = from b in _garantiPayCategoryRepository.Table
                where b.Name.Contains(bankInstallmentCategoryName)
                select b;
            return await query.FirstOrDefaultAsync();
        }

        public async Task<IPagedList<OmniGarantiPayCategory>> GetGarantiPayInstallmentCategoryList(int pageIndex = 0, int pageSize = Int32.MaxValue)
        {
            var builder = Builders<OmniGarantiPayCategory>.Filter;
            var filter = builder.Where(b => true);
            var builderSort = Builders<OmniGarantiPayCategory>.Sort.Ascending(b => b.Name);
            var query = _garantiPayCategoryRepository.Collection;

            return await PagedList<OmniGarantiPayCategory>.Create(query, filter, builderSort, pageIndex, pageSize);
        }

        public async Task InstallData()
        {
            var settings = new PaymentGarantiPaySettings
            {
                AdditionalFee = 0,
                AdditionalFeePercentage = false,
                IsInstallment = true,
                TestMode = true,
                DescriptionText = "Açıklama"
            };
            await _settingService.SaveSetting(settings);

            var pos = new OmniGarantiPayPos
            {
                IsActive = true,
                Name = "GarantiPay",
            };
          
            await InsertGarantiPayPos(pos);
        }

        public async Task UninstallData()
        {
            await _garantiPayPosRepository.Collection.Database.DropCollectionAsync(nameof(OmniGarantiPayPos));
            await _garantiPayCategoryRepository.Collection.Database.DropCollectionAsync(nameof(OmniGarantiPayCategory));
            await _bankOrderRepository.Collection.Database.DropCollectionAsync(nameof(OmniGarantiPayOrder));
        }
    }
}