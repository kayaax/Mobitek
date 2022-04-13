using Grand.Core.Configuration;
using Grand.Core.DependencyInjection;
using Grand.Core.TypeFinders;
using Grand.Plugin.Payments.GarantiPay.Domain;
using Grand.Plugin.Payments.GarantiPay.Services;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;

namespace Grand.Plugin.Payments.GarantiPay
{
    public class DependencyRegistrar:IDependencyInjection
    {
        public void Register(IServiceCollection serviceCollection, ITypeFinder typeFinder, GrandConfig config)
        {
            BsonClassMap.RegisterClassMap<OmniGarantiPayOrder>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(b=>b.Status);
                
            });
            BsonClassMap.RegisterClassMap<OmniGarantiPayCategory>(cm =>
            {
                cm.AutoMap();
            });
            BsonClassMap.RegisterClassMap<OmniGarantiPayPos>(cm =>
            {
                cm.AutoMap();
            });
            serviceCollection.AddHttpClient();
            serviceCollection.AddScoped<IGarantiPayOrderServices, GarantiPayOrderServices>();
            serviceCollection.AddScoped<PaymentGarantiPayProcessor>();
        }

        public int Order => 11;
    }
}