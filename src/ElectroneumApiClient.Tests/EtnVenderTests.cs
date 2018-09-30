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

        /// <summary>
        /// Verify currency conversion is accurate both ways
        /// </summary>
        /// <returns></returns>
        [Fact]
        public async Task ConvertAUCurrencyToEtnAndBack()
        {
            var currency = "aud";
            var vender = _mock.CreateVendor();
            var amt = _mock.GetAmt();

            var etn = await vender.CurrencyToEtnAsync(amt, currency);
            var local = await vender.EtnToCurrencyAsync(etn, currency);

            _out.WriteLine($"etn:{etn} local:{local}");
            Assert.Equal(local,amt);
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
