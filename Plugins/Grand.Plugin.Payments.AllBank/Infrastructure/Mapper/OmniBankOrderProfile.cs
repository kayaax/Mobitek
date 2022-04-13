using AutoMapper;
using Grand.Core.Mapper;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Models.BankOrder;

namespace Grand.Plugin.Payments.AllBank.Infrastructure.Mapper
{
    public class OmniBankOrderProfile: Profile, IAutoMapperProfile
    {

        public OmniBankOrderProfile()
        {
            CreateMap<OmniBankOrder, BankOrderModel>();
            CreateMap<BankOrderModel, OmniBankOrder>();
        }
        public int Order => 0;
    }
}