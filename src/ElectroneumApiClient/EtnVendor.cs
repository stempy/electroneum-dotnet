using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ElectroneumApiClient
{
    /// <summary>
    /// ETN Vendor lib based on ETN API Guide and PHP Core as below
    /// see: https://community.electroneum.com/t/using-the-etn-instant-payment-api/121
    /// see: https://github.com/electroneum/vendor-php/blob/beta/src/Vendor.php
    /// </summary>
    public class EtnVendor : IEtnVendor
    {
        public const int PayLoadTimeVarianceMinutes = -5;

        /// <summary>
        /// Version number of this vendor class.
        /// </summary>
        public const string API_VERSION = "0.1.0";

        /// <summary>
        /// Url to poll for payment confirmation.
        /// </summary>
        public const string URL_POLL = "https://poll.electroneum.com/vendor/check-payment";


        /// <summary>
        /// Url for the exchange rate JSON.
        /// </summary>
        public const string URL_SUPPLY = "https://supply.electroneum.com/app-value-v2.json";

        /// <summary>
        /// Url to load a QR code.
        /// </summary>
        public const string URL_QR = "https://chart.googleapis.com/chart?cht=qr&chs={0}x{1}&chld=L|0&chl={2}";

        /// <summary>
        /// Currencies accepted for converting to ETN.
        /// </summary>
        protected string[] currencies = new[] { "AUD", "BRL", "BTC", "CAD", "CDF", "CHF", "CLP", "CNY", "CZK", "DKK", "EUR", "GBP", "HKD", "HUF", "IDR", "ILS", "INR", "JPY", "KRW", "MXN", "MYR", "NOK", "NZD", "PHP", "PKR", "PLN", "RUB", "SEK", "SGD", "THB", "TRY", "TWD", "USD", "ZAR" };

        public IEnumerable<string> Currencies => currencies;

      

        /// <summary>
        /// Your vendor API key.
        /// </summary>
        public string ApiKey { get; private set; }

        /// <summary>
        /// Your vendor API secret.
        /// </summary>
        public string ApiSecret { get; private set; }

        /// <summary>
        /// The amount to charge in ETN.
        /// </summary>
        public decimal Etn { get; private set; }

        /// <summary>
        /// The outlet id.
        /// </summary>
        public string Outlet { get; private set; }

        /// <summary>
        /// The payment id.
        /// </summary>
        public string PaymentId { get; set; }

        /// <summary>
        /// cached currency list
        /// </summary>
        private static JObject _currencyList;

        /// <summary>
        /// So we can determine when last poll was done
        /// as electroneum only want 1 poll per second
        /// </summary>
        private static DateTime _lastPoll = DateTime.Now.AddHours(-1);


        public int QrImageWidth { get; set; } = 300;  // 600 fails with 400
        public int QrImageHeight { get; set; } = 300; // 600 fails with 400

        #region [Ctors..]
        public EtnVendor(string apiKey, string apiSecret)
        {
            ApiSecret = apiSecret;
            ApiKey = apiKey;
        }

        public EtnVendor(EtnVendorOptions options)
        {
            ApiKey = options.EtnVendorApiKey;
            ApiSecret = options.EtnVendorApiSecretKey;
        }
        #endregion

        /// <summary>
        /// Generate a cryptographic random payment id.
        /// </summary>
        /// <returns></returns>
        public string GeneratePaymentId()
        {
            try
            {
                return GetRandomHexNumber(10);//   bin2Hex(random_bytes(5));
            }
            catch (Exception ex)
            {
                // CryptGenRandom (Windows), getrandom(2) (Linux) or /dev/urandom (others) was unavailable to generate random bytes.
                throw new VendorException(ex.Message);
            }
        }

        #region [Get Currency Rate]

        private async Task<decimal> GetCurrencyRateAsync(string currency, bool forceRefresh = false)
        {
            // clear currency list 
            if (forceRefresh)
                _currencyList = null;

            // Check the currency is accepted.
            if (!currencies.Contains(currency.ToUpper()))
            {
                throw new VendorException("Unknown currency");
            }

            if (_currencyList == null)
            {
                // Get the JSON conversion data.
                using (var client = new HttpClient())
                {
                    try
                    {
                        var res = await client.GetAsync(new Uri(URL_SUPPLY));
                        if (!res.IsSuccessStatusCode)
                        {
                            throw new VendorException($"[{res.StatusCode}] could not load currency conversion JSON");
                        }

                        var value = await res.Content.ReadAsStringAsync();
                        _currencyList = JObject.Parse(value);
                    }
                    catch (Exception)
                    {
                        throw new VendorException("could not load currency conversion JSON");
                    }
                }
            }
            var currencyRateField = $"price_{currency.ToLower()}";
            if (!_currencyList.ContainsKey(currencyRateField))
                throw new VendorException($"could not get rate for currency {currency}");

            // Get the conversion rate.
            var rate = _currencyList[currencyRateField];
            var rateDec = Convert.ToDecimal(rate);
            if (rateDec <= 0)
            {
                throw new VendorException("Currency conversion rate not valid");
            }

            return rateDec;
        }

        /// <summary>
        /// Convert ETN to local currency
        /// </summary>
        /// <param name="etnAmount"></param>
        /// <param name="currency"></param>
        /// <param name="forceRefresh"></param>
        /// <returns></returns>
        public async Task<decimal> EtnToCurrencyAsync(decimal etnAmount, string currency, bool forceRefresh = false)
        {
            var rate = await GetCurrencyRateAsync(currency, forceRefresh);
            var localAmt = etnAmount * rate;
            localAmt = Convert.ToDecimal(string.Format("{0:#.00}", Convert.ToDecimal(localAmt.ToString())));
            return localAmt;
        }

        /// <summary>
        /// Convert local currency to ETN
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        public async Task<decimal> CurrencyToEtnAsync(decimal amount, string currency, bool forceRefresh=false)
        {
            var rateDec = await GetCurrencyRateAsync(currency);
            Etn = amount / rateDec;
            Etn = Convert.ToDecimal(string.Format("{0:#.00}", Convert.ToDecimal(Etn.ToString())));
            return Etn;
        }

        #endregion [Get Currency Rate]

        #region [Qr code]
        /// <summary>
        /// Return a QR image Url for given data (grouping the above functions into one
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <param name="outlet"></param>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        public async Task<string> GetQrAsync(decimal amount, string currency, string outlet, string paymentId = null)
        {
            return await GetQrAsync(amount, currency, outlet, paymentId, QrImageWidth, QrImageHeight); //   GetQrUrl(qrCode);
        }

        /// <summary>
        /// Get qr image url and set width/height
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <param name="outlet"></param>
        /// <param name="paymentId"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task<string> GetQrAsync(decimal amount, string currency, string outlet, string paymentId, int width, int height)
        {
            Etn = await CurrencyToEtnAsync(amount, currency);
            // Build the QR Code string.
            var qrCode = GetQrCode(Etn, outlet, paymentId);
            return GetQrUrl(qrCode, width, height);
        }

        /// <summary>
        /// Generate a QR code for a vendor transaction.
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="outlet"></param>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        private string GetQrCode(decimal amount, string outlet, string paymentId = null)
        {
            // Generate/validate the paymentId.
            if (paymentId == null)
            {
                paymentId = GeneratePaymentId();
            }
            else if (paymentId.Length != 10 || !ctype_xdigit(paymentId)) //|| num)
            {
                throw new VendorException("Qr code payment id is not valid");
            }

            // Validate the amount
            if (amount == 0)
            {
                throw new VendorException("Qr code amount is not valid");
            }

            // Validate the outlet.
            if (string.IsNullOrEmpty(outlet) || !ctype_xdigit(outlet))
            {
                throw new VendorException("Qr code outlet is not valid");
            }

            PaymentId = paymentId;
            Outlet = outlet;
            Etn = amount;
            var qrCode = $"etn-it-{Outlet}/{PaymentId}/{Etn}";
            // Return the QR code string.
            return qrCode;
        }

        /// <summary>
        /// Return a QR image Url for a QR code string.
        /// </summary>
        /// <param name="qrCode"></param>
        /// <returns></returns>
        private string GetQrUrl(string qrCode)
        {
            return string.Format(URL_QR,QrImageWidth,QrImageHeight,qrCode);// sprintf(Vendor::URL_QR, urlencode($qrCode));
        }

        /// <summary>
        /// Get qr url with specific width/height dimensions
        /// </summary>
        /// <param name="qrCode"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        private string GetQrUrl(string qrCode, int width, int height)
        {
            return string.Format(URL_QR, width, height, qrCode);// sprintf(Vendor::URL_QR, urlencode($qrCode));
        }

        #endregion

        #region [Generate Signature]

        public async Task<string> GenerateSignature(EtnPayload payload)
        {
            if (string.IsNullOrEmpty(ApiSecret))
            {
                throw new VendorException("No vendor API secret set");
            }
            var json = JsonConvert.SerializeObject(payload);
            var sig = HashHmac(json, ApiSecret);
            return await Task.FromResult(sig);
        }

        /// <summary>
        /// Generate signature to validate with verify signature
        /// This is not used for payment, better for testing purposes
        /// </summary>
        /// <param name="customer"></param>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <param name="reference"></param>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        public async Task<string> GenerateSignature(string customer, decimal amount, string currency, string reference, string paymentId = null)
        {
            if (string.IsNullOrEmpty(ApiSecret))
            {
                throw new VendorException("No vendor API secret set");
            }
            var payload = new EtnPayload()
            {
                PaymentId = paymentId ?? GeneratePaymentId(),
                Customer = customer,
                Amount = await CurrencyToEtnAsync(amount,currency),
                Key = ApiKey,
                Ref = reference,
                TimeStamp = DateTime.Now
            };
            return await GenerateSignature(payload);
        }

        /// <summary>
        /// Generate a signature for a payload.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        public string GenerateSignature(string payload)
        {
            // Check we have a vendor API secret.
            if (string.IsNullOrEmpty(ApiSecret))
            {
                throw new VendorException("No vendor API secret set");
            }
            // Check we have a valid payload.
            var payloadObj = JObject.Parse(payload);//  payloadArray = json_decode($payload, true);
            if (payloadObj == null)
            {
                throw new VendorException("Generate signature `payload` is not valid");
            }
            // Validate the signature.
            return HashHmac(payload, ApiSecret);
        }
        #endregion

        public bool VerifySignature(EtnPayload payload, string signature)
        {
            return VerifySignature(JsonConvert.SerializeObject(payload),signature);
        }

        /// <summary>
        /// Validate a webhook signature based on a payload.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public bool VerifySignature(string payload, string signature)
        {
            // Check we have a vendor API key.
            if (string.IsNullOrEmpty(ApiKey))
            {
                throw new VendorException("No vendor API key set");
            }
            // Check we have a vendor API secret.
            if (string.IsNullOrEmpty(ApiSecret))
            {
                throw new VendorException("No vendor API secret set");
            }
            // Check we have a valid payload.
            JObject payloadObj;
            try
            {
                payloadObj = JObject.Parse(payload);
            }
            catch (Exception ex)
            {
                throw new VendorException("Verify signature `payload` is not valid", ex);
            }

            // Check we have a valid signature.
            if (string.IsNullOrEmpty(signature) || signature.Length != 64 || !ctype_xdigit(signature))
            {
                throw new VendorException("Verify signature `signature` is not valid");
            }
            // Validate the signature.
            if ((string)payloadObj["key"] != ApiKey)
            {
                // This isn't the payload you are looking for.
                return false;
            }
            else if (signature != HashHmac(payload, ApiSecret))
            {
                // Invalid webhook signature.
                return false;
            }
            else if (  (DateTime)payloadObj["timestamp"] < DateTime.Now.AddMinutes(PayLoadTimeVarianceMinutes))
            {
                // Expired webhook
                return false;
            }
            else
            {
                // Valid webhook signature.
                return true;
            }
        }

        /// <summary>
        /// Poll the API to check a vendor payment. The signature will be generated if not supplied.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        public async Task<PollResult> CheckPaymentPoll(string payload, string signature = null)
        {
            // Generate the signature, if we need to.
            if (string.IsNullOrEmpty(signature))
            {
                signature = GenerateSignature(payload);
            }
            // Check the signature length.
            if (signature.Length != 64)
            {
                throw new VendorException("Check payment signature length invalid");
            }
            
            PollResult result = null;
            using (var c= new HttpClient())
            {
                var reqMsg = new HttpRequestMessage(HttpMethod.Post, URL_POLL);
                reqMsg.Headers.Add("Content-Type","application/json");
                reqMsg.Headers.Add("ETN-SIGNATURE",signature);
                reqMsg.Content = new StringContent(payload);

                // TODO: deliberate pause to limit poll requests
                //ensure we dont poll more than once a second as recommended by api guide at : https://community.electroneum.com/t/using-the-etn-instant-payment-api/121
                var totalMs = (DateTime.Now - _lastPoll).TotalMilliseconds;
                if (totalMs < 1000)
                {
                    await Task.Delay(1000);
                }
                var resp = await c.SendAsync(reqMsg);
                var content = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                {
                    throw new VendorException($"POLL Failed: [{resp.StatusCode}] {content}");
                }
                result = JsonConvert.DeserializeObject<PollResult>(content);
                _lastPoll = DateTime.Now;
            }
            return result;
        }

        /// <summary>
        /// Used to generate and verify signature
        /// </summary>
        /// <param name="message"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        private static string HashHmac(string message, string secret)
        {
            Encoding encoding = Encoding.UTF8;
            using (HMACSHA256 hmac = new HMACSHA256(encoding.GetBytes(secret)))
            {
                var msg = encoding.GetBytes(message);
                var hash = hmac.ComputeHash(msg);
                return BitConverter.ToString(hash).ToLower().Replace("-", string.Empty);
            }
        }


        bool OnlyHexInString(string test)
        {
            // For C-style hex notation (0xFF) you can use @"\A\b(0[xX])?[0-9a-fA-F]+\b\Z"
            return System.Text.RegularExpressions.Regex.IsMatch(test, @"\A\b[0-9a-fA-F]+\b\Z");
        }

        /// <summary>
        /// Helper to check if hex only in string
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private bool ctype_xdigit(string item)
        {
            return OnlyHexInString(item);
        }


        string random_bytes(int len)
        {
            var rng = new RNGCryptoServiceProvider();
            var key = new byte[len];
            rng.GetBytes(key);

            for (var i = 0; i < key.Length; ++i)
            {
                int keyByte = key[i] & 0xFE;
                var parity = 0;
                for (var b = keyByte; b != 0; b >>= 1) parity ^= b & 1;
                key[i] = (byte)(keyByte | (parity == 0 ? 1 : 0));
            }

            return Encoding.Default.GetString(key);
        }

        static Random random = new Random();
        public static string GetRandomHexNumber(int digits)
        {
            byte[] buffer = new byte[digits / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }

        #region [Bin2hex]

        string bin2Hex(string strBin)

        {
            int decNumber = bin2Dec(strBin);
            return dec2Hex(decNumber);
        }
        int bin2Dec(string strBin)
        {
            return Convert.ToInt16(strBin, 2);
        }
        string dec2Hex(int val)
        {
            return val.ToString("X");
        }
        #endregion
    }
}