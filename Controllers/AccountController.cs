using Hotel_MVC.Models;
using Hotel_MVC.ViewModels.AccountVM;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Hotel_MVC.Services;

namespace Hotel_MVC.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

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
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

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
                <p>Thank you for creating an account with us.</p>
                <p>Please confirm your email address by clicking the button below:</p>

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

                <p>If you did not create this account, you can safely ignore this email.</p>

                <p>Best regards,<br/>EMA Hotel Team</p>
            ";

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Confirm your EMA Hotel account",
                emailBody);

            return RedirectToAction(
                nameof(EmailConfirmationSent));
        }


        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            return View(new LoginVM { ReturnUrl = returnUrl });
        }

        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Login(LoginVM model, string? returnUrl = null)
        //{
        //    if (!ModelState.IsValid) 
        //        return View(model);

        //    var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

        //    if (result.Succeeded)
        //    {
        //        var user = await _userManager.FindByEmailAsync(model.Email);

        //        if (await _userManager.IsInRoleAsync(user, "Receptionist") && !await _userManager.IsInRoleAsync(user, "Admin"))
        //        {
        //            return RedirectToAction("Index", "Receptionist");
        //        }
        //        else if (await _userManager.IsInRoleAsync(user, "Admin"))
        //        {
        //            return RedirectToAction("Index", "AdminDashboard");
        //        }

        //        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        //        {
        //            return Redirect(returnUrl);
        //        }

        //        return RedirectToAction("Index", "Home");
        //    }

        //    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        //    return View(model);
        //}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(model.Email);


                if (await _userManager.IsInRoleAsync(user, "Receptionist") && !await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Receptionist");
                }
                else if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "AdminDashboard");
                }


                if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                {
                    return Redirect(model.ReturnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            var redirectUrl = Url.Action(
                nameof(ExternalLoginCallback),
                "Account",
                new { returnUrl });

            var properties = _signInManager.ConfigureExternalAuthenticationProperties(
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

                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();

            if (info == null)
            {
                return RedirectToAction(nameof(Login), new
                {
                    returnUrl
                });
            }

            var result = await _signInManager.ExternalLoginSignInAsync(
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
                return RedirectToAction(nameof(Login), new
                {
                    returnUrl
                });
            }

            var email = info.Principal.FindFirst(
                System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Google did not provide an email address.");

                return RedirectToAction(nameof(Login), new
                {
                    returnUrl
                });
            }

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = info.Principal.FindFirst(
                        System.Security.Claims.ClaimTypes.GivenName)?.Value ?? "",
                    LastName = info.Principal.FindFirst(
                        System.Security.Claims.ClaimTypes.Surname)?.Value ?? ""
                };

                var createResult = await _userManager.CreateAsync(user);

                if (!createResult.Succeeded)
                {
                    foreach (var error in createResult.Errors)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            error.Description);
                    }

                    return RedirectToAction(nameof(Login), new
                    {
                        returnUrl
                    });
                }

                await _userManager.AddToRoleAsync(user, "User");
            }

            var addLoginResult = await _userManager.AddLoginAsync(
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

                return RedirectToAction(nameof(Login), new
                {
                    returnUrl
                });
            }

            await _signInManager.SignInAsync(
                user,
                isPersistent: false);

            return await RedirectAfterLogin(returnUrl);
        }

        private async Task<IActionResult> RedirectAfterLogin(string? returnUrl)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user != null)
            {
                if (await _userManager.IsInRoleAsync(user, "Receptionist") &&
                    !await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "Receptionist");
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Index", "AdminDashboard");
                }
            }

            if (!string.IsNullOrEmpty(returnUrl) &&
                Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }


        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            var vm = new ProfileVM
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Nationality = user.Nationality,
                PassportOrNationalId = user.PassportOrNationalId
            };

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            user.FirstName = vm.FirstName;
            user.LastName = vm.LastName;
            user.PhoneNumber = vm.PhoneNumber;
            user.Nationality = vm.Nationality;

            // Keep Passport/ID controlled by the server.
            // Do not update it from the posted form.

            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }

                return View(vm);
            }

            TempData["SuccessMessage"] = "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ProfileVM vm)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                return NotFound();

            // Validate only the password fields
            if (string.IsNullOrWhiteSpace(vm.CurrentPassword))
            {
                ModelState.AddModelError(
                    nameof(vm.CurrentPassword),
                    "Current password is required.");
            }

            if (string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                ModelState.AddModelError(
                    nameof(vm.NewPassword),
                    "New password is required.");
            }

            if (string.IsNullOrWhiteSpace(vm.ConfirmNewPassword))
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
                // Restore profile information because the same VM is used by Profile.cshtml
                vm.FirstName = user.FirstName;
                vm.LastName = user.LastName;
                vm.Email = user.Email ?? string.Empty;
                vm.PhoneNumber = user.PhoneNumber;
                vm.Nationality = user.Nationality;
                vm.PassportOrNationalId = user.PassportOrNationalId;

                return View("Profile", vm);
            }

            var result = await _userManager.ChangePasswordAsync(
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

                // Restore profile information
                vm.FirstName = user.FirstName;
                vm.LastName = user.LastName;
                vm.Email = user.Email ?? string.Empty;
                vm.PhoneNumber = user.PhoneNumber;
                vm.Nationality = user.Nationality;
                vm.PassportOrNationalId = user.PassportOrNationalId;

                return View("Profile", vm);
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["SuccessMessage"] =
                "Your password has been updated successfully.";

            return RedirectToAction(nameof(Profile));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordVM());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordVM vm)
        {
            Console.WriteLine("========== FORGOT PASSWORD START ==========");
            Console.WriteLine($"EMAIL ENTERED: {vm.Email}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("MODEL STATE INVALID");
                return View(vm);
            }

            var user = await _userManager.FindByEmailAsync(vm.Email);

            Console.WriteLine($"USER FOUND: {user != null}");

            if (user == null)
            {
                Console.WriteLine("USER NOT FOUND");
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var confirmed = await _userManager.IsEmailConfirmedAsync(user);

            Console.WriteLine($"EMAIL CONFIRMED: {confirmed}");

            if (!confirmed)
            {
                Console.WriteLine("EMAIL NOT CONFIRMED");
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            Console.WriteLine("RESET TOKEN GENERATED");

            var resetLink = Url.Action(
                nameof(ResetPassword),
                "Account",
                new
                {
                    email = user.Email,
                    token = token
                },
                Request.Scheme);

            Console.WriteLine($"RESET LINK GENERATED: {resetLink}");

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

            Console.WriteLine("STARTING EMAIL SEND...");

            await _emailSender.SendEmailAsync(
                user.Email!,
                "Reset your EMA Hotel password",
                emailBody);

            Console.WriteLine("EMAIL SEND COMPLETED");

            Console.WriteLine("========== FORGOT PASSWORD END ==========");

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPassword(
    string? email,
    string? token)
        {
            if (string.IsNullOrEmpty(email) ||
                string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(ForgotPassword));
            }

            var vm = new ResetPasswordVM
            {
                Email = email,
                Token = token
            };

            return View(vm);
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(
    ResetPasswordVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var user = await _userManager.FindByEmailAsync(vm.Email);

            if (user == null)
            {
                return RedirectToAction(nameof(ResetPasswordConfirmation));
            }

            var result = await _userManager.ResetPasswordAsync(
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

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult ConfirmEmail(string? userId, string? token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction(nameof(Login));
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
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailVM vm)
        {
            if (string.IsNullOrEmpty(vm.UserId) || string.IsNullOrEmpty(vm.Token))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.FindByIdAsync(vm.UserId);

            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ConfirmEmailAsync(user, vm.Token);

            if (result.Succeeded)
            {
                return RedirectToAction(nameof(EmailConfirmed));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(vm);
        }

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

        private async Task<ProfileVM> BuildProfileVM()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
                throw new InvalidOperationException("User not found.");
            
            return new ProfileVM
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                Nationality = user.Nationality,
                PassportOrNationalId = user.PassportOrNationalId
            };
        }


    }
}

