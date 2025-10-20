using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using StarEvents.Data;
using StarEvents.Models.Domain;
using StarEvents.Models.ViewModels;
using StarEvents.Services.Interfaces;
using BCrypt.Net;

namespace StarEvents.Controllers
{
    [Authorize] // require authentication; per-action checks enforce admin
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ApplicationDbContext _db;

        // Default constructor fallback for MVC runtime
        public AdminController()
        {
            _db = new ApplicationDbContext();
        }

        // DI constructor
        public AdminController(IAdminService adminService, ApplicationDbContext db)
        {
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        private bool IsAdminAccount()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return false;

            if (User.IsInRole("Admin")) return true;

            try
            {
                return _db.Admins.Any(a => a.Email == email);
            }
            catch
            {
                return false;
            }
        }

        // ----------------------------------------------------------------------
        // DASHBOARD
        // ----------------------------------------------------------------------
        public ActionResult Dashboard()
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();

            var vm = new AdminDashboardViewModel
            {
                TotalEvents = _db.Events.Count(),
                ActiveEvents = _db.Events.Count(e => e.IsActive),
                TotalVenues = _db.Venues.Count(),
                TotalCustomers = _db.Customers.Count(),
                TotalOrganizers = _db.EventOrganizers.Count(),
                RecentBookings = _db.Bookings
                    .OrderByDescending(b => b.BookingDate)
                    .Take(10)
                    .Select(b => new RecentBookingViewModel
                    {
                        BookingId = b.BookingId,
                        BookingReference = b.BookingReference,
                        EventName = b.Event != null ? b.Event.EventName : string.Empty,
                        FinalAmount = b.FinalAmount,
                        BookingDate = b.BookingDate
                    })
                    .ToList()
            };

            return View(vm);
        }

        // ----------------------------------------------------------------------
        // EVENT MANAGEMENT
        // ----------------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult> ManageEvents()
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();

            if (_adminService != null)
                return View(await _adminService.GetEventsForAdminAsync());

            var eventsDb = _db.Events
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .OrderByDescending(e => e.EventDate)
                .ToList();

            return View(eventsDb);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleEventStatus(int eventId)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();

            var evt = await _db.Events.FindAsync(eventId);
            if (evt == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction("ManageEvents");
            }

            evt.IsActive = !evt.IsActive;
            _db.Entry(evt).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Event status updated.";
            return RedirectToAction("ManageEvents");
        }

        // ----------------------------------------------------------------------
        // VENUE MANAGEMENT
        // ----------------------------------------------------------------------
        [HttpGet]
        public ActionResult ManageVenues()
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            var venues = _db.Venues.OrderBy(v => v.VenueName).ToList();
            return View(venues);
        }

        [HttpGet]
        public ActionResult CreateVenue()
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            return View(new Venue());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateVenue(Venue model)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            if (!ModelState.IsValid) return View(model);

            _db.Venues.Add(model);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Venue created successfully.";
            return RedirectToAction("ManageVenues");
        }

        [HttpGet]
        public async Task<ActionResult> EditVenue(int id)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            var venue = await _db.Venues.FindAsync(id);
            if (venue == null)
            {
                TempData["ErrorMessage"] = "Venue not found.";
                return RedirectToAction("ManageVenues");
            }
            return View(venue);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditVenue(Venue model)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            if (!ModelState.IsValid) return View(model);

            var venue = await _db.Venues.FindAsync(model.VenueId);
            if (venue == null)
            {
                TempData["ErrorMessage"] = "Venue not found.";
                return RedirectToAction("ManageVenues");
            }

            venue.VenueName = model.VenueName?.Trim();
            venue.Address = model.Address?.Trim();
            venue.City = model.City?.Trim();
            venue.Province = model.Province?.Trim();
            venue.Capacity = model.Capacity;
            venue.ContactNumber = model.ContactNumber?.Trim();
            venue.Facilities = model.Facilities?.Trim();

            _db.Entry(venue).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Venue updated successfully.";
            return RedirectToAction("ManageVenues");
        }

        // ----------------------------------------------------------------------
        // USER MANAGEMENT
        // ----------------------------------------------------------------------
        [HttpGet]
        public ActionResult ManageUsers()
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();

            var customers = _db.Customers
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .ToList();

            var organizers = _db.EventOrganizers
                .OrderBy(o => o.CompanyName)
                .ToList();

            var model = new
            {
                Customers = customers,
                Organizers = organizers
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ToggleUserStatus(int userId, string userType)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();

            if (string.Equals(userType, "Customer", StringComparison.OrdinalIgnoreCase))
            {
                var customer = await _db.Customers.FindAsync(userId);
                if (customer != null)
                {
                    customer.IsActive = !customer.IsActive;
                    _db.Entry(customer).State = EntityState.Modified;
                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Customer status updated.";
                }
            }
            else if (string.Equals(userType, "Organizer", StringComparison.OrdinalIgnoreCase))
            {
                var org = await _db.EventOrganizers.FindAsync(userId);
                if (org != null)
                {
                    org.IsActive = !org.IsActive;
                    _db.Entry(org).State = EntityState.Modified;
                    await _db.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Organizer status updated.";
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Unknown user type.";
            }

            return RedirectToAction("ManageUsers");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyOrganizer(int organizerId)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();

            var org = await _db.EventOrganizers.FindAsync(organizerId);
            if (org == null)
            {
                TempData["ErrorMessage"] = "Organizer not found.";
                return RedirectToAction("ManageUsers");
            }

            org.IsVerified = true;
            _db.Entry(org).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Organizer verified successfully.";
            return RedirectToAction("ManageUsers");
        }

        // ----------------------------------------------------------------------
        // ADMIN ACCOUNT CREATION
        // ----------------------------------------------------------------------
        [HttpGet]
        public ActionResult CreateAdmin()
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateAdmin(RegisterViewModel model, string adminLevel)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            if (!ModelState.IsValid) return View(model);

            var exists = _db.Set<BaseUser>().Any(u => u.Email == model.Email);
            if (exists)
            {
                ModelState.AddModelError("", "Email already registered.");
                return View(model);
            }

            var admin = new Admin
            {
                Email = model.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FirstName = model.FirstName?.Trim(),
                LastName = model.LastName?.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                Role = UserRole.Admin,
                AdminLevel = adminLevel ?? "Admin"
            };

            _db.Admins.Add(admin);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Admin account created successfully.";
            return RedirectToAction("Dashboard");
        }

        // ----------------------------------------------------------------------
        // SALES REPORTS & DISCOUNTS
        // ----------------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult> SalesReport(DateTime? from, DateTime? to)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();

            var f = from ?? DateTime.UtcNow.Date.AddMonths(-1);
            var t = to ?? DateTime.UtcNow.Date;
            var vm = await _adminService.GetSalesReportAsync(f, t);
            return View(vm);
        }

        [HttpGet]
        public async Task<ActionResult> Discounts()
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            return View(await _adminService.GetDiscountsAsync());
        }

        [HttpGet]
        public async Task<ActionResult> EditDiscount(int id = 0)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            if (id == 0) return View(new DiscountViewModel { IsActive = true });
            var vm = await _adminService.GetDiscountAsync(id);
            if (vm == null) return HttpNotFound();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditDiscount(DiscountViewModel vm)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            if (!ModelState.IsValid) return View(vm);

            var r = await _adminService.CreateOrUpdateDiscountAsync(vm);
            if (!r.Success) { ModelState.AddModelError("", r.Error); return View(vm); }

            TempData["SuccessMessage"] = "Discount saved successfully.";
            return RedirectToAction("Discounts");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteDiscount(int id)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();
            await _adminService.DeleteDiscountAsync(id);
            TempData["SuccessMessage"] = "Discount deleted successfully.";
            return RedirectToAction("Discounts");
        }

        // ----------------------------------------------------------------------
        // EXPORT SALES REPORT AS CSV
        // ----------------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult> ExportSalesReport(DateTime? from, DateTime? to)
        {
            if (!IsAdminAccount()) return new HttpUnauthorizedResult();

            var f = from ?? DateTime.UtcNow.Date.AddMonths(-1);
            var t = to ?? DateTime.UtcNow.Date;

            var report = await _adminService.GetSalesReportAsync(f, t);

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Event,EventDate,TicketsSold,Revenue");
            foreach (var it in report.Items)
            {
                var date = it.EventDate == DateTime.MinValue ? "" : it.EventDate.ToString("yyyy-MM-dd");
                sb.AppendLine($"\"{it.EventName}\",{date},{it.TicketsSold},{it.Revenue:F2}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"SalesReport_{f:yyyyMMdd}_{t:yyyyMMdd}.csv";
            return File(bytes, "text/csv", fileName);
        }
    }
}
