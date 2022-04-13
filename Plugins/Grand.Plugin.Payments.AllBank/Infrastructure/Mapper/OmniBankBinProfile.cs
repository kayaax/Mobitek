using AutoMapper;
using Grand.Core.Mapper;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Models.BinList;

namespace Grand.Plugin.Payments.AllBank.Infrastructure.Mapper
{
    public class OmniBankBinProfile  :Profile,IAutoMapperProfile
    {
        public int Order => 0;

        public OmniBankBinProfile()
        {
            CreateMap<OmniBankBin, OmniBankBinModel>();

            CreateMap<OmniBankBinModel, OmniBankBin>();

        }

    }
}