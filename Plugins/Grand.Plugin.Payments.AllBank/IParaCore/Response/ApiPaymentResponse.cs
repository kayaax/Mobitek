using System.Xml.Serialization;

namespace Grand.Plugin.Payments.AllBank.IParaCore.Response
{
    /// <summary>
    /// 3D secure olmadan ödeme servis çıktı parametre alanlarını temsil etmektedir.
    /// </summary>
    [XmlRoot("authResponse")]
    public class ApiPaymentResponse : BaseResponse
    {
        [XmlElement("amount")]
        public string Amount { get; set; }
        [XmlElement("orderId")]
        public string OrderId { get; set; }
    }
}
