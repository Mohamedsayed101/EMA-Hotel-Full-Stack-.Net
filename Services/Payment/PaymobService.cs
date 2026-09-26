using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Hotel_MVC.Models.Payment;
using Microsoft.Extensions.Options;

namespace Hotel_MVC.Services.Payment
{
    public class PaymobService : IPaymobService
    {
        private readonly HttpClient _httpClient;
        private readonly PaymobOptions _options;

        public PaymobService(
            HttpClient httpClient,
            IOptions<PaymobOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<string> CreateCardSavingCheckoutAsync(
            string userId,
            string email,
            string phoneNumber,
            string firstName,
            string lastName)
        {
            var request = new
            {
                amount = 100,
                currency = "EGP",

                payment_methods = new[]
                {
                    _options.CardIntegrationId
                },

                items = new[]
                {
                    new
                    {
                        name = "EMA Hotel Card Verification",
                        amount = 100,
                        description = "Card verification for saving a card",
                        quantity = 1
                    }
                },

                billing_data = new
                {
                    first_name = string.IsNullOrWhiteSpace(firstName)
                        ? "Guest"
                        : firstName,

                    last_name = string.IsNullOrWhiteSpace(lastName)
                        ? "User"
                        : lastName,

                    email = email,

                    phone_number = string.IsNullOrWhiteSpace(phoneNumber)
                        ? "01000000000"
                        : phoneNumber,

                    apartment = "NA",
                    floor = "NA",
                    street = "NA",
                    building = "NA",
                    postal_code = "NA",
                    city = "Fayoum",
                    state = "Fayoum",
                    country = "EG",
                    shipping_method = "NA"
                },

                extras = new
                {
                    user_id = userId,
                    purpose = "save_card"
                },

                special_reference =
                    $"SAVE-CARD-{userId}-{Guid.NewGuid():N}",

                notification_url =
                    "https://YOUR-DOMAIN.com/Account/PaymobCallback",

                redirection_url =
                    "https://YOUR-DOMAIN.com/Account/PaymobReturn"
            };

            var json = JsonSerializer.Serialize(request);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json");

            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                $"{_options.BaseUrl}/v1/intention/");

            requestMessage.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Token",
                    _options.SecretKey);

            requestMessage.Content = content;

            using var response =
                await _httpClient.SendAsync(requestMessage);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Paymob Create Intention failed. " +
                    $"Status: {(int)response.StatusCode}. " +
                    $"Response: {responseBody}");
            }

            using var document =
                JsonDocument.Parse(responseBody);

            if (!document.RootElement.TryGetProperty(
                    "client_secret",
                    out var clientSecretElement))
            {
                throw new InvalidOperationException(
                    "Paymob response does not contain client_secret.");
            }

            var clientSecret =
                clientSecretElement.GetString();

            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                throw new InvalidOperationException(
                    "Paymob returned an empty client_secret.");
            }

            return clientSecret;
        }
    }
}