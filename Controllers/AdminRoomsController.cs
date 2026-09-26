using Hotel_MVC.Data;
using Hotel_MVC.Enums;
using Hotel_MVC.Models;
using Hotel_MVC.ViewModels.Room;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminRoomsController : Controller
    {
        private readonly AppDbContext _context;
        public AdminRoomsController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index(
     string? search,
     RoomStatus? status,
     string? sort,
     int page = 1)
        {
            const int pageSize = 10;

            var query = _context.Rooms
                .Include(r => r.RoomType)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(r =>
                    r.RoomNumber.Contains(search) ||
                    r.RoomType.Name.Contains(search));

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            query = sort switch
            {
                "type" => query.OrderBy(r => r.RoomType.Name),
                "floor_desc" => query.OrderByDescending(r => r.FloorNumber),
                "status" => query.OrderBy(r => r.Status),
                _ => query.OrderBy(r => r.RoomNumber)
            };

            var total = await query.CountAsync();

            var rooms = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Status = status;
            ViewBag.Sort = sort;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);

            return View(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.RoomTypes = await _context.RoomTypes.OrderBy(x => x.Name).ToListAsync();
            return View(new RoomFormVM());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.RoomTypes = await _context.RoomTypes.OrderBy(x => x.Name).ToListAsync();
                return View(vm);
            }

            if (await _context.Rooms.AnyAsync(r => r.RoomNumber == vm.RoomNumber))
            {
                ModelState.AddModelError(nameof(vm.RoomNumber), "Room number already exists.");
                ViewBag.RoomTypes = await _context.RoomTypes.OrderBy(x => x.Name).ToListAsync();
                return View(vm);
            }

            _context.Rooms.Add(new Room
            {
                RoomNumber = vm.RoomNumber.Trim(),
                FloorNumber = vm.FloorNumber,
                RoomTypeId = vm.RoomTypeId,
                Status = vm.Status
            });
            await _context.SaveChangesAsync();
            TempData["Success"] = "Room created successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null) 
                return NotFound();
            ViewBag.RoomTypes = await _context.RoomTypes.OrderBy(x => x.Name).ToListAsync();
            return View(new RoomFormVM
            {
                Id = room.Id,
                RoomNumber = room.RoomNumber,
                FloorNumber = room.FloorNumber,
                RoomTypeId = room.RoomTypeId,
                Status = room.Status
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomFormVM vm)
        {
            if (id != vm.Id) 
                return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.RoomTypes = await _context.RoomTypes.OrderBy(x => x.Name).ToListAsync();
                return View(vm);
            }

            var room = await _context.Rooms.FindAsync(id);
            if (room == null) 
                return NotFound();

            if (await _context.Rooms.AnyAsync(r => r.Id != id && r.RoomNumber == vm.RoomNumber))
            {
                ModelState.AddModelError(nameof(vm.RoomNumber), "Room number already exists.");
                ViewBag.RoomTypes = await _context.RoomTypes.OrderBy(x => x.Name).ToListAsync();
                return View(vm);
            }

            room.RoomNumber = vm.RoomNumber.Trim();
            room.FloorNumber = vm.FloorNumber;
            room.RoomTypeId = vm.RoomTypeId;
            room.Status = vm.Status;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Room updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var room = await _context.Rooms.Include(r => r.RoomType).FirstOrDefaultAsync(r => r.Id == id);
            return room == null ? NotFound() : View(room);
        }

        [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var room = await _context.Rooms.Include(r => r.Bookings).FirstOrDefaultAsync(r => r.Id == id);
            if (room == null) 
                return NotFound();

            if (room.Bookings.Any())
            {
                TempData["Error"] =
                    "This room cannot be deleted because it has booking history. Set it to Maintenance instead.";

                return RedirectToAction(nameof(Index));
            }

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Room deleted successfully.";
            return RedirectToAction(nameof(Index));
        }
    }
}
