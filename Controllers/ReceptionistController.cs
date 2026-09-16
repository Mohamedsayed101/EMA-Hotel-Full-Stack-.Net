using Hotel_MVC.Data;
using Hotel_MVC.Enums;
using Hotel_MVC.Models;
using Hotel_MVC.ViewModels.Receptionist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace Hotel_MVC.Controllers
{
    [Authorize(Roles = "Receptionist,Admin")]
    public class ReceptionistController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReceptionistController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? search, BookingStatus? statusFilter, int page = 1, int pageSize = 10)
        {
            var today = DateTime.Today;

            ViewBag.ArrivalsToday = await _context.Bookings
                .CountAsync(b => b.CheckInDate.Date == today && b.Status != BookingStatus.Cancelled);

            ViewBag.DeparturesToday = await _context.Bookings
                .CountAsync(b => b.CheckOutDate.Date == today && b.Status == BookingStatus.CheckedIn);

            var query = _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .AsQueryable();

            if (statusFilter.HasValue)
                query = query.Where(b => b.Status == statusFilter.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(b =>
                    b.BookingReference.Contains(term) ||
                    b.Guest.FirstName.Contains(term) ||
                    b.Guest.LastName.Contains(term) ||
                    b.Guest.Email.Contains(term));
            }

            query = query.OrderByDescending(b => b.CreatedAt);

            var totalCount = await query.CountAsync();

            var bookings = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var vm = new ReceptionistBookingVM
            {
                Filter = new ReceptionistBookingFilterVM
                {
                    Search = search,
                    StatusFilter = statusFilter,
                    Page = page,
                    PageSize = pageSize
                },
                Bookings = bookings,
                TotalCount = totalCount
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "Only pending bookings can be confirmed.";
                return RedirectToAction(nameof(Index));
            }

            booking.Status = BookingStatus.Confirmed;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking {booking.BookingReference} confirmed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.Confirmed)
            {
                TempData["Error"] = "Only confirmed bookings can be checked in.";
                return RedirectToAction(nameof(Index));
            }

            booking.Status = BookingStatus.CheckedIn;
            booking.ActualCheckInAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking {booking.BookingReference} checked in.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.CheckedIn)
            {
                TempData["Error"] = "Only checked-in bookings can be checked out.";
                return RedirectToAction(nameof(Index));
            }

            booking.Status = BookingStatus.CheckedOut;
            booking.ActualCheckOutAt = DateTime.Now;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Booking {booking.BookingReference} checked out.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> CreateWalkIn()
        {
            ViewBag.RoomTypes = new SelectList(await _context.RoomTypes.ToListAsync(), "Id", "Name");
            return View(new WalkInBookingVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWalkIn(WalkInBookingVM vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoomTypes = new SelectList(await _context.RoomTypes.ToListAsync(), "Id", "Name");
                return View(vm);
            }

            var roomType = await _context.RoomTypes.FindAsync(vm.RoomTypeId);
            if (roomType == null) return NotFound();

            var bookedRoomIds = _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled)
                .Where(b => b.CheckInDate < vm.CheckOut && b.CheckOutDate > vm.CheckIn)
                .Select(b => b.RoomId);

            var availableRoom = await _context.Rooms
                .Where(r => r.RoomTypeId == vm.RoomTypeId)
                .Where(r => r.Status == RoomStatus.Available)
                .Where(r => !bookedRoomIds.Contains(r.Id))
                .FirstOrDefaultAsync();

            if (availableRoom == null)
            {
                ModelState.AddModelError(string.Empty, "No rooms of this type are available for these dates.");
                ViewBag.RoomTypes = new SelectList(await _context.RoomTypes.ToListAsync(), "Id", "Name");
                return View(vm);
            }

            var guest = await _userManager.FindByEmailAsync(vm.GuestEmail);
            if (guest == null)
            {
                guest = new ApplicationUser
                {
                    UserName = vm.GuestEmail,
                    Email = vm.GuestEmail,
                    FirstName = vm.GuestFirstName,
                    LastName = vm.GuestLastName,
                    PhoneNumber = vm.GuestPhone,
                    EmailConfirmed = true
                };

                await _userManager.CreateAsync(guest, "WalkIn@12345");
                await _userManager.AddToRoleAsync(guest, "User");
            }

            var nights = (vm.CheckOut - vm.CheckIn).Days;

            var booking = new Booking
            {
                BookingReference = $"WALK-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}",
                CheckInDate = vm.CheckIn,
                CheckOutDate = vm.CheckOut,
                NumberOfGuests = vm.Guests,
                Status = BookingStatus.Confirmed,
                TotalAmount = roomType.PricePerNight * Math.Max(1, nights),
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = vm.PaymentMethod,
                CreatedAt = DateTime.Now,
                GuestId = guest.Id,
                RoomId = availableRoom.Id
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Walk-in reservation {booking.BookingReference} successfully created.";
            return RedirectToAction(nameof(Index));
        }
    }
}
