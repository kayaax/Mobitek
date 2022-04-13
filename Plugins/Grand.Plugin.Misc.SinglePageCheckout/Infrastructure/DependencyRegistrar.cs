using Grand.Core.Configuration;
using Grand.Core.DependencyInjection;
using Grand.Core.TypeFinders;
using Grand.Plugin.Misc.SinglePageCheckout.Filters;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Grand.Plugin.Misc.SinglePageCheckout.Infrastructure
{
    public class DependencyInjection : IDependencyInjection
    {
        public virtual void Register(IServiceCollection serviceCollection, ITypeFinder typeFinder, GrandConfig config)
        {
            serviceCollection.AddScoped<SinglePageCheckoutPlugin>();
            serviceCollection.AddScoped<IFilterProvider, SpcProvider>();
        }

        public int Order
        {
            get { return 1; }
        }
    }
}
