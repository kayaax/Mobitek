﻿using Grand.Plugin.Payments.AllBank.IParaCore.Entity;
using Grand.Plugin.Payments.AllBank.IParaCore.Response;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Grand.Plugin.Payments.AllBank.IParaCore.Request
{
    /// <summary>
    /// 3D Secure ile ödeme 2. Adımında gerekli olan 3D servis girdi parametrelerini temsil eder.
    /// </summary>
    [XmlRoot("auth")]
    public class ThreeDPaymentCompleteRequest : BaseRequest
    {

        [XmlElement("threeD")]
        public string ThreeD { get; set; }

        [XmlElement("orderId")]
        public string OrderId { get; set; }

        [XmlElement("amount")]
        public string Amount { get; set; }


        [XmlElement("cardOwnerName")]
        public string CardOwnerName { get; set; }

        [XmlElement("cardNumber")]
        public string CardNumber { get; set; }

        [XmlElement("cardExpireMonth")]
        public string CardExpireMonth { get; set; }

        [XmlElement("cardExpireYear")]
        public string CardExpireYear { get; set; }

        [XmlElement("installment")]
        public string Installment { get; set; }

        [XmlElement("cardCvc")]
        public string Cvc { get; set; }


        [XmlElement("vendorId")]
        public string VendorId { get; set; }
        [XmlElement("userId")]
        public string UserId { get; set; }
        [XmlElement("cardId")]
        public string CardId { get; set; }

        [XmlElement("threeDSecureCode")]
        public string ThreeDSecureCode { get; set; }

        [XmlArray("products"), XmlArrayItem(typeof(Product), ElementName = "product", IsNullable = false)]
        public List<Product> Products { get; set; }

        [XmlElement("purchaser")]
        public Purchaser Purchaser { get; set; }

        /// <summary>
        /// 3D Secure 2. Adımında ödeme onayı sağlanarak tahsilat gerçekleştirilmesi için gerekli olan servis isteğini temsil eder.
        /// </summary>
        /// <param name="request">Ödeme Onayı sağlamak için gerekli olan girdilerin olduğu sınıfı temsil eder.</param>
        /// <param name="options">Kullanıcıya özel olarak belirlenen ayarları temsil eder.</param>
        /// <returns></returns>
        public static ThreeDPaymentCompleteResponse Execute(ThreeDPaymentCompleteRequest request, Settings options)
        {
            options.TransactionDate = Helper.GetTransactionDateString();
            options.HashString = options.PrivateKey + request.OrderId + request.Amount + request.Mode + request.ThreeDSecureCode + options.TransactionDate;
            return RestHttpCaller.Create().PostXML<ThreeDPaymentCompleteResponse>(options.BaseUrl + "rest/payment/auth", Helper.GetHttpHeaders(options, Helper.application_xml), request);
        }
    }




}

