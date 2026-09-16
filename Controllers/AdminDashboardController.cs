using Hotel_MVC.Data;
using Hotel_MVC.Enums;
using Hotel_MVC.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _context;
        public AdminDashboardController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;

            var totalRooms = await _context.Rooms.CountAsync();
            var availableRooms = await _context.Rooms.CountAsync(r => r.Status == RoomStatus.Available);

            var arrivalsToday = await _context.Bookings
                .CountAsync(b => b.CheckInDate.Date == today && b.Status != BookingStatus.Cancelled);

           
            var departuresToday = await _context.Bookings
                .CountAsync(b => b.CheckOutDate.Date == today && b.Status != BookingStatus.Cancelled);

            var totalBookings = await _context.Bookings.CountAsync();

            var revenueThisMonth = await _context.Bookings
                .Where(b => b.CreatedAt.Month == DateTime.Now.Month &&
                            b.CreatedAt.Year == DateTime.Now.Year &&
                            b.Status != BookingStatus.Cancelled)
                .SumAsync(b => (decimal?)b.TotalAmount) ?? 0;

            var activeOccupiedRooms = await _context.Bookings
                .CountAsync(b => b.CheckInDate <= today && b.CheckOutDate >= today && b.Status == BookingStatus.CheckedIn);

            var occupancyRate = totalRooms > 0 ? Math.Round((double)activeOccupiedRooms / totalRooms * 100, 1) : 0;

            var vm = new DashboardVM
            {
                TotalRooms = totalRooms,
                AvailableRooms = availableRooms,
                ArrivalsToday = arrivalsToday,
                DeparturesToday = departuresToday,
                TotalBookings = totalBookings,
                RevenueThisMonth = revenueThisMonth,
                OccupancyRate = occupancyRate
            };

            return View(vm);
        }
    }
}