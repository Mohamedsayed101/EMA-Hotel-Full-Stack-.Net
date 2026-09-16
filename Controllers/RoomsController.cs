using Hotel_MVC.Data;
using Hotel_MVC.Enums;
using Hotel_MVC.Services;
using Hotel_MVC.ViewModels.Room;
using Hotel_MVC.ViewModels.SearchVM;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel_MVC.Controllers
{
    public class RoomsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IGeminiService _geminiService;

        public RoomsController(AppDbContext context, IGeminiService geminiService)
        {
            _context = context;
            _geminiService = geminiService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.RoomTypes
                .Select(rt => new RoomBrowseItemVM
                {
                    Id = rt.Id,
                    Name = rt.Name,
                    Description = rt.Description,
                    ViewType = rt.ViewType,
                    Capacity = rt.Capacity,
                    PricePerNight = rt.PricePerNight,
                    Amenities = rt.Amenities,
                    CoverImageUrl = rt.RoomImages
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return View(rooms);
        }

        [HttpGet]
        public IActionResult Search(SearchAvailabilityVM search)
        {
            if (search.CheckOut <= search.CheckIn)
            {
                ModelState.AddModelError(nameof(search.CheckOut), "Check-out must be after check-in.");
                return View("Index", search);
            }

            var bookedRoomIds = _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled)
                .Where(b => b.CheckInDate < search.CheckOut && b.CheckOutDate > search.CheckIn)
                .Select(b => b.RoomId);

            var availableRoomsQuery = _context.Rooms
                .Where(r => r.Status == RoomStatus.Available)
                .Where(r => !bookedRoomIds.Contains(r.Id));

            var nights = (search.CheckOut - search.CheckIn).Days;

            var availableCountsByType = availableRoomsQuery
                .GroupBy(r => r.RoomTypeId)
                .Select(g => new { RoomTypeId = g.Key, AvailableCount = g.Count() });

            var query =
                from rt in _context.RoomTypes
                join a in availableCountsByType on rt.Id equals a.RoomTypeId
                select new RoomSearchResultItemVM
                {
                    RoomTypeId = rt.Id,
                    Name = rt.Name,
                    ViewType = rt.ViewType,
                    Capacity = rt.Capacity,
                    PricePerNight = rt.PricePerNight,
                    TotalPriceForStay = rt.PricePerNight * nights,
                    AvailableRoomsCount = a.AvailableCount,
                    CoverImageUrl = rt.RoomImages
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                };

            query = query.Where(r => r.Capacity >= search.Guests);

            if (!string.IsNullOrEmpty(search.ViewType))
                query = query.Where(r => r.ViewType == search.ViewType);

            if (search.MaxPrice.HasValue)
                query = query.Where(r => r.PricePerNight <= search.MaxPrice.Value);

            query = search.SortBy switch
            {
                "price_desc" => query.OrderByDescending(r => r.PricePerNight),
                "capacity" => query.OrderByDescending(r => r.Capacity),
                _ => query.OrderBy(r => r.PricePerNight)
            };

            var totalCount = query.Count();

            var pagedResults = query
                .Skip((search.Page - 1) * search.PageSize)
                .Take(search.PageSize)
                .ToList();

            var vm = new RoomSearchResultsViewModel
            {
                Search = search,
                Results = pagedResults,
                TotalCount = totalCount
            };

            return View(vm);
        }

        // --- SMART SEARCH ENDPOINT (Book by Sentence) ---
        [HttpPost]
        public async Task<IActionResult> SmartSearch([FromBody] SmartSearchRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Query))
            {
                return BadRequest(new { error = "Search query cannot be empty." });
            }

            // 1. Send natural language to Gemini AI to extract filter parameters
            var filters = await _geminiService.ParseBookingSentenceAsync(request.Query);

            if (filters == null)
            {
                return BadRequest(new { error = "Unable to process the search query via AI service." });
            }

            // 2. Query RoomTypes based on extracted parameters
            var query = _context.RoomTypes.AsQueryable();

            if (filters.MaxPricePerNight.HasValue && filters.MaxPricePerNight.Value > 0)
            {
                query = query.Where(rt => rt.PricePerNight <= filters.MaxPricePerNight.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.Keywords))
            {
                var kw = filters.Keywords.ToLower();
                query = query.Where(rt => rt.Name.ToLower().Contains(kw) ||
                                          rt.Description.ToLower().Contains(kw) ||
                                          rt.ViewType.ToLower().Contains(kw) ||
                                          rt.Amenities.ToLower().Contains(kw));
            }

            // Optional: If check-in / check-out dates were extracted, integrate availability filtering
            if (filters.CheckIn.HasValue && filters.CheckIn.Value > DateTime.MinValue &&
                filters.CheckOut.HasValue && filters.CheckOut.Value > filters.CheckIn.Value)
            {
                var bookedRoomIds = _context.Bookings
                    .Where(b => b.Status != BookingStatus.Cancelled)
                    .Where(b => b.CheckInDate < filters.CheckOut.Value && b.CheckOutDate > filters.CheckIn.Value)
                    .Select(b => b.RoomId);

                var availableRoomTypeIds = _context.Rooms
                    .Where(r => r.Status == RoomStatus.Available && !bookedRoomIds.Contains(r.Id))
                    .Select(r => r.RoomTypeId)
                    .Distinct();

                query = query.Where(rt => availableRoomTypeIds.Contains(rt.Id));
            }

            var results = await query
                .Select(rt => new RoomBrowseItemVM
                {
                    Id = rt.Id,
                    Name = rt.Name,
                    Description = rt.Description,
                    ViewType = rt.ViewType,
                    Capacity = rt.Capacity,
                    PricePerNight = rt.PricePerNight,
                    Amenities = rt.Amenities,
                    CoverImageUrl = rt.RoomImages
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return Json(new { filters, results });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var roomType = await _context.RoomTypes
                .Include(rt => rt.RoomImages)
                .Include(rt => rt.Rooms)
                .ThenInclude(r => r.Bookings)
                .ThenInclude(b => b.Review)
                .FirstOrDefaultAsync(rt => rt.Id == id);

            if (roomType == null)
                return NotFound();

            return View(roomType);
        }
    }

    public class SmartSearchRequest
    {
        public string Query { get; set; } = string.Empty;
    }
}