using System;
using Grand.Core.Configuration;
using Grand.Core.DependencyInjection;
using Grand.Core.TypeFinders;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Services;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;

namespace Grand.Plugin.Payments.AllBank
{
    public class DependencyRegistrar:IDependencyInjection
    {
        public void Register(IServiceCollection serviceCollection, ITypeFinder typeFinder, GrandConfig config)
        {
            
            
            BsonClassMap.RegisterClassMap<OmniBankBin>(cm =>
            {
                cm.AutoMap();
            });
            BsonClassMap.RegisterClassMap<OmniBankOrder>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(b=>b.Status);
                
            });
            BsonClassMap.RegisterClassMap<OmniBankPos>(cm =>
            {
                cm.AutoMap();
                cm.UnmapMember(c=>c.BankType);
                
            });
            BsonClassMap.RegisterClassMap<OmniBankInstallmentCategory>(cm =>
            {
                cm.AutoMap();
            });
           
            serviceCollection.AddHttpClient();
            serviceCollection.AddScoped<IBankPosService, BankPosService>();
            serviceCollection.AddScoped<IBankOrderService, BankOrderService>();
            serviceCollection.AddScoped<IBankBinService, BankBinService>();
            serviceCollection.AddScoped<IInstallDataService, InstallDataService>();
            serviceCollection.AddSingleton<IPaymentProviderFactory, PaymentProviderFactory>();
            serviceCollection.AddScoped<AllBankPaymentProcessor>();

        }

        public int Order => 10;
    }
}