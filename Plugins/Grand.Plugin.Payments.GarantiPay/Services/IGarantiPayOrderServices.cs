using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grand.Domain;
using Grand.Plugin.Payments.GarantiPay.Domain;

namespace Grand.Plugin.Payments.GarantiPay.Services
{
    public interface IGarantiPayOrderServices
    {
        Task InsertOmniGarantiPayOrder(OmniGarantiPayOrder bankOrder);
        Task UpdateOmniGarantiPayOrder(OmniGarantiPayOrder bankOrder);
        Task DeleteOmniGarantiPayOrder(OmniGarantiPayOrder bankOrder);
        Task<OmniGarantiPayOrder> GetOmniGarantiPayOrderId(string id);
        Task<List<OmniGarantiPayOrder>> GetOmniGarantiPayCustomerId(Guid customerGuid);
        Task<OmniGarantiPayOrder> GetOmniGarantiPayPaymentSession(Guid paymentInfoSession);
        Task<OmniGarantiPayOrder> GetOmniGarantiPayGuidId(Guid orderGuid);
        Task<OmniGarantiPayOrder> GetOmniGarantiPayNumber(string orderNumber);
        Task<OmniGarantiPayOrder> GetOmniGarantiPayTokenId(string token);
        Task<IList<OmniGarantiPayOrder>> GetOmniGarantiPayList();
        Task<IPagedList<OmniGarantiPayOrder>> GetOmniGarantiPayOrderPageList(string customerId = null, int pageIndex = 0, int pageSize = int.MaxValue);
        Task InsertGarantiPayPos(OmniGarantiPayPos bankPos);
        Task UpdateGarantiPayPos(OmniGarantiPayPos bankPos);
        Task<IPagedList<OmniGarantiPayPos>> GetGarantiPayPosPageList(string bankId = null,  int pageIndex = 0, int pageSize = int.MaxValue);

        Task<IList<OmniGarantiPayPos>> GetGarantiPayPosList();
        Task<OmniGarantiPayPos> GetGarantiPayPosId(string id);
        Task InsertGarantiPayPosInstallment(BankInstallment bankInstallment);
        Task UpdateGarantiPayPosInstallment(BankInstallment bankInstallment);
        Task DeleteGarantiPayPosInstallment(BankInstallment bankInstallment);

      
        Task InsertGarantiPayInstallmentCategory(OmniGarantiPayCategory bankInstallmentCategory);
        Task UpdateGarantiPayInstallmentCategory(OmniGarantiPayCategory bankInstallmentCategory);
        Task DeleteGarantiPayInstallmentCategory(OmniGarantiPayCategory bankInstallmentCategory);
        Task<OmniGarantiPayCategory> GetGarantiPayInstallmentCategoryId(string id);
        Task<OmniGarantiPayCategory> GetGarantiPayInstallmentCategoryName(string bankInstallmentCategoryName);
        Task<IPagedList<OmniGarantiPayCategory>> GetGarantiPayInstallmentCategoryList(int pageIndex = 0, int pageSize = Int32.MaxValue);

        Task InstallData();
        Task UninstallData();

    }
}