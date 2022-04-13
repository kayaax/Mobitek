using System.Collections.Generic;

namespace Grand.Plugin.Payments.AllBank.IParaCore.Response
{
    /// <summary>
    /// Cüzdana kart ekleme servis çıktı parametre alanlarını temsil eder.
    /// </summary>
    public class BankCardCreateResponse : BaseResponse
    {
        public string cardId { get; set; }

        public string maskNumber { get; set; }

        public string alias { get; set; }

        public string bankId { get; set; }

        public string bankName { get; set; }

        public string cardFamilyName { get; set; }

        public string supportsInstallment { get; set; }
        public List<string> supportedInstallments { get; set; }
        public string type { get; set; }

        public string serviceProvider { get; set; }

        public string threeDSecureMandatory { get; set; }
        public string cvcMandatory { get; set; }
    }
}
