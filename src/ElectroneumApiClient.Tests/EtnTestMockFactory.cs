using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ElectroneumApiClient.Tests
{
    public class EtnTestMockFactory
    {
        private static string _etnVendorApiKey;
        private static string _etnVendorApiSecretKey;
        private static string _outlet;

        public string EtnVendorApiKey => _etnVendorApiKey;
        public string EtnVendorApiSecretKey => _etnVendorApiSecretKey;

        public EtnTestMockFactory()
        {
            _etnVendorApiKey = Environment.GetEnvironmentVariable("EtnVendorApiKey");
            _etnVendorApiSecretKey = Environment.GetEnvironmentVariable("EtnVendorApiSecretKey");
            _outlet = Environment.GetEnvironmentVariable("EtnVendorOutlet");

            if (string.IsNullOrEmpty(_etnVendorApiKey))
                throw new ArgumentNullException(nameof(_etnVendorApiKey) + $" set Environment variable: EtnVendorApiKey with your vendorapikey");

            if (string.IsNullOrEmpty(_etnVendorApiSecretKey))
                throw new ArgumentNullException(nameof(_etnVendorApiSecretKey)+$" set Environment variable: EtnVendorApiSecretKey with your api secret key");

            if (string.IsNullOrEmpty(_outlet))
                throw new ArgumentNullException(nameof(_outlet)+$" set Environment variable: EtnVendorOutlet with your outlet id");
        }

        public EtnVendor CreateVendor()
        {
            return new EtnVendor(EtnVendorApiKey, EtnVendorApiSecretKey);
        }

        public string GetOutlet()
        {
            return Environment.GetEnvironmentVariable("EtnVendorOutlet");
        }
        public decimal GetAmt()
        {
            return 20.5m;
        }

        public async Task<(string json, string signature)> CreateFreshPayLoadAndSigAsync()
        {
            var p = new EtnPayload()
            {
                PaymentId = Guid.NewGuid().ToString(),
                Amount = 20,
                Customer = "testcustomer@customer.com",
                Key = EtnVendorApiKey,
                Ref = "internalref",
                TimeStamp = DateTime.Now
            };
            var sig =await CreateVendor().GenerateSignature(p);
            return (JsonConvert.SerializeObject(p), sig);
        }
    }
}