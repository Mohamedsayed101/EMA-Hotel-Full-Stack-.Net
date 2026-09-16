using System.Threading.Tasks;
using Hotel_MVC.Models;

namespace Hotel_MVC.Services
{
    public interface IGeminiService
    {
        Task<string> GetConciergeResponseAsync(string userMessage, string contextFacts);
        Task<SmartSearchParameters?> ParseBookingSentenceAsync(string sentence);
    }
}