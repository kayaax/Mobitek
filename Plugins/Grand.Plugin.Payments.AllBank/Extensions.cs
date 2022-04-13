using Grand.Core.Mapper;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Models.BankInstallments;
using Grand.Plugin.Payments.AllBank.Models.BankOrder;
using Grand.Plugin.Payments.AllBank.Models.BankPoses;
using Grand.Plugin.Payments.AllBank.Models.BinList;

namespace Grand.Plugin.Payments.AllBank
{
    public static class MyExtensions
    {
        public static OmniBankPosModel ToModel(this OmniBankPos entity)
        {
            return entity.MapTo<OmniBankPos, OmniBankPosModel>();
        }

        public static OmniBankPos ToEntity(this OmniBankPosModel model)
        {
            return model.MapTo<OmniBankPosModel, OmniBankPos>();
        }
        public static OmniBankBinModel ToModel(this OmniBankBin entity)
        {
            return entity.MapTo<OmniBankBin, OmniBankBinModel>();
        }

        public static OmniBankBin ToEntity(this OmniBankBinModel model)
        {
            return model.MapTo<OmniBankBinModel, OmniBankBin>();
        }

        public static OmniBankOrder ToEntity(this BankOrderModel model)
        {
            return model.MapTo<BankOrderModel, OmniBankOrder>();
        }

        public static BankOrderModel ToModel(this OmniBankOrder entity)
        {
            return entity.MapTo<OmniBankOrder, BankOrderModel>();
        }
        public static OmniBankInstallmentCategoryModel ToModel(this OmniBankInstallmentCategory entity)
        {
            return entity.MapTo<OmniBankInstallmentCategory, OmniBankInstallmentCategoryModel>();
        }
        public static OmniBankInstallmentCategory ToEntity(this OmniBankInstallmentCategoryModel model)
        {
            return model.MapTo<OmniBankInstallmentCategoryModel, OmniBankInstallmentCategory>();        }
    }
}