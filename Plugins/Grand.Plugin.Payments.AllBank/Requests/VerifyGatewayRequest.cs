using System.Collections.Generic;
using Grand.Plugin.Payments.AllBank.Models.Enums;

namespace Grand.Plugin.Payments.AllBank.Requests
{
    public class VerifyGatewayRequest
    {
        /// <summary>
        /// kullanıcı ip adresi
        /// </summary>
        public string CustomerIpAddress { get; set; }
        /// <summary>
        /// üretic kartı
        /// </summary>
        public bool ManufacturerCard { get; set; }
        /// <summary>
        /// banka listesi
        /// </summary>
        public BankNames BankName { get; set; }
        /// <summary>
        /// parametreler
        /// </summary>
        public Dictionary<string, string> BankParameters { get; set; } = new Dictionary<string, string>();
    }
}