using Grand.Plugin.Payments.AllBank.Requests;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Grand.Plugin.Payments.AllBank.Results;

namespace Grand.Plugin.Payments.AllBank
{
    public interface IPaymentProvider
    {
        /// <summary>
        /// 3D ödeme yapabilmek için gerekli parametrelenin aldığımız method
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request);
        /// <summary>
        /// Bankadan dönen bilginin işlendiği method
        /// </summary>
        /// <param name="request"></param>
        /// <param name="gatewayRequest"></param>
        /// <param name="form"></param>
        /// <returns></returns>
        Task<VerifyGatewayResult> VerifyGateway(VerifyGatewayRequest request, PaymentGatewayRequest gatewayRequest, IFormCollection form);
        /// <summary>
        /// Siparişi bankadan iptal etmek isterseniz kullanabilirsiniz
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<CancelPaymentResult> CancelRequest(CancelPaymentRequest request);
        /// <summary>
        /// Bankadan alınan ödemenin iadesini yapabileceğiniz method
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<RefundPaymentResult> RefundRequest(RefundPaymentRequest request);
        /// <summary>
        /// Bankaya siparişle ilgili detay sorabilirsiniz.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<PaymentDetailResult> PaymentDetailRequest(PaymentDetailRequest request);
    }
}
