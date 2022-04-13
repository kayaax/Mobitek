using Grand.Plugin.Payments.AllBank.Models.Enums;
using Grand.Plugin.Payments.AllBank.Providers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Grand.Plugin.Payments.AllBank
{
    public class PaymentProviderFactory : IPaymentProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        public PaymentProviderFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        /// <summary>
        /// gelen bankaya göre provider seçiliyor
        /// Banka tanımları burdan yapılıyor
        /// </summary>
        private static readonly Dictionary<BankNames, Type> ProviderTypes = new Dictionary<BankNames, Type>
        {
            //NestPay(AkBank, IsBankasi, HalkBank, ZiraatBankasi, TurkEkonomiBankasi, IngBank, TurkiyeFinans, AnadoluBank, HSBC, SekerBank)
            { BankNames.AkBank, typeof(NestPayPaymentProvider) },
            { BankNames.IsBankasi, typeof(NestPayPaymentProvider) },
            { BankNames.HalkBank, typeof(NestPayPaymentProvider) },
            { BankNames.ZiraatBankasi, typeof(NestPayPaymentProvider) },
            { BankNames.TurkEkonomiBankasi, typeof(NestPayPaymentProvider) },
            { BankNames.IngBank, typeof(NestPayPaymentProvider) },
            { BankNames.TurkiyeFinans, typeof(NestPayPaymentProvider) },
            { BankNames.AnadoluBank, typeof(NestPayPaymentProvider) },
            { BankNames.HSBC, typeof(NestPayPaymentProvider) },
            { BankNames.SekerBank, typeof(NestPayPaymentProvider) },

            //Denizbank(InterVpos)
            { BankNames.DenizBank, typeof(DenizbankPaymentProvider) },

            //Finansbank(PayFor)
            { BankNames.FinansBank, typeof(FinansbankPaymentProvider) },

            //Garanti(GVP)
            { BankNames.Garanti, typeof(GarantiPaymentProvider) },

            //KuveytTurk
            { BankNames.KuveytTurk, typeof(KuveytTurkPaymentProvider) },

            //Vakıfbank(GET 7/24)
            { BankNames.VakifBank, typeof(VakifBankPaymentProvider) },

            //POSNET(Yapıkredi, AlbarakaTurk)
            { BankNames.Yapikredi, typeof(PosnetPaymentProvider) },
            { BankNames.Albaraka, typeof(PosnetPaymentProvider) },
            
            //IYZICO
            {BankNames.Iyzico,typeof(IyzicoPaymentProvider)},
            //PAYTR
            {BankNames.PayTr,typeof(PaytrPaymentProvider)},
            //IPARA
            {BankNames.IPara,typeof(ParaPaymentProvider)}
        };
        public IPaymentProvider Create(BankNames bankName)
        {
            if (!ProviderTypes.ContainsKey(bankName))
                throw new NotSupportedException("Bank not supported");

            Type type = ProviderTypes[bankName];
            return ActivatorUtilities.CreateInstance(_serviceProvider, type) as IPaymentProvider;
        }

        public string CreatePaymentFormHtml(IDictionary<string, object> parameters, Uri actionUrl, bool appendSubmitScript = true)
        {
            if (parameters == null || !parameters.Any())
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (actionUrl == null)
            {
                throw new ArgumentNullException(nameof(actionUrl));
            }

            string formId = "PaymentForm";
            StringBuilder formBuilder = new StringBuilder();
            formBuilder.Append($"<form id=\"{formId}\" name=\"{formId}\" action=\"{actionUrl}\" role=\"form\" method=\"POST\">");

            foreach (KeyValuePair<string, object> parameter in parameters)
            {
                formBuilder.Append($"<input type=\"hidden\" name=\"{parameter.Key}\" value=\"{parameter.Value}\">");
            }

            formBuilder.Append("</form>");

            if (appendSubmitScript)
            {
                StringBuilder scriptBuilder = new StringBuilder();
                scriptBuilder.Append("<script>");
                scriptBuilder.Append($"document.{formId}.submit();");
                scriptBuilder.Append("</script>");
                formBuilder.Append(scriptBuilder.ToString());
            }
            return formBuilder.ToString();
        }
    }
}
