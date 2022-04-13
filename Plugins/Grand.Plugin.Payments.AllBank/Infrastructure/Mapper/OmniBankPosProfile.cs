using AutoMapper;
using Grand.Core.Mapper;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Models.BankPoses;

namespace Grand.Plugin.Payments.AllBank.Infrastructure.Mapper
{
    public class OmniBankPosProfile : Profile, IAutoMapperProfile
    {
        public int Order => 0;

        public OmniBankPosProfile()
        {
            CreateMap<OmniBankPos, OmniBankPosModel>()
                
                .ForMember(dest => dest.PictureUrl, mo => mo.Ignore())
                .ForMember(dest => dest.BankTypeName, mo => mo.Ignore())
                .ForMember(dest => dest.AvailableBankTypeList, mo => mo.Ignore())
                .ForMember(dest => dest.AddBankInstallmentModel, mo => mo.Ignore())
                .ForMember(dest => dest.BankInstallmentModels, mo => mo.Ignore())
                .ForMember(dest => dest.BankImageId, mo => mo.Ignore())
                .ForMember(dest => dest.BankColor, mo => mo.Ignore())
                .ForMember(dest => dest.ParameterModel, mo => mo.Ignore());

            CreateMap<OmniBankPosModel, OmniBankPos>()
                .ForMember(dest => dest.CreatedOnUtc, mo => mo.Ignore())
                .ForMember(dest => dest.BankType, mo => mo.Ignore())
                .ForMember(dest => dest.BankInstallments, mo => mo.Ignore())
                .ForMember(dest => dest.UpdatedOnUtc, mo => mo.Ignore());

            



        }
    }
}
