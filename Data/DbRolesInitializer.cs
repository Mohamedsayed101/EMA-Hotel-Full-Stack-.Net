using Hotel_MVC.Models;
using Microsoft.AspNetCore.Identity;

namespace Hotel_MVC.Data
{
    public class DbRolesInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            string[] roles = { "Admin", "Receptionist", "User" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }
            var adminEmail = "admin@hotel.com";
            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "System",
                    LastName = "Admin",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(admin, "Admin@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(admin, "Admin");
            }

            var receptionistEmail = "reception@hotel.com";
            var receptionistUser = await userManager.FindByEmailAsync(receptionistEmail);
            if (receptionistUser == null)
            {
                receptionistUser = new ApplicationUser
                {
                    UserName = receptionistEmail,
                    Email = receptionistEmail,
                    FirstName = "Hotel",
                    LastName = "Reception",
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(receptionistUser, "Reception@123");
                await userManager.AddToRoleAsync(receptionistUser, "Receptionist");
            }
        }

    }
}
    
