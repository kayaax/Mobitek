﻿using System.Collections.Generic;

namespace Grand.Plugin.Payments.AllBank.IParaCore.Response
{

    /// <summary>
    /// Bin Sorgulama servisi sonucunda oluşan servis çıktı parametre alanlarını temsil etmektedir. 
    /// </summary>
    public class BinNumberInquiryResponse : BaseResponse
    {

        public string bankId { get; set; }
        public string bankName { get; set; }

        public string cardFamilyName { get; set; }

        public string supportsInstallment { get; set; }
        public List<string> supportedInstallments { get; set; }
        public string type { get; set; }

        public string serviceProvider { get; set; }

        public string cardThreeDSecureMandatory { get; set; }
        public string merchantThreeDSecureMandatory { get; set; }
        public string cvcMandatory { get; set; }

        public string businessCard { get; set; }
    }

}
