using System;
using System.Linq;
using System.Threading.Tasks;
using ElectroneumApiClient;
using ElectroneumApiWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace ElectroneumApiWebApp.Controllers
{
    [Route("etnwebhook")]
    public class EtnPaymentWebHookController : Controller
    {
        private readonly EtnWebhookValidator _webhookValidator;
        private readonly ILogger<EtnPaymentWebHookController> _logger;
        private readonly EtnOutletOptions _outletOptions;

        private readonly IEtnVendor _vendor;// this is not needed for validation, only to generate signature for testing

        public EtnPaymentWebHookController(EtnWebhookValidator webhookValidator, 
                                                IEtnVendor vendor,    // this is not needed for validation, only to generate signature for testing
                                                ILogger<EtnPaymentWebHookController> logger, 
                                                IOptions<EtnOutletOptions> outletOptions)
        {
            _webhookValidator = webhookValidator;
            _vendor = vendor;
            _logger = logger;
            _outletOptions = outletOptions.Value;
        }

        private static string _testingSig;

        private string GetEtnXTestingSig()
        {
            if (_testingSig == null)
            {
                _testingSig = Guid.NewGuid().ToString();
            }

            return _testingSig;
        }

        /// <summary>
        /// Public url for processing incoming webhook requests
        /// must return fast
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        [HttpPost("payment")]
        public async Task<IActionResult> PostEtnWebHook(EtnPayload payload)
        {
            bool isTesting = Request.Headers.ContainsKey("X-ETN-TESTING");
            if (isTesting)
            {
                if ((string) Request.Headers["X-ETN-TESTING"] != GetEtnXTestingSig())
                {
                    throw new Exception($"Invalid testing signature");
                }
            }

            try
            {
                var userAgent = GetHeaderOrThrow("User-Agent");
                var signature = GetHeaderOrThrow(EtnConstants.EtnSignatureRequestHeader);
                _logger.LogTrace("Validating payload {0} ",payload);

                if (isTesting)
                {
                    userAgent = "Electroneum";
                }

                var payloadResult = await _webhookValidator.ValidateEtnWebHookPayloadAsync(userAgent, payload, signature);
                if (payloadResult != null)
                {
                    // success result
                    // log and store in database table somewhere
                    _logger.LogInformation($"[Test:{isTesting}] Payment Received: timestamp:{payloadResult.TimeStamp} id:{payloadResult.PaymentId} amt:{payloadResult.Amount} customer:{payloadResult.Customer} ref:{payloadResult.Ref} ");

                    // handle fulfulment process
                }
            }
            catch (VendorException vex)
            {
                ModelState.AddModelError("",vex.Message);
                return new BadRequestObjectResult(ModelState);
            }

            return new OkResult();
        }

        /// <summary>
        /// Test webhook is working and validating
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var etnPayload = new EtnPayload()
            {
                Amount = 1,
                Customer = "testcustomer@someone.com"                
            };

            return View(etnPayload);
        }

        [HttpPost]
        public async Task<IActionResult> Index(EtnPayload model)
        {
            model.Key = _outletOptions.EtnVendorApiKey;
            model.TimeStamp = DateTime.Now;

            var sig = await _vendor.GenerateSignature(model);
            Request.Headers.Add(EtnConstants.EtnSignatureRequestHeader,new StringValues(sig));
            Request.Headers.Add("X-ETN-TESTING", GetEtnXTestingSig());
            //Request.Headers.FirstOrDefault(x=>x.Key=="User-Agent").Value = new StringValues("Electroneum/0.1.0 (+https://electroneum.com/instant-payments)");


            return await PostEtnWebHook(model);
        }


        private string GetHeaderOrThrow(string header)
        {
            if (Request.Headers.ContainsKey(header))
                return Request.Headers.FirstOrDefault(x => x.Key == header).Value;
            throw new VendorException($"Missing header-{header}");
        }


    }
}