using Hotel_MVC.Data;
using Hotel_MVC.Enums;
using Hotel_MVC.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel_MVC.Controllers
{
    [Authorize(Roles = "Admin,Receptionist")]
    public class AdminBookingsController : Controller
    {
        private readonly AppDbContext _context;
        public AdminBookingsController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index(BookingAdminListVM vm, string? sort)
        {
            var query = _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(vm.Search))
                query = query.Where(b => b.BookingReference.Contains(vm.Search) ||
                    b.Guest.FirstName.Contains(vm.Search) ||
                    b.Guest.LastName.Contains(vm.Search) ||
                    b.Guest.Email!.Contains(vm.Search));

            if (vm.Status.HasValue)
                query = query.Where(b => b.Status == vm.Status.Value);

            if (vm.Date.HasValue)
            {
                var date = vm.Date.Value.Date;
                query = query.Where(b => b.CheckInDate.Date <= date && b.CheckOutDate.Date > date);
            }

            query = sort switch
            {
                "date" => query.OrderBy(b => b.CheckInDate),
                "total_desc" => query.OrderByDescending(b => b.TotalAmount),
                "guest" => query.OrderBy(b => b.Guest.LastName),
                _ => query.OrderByDescending(b => b.CreatedAt)
            };
            ViewBag.Sort = sort;
            vm.TotalCount = await query.CountAsync();
            vm.Bookings = await query.Skip((vm.Page - 1) * vm.PageSize).Take(vm.PageSize).ToListAsync();

            return View(vm);
        }

        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Guest)
                .Include(b => b.Room).ThenInclude(r => r.RoomType)
                .Include(b => b.BookingExtras).ThenInclude(x => x.Extra)
                .Include(b => b.Review)
                .FirstOrDefaultAsync(b => b.Id == id);

            return booking == null ? NotFound() : View(booking);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string? reason)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null) return NotFound();

            if (booking.Status is BookingStatus.CheckedIn or BookingStatus.CheckedOut)
            {
                TempData["Error"] = "A checked-in or completed booking cannot be cancelled.";
                return RedirectToAction(nameof(Details), new { id });
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = string.IsNullOrWhiteSpace(reason) ? "Cancelled by staff" : reason.Trim();
            await _context.SaveChangesAsync();

            TempData["Success"] = "Booking cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}

