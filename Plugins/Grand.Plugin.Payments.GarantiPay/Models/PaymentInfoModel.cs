using System;
using Grand.Core.Models;

namespace Grand.Plugin.Payments.GarantiPay.Models
{
    public class PaymentInfoModel:BaseModel
    {
      
        public string Html { get; set; }
        public string PaymentUrl { get; set; }
        

      
    }
}