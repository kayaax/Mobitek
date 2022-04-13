using System;

namespace Grand.Plugin.Payments.AllBank.Iyzico.Request
{
    public class RetrieveCheckoutFormRequest : BaseRequest
    {
        public String Token { set; get; }

        public override String ToPKIRequestString()
        {
            return ToStringRequestBuilder.NewInstance()
                .AppendSuper(base.ToPKIRequestString())
                .Append("token", Token)
                .GetRequestString();
        }
    }
}
