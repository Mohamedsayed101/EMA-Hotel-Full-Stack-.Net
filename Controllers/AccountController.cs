using Hotel_MVC.Models;
using Hotel_MVC.ViewModels.AccountVM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Hotel_MVC.Services;
using Hotel_MVC.Services.Payment;

namespace Hotel_MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _environment;
        private readonly IPaymobService _paymobService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IWebHostEnvironment environment,
            IPaymobService paymobService)

        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _environment = environment;
            _paymobService = paymobService;
        }

        // =========================================================
        // REGISTER
        // =========================================================

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = new ApplicationUser
            {
                UserName = vm.Email,
                Email = vm.Email,
                PhoneNumber = vm.PhoneNumber,
                FirstName = vm.FirstName,
                LastName = vm.LastName,
                Nationality = vm.Nationality,
                PassportOrNationalId = vm.PassportOrNationalId
            };

            var result = await _userManager.CreateAsync(user, vm.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(vm);
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                "User");

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                await _userManager.DeleteAsync(user);

                return View(vm);
            }

            // Generate email confirmation token
            var token =
                await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // Generate confirmation URL
            var confirmationLink = Url.Action(
                nameof(ConfirmEmail),
                "Account",
                new
                {
                    userId = user.Id,
                    token = token
                },
                Request.Scheme);

            var emailBody = $@"
                <h2>Welcome to EMA Hotel</h2>

                <p>Hello {user.FirstName},</p>

                <p>
                    Thank you for creating an account with us.
                </p>

                <p>
                    Please confirm your email address by clicking
                    the button below:
                </p>

                <p>
                    <a href=""{confirmationLink}""
                       style=""display:inline-block;
                              padding:12px 24px;
                              background-color:#b08d57;
                              color:white;
                              text-decoration:none;
                              border-radius:6px;"">
                        Confirm Email
                    </a>
                </p>

                <p>
                    If you did not create this account,
                    you can safely ignore this email.
                </p>

                <p>
                    Best regards,<br/>
                    EMA Hotel Team
                </p>
            ";

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Confirm your EMA Hotel account",
                emailBody);

            return RedirectToAction(
                nameof(EmailConfirmationSent));
        }

        // =========================================================
        // LOGIN
        // =========================================================

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginVM
            {
                ReturnUrl = returnUrl
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user =
                    await _userManager.FindByEmailAsync(model.Email);

                if (user != null)
                {
                    if (await _userManager.IsInRoleAsync(
                            user,
                            "Receptionist")
                        &&
                        !await _userManager.IsInRoleAsync(
                            user,
                            "Admin"))
                    {
                        return RedirectToAction(
                            "Index",
                            "Receptionist");
                    }

                    if (await _userManager.IsInRoleAsync(
                            user,
                            "Admin"))
                    {
                        return RedirectToAction(
                            "Index",
                            "AdminDashboard");
                    }
                }

                if (!string.IsNullOrEmpty(model.ReturnUrl)
                    &&
                    Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction(
                    "Index",
                    "Home");
            }

            ModelState.AddModelError(
                string.Empty,
                "Invalid login attempt.");

            return View(model);
        }

        // =========================================================
        // GOOGLE / EXTERNAL LOGIN
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ExternalLogin(
            string provider,
            string? returnUrl = null)
        {
            var redirectUrl = Url.Action(
                nameof(ExternalLoginCallback),
                "Account",
                new
                {
                    returnUrl
                });

            var properties =
                _signInManager.ConfigureExternalAuthenticationProperties(
                    provider,
                    redirectUrl);

            return Challenge(properties, provider);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(
            string? returnUrl = null,
            string? remoteError = null)
        {
            if (!string.IsNullOrEmpty(remoteError))
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"External provider error: {remoteError}");

                return RedirectToAction(
                    nameof(Login),
                    new
                    {
                        returnUrl
                    });
            }

            var info =
                await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                return RedirectToAction(
                    nameof(Login),
                    new
                    {
                        returnUrl
                    });
            }

            var result =
                await _signInManager.ExternalLoginSignInAsync(
                    info.LoginProvider,
                    info.ProviderKey,
                    isPersistent: false,
                    bypassTwoFactor: true);

            if (result.Succeeded)
            {
                return await RedirectAfterLogin(returnUrl);
            }

            if (result.IsLockedOut)
            {
                return RedirectToAction(
                    nameof(Login),
                    new
                    {
                        returnUrl
                    });
            }

            var email =
                info.Principal.FindFirst(
                    System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Google did not provide an email address.");

                return RedirectToAction(
                    nameof(Login),
                    new
                    {
                        returnUrl
                    });
            }

            var user =
                await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,

                    FirstName =
                        info.Principal.FindFirst(
                            System.Security.Claims.ClaimTypes.GivenName)
                        ?.Value ?? "",

                    LastName =
                        info.Principal.FindFirst(
                            System.Security.Claims.ClaimTypes.Surname)
                        ?.Value ?? ""
                };

                var createResult =
                    await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return RedirectToAction(
                        nameof(Login),
                        new
                        {
                            returnUrl
                        });
                }

                await _userManager.AddToRoleAsync(
                    user,
                    "User");
            }

            var addLoginResult =
                await _userManager.AddLoginAsync(
                    user,
                    info);

            if (!addLoginResult.Succeeded)
            {
                foreach (var error in addLoginResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return RedirectToAction(
                    nameof(Login),
                    new
                    {
                        returnUrl
                    });
            }

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            return await RedirectAfterLogin(returnUrl);
        }

        private async Task<IActionResult> RedirectAfterLogin(
            string? returnUrl)
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user != null)
            {
                if (await _userManager.IsInRoleAsync(
                        user,
                        "Receptionist")
                    &&
                    !await _userManager.IsInRoleAsync(
                        user,
                        "Admin"))
                {
                    return RedirectToAction(
                        "Index",
                        "Receptionist");
                }

                if (await _userManager.IsInRoleAsync(
                        user,
                        "Admin"))
                {
                    return RedirectToAction(
                        "Index",
                        "AdminDashboard");
                }
            }

            if (!string.IsNullOrEmpty(returnUrl)
                &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(
                "Index",
                "Home");
        }

        // =========================================================
        // LOGOUT
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction(
                "Login",
                "Account");
        }

        // =========================================================
        // ACCESS DENIED
        // =========================================================

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // =========================================================
        // PROFILE - GET
        // =========================================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            var vm = new ProfileVM
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Nationality = user.Nationality,
                PassportOrNationalId =
                    user.PassportOrNationalId,

                ProfileImagePath =
                    user.ProfileImagePath,

                // Stay Preferences
                BreakfastPreference =
                    user.BreakfastPreference,

                AirportPickupPreference =
                    user.AirportPickupPreference,

                LateCheckoutPreference =
                    user.LateCheckoutPreference,

                PreferredRoomView =
                    user.PreferredRoomView,

                SpecialRequests =
                    user.SpecialRequests
            };

            return View(vm);
        }

        // =========================================================
        // PROFILE - UPDATE INFORMATION
        // =========================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            user.FirstName = vm.FirstName;
            user.LastName = vm.LastName;
            user.PhoneNumber = vm.PhoneNumber;
            user.Nationality = vm.Nationality;

            // Keep Passport/ID controlled by the server.
            // Do not update it from the posted form.

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                return View(vm);
            }

            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(
                nameof(Profile));
        }

        // =========================================================
        // PROFILE PHOTO
        // =========================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeProfileImage(
            IFormFile? ProfileImage)
        {
            if (ProfileImage == null ||
                ProfileImage.Length == 0)
            {
                TempData["ErrorMessage"] =
                    "Please select an image.";

                return RedirectToAction(
                    nameof(Profile));
            }

            // Maximum file size: 5 MB
            if (ProfileImage.Length >
                5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] =
                    "The image size must not exceed 5 MB.";

                return RedirectToAction(
                    nameof(Profile));
            }

            var allowedExtensions = new[]
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

            var extension =
                Path.GetExtension(
                    ProfileImage.FileName)
                .ToLowerInvariant();

            if (!allowedExtensions.Contains(
                    extension))
            {
                TempData["ErrorMessage"] =
                    "Only JPG, JPEG, PNG, and WEBP images are allowed.";

                return RedirectToAction(
                    nameof(Profile));
            }

            var allowedContentTypes = new[]
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };

            if (!allowedContentTypes.Contains(
                    ProfileImage.ContentType
                        .ToLowerInvariant()))
            {
                TempData["ErrorMessage"] =
                    "Invalid image type.";

                return RedirectToAction(
                    nameof(Profile));
            }

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            var uploadsFolder =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "profiles");

            Directory.CreateDirectory(
                uploadsFolder);

            // Never trust the original file name.
            var fileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath =
                Path.Combine(
                    uploadsFolder,
                    fileName);

            var oldImagePath =
                user.ProfileImagePath;

            // Save the new image first.
            await using (var stream =
                new FileStream(
                    filePath,
                    FileMode.Create))
            {
                await ProfileImage.CopyToAsync(
                    stream);
            }

            user.ProfileImagePath =
                $"/uploads/profiles/{fileName}";

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                // Remove the new image
                // if database update failed.
                if (System.IO.File.Exists(
                        filePath))
                {
                    System.IO.File.Delete(
                        filePath);
                }

                TempData["ErrorMessage"] =
                    "Could not update your profile photo.";

                return RedirectToAction(
                    nameof(Profile));
            }

            // Delete the old uploaded image
            // only after DB update succeeds.
            if (!string.IsNullOrWhiteSpace(
                    oldImagePath))
            {
                var oldFileName =
                    Path.GetFileName(
                        oldImagePath);

                var oldFilePath =
                    Path.Combine(
                        uploadsFolder,
                        oldFileName);

                if (System.IO.File.Exists(
                        oldFilePath))
                {
                    System.IO.File.Delete(
                        oldFilePath);
                }
            }

            await _signInManager.RefreshSignInAsync(
                user);

            TempData["SuccessMessage"] =
                "Your profile photo has been updated successfully.";

            return RedirectToAction(
                nameof(Profile));
        }

        // =========================================================
        // CHANGE PASSWORD
        // =========================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ProfileVM vm)
        {
            // ProfileVM also contains required profile fields.
            // The password form doesn't submit them.
            ModelState.Clear();

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            if (string.IsNullOrWhiteSpace(
                    vm.CurrentPassword))
            {
                ModelState.AddModelError(
                    nameof(vm.CurrentPassword),
                    "Current password is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    vm.NewPassword))
            {
                ModelState.AddModelError(
                    nameof(vm.NewPassword),
                    "New password is required.");
            }

            if (string.IsNullOrWhiteSpace(
                    vm.ConfirmNewPassword))
            {
                ModelState.AddModelError(
                    nameof(vm.ConfirmNewPassword),
                    "Please confirm your new password.");
            }

            if (!string.Equals(
                    vm.NewPassword,
                    vm.ConfirmNewPassword,
                    StringComparison.Ordinal))
            {
                ModelState.AddModelError(
                    nameof(vm.ConfirmNewPassword),
                    "The new passwords do not match.");
            }

            if (!ModelState.IsValid)
            {
                vm.FirstName =
                    user.FirstName;

                vm.LastName =
                    user.LastName;

                vm.Email =
                    user.Email ?? string.Empty;

                vm.PhoneNumber =
                    user.PhoneNumber;

                vm.Nationality =
                    user.Nationality;

                vm.PassportOrNationalId =
                    user.PassportOrNationalId;

                vm.ProfileImagePath =
                    user.ProfileImagePath;

                return View(
                    "Profile",
                    vm);
            }

            var result =
                await _userManager.ChangePasswordAsync(
                    user,
                    vm.CurrentPassword!,
                    vm.NewPassword!);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(
                        nameof(vm.NewPassword),
                        error.Description);
                }

                vm.FirstName =
                    user.FirstName;

                vm.LastName =
                    user.LastName;

                vm.Email =
                    user.Email ?? string.Empty;

                vm.PhoneNumber =
                    user.PhoneNumber;

                vm.Nationality =
                    user.Nationality;

                vm.PassportOrNationalId =
                    user.PassportOrNationalId;

                vm.ProfileImagePath =
                    user.ProfileImagePath;

                return View(
                    "Profile",
                    vm);
            }

            await _signInManager.RefreshSignInAsync(
                user);

            TempData["SuccessMessage"] =
                "Your password has been updated successfully.";

            return RedirectToAction(
                nameof(Profile));
        }

        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(
                new ForgotPasswordVM());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(
            ForgotPasswordVM vm)
        {
            Console.WriteLine(
                "========== FORGOT PASSWORD START ==========");

            Console.WriteLine(
                $"EMAIL ENTERED: {vm.Email}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine(
                    "MODEL STATE INVALID");

                return View(vm);
            }

            var user =
                await _userManager.FindByEmailAsync(
                    vm.Email);

            Console.WriteLine(
                $"USER FOUND: {user != null}");

            if (user == null)
            {
                Console.WriteLine(
                    "USER NOT FOUND");

                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }

            var confirmed =
                await _userManager.IsEmailConfirmedAsync(
                    user);

            Console.WriteLine(
                $"EMAIL CONFIRMED: {confirmed}");

            if (!confirmed)
            {
                Console.WriteLine(
                    "EMAIL NOT CONFIRMED");

                return RedirectToAction(
                    nameof(ForgotPasswordConfirmation));
            }

            var token =
                await _userManager
                    .GeneratePasswordResetTokenAsync(
                        user);

            Console.WriteLine(
                "RESET TOKEN GENERATED");

            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new
                {
                    email = user.Email,
                    token = token
                },
                Request.Scheme);

            Console.WriteLine(
                $"RESET LINK GENERATED: {resetLink}");

            var emailBody = $@"
                <h2>Reset your EMA Hotel password</h2>

                <p>Hello {user.FirstName},</p>

                <p>
                    We received a request to reset the password
                    for your EMA Hotel account.
                </p>

                <p>
                    Click the button below to choose a new password:
                </p>

                <p>
                    <a href=""{resetLink}""
                       style=""display:inline-block;
                              padding:12px 24px;
                              background-color:#b08d57;
                              color:white;
                              text-decoration:none;
                              border-radius:6px;"">
                        Reset Password
                    </a>
                </p>

                <p>
                    If you did not request a password reset,
                    you can safely ignore this email.
                </p>

                <p>
                    Best regards,<br/>
                    EMA Hotel Team
                </p>
            ";

            Console.WriteLine(
                "STARTING EMAIL SEND...");

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Reset your EMA Hotel password",
                emailBody);

            Console.WriteLine(
                "EMAIL SEND COMPLETED");

            Console.WriteLine(
                "========== FORGOT PASSWORD END ==========");

            return RedirectToAction(
                nameof(ForgotPasswordConfirmation));
        }

        // =========================================================
        // FORGOT PASSWORD CONFIRMATION
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // =========================================================
        // RESET PASSWORD - GET
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(
            string? email,
            string? token)
        {
            if (string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(token))
            {
                return RedirectToAction(
                    nameof(ForgotPassword));
            }

            var vm = new ResetPasswordVM
            {
                Email = email,
                Token = token
            };

            return View(vm);
        }

        // =========================================================
        // RESET PASSWORD - POST
        // =========================================================

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
            ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user =
                await _userManager.FindByEmailAsync(
                    vm.Email);

            if (user == null)
            {
                return RedirectToAction(
                    nameof(ResetPasswordConfirmation));
            }

            var result =
                await _userManager.ResetPasswordAsync(
                    user,
                    vm.Token,
                    vm.NewPassword);

            if (result.Succeeded)
            {
                return RedirectToAction(
                    nameof(ResetPasswordConfirmation));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(vm);
        }

        // =========================================================
        // RESET PASSWORD CONFIRMATION
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // =========================================================
        // CONFIRM EMAIL
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ConfirmEmail(
            string? userId,
            string? token)
        {
            if (string.IsNullOrEmpty(userId) ||
                string.IsNullOrEmpty(token))
            {
                return RedirectToAction(
                    nameof(Login));
            }

            var vm = new ConfirmEmailVM
            {
                UserId = userId,
                Token = token
            };

            return View(vm);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmEmail(
            ConfirmEmailVM vm)
        {
            if (string.IsNullOrEmpty(vm.UserId) ||
                string.IsNullOrEmpty(vm.Token))
            {
                return RedirectToAction(
                    nameof(Login));
            }

            var user =
                await _userManager.FindByIdAsync(
                    vm.UserId);

            if (user == null)
            {
                return RedirectToAction(
                    nameof(Login));
            }

            var result =
                await _userManager.ConfirmEmailAsync(
                    user,
                    vm.Token);

            if (result.Succeeded)
            {
                return RedirectToAction(
                    nameof(EmailConfirmed));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description);
            }

            return View(vm);
        }

        // =========================================================
        // EMAIL CONFIRMATION PAGES
        // =========================================================

        [AllowAnonymous]
        [HttpGet]
        public IActionResult EmailConfirmationSent()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult EmailConfirmed()
        {
            return View();
        }

        // =========================================================
        // STAY PREFERENCES
        // =========================================================

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStayPreferences(
            StayPreferencesVM vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] =
                    "Please check your preferences and try again.";

                return RedirectToAction(nameof(Profile));
            }

            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            user.BreakfastPreference =
                vm.BreakfastPreference;

            user.AirportPickupPreference =
                vm.AirportPickupPreference;

            user.LateCheckoutPreference =
                vm.LateCheckoutPreference;

            user.PreferredRoomView =
                vm.PreferredRoomView;

            user.SpecialRequests =
                vm.SpecialRequests;

            var result =
                await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    TempData["ErrorMessage"] +=
                        error.Description + " ";
                }

                return RedirectToAction(nameof(Profile));
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] =
                "Your stay preferences have been updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        // =========================================================
        // BUILD PROFILE VM
        // =========================================================

        private async Task<ProfileVM> BuildProfileVM()
        {
            var user =
                await _userManager.GetUserAsync(User);

            if (user == null)
            {
                throw new InvalidOperationException(
                    "User not found.");
            }

            return new ProfileVM
            {
                FirstName = user.FirstName,

                LastName = user.LastName,

                Email = user.Email ?? string.Empty,

                PhoneNumber = user.PhoneNumber,

                Nationality = user.Nationality,

                PassportOrNationalId =
                    user.PassportOrNationalId,

                ProfileImagePath =
                    user.ProfileImagePath,

                BreakfastPreference =
                    user.BreakfastPreference,

                AirportPickupPreference =
                    user.AirportPickupPreference,

                LateCheckoutPreference =
                    user.LateCheckoutPreference,

                PreferredRoomView =
                    user.PreferredRoomView,

                SpecialRequests =
                    user.SpecialRequests
            };
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> AddCard()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return Challenge();

            if (user.PaymobCardToken != null)
            {
                TempData["Error"] = "You already have a saved card.";
                return RedirectToAction(nameof(Profile));
            }

            var clientSecret =
                await _paymobService.CreateCardSavingCheckoutAsync(
                    user.Id,
                    user.Email ?? string.Empty,
                    user.PhoneNumber ?? string.Empty,
                    user.FirstName,
                    user.LastName);

            return Content(clientSecret);
        }
    }
}