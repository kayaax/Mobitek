using FluentValidation;
using Grand.Plugin.Payments.AllBank.Models;
using Grand.Services.Localization;
using System;

namespace Grand.Plugin.Payments.AllBank.Validators
{
    public class PaymentInfoValidator:AbstractValidator<PaymentInfoModel>
    {
        public PaymentInfoValidator(AllBankPaymentSettings allBankPaymentSettings,ILocalizationService localizationService)
        {
            //RuleFor(x => x.CardNumber).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardNumber.Required"));
            //RuleFor(x => x.CardCode).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardCode.Required"));

            RuleFor(x => x.CardholderName).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardholderName.Required"));
            RuleFor(x => x.CardNumber).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardNumber.Required"));
            RuleFor(x => x.CardNumber).CreditCard().WithMessage(localizationService.GetResource("Payment.CardNumber.Wrong"));
            RuleFor(x => x.CardCode).Matches(@"^[0-9]{3,4}$").WithMessage(localizationService.GetResource("Payment.CardCode.Wrong"));
            RuleFor(x => x.CardCode).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardCode.Required"));
            RuleFor(x => x.ExpirationDate).Must((x, context) =>
             {
                //not specified yet
                if (string.IsNullOrEmpty(x.ExpirationDate))
                     return true;

                //the cards remain valid until the last calendar day of that month
                //If, for example, an expiration date reads 06/15, this means it can be used until midnight on June 30, 2015
                var enteredDate = new DateTime(int.Parse($"20{x.ExpirationDate.Substring(3, 2)}"), int.Parse(x.ExpirationDate.Substring(0, 2)), 1).AddMonths(1);

                 if (enteredDate < DateTime.Now)
                     return false;

                 return true;
             }).WithMessage(localizationService.GetResource("Payment.ExpirationDate.Expired"));
        }
    }
}