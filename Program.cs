using Hotel_MVC.Data;
using Hotel_MVC.Models;
using Hotel_MVC.Models.Payment;
using Hotel_MVC.Services;
using Hotel_MVC.Services.Payment;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hotel_MVC
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Environment.IsDevelopment()
                ? builder.Configuration.GetConnectionString("MyConnection")
                : builder.Configuration.GetConnectionString("MonsterAspConnection");

            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
            })
             .AddEntityFrameworkStores<AppDbContext>()
             .AddDefaultTokenProviders();

            builder.Services.Configure<PaymobOptions>(builder.Configuration.GetSection("Paymob"));
            builder.Services.AddHttpClient<IPaymobService, PaymobService>();

            var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
            var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

            var authenticationBuilder = builder.Services.AddAuthentication();

            if (!string.IsNullOrWhiteSpace(googleClientId) &&
                !string.IsNullOrWhiteSpace(googleClientSecret))
            {
                authenticationBuilder.AddGoogle(options =>
                {
                    options.ClientId = googleClientId;
                    options.ClientSecret = googleClientSecret;
                });
            }

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.AccessDeniedPath = "/Account/AccessDenied";
            });

            builder.Services.AddControllersWithViews();

            // Email Service
            builder.Services.Configure<EmailSettings>(
                builder.Configuration.GetSection("EmailSettings"));

            builder.Services.AddScoped<IEmailSender, EmailSender>();

            // Phase 4: Register Typed HttpClient for Gemini AI Service
            builder.Services.AddHttpClient<IGeminiService, GeminiService>();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            using (var scope = app.Services.CreateScope())
            {
                await DbRolesInitializer.SeedAsync(scope.ServiceProvider);
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await DbRoomsDataInitializer.SeedAsync(context);
            }

            app.Run();
        }
    }
}