using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using StarEvents.Data;
using StarEvents.Models.ViewModels;
using StarEvents.Services.Interfaces;

namespace StarEvents.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authService;
        private readonly ApplicationDbContext _context;

        public AccountController(IAuthenticationService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        // GET: Register
        [HttpGet]
        public ActionResult Register() => View(new RegisterViewModel());

        // POST: Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _authService.RegisterCustomerAsync(model);
            if (result.Success)
            {
                TempData["SuccessMessage"] = "Registration successful. Please login.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError("", result.ErrorMessage ?? "Registration failed");
            return View(model);
        }

        // GET: Login
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _authService.LoginAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError("", result.ErrorMessage ?? "Invalid credentials");
                return View(model);
            }

            // Build authentication ticket
            var userData = $"{result.Role};{result.UserId}";
            var ticket = new FormsAuthenticationTicket(
                1,
                model.Email,
                DateTime.Now,
                DateTime.Now.AddHours(8),
                model.RememberMe,
                userData,
                FormsAuthentication.FormsCookiePath
            );

            var encTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket)
            {
                HttpOnly = true,
                Expires = model.RememberMe ? ticket.Expiration : DateTime.MinValue,
                Path = FormsAuthentication.FormsCookiePath
            };

            Response.Cookies.Add(cookie);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }

        // POST: Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            try
            {
                // Server-side sign out
                _authService?.SignOut();
                FormsAuthentication.SignOut();

                // Remove auth cookie(s) from response - overwrite with expired cookie
                var expiredCookie = new HttpCookie(FormsAuthentication.FormsCookieName, string.Empty)
                {
                    HttpOnly = true,
                    Expires = DateTime.UtcNow.AddDays(-1),
                    Path = FormsAuthentication.FormsCookiePath
                };

                // If original cookie had a domain, also try to remove that variant
                var original = Request.Cookies[FormsAuthentication.FormsCookieName];
                if (original != null && !string.IsNullOrEmpty(original.Domain))
                {
                    var expiredWithDomain = new HttpCookie(FormsAuthentication.FormsCookieName, string.Empty)
                    {
                        HttpOnly = true,
                        Expires = DateTime.UtcNow.AddDays(-1),
                        Path = FormsAuthentication.FormsCookiePath,
                        Domain = original.Domain
                    };
                    Response.Cookies.Set(expiredWithDomain);
                }

                Response.Cookies.Set(expiredCookie);

                // Remove any request cookie copy so AuthenticateRequest won't pick it up for this request
                if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
                    Request.Cookies.Remove(FormsAuthentication.FormsCookieName);

                // Clear session and cache
                try
                {
                    Session?.Clear();
                    Session?.Abandon();
                }
                catch { /* ignore */ }

                Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
                Response.Cache.SetCacheability(HttpCacheability.NoCache);
                Response.Cache.SetNoStore();

                // Reset the current principal to anonymous for this request and thread
                var anonymous = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(string.Empty), new string[] { });
                System.Web.HttpContext.Current.User = anonymous;
                System.Threading.Thread.CurrentPrincipal = anonymous;
            }
            catch
            {
                // ignore logout exceptions
            }

            return RedirectToAction("Login", "Account");
        }

        // GET: Profile
        [HttpGet]
        [Authorize]
        [ActionName("Profile")]
        public ActionResult UserProfile()
        {
            var userEmail = User?.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail))
                return RedirectToAction("Login");

            var user = _context.Set<Models.Domain.BaseUser>().FirstOrDefault(u => u.Email == userEmail);
            if (user == null)
                return RedirectToAction("Login");

            var model = new ProfileViewModel
            {
                UserId = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role.ToString()
            };

            if (user is Models.Domain.Customer c)
            {
                model.Address = c.Address;
                model.LoyaltyPoints = c.LoyaltyPoints;
            }
            else if (user is Models.Domain.EventOrganizer o)
            {
                model.CompanyName = o.CompanyName;
                model.IsVerified = o.IsVerified;
            }
            else if (user is Models.Domain.Admin a)
            {
                model.AdminLevel = a.AdminLevel;
            }

            return View(model);
        }

        // POST: Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        [ActionName("Profile")]
        [Authorize]
        public async Task<ActionResult> UserProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = _context.Set<Models.Domain.BaseUser>().FirstOrDefault(u => u.Id == model.UserId);
            if (user == null) return RedirectToAction("Login");

            user.FirstName = model.FirstName?.Trim();
            user.LastName = model.LastName?.Trim();
            user.PhoneNumber = model.PhoneNumber?.Trim();

            // Password update
            if (!string.IsNullOrWhiteSpace(model.NewPassword))
            {
                if (model.NewPassword != model.ConfirmNewPassword)
                {
                    ModelState.AddModelError("", "New password and confirmation do not match.");
                    return View(model);
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            }

            // Role-specific updates
            if (user is Models.Domain.Customer cust)
            {
                cust.Address = model.Address?.Trim();
            }
            else if (user is Models.Domain.EventOrganizer org)
            {
                org.CompanyName = model.CompanyName?.Trim();
            }
            else if (user is Models.Domain.Admin adm)
            {
                adm.AdminLevel = model.AdminLevel?.Trim();
            }

            _context.Entry(user).State = System.Data.Entity.EntityState.Modified;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction("Profile");
        }
    }
}
