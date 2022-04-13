using AutoMapper;
using Grand.Core.Mapper;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Models.BankInstallments;

namespace Grand.Plugin.Payments.AllBank.Infrastructure.Mapper
{
    public class OmniCategoryProfile : Profile, IAutoMapperProfile
    {

        public OmniCategoryProfile()
        {
            CreateMap<OmniBankInstallmentCategory, OmniBankInstallmentCategoryModel>();
              
            CreateMap<OmniBankInstallmentCategoryModel, OmniBankInstallmentCategory>();
                
        }
        public int Order => 0;
    }
}
