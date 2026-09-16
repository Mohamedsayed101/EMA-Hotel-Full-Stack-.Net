using Hotel_MVC.Data;
using Hotel_MVC.Models;
using Hotel_MVC.ViewModels.Contact;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace Hotel_MVC.Controllers
{
    public class HomeController : Controller
    {
        public async Task<IActionResult> Index([FromServices] AppDbContext context)
        {
            var reviews = await context.Reviews
                .Include(r => r.Guest)
                .OrderByDescending(r => r.Id)
                .Take(3)
                .ToListAsync();

            ViewBag.AverageRating = await context.Reviews.AnyAsync()
                ? await context.Reviews.AverageAsync(r => (double)r.Rating)
                : 5.0;

            return View(reviews);
        }

        public IActionResult About()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View(new ContactVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendMessage(ContactVM vm)
        {
            if (!ModelState.IsValid)
                return View("Contact", vm);

            
            TempData["SuccessMessage"] = "Thank you! Your message has been received and our concierge team will respond shortly.";
            return RedirectToAction(nameof(Contact));
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error(int? statusCode = null)
        {
            ViewBag.StatusCode = statusCode;
            if (statusCode == 404)
            {
                ViewBag.ErrorMessage = "The page or sanctuary you are looking for does not exist.";
            }
            else
            {
                ViewBag.ErrorMessage = "An unexpected error occurred. Our concierge team is working to resolve it.";
            }

            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
