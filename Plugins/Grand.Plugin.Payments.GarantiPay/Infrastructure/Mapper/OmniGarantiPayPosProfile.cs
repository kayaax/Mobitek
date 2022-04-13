using AutoMapper;
using Grand.Core.Mapper;
using Grand.Plugin.Payments.GarantiPay.Domain;
using Grand.Plugin.Payments.GarantiPay.Models.BankPos;

namespace Grand.Plugin.Payments.GarantiPay.Infrastructure.Mapper
{
    public class OmniGarantiPayPosProfile : Profile, IAutoMapperProfile
    {
        public int Order => 0;

        public OmniGarantiPayPosProfile()
        {
            CreateMap<OmniGarantiPayPos, OmniBankPosModel>()
                .ForMember(dest => dest.AvailableBankTypeList, mo => mo.Ignore())
                .ForMember(dest => dest.AddBankInstallmentModel, mo => mo.Ignore())
                .ForMember(dest => dest.BankInstallmentModels, mo => mo.Ignore())
                .ForMember(dest => dest.ParameterModel, mo => mo.Ignore());

            CreateMap<OmniBankPosModel, OmniGarantiPayPos>()

                .ForMember(dest => dest.BankInstallments, mo => mo.Ignore());





        }
    }
}
