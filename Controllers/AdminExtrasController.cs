using Hotel_MVC.Data;
using Hotel_MVC.Enums;
using Hotel_MVC.Models;
using Hotel_MVC.ViewModels.Extra;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hotel_MVC.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminExtrasController : Controller
    {
        private readonly AppDbContext _context;
        public AdminExtrasController(AppDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> Index(string? search, ExtraPricingType? pricingType, int page = 1)
        {
            const int pageSize = 10;
            var query = _context.Extras.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
                query = query.Where(e => e.Name.Contains(search));

            if (pricingType.HasValue)
                query = query.Where(e => e.PricingType == pricingType.Value);

            var total = await query.CountAsync();
            var items = await query.OrderBy(e => e.Name)
                                   .Skip((page - 1) * pageSize)
                                   .Take(pageSize)
                                   .ToListAsync();

            ViewBag.Search = search;
            ViewBag.PricingType = pricingType;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)pageSize);
            return View(items);
        }

        [HttpGet]
        public IActionResult Create() => View(new ExtraFormVM());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ExtraFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fill in all required fields correctly.";
                return View(vm);
            }

            var trimmedName = vm.Name?.Trim();
            if (await _context.Extras.AnyAsync(e => e.Name == trimmedName))
            {
                ModelState.AddModelError(nameof(vm.Name), "An extra with this name already exists.");
                return View(vm);
            }

            var extra = new Extra
            {
                Name = vm.Name.Trim(),
                Price = vm.Price,
                PricingType = vm.PricingType
            };

            _context.Extras.Add(extra);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Service added successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var extra = await _context.Extras.FindAsync(id);
            if (extra == null) return NotFound();

            var vm = new ExtraFormVM
            {
                Id = extra.Id,
                Name = extra.Name,
                Price = extra.Price,
                PricingType = extra.PricingType
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ExtraFormVM vm)
        {
            if (id != vm.Id) 
                return BadRequest();
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "Please fix the validation errors.";
                return View(vm);
            }
            

            var trimmedName = vm.Name?.Trim();
            if (await _context.Extras.AnyAsync(e => e.Name == trimmedName && e.Id != id))
            {
                ModelState.AddModelError(nameof(vm.Name), "Another service with this name already exists.");
                return View(vm);
            }

            var extra = await _context.Extras.FindAsync(id);
            if (extra == null) return NotFound();

            extra.Name = vm.Name.Trim();
            extra.Price = vm.Price;
            extra.PricingType = vm.PricingType;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Service updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var extra = await _context.Extras.FindAsync(id);
            if (extra == null) return NotFound();
            return View(extra);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var extra = await _context.Extras.FindAsync(id);
            if (extra == null)
            {
                TempData["Error"] = "The requested add-on was not found.";
                return RedirectToAction(nameof(Index));
            }

           
            _context.Extras.Remove(extra);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Service removed successfully.";
           
            return RedirectToAction(nameof(Index));
        }
    }
}