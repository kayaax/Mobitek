using Newtonsoft.Json;
using System;

namespace Grand.Plugin.Payments.AllBank.Iyzico.Models
{
    public class InstallmentPrice
    {
        [JsonProperty(PropertyName = "InstallmentPrice")]
        public String Price { get; set; }
        public String TotalPrice { get; set; }
        public int? InstallmentNumber { get; set; }
    }
}
