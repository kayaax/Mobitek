using Grand.Plugin.Payments.AllBank.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grand.Domain;

namespace Grand.Plugin.Payments.AllBank.Services
{
    public interface IBankOrderService
    {
        Task InsertBankOrder(OmniBankOrder bankOrder);
        Task UpdateBankOrder(OmniBankOrder bankOrder);
        Task DeleteBankOrder(OmniBankOrder bankOrder);
        Task<OmniBankOrder> GetBankOrderId(string id);
        Task<List<OmniBankOrder>> GetBankOrderCustomerId(Guid customerGuid);
        Task<OmniBankOrder> GetBankOrderPaymentSession(Guid paymentInfoSession);
        Task<OmniBankOrder> GetBankOrderGuidId(Guid orderGuid);
        Task<OmniBankOrder> GetBankOrderOrderNumber(string orderNumber);
        Task<OmniBankOrder> GetBankOrderTokenId(string token);
        Task<IList<OmniBankOrder>> GetBankOrderList();
        Task<IPagedList<OmniBankOrder>> GetBankOrderPageList(string customerId = null, int pageIndex = 0, int pageSize = int.MaxValue);

    }
}
