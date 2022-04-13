using FluentValidation;
using Grand.Core.Validators;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Services.Localization;
using System.Collections.Generic;

namespace Grand.Plugin.Payments.AllBank.Validators.Bank
{
    public class BankBinValidator : BaseGrandValidator<OmniBankBin>
    {
        public BankBinValidator(IEnumerable<IValidatorConsumer<OmniBankBin>> validators,
            ILocalizationService localizationService) : base(validators)
        {
            RuleFor(x => x.BankCode).NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payment.AllBank.BankBin.Fields.Code.Required"))
                .MinimumLength(2)
                .MaximumLength(5);
            RuleFor(x => x.BinNumber).NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payment.AllBank.BankBin.Fields.No.Required"))
                .MinimumLength(6)
                .MaximumLength(6);
        }
    }
}
