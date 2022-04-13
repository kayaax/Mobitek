using Grand.Domain;
using Grand.Plugin.Payments.AllBank.Domain;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Grand.Plugin.Payments.AllBank.Services
{
    public interface IBankPosService
    {
        Task InsertBankPos(OmniBankPos bankPos);
        Task UpdateBankPos(OmniBankPos bankPos);
        Task DeleteBankPos(OmniBankPos bankPos);
        Task<OmniBankPos> GetBankPosId(string id);
        Task<IList<OmniBankPos>> GetDefaultBankPos();
        Task<OmniBankPos> GetDefaultBankPosPrimary(bool primary = false);
        Task<OmniBankPos> GetDefaultBankPosName(string bankName);

        Task<IList<OmniBankPos>> GetBankPosList();
        Task<IPagedList<OmniBankPos>> GetBankPosPageList(string bankId = null,int bankTypeId=0,bool showHidden = true, int pageIndex = 0, int pageSize = int.MaxValue);
        Task<IPagedList<OmniBankPos>> GetBankPosPageList(string bankId = null,int bankTypeId=0, int pageIndex = 0, int pageSize = int.MaxValue);
        Task InsertBankPosInstallment(BankInstallment bankInstallment);
        Task UpdateBankPosInstallment(BankInstallment bankInstallment);
        Task DeleteBankPosInstallment(BankInstallment bankInstallment);
        
        Task InsertBankInstallmentCategory(OmniBankInstallmentCategory bankInstallmentCategory); 
        Task UpdateBankInstallmentCategory(OmniBankInstallmentCategory bankInstallmentCategory); 
        Task DeleteBankInstallmentCategory(OmniBankInstallmentCategory bankInstallmentCategory);
        Task<OmniBankInstallmentCategory> GetBankInstallmentCategoryId(string id); 
        Task<OmniBankInstallmentCategory> GetBankInstallmentCategoryName(string bankInstallmentCategoryName); 

        Task<IPagedList<OmniBankInstallmentCategory>> GetBankInstallmentCategoryList(int pageIndex = 0, int pageSize = Int32.MaxValue);

    }
}
