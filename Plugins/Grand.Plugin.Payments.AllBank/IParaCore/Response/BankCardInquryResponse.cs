using Grand.Plugin.Payments.AllBank.IParaCore.Entity;
using System.Collections.Generic;

namespace Grand.Plugin.Payments.AllBank.IParaCore.Response
{
    /// <summary>
    /// Cüzdanda bulunan kartları getirmek için kullanılan servis çıktı parametrelerini temsil etmektedir.
    /// </summary>
    public class BankCardInquryResponse : BaseResponse
    {
        public List<BankCard> cards { get; set; }

    }
}
