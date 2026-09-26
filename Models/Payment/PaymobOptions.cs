namespace Hotel_MVC.Models.Payment
{
    public class PaymobOptions
    {
        public string SecretKey { get; set; } = string.Empty;

        public string HmacSecret { get; set; } = string.Empty;

        public int CardIntegrationId { get; set; }

        public string BaseUrl { get; set; } = "https://accept.paymob.com";
    }
}