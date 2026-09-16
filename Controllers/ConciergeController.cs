using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Hotel_MVC.Data;
using Hotel_MVC.Services;
using System.Text.RegularExpressions;

namespace Hotel_MVC.Controllers
{
    public class ConciergeController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IGeminiService _geminiService;

        public ConciergeController(AppDbContext context, IGeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return Json(new { reply = "Please enter a valid message." });
            }

            var sb = new StringBuilder();

            // 1. Inject static hotel policies and overview facts sheet
            sb.AppendLine(HotelFacts.GetSheet());
            sb.AppendLine();

            // 2. Fetch live room inventory from the database
            var rooms = await _context.Rooms
                .Include(r => r.RoomType)
                .AsNoTracking()
                .ToListAsync();

            sb.AppendLine("--- LIVE ROOM INVENTORY & AVAILABILITY ---");
            foreach (var r in rooms)
            {
                var typeName = r.RoomType?.Name ?? "Standard Room";
                var price = r.RoomType?.PricePerNight ?? 0;
                var capacity = r.RoomType?.Capacity ?? 2;

                sb.AppendLine($"- Room Number: {r.RoomNumber}, Type: {typeName}, Price: ${price}/night, Capacity: {capacity}, Status: {r.Status}");
            }

            string contextFacts = sb.ToString();

            // 3. Call Gemini service with the guest question and the combined facts sheet
            string reply = await _geminiService.GetConciergeResponseAsync(request.Message, contextFacts);

            // 4. Automatically auto-link any room names mentioned in the AI response
            if (!reply.StartsWith("Error"))
            {
                try
                {
                    var allRoomTypes = await _context.RoomTypes
                        .Select(rt => new { rt.Id, rt.Name })
                        .ToListAsync();

                    foreach (var room in allRoomTypes)
                    {
                        if (!string.IsNullOrEmpty(room.Name) && reply.Contains(room.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            // Create a styled hyperlink matching your hotel's aesthetic
                            var roomLink = $"<a href='/Rooms/Details/{room.Id}' class='text-amber-800 font-semibold underline hover:text-amber-950' target='_blank'>{room.Name}</a>";

                            reply = Regex.Replace(
                                reply,
                                Regex.Escape(room.Name),
                                roomLink,
                                RegexOptions.IgnoreCase
                            );
                        }
                    }
                }
                catch
                {
                    // Fall back to plain text reply if database parsing fails
                }
            }

            return Json(new { reply });
        }
    }

    public class ChatRequest
    {
        public string Message { get; set; } = string.Empty;
    }
}