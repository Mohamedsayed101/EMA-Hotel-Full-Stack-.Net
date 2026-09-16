using Hotel_MVC.Data;
using Hotel_MVC.Enums;
using Hotel_MVC.Models;
using Hotel_MVC.ViewModels.Booking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Hotel_MVC.Controllers
{
    [Authorize]
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BookingController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int roomTypeId, DateTime? checkIn, DateTime? checkOut, int guests = 2)
        {
            var validCheckIn = checkIn.HasValue && checkIn.Value >= DateTime.Today ? checkIn.Value : DateTime.Today;
            var validCheckOut = checkOut.HasValue && checkOut.Value > validCheckIn ? checkOut.Value : validCheckIn.AddDays(1);

            var roomType = await _context.RoomTypes
                .Include(rt => rt.RoomImages)
                .FirstOrDefaultAsync(rt => rt.Id == roomTypeId);

            if (roomType == null)
                return NotFound();

            var nights = (validCheckOut - validCheckIn).Days;


            ViewBag.Extras = await _context.Extras.ToListAsync();

            var vm = new CreateBookingVM
            {
                RoomTypeId = roomType.Id,
                RoomTypeName = roomType.Name,
                CoverImageUrl = roomType.RoomImages.OrderBy(i => i.DisplayOrder).Select(i => i.ImageUrl).FirstOrDefault(),
                CheckIn = validCheckIn,
                CheckOut = validCheckOut,
                Guests = guests,
                PricePerNight = roomType.PricePerNight
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBookingVM vm, List<int>? selectedExtras)
        {
            if (vm.CheckIn < DateTime.Today)
                ModelState.AddModelError(nameof(vm.CheckIn), "Check-in date cannot be in the past.");

            if (vm.CheckOut <= vm.CheckIn)
                ModelState.AddModelError(nameof(vm.CheckOut), "Check-out must be after check-in.");

            if (!ModelState.IsValid)
            {
                ViewBag.Extras = await _context.Extras.ToListAsync();
                return View(vm);
            }

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
                ModelState.AddModelError(string.Empty, "Sorry, this room is no longer available for these dates.");
                ViewBag.Extras = await _context.Extras.ToListAsync();
                return View(vm);
            }

            var userId = _userManager.GetUserId(User);
            var nights = (vm.CheckOut - vm.CheckIn).Days;
            decimal baseTotal = vm.PricePerNight * nights;
            decimal extrasTotal = 0;

            var booking = new Booking
            {
                BookingReference = GenerateBookingReference(),
                CheckInDate = vm.CheckIn,
                CheckOutDate = vm.CheckOut,
                NumberOfGuests = vm.Guests,
                Status = BookingStatus.Pending,
                PaymentStatus = PaymentStatus.Pending,
                PaymentMethod = vm.PaymentMethod,
                CreatedAt = DateTime.Now,
                GuestId = userId,
                RoomId = availableRoom.Id
            };


            if (selectedExtras != null && selectedExtras.Any())
            {
                var extras = await _context.Extras.Where(e => selectedExtras.Contains(e.Id)).ToListAsync();
                foreach (var extra in extras)
                {
                    decimal extraCost = extra.PricingType == ExtraPricingType.PerNight ? (extra.Price * nights) : extra.Price;
                    extrasTotal += extraCost;

                    booking.BookingExtras.Add(new BookingExtra
                    {
                        ExtraId = extra.Id,
                        UnitPrice = extra.Price,
                        Quantity = 1
                    });
                }
            }

            booking.TotalAmount = baseTotal + extrasTotal;

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();


            if (vm.PaymentMethod == PaymentMethod.Card)
            {
                return RedirectToAction(nameof(Payment), new { id = booking.Id });
            }

            return RedirectToAction(nameof(Confirmation), new { id = booking.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == userId);

            if (booking == null) return NotFound();

            return View(booking);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == userId);

            if (booking == null) return NotFound();

            booking.PaymentStatus = PaymentStatus.Paid;
            booking.Status = BookingStatus.Confirmed;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Payment processed successfully! Your stay is guaranteed.";
            return RedirectToAction(nameof(Confirmation), new { id = booking.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            if (booking.GuestId != userId && !User.IsInRole("Admin") && !User.IsInRole("Receptionist"))
                return Forbid();

            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> Voucher(int id)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null) return NotFound();

            if (booking.GuestId != userId && !User.IsInRole("Admin") && !User.IsInRole("Receptionist"))
                return Forbid();

            return View(booking);
        }

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .ThenInclude(rt => rt.RoomImages)
                .Include(b => b.Review)
                .Where(b => b.GuestId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(int id)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings.FirstOrDefaultAsync(b => b.Id == id && b.GuestId == userId);

            if (booking == null) return NotFound();

            if (booking.CheckInDate <= DateTime.Today.AddDays(1))
            {
                TempData["ErrorMessage"] = "Cannot cancel reservation within 24 hours of check-in.";
                return RedirectToAction(nameof(MyBookings));
            }

            booking.Status = BookingStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Reservation cancelled successfully.";
            return RedirectToAction(nameof(MyBookings));
        }

        private static string GenerateBookingReference()
        {
            return $"EMA-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}";
        }
    }
}
