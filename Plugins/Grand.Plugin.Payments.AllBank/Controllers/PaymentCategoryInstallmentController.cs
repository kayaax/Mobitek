using Grand.Framework.Controllers;
using Grand.Framework.Kendoui;
using Grand.Framework.Mvc;
using Grand.Framework.Security.Authorization;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Models.BankInstallments;
using Grand.Plugin.Payments.AllBank.Services;
using Grand.Services.Catalog;
using Grand.Services.Localization;
using Grand.Services.Security;
using Grand.Web.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Plugin.Payments.AllBank.Controllers
{
    [Area("Admin")]
    [PermissionAuthorize(PermissionSystemName.Plugins)]
    public class PaymentCategoryInstallmentController : BaseAdminController
    {
        #region Fields

        private readonly IBankPosService _bankPosService;
        private readonly ILocalizationService _localizationService;
        private readonly ICategoryService _categoryService;

        #endregion

        #region Ctor

        public PaymentCategoryInstallmentController(
            IBankPosService bankPosService,
            ILocalizationService localizationService,
            ICategoryService categoryService
        )
        {
            _bankPosService = bankPosService;
            _localizationService = localizationService;
            _categoryService = categoryService;
        }

        #endregion

        #region Method

        public IActionResult Index() => RedirectToAction("List");

        public async Task<IActionResult> List()
        {
            var model = new BankCategoryList();
            var categories = await _categoryService.GetAllCategories();
            model.Categories.Insert(0,
                new SelectListItem {Text = _localizationService.GetResource("Admin.Common.All"), Value = "0"});
            foreach (var category in categories)
            {
                model.Categories.Add(new SelectListItem {
                    Text = category.Name,
                    Value = category.Id
                });
            }

            return View("~/Plugins/Payments.AllBank/Views/PaymentBankPos/CategoryInstallment.cshtml", model);
        }

        #region BankInstallmentCategory

        public async Task<IActionResult> BankInstallmentCategories(DataSourceRequest command)
        {
            var bankCategoryList =
                await _bankPosService.GetBankInstallmentCategoryList(command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult {
                Data = bankCategoryList.Select(x =>
                {
                    var model = x.ToModel();
                    return model;
                }),
                Total = bankCategoryList.TotalCount
            };
            return Json(gridModel);
        }

        public async Task<IActionResult> BankInstallmentCategoriesAdd(string categoryId, int maxInstallment)
        {
            var category = await _categoryService.GetCategoryById(categoryId);
            var installment = new OmniBankInstallmentCategory {
                CategoryId = categoryId,
                Name = category.Name,
                MaxInstallment = maxInstallment
            };
            await _bankPosService.InsertBankInstallmentCategory(installment);
            return Json(new {Result = true});
        }

        [PermissionAuthorizeAction(PermissionActionName.Edit)]
        [HttpPost]
        public async Task<IActionResult> BankInstallmentCategoriesUpdate(BankCategoryList model)
        {
            if (ModelState.IsValid)
            {
                var bankInstallmentCategory = await _bankPosService.GetBankInstallmentCategoryId(model.Id);
                bankInstallmentCategory.MaxInstallment = model.MaxInstallment;
                bankInstallmentCategory.Name = model.Name;
                bankInstallmentCategory.CategoryId = model.CategoryId;
                await _bankPosService.UpdateBankInstallmentCategory(bankInstallmentCategory);
                return new NullJsonResult();
            }

            return ErrorForKendoGridJson(ModelState);
        }

        [HttpPost]
        public async Task<IActionResult> BankInstallmentCategoriesDelete(BankCategoryList model)
        {
            if (ModelState.IsValid)
            {
                var bankInstallmentCategory = await _bankPosService.GetBankInstallmentCategoryId(model.Id);
                await _bankPosService.DeleteBankInstallmentCategory(bankInstallmentCategory);
                return new NullJsonResult();
            }

            return ErrorForKendoGridJson(ModelState);
        }

        #endregion

        #endregion
    }
}