using System;
using Newtonsoft.Json;

namespace ElectroneumApiClient
{
    /// <summary>
    /// see: https://community.electroneum.com/t/using-the-etn-instant-payment-api/121
    /// find: The webhook payload is sent with a HTTP POST request and the body contains the following JSON
    /// Payload
    /// </summary>
    public class EtnPayload
    {
        [JsonProperty("payment_id")]
        public string PaymentId {get;set;}   // string(10) 7ce25b4dc0
        [JsonProperty("amount")]
        public decimal Amount { get; set; }      // 1234.56
        [JsonProperty("key")]
        public string Key { get; set; }          // string(32) vendor api key key_live_1234567890abcdefghijklm
        [JsonProperty("timestamp")]
        public DateTime TimeStamp { get; set; }  // 2018-06-11T11:02:31+01:00
        [JsonProperty("customer")]
        public string Customer { get;set; }      // customer@example.com
        [JsonProperty("ref")]
        public string Ref { get; set; }          // string(13) 1234567890abc
    }

    public enum PollStatus
    {
        PaymentNotSent =0,
        PaymentSent = 1
    }

    public class PollResult
    {
        [JsonProperty("status")]
        public PollStatus Status { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }
}