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

        public BookingController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // =========================================================
        // CREATE - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Create(
            int roomTypeId,
            DateTime? checkIn,
            DateTime? checkOut,
            int guests = 2)
        {
            var validCheckIn =
                checkIn.HasValue && checkIn.Value >= DateTime.Today
                    ? checkIn.Value
                    : DateTime.Today;

            var validCheckOut =
                checkOut.HasValue && checkOut.Value > validCheckIn
                    ? checkOut.Value
                    : validCheckIn.AddDays(1);

            var roomType = await _context.RoomTypes
                .Include(rt => rt.RoomImages)
                .FirstOrDefaultAsync(rt => rt.Id == roomTypeId);

            if (roomType == null)
                return NotFound();

            ViewBag.Extras = await _context.Extras.ToListAsync();

            var vm = new CreateBookingVM
            {
                RoomTypeId = roomType.Id,
                RoomTypeName = roomType.Name,
                CoverImageUrl = roomType.RoomImages
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault(),

                CheckIn = validCheckIn,
                CheckOut = validCheckOut,
                Guests = guests,
                PricePerNight = roomType.PricePerNight
            };

            return View(vm);
        }

        // =========================================================
        // CREATE - POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateBookingVM vm,
            List<int>? selectedExtras)
        {
            // Validate dates
            if (vm.CheckIn < DateTime.Today)
            {
                ModelState.AddModelError(
                    nameof(vm.CheckIn),
                    "Check-in date cannot be in the past.");
            }

            if (vm.CheckOut <= vm.CheckIn)
            {
                ModelState.AddModelError(
                    nameof(vm.CheckOut),
                    "Check-out must be after check-in.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Extras = await _context.Extras.ToListAsync();
                return View(vm);
            }

            // =====================================================
            // CHECK ROOM AVAILABILITY
            // =====================================================

            var bookedRoomIds = _context.Bookings
                .Where(b => b.Status != BookingStatus.Cancelled)
                .Where(b =>
                    b.CheckInDate < vm.CheckOut &&
                    b.CheckOutDate > vm.CheckIn)
                .Select(b => b.RoomId);

            var availableRoom = await _context.Rooms
                .Where(r => r.RoomTypeId == vm.RoomTypeId)
                .Where(r => r.Status == RoomStatus.Available)
                .Where(r => !bookedRoomIds.Contains(r.Id))
                .FirstOrDefaultAsync();

            if (availableRoom == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Sorry, this room is no longer available for these dates.");

                ViewBag.Extras = await _context.Extras.ToListAsync();

                return View(vm);
            }

            // =====================================================
            // USER
            // =====================================================

            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
                return Challenge();

            // =====================================================
            // CALCULATE PRICE
            // =====================================================

            var nights = (vm.CheckOut - vm.CheckIn).Days;

            decimal baseTotal =
                vm.PricePerNight * nights;

            decimal extrasTotal = 0;

            // =====================================================
            // CREATE BOOKING
            // =====================================================

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

            // =====================================================
            // EXTRAS
            // =====================================================

            if (selectedExtras != null && selectedExtras.Any())
            {
                var extras = await _context.Extras
                    .Where(e => selectedExtras.Contains(e.Id))
                    .ToListAsync();

                foreach (var extra in extras)
                {
                    decimal extraCost =
                        extra.PricingType == ExtraPricingType.PerNight
                            ? extra.Price * nights
                            : extra.Price;

                    extrasTotal += extraCost;

                    booking.BookingExtras.Add(
                        new BookingExtra
                        {
                            ExtraId = extra.Id,
                            UnitPrice = extra.Price,
                            Quantity = 1
                        });
                }
            }

            // =====================================================
            // TOTAL
            // =====================================================

            booking.TotalAmount =
                baseTotal + extrasTotal;

            _context.Bookings.Add(booking);

            await _context.SaveChangesAsync();

            // =====================================================
            // PAYMENT
            // =====================================================

            if (vm.PaymentMethod == PaymentMethod.Card)
            {
                return RedirectToAction(
                    nameof(Payment),
                    new
                    {
                        id = booking.Id
                    });
            }

            // =====================================================
            // NON-CARD PAYMENT
            // =====================================================

            booking.Status = BookingStatus.Confirmed;

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(Confirmation),
                new
                {
                    id = booking.Id
                });
        }

        // =========================================================
        // PAYMENT - GET
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Payment(int id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == id &&
                        b.GuestId == userId);

            if (booking == null)
                return NotFound();

            return View(booking);
        }

        // =========================================================
        // PROCESS PAYMENT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessPayment(
            int bookingId)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == bookingId &&
                        b.GuestId == userId);

            if (booking == null)
                return NotFound();

            // Prevent paying an already completed/cancelled booking
            if (booking.Status == BookingStatus.Cancelled ||
                booking.Status == BookingStatus.CheckedIn ||
                booking.Status == BookingStatus.CheckedOut)
            {
                TempData["ErrorMessage"] =
                    "This reservation cannot be paid for.";

                return RedirectToAction(
                    nameof(MyBookings));
            }

            booking.PaymentStatus =
                PaymentStatus.Paid;

            booking.Status =
                BookingStatus.Confirmed;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Payment processed successfully! Your stay is guaranteed.";

            return RedirectToAction(
                nameof(Confirmation),
                new
                {
                    id = booking.Id
                });
        }

        // =========================================================
        // CONFIRMATION
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(
                    b => b.Id == id);

            if (booking == null)
                return NotFound();

            if (booking.GuestId != userId &&
                !User.IsInRole("Admin") &&
                !User.IsInRole("Receptionist"))
            {
                return Forbid();
            }

            return View(booking);
        }

        // =========================================================
        // VOUCHER
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Voucher(int id)
        {
            var userId = _userManager.GetUserId(User);

            var booking = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                .Include(b => b.Guest)
                .FirstOrDefaultAsync(
                    b => b.Id == id);

            if (booking == null)
                return NotFound();

            if (booking.GuestId != userId &&
                !User.IsInRole("Admin") &&
                !User.IsInRole("Receptionist"))
            {
                return Forbid();
            }

            return View(booking);
        }

        // =========================================================
        // MY BOOKINGS
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> MyBookings()
        {
            var userId = _userManager.GetUserId(User);

            var bookings = await _context.Bookings
                .Include(b => b.Room)
                    .ThenInclude(r => r.RoomType)
                        .ThenInclude(rt => rt.RoomImages)
                .Include(b => b.Review)
                .Where(b => b.GuestId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // =========================================================
        // CANCEL BOOKING
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(
            int id,
            string? cancellationReason)
        {
            var userId = _userManager.GetUserId(User);

            // -----------------------------------------------------
            // Find booking belonging to current user
            // -----------------------------------------------------

            var booking = await _context.Bookings
                .FirstOrDefaultAsync(
                    b =>
                        b.Id == id &&
                        b.GuestId == userId);

            if (booking == null)
                return NotFound();

            // -----------------------------------------------------
            // Booking must be Confirmed
            // -----------------------------------------------------

            if (booking.Status != BookingStatus.Confirmed)
            {
                TempData["ErrorMessage"] =
                    "Only confirmed reservations can be cancelled.";

                return RedirectToAction(
                    nameof(MyBookings));
            }

            // -----------------------------------------------------
            // Cancellation policy
            // -----------------------------------------------------

            var timeUntilCheckIn =
                booking.CheckInDate - DateTime.Now;

            if (timeUntilCheckIn <= TimeSpan.FromHours(24))
            {
                TempData["ErrorMessage"] =
                    "Cannot cancel reservation within 24 hours of check-in.";

                return RedirectToAction(
                    nameof(MyBookings));
            }

            // -----------------------------------------------------
            // Cancel booking
            // -----------------------------------------------------

            booking.Status =
                BookingStatus.Cancelled;

            booking.CancellationReason =
                string.IsNullOrWhiteSpace(cancellationReason)
                    ? null
                    : cancellationReason.Trim();

            // -----------------------------------------------------
            // Save
            // -----------------------------------------------------

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Reservation cancelled successfully.";

            return RedirectToAction(
                nameof(MyBookings));
        }

        // =========================================================
        // BOOKING REFERENCE
        // =========================================================

        private static string GenerateBookingReference()
        {
            return
                $"EMA-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..5].ToUpper()}";
        }
    }
}