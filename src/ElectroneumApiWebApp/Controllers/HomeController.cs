using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ElectroneumApiClient;
using ElectroneumApiWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElectroneumApiWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IEtnVendor _etnVendor;
        private readonly EtnOutletOptions _etnOptions;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IEtnVendor etnVendor, ILogger<HomeController> logger, IOptions<EtnOutletOptions> etnOptions)
        {
            _etnVendor = etnVendor;
            _etnOptions = etnOptions.Value;
            _logger = logger;
        }

        public IActionResult GenerateQr()
        {
            ViewBag.Currencies = _etnVendor.Currencies;
            return View(new EtnPayloadViewModel(){Amount = 5.0m,Currency = "aud"});
        }

        [HttpPost]
        public async Task<IActionResult> GenerateQr(EtnPayloadViewModel model)
        {
            ViewBag.Currencies = _etnVendor.Currencies;
            model.AmountEtn = await _etnVendor.CurrencyToEtnAsync(model.Amount, model.Currency);

            var width = 250;  // optional
            var height = 250; // optional
            var samplePaymentId = "a23456789b"; // optional

            var result = await _etnVendor.GetQrAsync(model.Amount, model.Currency, _etnOptions.EtnOutlet, samplePaymentId);
            //var result = await _etnVendor.GetQrAsync(model.Amount, model.Currency, _etnOptions.EtnOutlet, samplePaymentId,width,height);
            model.QrUrl = result;
            _logger.LogInformation("Generated QR for :{0}",model);
            ViewBag.QrCodeString = result.Substring(result.IndexOf("chl=", StringComparison.Ordinal) + 4);
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
