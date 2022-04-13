using FluentValidation;
using Grand.Core.Validators;
using Grand.Plugin.Payments.AllBank.Models.BankPoses;
using Grand.Services.Localization;
using System.Collections.Generic;

namespace Grand.Plugin.Payments.AllBank.Validators.Bank
{
    public class BankPosValidator:BaseGrandValidator<OmniBankPosModel>
    {
        public BankPosValidator(IEnumerable<IValidatorConsumer<OmniBankPosModel>> validators,
            ILocalizationService localizationService) : base(validators)
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payment.AllBank.BankPos.Fields.Name.Required"));
           
        }
    }
}