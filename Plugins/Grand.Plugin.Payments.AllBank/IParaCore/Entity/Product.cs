using System.Xml.Serialization;

namespace Grand.Plugin.Payments.AllBank.IParaCore.Entity
{
    /// <summary>
    /// Bu sınıf 3D secure olmadan ödeme kısmında ürün bilgisinin kullanılacağı yerde ve
    /// 3D Secure ile Ödeme'nin 2. adımında ürün bilgisinin istendiği yerde kullanılır.
    /// </summary>
    public class Product
    {
        [XmlElement("productCode")]
        public string Code { get; set; }

        [XmlElement("productName")]
        public string Title { get; set; }

        [XmlElement("quantity")]
        public int Quantity { get; set; }

        [XmlElement("price")]
        public string Price { get; set; }
    }
}