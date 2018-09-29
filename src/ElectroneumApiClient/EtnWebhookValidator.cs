using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ElectroneumApiClient
{
    public class EtnWebhookValidator
    {
        private readonly EtnVendor _vendor;

        public EtnWebhookValidator(string apiKey,string apiSecret)
        {
            _vendor = new EtnVendor(apiKey,apiSecret);
        }

        /// <summary>
        /// ParseEtnWebHookPayload
        /// </summary>
        /// <param name="userAgent">comes from request user_agent</param>
        /// <param name="payload">json payload</param>
        /// <param name="signature">comes from request header ETN_SIGNATURE</param>
        /// <returns></returns>
        public async Task<EtnPayload> ValidateEtnWebHookPayloadAsync(string userAgent, string payload, string signature)
        {
            if (!userAgent.StartsWith("Electroneum"))
                throw new VendorException("invalid useragent on webhook");


            var isValid = _vendor.VerifySignature(payload, signature);
            if (!isValid)
            {
                throw new VendorException("invalid signature");
            }
            var payloadObj = JsonConvert.DeserializeObject<EtnPayload>(payload);
            return await Task.FromResult(payloadObj);
        }

        public async Task<EtnPayload> ValidateEtnWebHookPayloadAsync(string userAgent, EtnPayload payload, string signature)
        {
            if (!userAgent.StartsWith("Electroneum"))
                throw new VendorException("invalid useragent on webhook");

            var isValid = _vendor.VerifySignature(payload, signature);
            if (!isValid)
            {
                throw new VendorException("invalid signature");
            }

            return await Task.FromResult(payload);
        }
    }
}