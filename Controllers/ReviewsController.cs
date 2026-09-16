using Hotel_MVC.Data;
using Hotel_MVC.Enums;
using Hotel_MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel_MVC.Controllers
{
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int bookingId)
        {
            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Review)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == userId);

            if (booking == null) return NotFound();

            if (booking.Status != BookingStatus.CheckedOut && booking.Status != BookingStatus.CheckedIn)
            {
                TempData["Error"] = "You can review a stay only after check-out.";
                return RedirectToAction("MyBookings", "Booking");
            }

            if (booking.Review != null)
            {
                TempData["Error"] = "You already reviewed this stay.";
                return RedirectToAction("MyBookings", "Booking");
            }

            ViewBag.BookingId = bookingId;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int bookingId, int rating, string? comment)
        {
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5.";
                return RedirectToAction("MyBookings", "Booking");
            }

            var userId = _userManager.GetUserId(User);
            var booking = await _context.Bookings
                .Include(b => b.Review)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == userId);

            if (booking == null) return NotFound();

            if (booking.Review != null)
            {
                TempData["Error"] = "You already reviewed this stay.";
                return RedirectToAction("MyBookings", "Booking");
            }

            _context.Reviews.Add(new Review
            {
                BookingId = bookingId,
                GuestId = userId!,
                Rating = rating,
                Comment = comment
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Thank you for your review.";
            return RedirectToAction("MyBookings", "Booking");
        }
    }
}