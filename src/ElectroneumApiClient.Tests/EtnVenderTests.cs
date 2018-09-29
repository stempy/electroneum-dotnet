using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ElectroneumApiClient.Tests
{
    public class EtnVenderTests : TestBase
    {
        public EtnVenderTests(ITestOutputHelper output):base(output)
        {
        }

        [Fact]
        public void GeneratePaymentId()
        {
            var vender =  _mock.CreateVendor();
            var paymentId = vender.GeneratePaymentId();
            _out.WriteLine(paymentId);
        }

        [Fact]
        public async Task ConvertAUCurrencyToEtn()
        {
            var currency = "aud";
            var vender = _mock.CreateVendor();
            var i = await vender.CurrencyToEtnAsync(_mock.GetAmt(), currency);
            _out.WriteLine(i.ToString());
        }

        [Fact]
        public async Task GenerateQrCode()
        {
            var vendor = _mock.CreateVendor();
            var qr = await vendor.GetQrAsync(_mock.GetAmt(), "aud", _mock.GetOutlet(), "A232432BCC");
            _out.WriteLine(qr);
        }

        [Fact]
        public async Task CreatePayloadAndSignatureAndVerifySignature()
        {
            var mock = await _mock.CreateFreshPayLoadAndSigAsync();
            var result = _mock.CreateVendor().VerifySignature(mock.json, mock.signature);
            Assert.True(result);
        }
    }
}
