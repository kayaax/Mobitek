using Grand.Core;
using Grand.Domain.Data;
using Grand.Plugin.Payments.AllBank.Domain;
using Grand.Plugin.Payments.AllBank.Models.Enums;
using Grand.Services.Configuration;
using Grand.Services.Media;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Grand.Services.Catalog;

namespace Grand.Plugin.Payments.AllBank.Services
{
    public class InstallDataService : IInstallDataService
    {
        private readonly IRepository<OmniBankBin> _bankBinRepository;
       
        private readonly IRepository<OmniBankPos> _bankPosRepository;
        private readonly IPictureService _pictureService;
        private readonly IRepository<OmniBankOrder> _bankOrderRepository;
        private readonly IRepository<OmniBankInstallmentCategory> _bankInstallmentCategoryRepository;
        private readonly ISettingService _settingService;        
        private readonly ICategoryService _categoryService;
        public InstallDataService(IServiceProvider serviceProvider)
        {
            _bankBinRepository = serviceProvider.GetRequiredService<IRepository<OmniBankBin>>();
            _bankPosRepository = serviceProvider.GetRequiredService<IRepository<OmniBankPos>>();
            _bankInstallmentCategoryRepository = serviceProvider.GetRequiredService<IRepository<OmniBankInstallmentCategory>>();
            _bankOrderRepository = serviceProvider.GetRequiredService<IRepository<OmniBankOrder>>();
            _pictureService = serviceProvider.GetRequiredService<IPictureService>();
            _settingService = serviceProvider.GetRequiredService<ISettingService>();       
            _categoryService = serviceProvider.GetRequiredService<ICategoryService>();
        }

        private async Task InstallBankBin()
        {
            var settings = new AllBankPaymentSettings {
                IsInstallment = false,
                TestMode = true,
                AdditionalFee =0,
                AdditionalFeePercentage =  false,
                InstallmentCategoryBased = false,
                DescriptionText = ""
            };
            await _settingService.SaveSetting(settings);

            await _bankBinRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<OmniBankBin>((Builders<OmniBankBin>.IndexKeys.Ascending(x => x.BinNumber)),
            new CreateIndexOptions() { Name = "BinNumber", Unique = true }));
            await _bankPosRepository.Collection.Indexes.CreateOneAsync(new CreateIndexModel<OmniBankPos>(
            (Builders<OmniBankPos>.IndexKeys.Ascending(x => x.Name)),
            new CreateIndexOptions() { Name = "Name", Unique = true }));
            //pictures
            var sampleImagesPath = CommonHelper.MapPath("~/Plugins/Payments.AllBank/Content/Banks/");
            var byte1 = File.ReadAllBytes(sampleImagesPath + "akbank.jpg");
            var byte2 = File.ReadAllBytes(sampleImagesPath + "albaraka.jpg");
            var byte3 = File.ReadAllBytes(sampleImagesPath + "anadolubank.jpg");
            var byte4 = File.ReadAllBytes(sampleImagesPath + "denizbank.jpg");
            var byte5 = File.ReadAllBytes(sampleImagesPath + "finansbank.jpg");
            var byte6 = File.ReadAllBytes(sampleImagesPath + "garanti.jpg");
            var byte7 = File.ReadAllBytes(sampleImagesPath + "halkbank.jpg");
            var byte8 = File.ReadAllBytes(sampleImagesPath + "hsbc.jpg");
            var byte9 = File.ReadAllBytes(sampleImagesPath + "ingbank.jpg");
            var byte10 = File.ReadAllBytes(sampleImagesPath + "ipara.jpg");
            var byte11 = File.ReadAllBytes(sampleImagesPath + "isbankasi.jpg");
            var byte12 = File.ReadAllBytes(sampleImagesPath + "iyzico.jpg");
            var byte13 = File.ReadAllBytes(sampleImagesPath + "kuveytturk.jpg");
            var byte14 = File.ReadAllBytes(sampleImagesPath + "paytr.jpg");
            var byte15 = File.ReadAllBytes(sampleImagesPath + "sekerbank.jpg");
            var byte16 = File.ReadAllBytes(sampleImagesPath + "turkekonomibankasi.jpg");
            var byte17 = File.ReadAllBytes(sampleImagesPath + "turkiyefinans.jpg");
            var byte18 = File.ReadAllBytes(sampleImagesPath + "vakifbank.jpg");
            var byte19 = File.ReadAllBytes(sampleImagesPath + "yapikredi.jpg");
            var byte20 = File.ReadAllBytes(sampleImagesPath + "ziraatbankasi.jpg");

            var akbankPicture = await _pictureService.InsertPicture(byte1, "image/png", "akbank", validateBinary: false);
            var albarakaPicture = await _pictureService.InsertPicture(byte2, "image/png", "albarak", validateBinary: false);
            var anadoluPicture = await _pictureService.InsertPicture(byte3, "image/png", "anadolu", validateBinary: false);
            var denizPicture = await _pictureService.InsertPicture(byte4, "image/png", "deniz", validateBinary: false);
            var finansPicture = await _pictureService.InsertPicture(byte5, "image/png", "finans", validateBinary: false);
            var garantiPicture = await _pictureService.InsertPicture(byte6, "image/png", "garanti", validateBinary: false);
            var halkPicture = await _pictureService.InsertPicture(byte7, "image/png", "halk", validateBinary: false);
            var hsbcPicture = await _pictureService.InsertPicture(byte8, "image/png", "hsbc", validateBinary: false);
            var ingPicture = await _pictureService.InsertPicture(byte9, "image/png", "ing", validateBinary: false);
            var iparaPicture = await _pictureService.InsertPicture(byte10, "image/png", "ipara", validateBinary: false);
            var isbankasiPicture = await _pictureService.InsertPicture(byte11, "image/png", "isbankasi", validateBinary: false);
            var iyzicoPicture = await _pictureService.InsertPicture(byte12, "image/png", "iyzico", validateBinary: false);
            var kuveytPicture = await _pictureService.InsertPicture(byte13, "image/png", "kuveyt", validateBinary: false);
            var paytrPicture = await _pictureService.InsertPicture(byte14, "image/png", "paytr", validateBinary: false);
            var sekerbankPicture = await _pictureService.InsertPicture(byte15, "image/png", "seker", validateBinary: false);
            var turkiyeEkonomiPicture = await _pictureService.InsertPicture(byte16, "image/png", "teb", validateBinary: false);
            var turkiyeFinansPicture = await _pictureService.InsertPicture(byte17, "image/png", "turkiyeFinans", validateBinary: false);
            var vakifPicture = await _pictureService.InsertPicture(byte18, "image/png", "vakif", validateBinary: false);
            var yapikrediPicture = await _pictureService.InsertPicture(byte19, "image/png", "yapikredi", validateBinary: false);
            var ziraatPicture = await _pictureService.InsertPicture(byte20, "image/png", "ziraatbank", validateBinary: false);
            var bankPosList = new List<OmniBankPos>();

            var iyzicoPos = new OmniBankPos {
                BankTypeId = (int)BankType.Ortak,
                Name = BankNames.Iyzico.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.Iyzico),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = iyzicoPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,

            };
            bankPosList.Add(iyzicoPos);
            var akbankPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.AkBank.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.AkBank),
                IsActive = false,
                Primary = false,
                PictureId = akbankPicture.Id,
                PrimaryBank = false,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow,
            };
            bankPosList.Add(akbankPos);

            var albarakaPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.Albaraka.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.Albaraka),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = albarakaPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(albarakaPos);
            var anadoluPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.AnadoluBank.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.AnadoluBank),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = anadoluPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(anadoluPos);
            var denizPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.DenizBank.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.DenizBank),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = denizPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(denizPos);
            var garantiPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.Garanti.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.Garanti),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = garantiPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(garantiPos);
            var halkPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.HalkBank.GetDisplayName(),
                SystemName = "HalkBank",
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = halkPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(halkPos);
            var hsbcPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.HSBC.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.HSBC),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = hsbcPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(hsbcPos);
            var ingPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.IngBank.GetDisplayName(),
                SystemName =Enum.GetName(typeof(BankNames),BankNames.IngBank),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = ingPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(ingPos);
            var iparaPos = new OmniBankPos {
                BankTypeId = (int)BankType.Ortak,
                Name = BankNames.IPara.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.IPara),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = iparaPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(iparaPos);
            var isbankPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.IsBankasi.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.IsBankasi),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = isbankasiPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(isbankPos);
            var kuveytPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.KuveytTurk.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.KuveytTurk),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = kuveytPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(kuveytPos);
            var paytrPos = new OmniBankPos {
                BankTypeId = (int)BankType.Ortak,
                Name = BankNames.PayTr.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.PayTr),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = paytrPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(paytrPos);
            var finansPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.FinansBank.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.FinansBank),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = finansPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(finansPos);
            var sekerPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.SekerBank.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.SekerBank),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = sekerbankPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(sekerPos);
            var tebPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.TurkEkonomiBankasi.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.TurkEkonomiBankasi),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = turkiyeEkonomiPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(tebPos);
            var turkiyeFinansPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.TurkiyeFinans.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.TurkiyeFinans),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = turkiyeFinansPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(turkiyeFinansPos);
            var vakifPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.VakifBank.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.VakifBank),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = vakifPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(vakifPos);
            var yapiKrediPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.Yapikredi.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.Yapikredi),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = yapikrediPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(yapiKrediPos);
            var ziraatPos = new OmniBankPos {
                BankTypeId = (int)BankType.Bank,
                Name = BankNames.ZiraatBankasi.GetDisplayName(),
                SystemName = Enum.GetName(typeof(BankNames),BankNames.ZiraatBankasi),
                IsActive = false,
                Primary = false,
                PrimaryBank = false,
                PictureId = ziraatPicture.Id,
                CreatedOnUtc = DateTime.UtcNow,
                UpdatedOnUtc = DateTime.UtcNow
            };
            bankPosList.Add(ziraatPos);
            await _bankPosRepository.InsertManyAsync(bankPosList); 
         
        }

        private async Task UninstallBank()
        {
            await _bankBinRepository.Collection.Database.DropCollectionAsync("OmniBankBin");
            await _bankPosRepository.Collection.Database.DropCollectionAsync("OmniBankPos");
            await _bankOrderRepository.Collection.Database.DropCollectionAsync("OmniBankOrder");
            await _bankInstallmentCategoryRepository.Collection.Database.DropCollectionAsync("OmniBankInstallmentCategory");
        }

        public async Task InstallData()
        {
            await InstallBankBin();
        }

        public async Task UninstallData()
        {
            await UninstallBank();
        }
    }
}
