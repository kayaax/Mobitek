using AutoMapper;
using Grand.Core.Mapper;
using Grand.Plugin.Payments.GarantiPay.Domain;
using Grand.Plugin.Payments.GarantiPay.Models.BankOrder;

namespace Grand.Plugin.Payments.GarantiPay.Infrastructure.Mapper
{
    public class OmniGarantiPayOrderProfile: Profile, IAutoMapperProfile
    {

        public OmniGarantiPayOrderProfile()
        {
            CreateMap<OmniGarantiPayOrder, BankOrderModel>();
            CreateMap<BankOrderModel, OmniGarantiPayOrder>();
        }
        public int Order => 0;
    }
}