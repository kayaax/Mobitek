using AutoMapper;
using Grand.Domain.Customers;
using Grand.Core.Mapper;
using Grand.Web.Areas.Admin.Models.Customers;

namespace Grand.Web.Areas.Admin.Infrastructure.Mapper.Profiles
{
    public class CustomerRoleProfile : Profile, IAutoMapperProfile
    {
        public CustomerRoleProfile()
        {
            CreateMap<CustomerRole, CustomerRoleModel>();
            CreateMap<CustomerRoleModel, CustomerRole>()
                .ForMember(dest => dest.Id, mo => mo.Ignore());
        }

        public int Order => 0;
    }
}