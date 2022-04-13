using Grand.Domain.Data;
using Grand.Plugin.Payments.AllBank.Domain;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grand.Domain;

namespace Grand.Plugin.Payments.AllBank.Services
{
    public class BankOrderService:IBankOrderService
    {
        private readonly IRepository<OmniBankOrder> _bankOrderRepository;

        public BankOrderService(IRepository<OmniBankOrder> bankOrderRepository)
        {
            _bankOrderRepository = bankOrderRepository;
        }
        public async Task InsertBankOrder(OmniBankOrder bankOrder)
        {
            if (bankOrder == null)
                throw new ArgumentNullException(nameof(bankOrder));
            await _bankOrderRepository.InsertAsync(bankOrder);
        }
        public async Task UpdateBankOrder(OmniBankOrder bankOrder)
        {
            if (bankOrder == null)
                throw new ArgumentNullException(nameof(bankOrder));
            await _bankOrderRepository.UpdateAsync(bankOrder);
        }
        public async Task DeleteBankOrder(OmniBankOrder bankOrder)
        {
            if (bankOrder == null)
                throw new ArgumentNullException(nameof(bankOrder));
            await _bankOrderRepository.DeleteAsync(bankOrder);
        }
        public async Task<OmniBankOrder> GetBankOrderId(string id)
        {
            return await _bankOrderRepository.GetByIdAsync(id);
        }

        public async Task<List<OmniBankOrder>> GetBankOrderCustomerId(Guid customerGuid)
        {
            var query = from b in _bankOrderRepository.Table
                        where b.CustomerId == customerGuid.ToString("N")
                        orderby b.CustomerId
                        select b;

            var bankOrder = await query.ToListAsync();
            return bankOrder;
        }

        public async Task<OmniBankOrder> GetBankOrderPaymentSession(Guid paymentInfoSession)
        {
            var query = from b in _bankOrderRepository.Table
                        where b.PaymentInfoSession == paymentInfoSession
                        orderby b.CreateDate descending 
                        select b;

            var bankOrder = await query.FirstOrDefaultAsync();
            return bankOrder;
        }

        public async Task<OmniBankOrder> GetBankOrderGuidId(Guid orderGuid)
        {
            var query = from b in _bankOrderRepository.Table
                where b.OrderGuid == orderGuid
                orderby b.CustomerId
                select b;

            var bankOrder = await query.FirstOrDefaultAsync();
            return bankOrder;

        }

        public async Task<OmniBankOrder> GetBankOrderOrderNumber(string orderNumber)
        {
            var query = from b in _bankOrderRepository.Table
                where b.OrderNumber == orderNumber
                orderby b.CustomerId
                select b;

            var bankOrder = await query.FirstOrDefaultAsync();
            return bankOrder;
        }

        public async Task<OmniBankOrder> GetBankOrderTokenId(string token)
        {
            var query = from b in _bankOrderRepository.Table
                where b.Token.Contains(token)
                orderby b.CustomerId
                select b;
            var bankOrder = await query.FirstOrDefaultAsync();
            return bankOrder;
        }
        public async Task<IList<OmniBankOrder>> GetBankOrderList()
        {
            var query = from b in _bankOrderRepository.Table 
                                                  orderby b.CustomerId
                                                  select b;
            var bankOrders = await query.ToListAsync();
            return bankOrders;
        }

        public async Task<IPagedList<OmniBankOrder>> GetBankOrderPageList(string customerId = null,  int pageIndex = 0,
            int pageSize = Int32.MaxValue)
        {
            var builder = Builders<OmniBankOrder>.Filter;
            var filter = builder.Where(b => true);
            if (!String.IsNullOrEmpty(customerId))
                filter = filter & builder.Where(b => b.CustomerId.Contains(customerId));
            var builderSort = Builders<OmniBankOrder>.Sort.Descending(x => x.Id);
            var query = _bankOrderRepository.Collection;
            var bankOrderList = await PagedList<OmniBankOrder>.Create(query, filter, builderSort, pageIndex, pageSize);
            return bankOrderList;
        }
    }
}
