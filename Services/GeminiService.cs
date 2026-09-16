using System;
using System.Net.Http;
using System.Text;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Hotel_MVC.Models;

namespace Hotel_MVC.Services
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        // Define primary and fallback models to rotate through if rate-limited (429)
        private readonly string[] _modelChain = new[]
        {
            "gemini-3.6-flash",
            "gemini-3.1-flash-lite",
            "gemini-2.5-flash"
        };

        public GeminiService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["GeminiApi:ApiKey"]?.Trim() ?? string.Empty;
        }

        // --- FEATURE 1: Concierge Chat ---
        public async Task<string> GetConciergeResponseAsync(string userMessage, string contextFacts)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                return "API key missing. Configure 'GeminiApi:ApiKey' in appsettings.Development.json.";
            }

            var systemPrompt = "You are a helpful hotel concierge assistant at EMA Hotel & Suites. " +
                               "Answer guest queries using ONLY the provided hotel database facts below. " +
                               "If information is missing, instruct the guest to contact front desk staff.\n\n" +
                               $"HOTEL DATABASE FACTS:\n{contextFacts}\n\n" +
                               $"GUEST QUESTION: {userMessage}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = systemPrompt }
                        }
                    }
                }
            };

            return await ExecuteWithModelFallbackAsync(payload);
        }

        // --- FEATURE 2: Book by Sentence (Natural Language Search Parser) ---
        public async Task<SmartSearchParameters?> ParseBookingSentenceAsync(string sentence)
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) return null;

            var currentDate = DateTime.Today.ToString("yyyy-MM-dd");

            var prompt = $@"
Extract room search filters from this guest request: '{sentence}'.
Today's Date: {currentDate}.

Return ONLY a raw JSON object matching this exact schema (no markdown formatting):{{
  ""checkIn"": ""YYYY-MM-DD or null"",
  ""checkOut"": ""YYYY-MM-DD or null"",
  ""maxPricePerNight"": 0.0 or null,
  ""keywords"": ""extracted room preferences like sea view, suite, balcony, etc.""}}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            try
            {
                var aiText = await ExecuteWithModelFallbackAsync(payload);
                if (string.IsNullOrWhiteSpace(aiText) || aiText.StartsWith("Error")) return null;

                var cleanJson = CleanJsonResponse(aiText);
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                return JsonSerializer.Deserialize<SmartSearchParameters>(cleanJson, options);
            }
            catch
            {
                return null;
            }
        }

        // --- Failover Execution Helper ---
        // Loops through the model chain if it encounters a 429 Too Many Requests status code
        private async Task<string> ExecuteWithModelFallbackAsync(object payload)
        {
            string lastError = "No request executed.";

            foreach (var modelName in _modelChain)
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKey}";

                try
                {
                    _httpClient.DefaultRequestHeaders.Authorization = null;

                    var jsonContent = JsonSerializer.Serialize(payload);
                    var requestContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                    var response = await _httpClient.PostAsync(url, requestContent);

                    if (response.IsSuccessStatusCode)
                    {
                        var rawJson = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(rawJson);

                        return doc.RootElement
                            .GetProperty("candidates")[0]
                            .GetProperty("content")
                            .GetProperty("parts")[0]
                            .GetProperty("text")
                            .GetString() ?? "No answer text returned.";
                    }

                    var errorBody = await response.Content.ReadAsStringAsync();
                    lastError = $"Error ({(int)response.StatusCode}): {response.ReasonPhrase}. Details: {errorBody}";

                    // If it's a 429 rate limit error, log/continue to the next fallback model in line
                    if ((int)response.StatusCode == 429)
                    {
                        await Task.Delay(500); // Brief buffer before hitting next fallback model
                        continue;
                    }

                    // For other non-429 errors (like bad requests), return immediately
                    return lastError;
                }
                catch (Exception ex)
                {
                    lastError = $"Error connecting to AI service ({modelName}): {ex.Message}";
                }
            }

            return lastError;
        }

        // Helper to strip markdown block ticks if model includes them
        private static string CleanJsonResponse(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText)) return string.Empty;
            var cleaned = rawText.Trim();
            if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(7);
            if (cleaned.StartsWith("```"))
                cleaned = cleaned.Substring(3);
            if (cleaned.EndsWith("```"))
                cleaned = cleaned.Substring(0, cleaned.Length - 3);
            return cleaned.Trim();
        }
    }
}