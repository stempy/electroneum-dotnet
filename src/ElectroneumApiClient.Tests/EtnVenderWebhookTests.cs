using System.Threading.Tasks;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace ElectroneumApiClient.Tests
{
    public class EtnVenderWebhookTests : TestBase
    {
        public EtnVenderWebhookTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task TestGetPayload()
        {
            var webHook = new EtnWebhookValidator(_mock.EtnVendorApiKey,_mock.EtnVendorApiSecretKey);
            var sample = await _mock.CreateFreshPayLoadAndSigAsync();
            var userAgent = "Electroneum/0.1.0 (+https://electroneum.com/instant-payments)";
            var payLoad= await webHook.ValidateEtnWebHookPayloadAsync(userAgent, sample.json,sample.signature);
            _out.WriteLine(JsonConvert.SerializeObject(payLoad,Formatting.Indented));
        }
    }
}