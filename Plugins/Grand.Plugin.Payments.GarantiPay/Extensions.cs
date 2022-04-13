using Grand.Core.Mapper;
using Grand.Plugin.Payments.GarantiPay.Domain;
using Grand.Plugin.Payments.GarantiPay.Models.BankInstallments;
using Grand.Plugin.Payments.GarantiPay.Models.BankOrder;
using Grand.Plugin.Payments.GarantiPay.Models.BankPos;

namespace Grand.Plugin.Payments.GarantiPay
{
    public static class MyExtensions
    {
        public static OmniGarantiPayPos ToEntity(this OmniBankPosModel model)
        {
            return model.MapTo<OmniBankPosModel, OmniGarantiPayPos>();
        }
        public static OmniBankPosModel ToModel(this OmniGarantiPayPos entity)
        {
            return entity.MapTo<OmniGarantiPayPos, OmniBankPosModel>();
        }
        public static OmniGarantiPayOrder ToEntity(this BankOrderModel model)
        {
            return model.MapTo<BankOrderModel, OmniGarantiPayOrder>();
        }
        public static BankOrderModel ToModel(this OmniGarantiPayOrder entity)
        {
            return entity.MapTo<OmniGarantiPayOrder, BankOrderModel>();
        }
        public static OmniBankInstallmentCategoryModel ToModel(this OmniGarantiPayCategory entity)
        {
            return entity.MapTo<OmniGarantiPayCategory, OmniBankInstallmentCategoryModel>();
        }
        public static OmniGarantiPayCategory ToEntity(this OmniBankInstallmentCategoryModel model)
        {
            return model.MapTo<OmniBankInstallmentCategoryModel, OmniGarantiPayCategory>();        }
    }
}