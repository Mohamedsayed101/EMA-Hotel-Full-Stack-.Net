using Hotel_MVC.Data;
using Hotel_MVC.Models;
using Hotel_MVC.ViewModels.RoomType;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminRoomTypesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
        private const long MaxFileSizeBytes = 5 * 1024 * 1024;

        public AdminRoomTypesController(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        
        [HttpGet]
        public async Task<IActionResult> Index(RoomTypeListFilterVM filter)
        {
            var query = _context.RoomTypes
                .Include(rt => rt.RoomImages)
                .Include(rt => rt.Rooms)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                query = query.Where(rt => rt.Name.Contains(filter.Search));

            query = filter.SortBy switch
            {
                "price_desc" => query.OrderByDescending(rt => rt.PricePerNight),
                "name" => query.OrderBy(rt => rt.Name),
                _ => query.OrderBy(rt => rt.PricePerNight)
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(rt => new RoomTypeListItemVM
                {
                    Id = rt.Id,
                    Name = rt.Name,
                    Capacity = rt.Capacity,
                    PricePerNight = rt.PricePerNight,
                    ViewType = rt.ViewType,
                    RoomCount = rt.Rooms.Count,
                    CoverImageUrl = rt.RoomImages
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var vm = new RoomTypeListVM
            {
                Filter = filter,
                RoomTypes = items,
                TotalCount = totalCount
            };

            return View(vm);
        }

        
        [HttpGet]
        public IActionResult Create()
        {
            return View(new RoomTypeFormVM());
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RoomTypeFormVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var roomType = new RoomType
            {
                Name = vm.Name,
                Description = vm.Description,
                Capacity = vm.Capacity,
                PricePerNight = vm.PricePerNight,
                ViewType = vm.ViewType,
                CancellationPolicy = vm.CancellationPolicy,
                Amenities = vm.Amenities
            };

            _context.RoomTypes.Add(roomType);
            await _context.SaveChangesAsync(); 

            await SaveNewImagesAsync(roomType.Id, vm.NewImages);

            TempData["Success"] = $"Room type \"{roomType.Name}\" created.";
            return RedirectToAction(nameof(Index));
        }

        
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var roomType = await _context.RoomTypes
                .Include(rt => rt.RoomImages)
                .FirstOrDefaultAsync(rt => rt.Id == id);

            if (roomType == null) 
                return NotFound();

            var vm = new RoomTypeFormVM
            {
                Id = roomType.Id,
                Name = roomType.Name,
                Description = roomType.Description,
                Capacity = roomType.Capacity,
                PricePerNight = roomType.PricePerNight,
                ViewType = roomType.ViewType,
                CancellationPolicy = roomType.CancellationPolicy,
                Amenities = roomType.Amenities,
                ExistingImages = roomType.RoomImages
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ExistingImageVM { Id = i.Id, ImageUrl = i.ImageUrl })
                    .ToList()
            };

            return View(vm);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, RoomTypeFormVM vm)
        {
            if (id != vm.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                
                vm.ExistingImages = await _context.RoomImages
                    .Where(i => i.RoomTypeId == id)
                    .OrderBy(i => i.DisplayOrder)
                    .Select(i => new ExistingImageVM { Id = i.Id, ImageUrl = i.ImageUrl })
                    .ToListAsync();
                return View(vm);
            }

            var roomType = await _context.RoomTypes.FindAsync(id);
            if (roomType == null) return NotFound();

            roomType.Name = vm.Name;
            roomType.Description = vm.Description;
            roomType.Capacity = vm.Capacity;
            roomType.PricePerNight = vm.PricePerNight;
            roomType.ViewType = vm.ViewType;
            roomType.CancellationPolicy = vm.CancellationPolicy;
            roomType.Amenities = vm.Amenities;

            
            if (vm.ImagesToDelete != null && vm.ImagesToDelete.Any())
            {
                var imagesToDelete = await _context.RoomImages
                    .Where(i => vm.ImagesToDelete.Contains(i.Id) && i.RoomTypeId == id)
                    .ToListAsync();

                foreach (var img in imagesToDelete)
                    DeletePhysicalFile(img.ImageUrl);

                _context.RoomImages.RemoveRange(imagesToDelete);
            }

            await SaveNewImagesAsync(id, vm.NewImages);

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Room type \"{roomType.Name}\" updated.";
            return RedirectToAction(nameof(Index));
        }

        
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .FirstOrDefaultAsync(rt => rt.Id == id);

            if (roomType == null) 
                return NotFound();

            return View(roomType);
        }

        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var roomType = await _context.RoomTypes
                .Include(rt => rt.Rooms)
                .Include(rt => rt.RoomImages)
                .FirstOrDefaultAsync(rt => rt.Id == id);

            if (roomType == null) return NotFound();

            if (roomType.Rooms.Any())
            {
                TempData["Error"] = $"Can't delete \"{roomType.Name}\" — it still has {roomType.Rooms.Count} room(s). Delete those rooms first.";
                return RedirectToAction(nameof(Index));
            }

            foreach (var img in roomType.RoomImages)
                DeletePhysicalFile(img.ImageUrl);

            _context.RoomTypes.Remove(roomType); 
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Room type \"{roomType.Name}\" deleted.";
            return RedirectToAction(nameof(Index));
        }

        

        private async Task SaveNewImagesAsync(int roomTypeId, List<IFormFile>? files)
        {
            if (files == null || !files.Any())
                return;

            var folderPath = Path.Combine(_env.WebRootPath, "Images", "Rooms");
            Directory.CreateDirectory(folderPath);

            var currentMaxOrder = await _context.RoomImages
                .Where(i => i.RoomTypeId == roomTypeId)
                .Select(i => (int?)i.DisplayOrder)
                .MaxAsync() ?? -1;

            int order = currentMaxOrder + 1;

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!AllowedExtensions.Contains(ext) || file.Length > MaxFileSizeBytes || file.Length == 0)
                    continue; 

                var fileName = $"{Guid.NewGuid()}{ext}";
                var filePath = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _context.RoomImages.Add(new RoomImage
                {
                    RoomTypeId = roomTypeId,
                    ImageUrl = $"/Images/Rooms/{fileName}",
                    DisplayOrder = order++
                });
            }

            await _context.SaveChangesAsync();
        }

        private void DeletePhysicalFile(string imageUrl)
        {
            var relativePath = imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(_env.WebRootPath, relativePath);

            if (System.IO.File.Exists(fullPath))
                System.IO.File.Delete(fullPath);
        }
    }
}
