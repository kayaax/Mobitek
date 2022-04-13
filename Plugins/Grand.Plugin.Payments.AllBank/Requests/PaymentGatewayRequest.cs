using System;
using System.Collections.Generic;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Plugin.Payments.AllBank.Iyzico.Models;
using Grand.Plugin.Payments.AllBank.Models.Enums;
using Grand.Services.Payments;
using Currency = Grand.Domain.Directory.Currency;

namespace Grand.Plugin.Payments.AllBank.Requests
{
    public class PaymentGatewayRequest
    {
        /// <summary>
        /// Kredi kartı üstündeki ad
        /// </summary>
        public string CardHolderName { get; set; }

        /// <summary>
        /// Kredi kartı numarası
        /// </summary>
        public string CardNumber { get; set; }

        /// <summary>
        /// son kullanma tarih için ay bilgisi
        /// </summary>
        public int ExpireMonth { get; set; }

        /// <summary>
        /// Kredi kartı skt yıl bilgisi
        /// </summary>
        public int ExpireYear { get; set; }

        /// <summary>
        /// kk cvc bilgisi
        /// </summary>
        public string CvvCode { get; set; }

        /// <summary>
        /// Kredi Kartı mı? yoksa Bankad Kartımı debit
        /// </summary>
        public string CardType { get; set; }

        /// <summary>
        /// visa master troy
        /// </summary>
        public string CardAssociation { get; set; }

        /// <summary>
        /// bonus world axsess
        /// </summary>
        public string CardFamily { get; set; }

        /// <summary>
        /// Taksit sayısı
        /// </summary>
        public int Installment { get; set; }

        /// <summary>
        /// Toplam tahsi edilecek tutar
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Sipariş numarası
        /// </summary>
        public string OrderNumber { get; set; }

        /// <summary>
        /// kur bilgisi TL USD EUR
        /// </summary>
        public string CurrencyIsoCode { get; set; }

        /// <summary>
        /// Kullanılan Dil 
        /// </summary>
        public string LanguageIsoCode { get; set; }

        /// <summary>
        /// kullanıcının ip adressi
        /// </summary>
        public string CustomerIpAddress { get; set; }

        /// <summary>
        /// siparişin gönderileceği ülke seçimi
        /// </summary>
        public Country Country { get; set; }

        /// <summary>
        /// Sipariş sahibi
        /// </summary>
        public Customer Customer { get; set; }
        /// <summary>
        /// ıyzico için sepet bilgisi
        /// </summary>
        public List<BasketItem> BasketItems { get; set; }
        /// <summary>
        /// sepet listesi
        /// </summary>
        public IList<ShoppingCartItem> ShoppingCartItems { get; set; }
       
        /// <summary>
        /// iyzico için tutar bilgisi
        /// </summary>
        public decimal IyzicoPrice { get; set; }
        /// <summary>
        /// sipariş kaydı
        /// </summary>
        public Order Order { get; set; }
        /// <summary>
        /// kur 
        /// </summary>
        public Currency Currency { get; set; }
        /// <summary>
        /// üretici kart
        /// </summary>
        public bool ManufacturerCard { get; set; }
        /// <summary>
        /// ortak ödeme sayfası
        /// </summary>
        public bool CommonPaymentPage { get; set; }
        /// <summary>
        /// Dönüş adresi
        /// </summary>
        public Uri CallbackUrl { get; set; }
        /// <summary>
        /// Banka lsitesi enum
        /// </summary>
        public BankNames BankName { get; set; }
        /// <summary>
        /// Test Modu
        /// </summary>
        public bool TestMode { get; set; }
        /// <summary>
        /// Banka parametreleri
        /// </summary>
        public Dictionary<string, string> BankParameters { get; set; } = new Dictionary<string, string>();
    }
}