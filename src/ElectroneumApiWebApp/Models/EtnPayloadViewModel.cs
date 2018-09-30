namespace ElectroneumApiWebApp.Models
{
    public class EtnPayloadViewModel
    {
        public decimal Amount { get; set; }
        public decimal AmountEtn { get; set; }
        public string Currency { get; set; }
        public string Reference { get; set; }
        public string QrUrl { get; set; }

        public int ImageSize { get; set; }
    }
}