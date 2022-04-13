using AutoMapper;
using Grand.Core.Mapper;
using Grand.Plugin.Payments.GarantiPay.Domain;
using Grand.Plugin.Payments.GarantiPay.Models.BankInstallments;

namespace Grand.Plugin.Payments.GarantiPay.Infrastructure.Mapper
{
    public class OmniGarantiPayCategoryProfile : Profile, IAutoMapperProfile
    {

        public OmniGarantiPayCategoryProfile()
        {
            CreateMap<OmniGarantiPayCategory, OmniBankInstallmentCategoryModel>();
              
            CreateMap<OmniBankInstallmentCategoryModel, OmniGarantiPayCategory>();
                
        }
        public int Order => 0;
    }
}
