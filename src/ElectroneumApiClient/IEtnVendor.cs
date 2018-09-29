using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElectroneumApiClient
{
    public interface IEtnVendor
    {
        /// <summary>
        /// Your vendor API key.
        /// </summary>
        string ApiKey { get; }

        /// <summary>
        /// Your vendor API secret.
        /// </summary>
        string ApiSecret { get; }

        /// <summary>
        /// The amount to charge in ETN.
        /// </summary>
        decimal Etn { get; }

        /// <summary>
        /// The outlet id.
        /// </summary>
        string Outlet { get; }

        /// <summary>
        /// The payment id.
        /// </summary>
        string PaymentId { get; set; }

        /// <summary>
        /// Generate a cryptographic random payment id.
        /// </summary>
        /// <returns></returns>
        string GeneratePaymentId();

        /// <summary>
        /// List of currencies
        /// </summary>
        IEnumerable<string> Currencies { get; }

        /// <summary>
        /// Convert local currency to ETN
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <returns></returns>
        Task<decimal> CurrencyToEtnAsync(decimal amount, string currency, bool forceRefresh = false);

        /// <summary>
        /// Return a QR image Url for given data (grouping the above functions into one
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="currency"></param>
        /// <param name="outlet"></param>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        Task<string> GetQrAsync(decimal amount, string currency, string outlet, string paymentId = null);

        Task<string> GenerateSignature(EtnPayload payload);

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
        Task<string> GenerateSignature(string customer, decimal amount, string currency, string reference, string paymentId = null);

        /// <summary>
        /// Generate a signature for a payload.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns>
        string GenerateSignature(string payload);

        /// <summary>
        /// Validate a webhook signature based on a payload.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        bool VerifySignature(string payload, string signature);

        /// <summary>
        /// Poll the API to check a vendor payment. The signature will be generated if not supplied.
        /// </summary>
        /// <param name="payload"></param>
        /// <param name="signature"></param>
        /// <returns></returns>
        Task<PollResult> CheckPaymentPoll(string payload, string signature = null);
    }
}