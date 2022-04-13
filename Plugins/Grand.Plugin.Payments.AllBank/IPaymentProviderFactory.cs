using System;
using System.Collections.Generic;
using Grand.Plugin.Payments.AllBank.Models.Enums;


namespace Grand.Plugin.Payments.AllBank
{
    public interface IPaymentProviderFactory
    {
        /// <summary>
        /// Bankaya göre hangi provider kullanılacak 
        /// </summary>
        /// <param name="bankName"></param>
        /// <returns></returns>
        IPaymentProvider Create(BankNames bankName);

        /// <summary>
        /// Eğer banka bize hazır form göndermiyorsa burda formu oluşturuyoruz.
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="actionUrl"></param>
        /// <param name="appendFormSubmitScript"></param>
        /// <returns></returns>
        string CreatePaymentFormHtml(IDictionary<string, object> parameters, Uri actionUrl,
            bool appendFormSubmitScript = true);
    }
}