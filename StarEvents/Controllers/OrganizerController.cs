using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StarEvents.Data;
using StarEvents.Models.Domain;
using StarEvents.Models.ViewModels;
using StarEvents.Services.Interfaces;

namespace StarEvents.Controllers
{
    [Authorize(Roles = "Organizer,Admin")]
    public class OrganizerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEventService _eventService;

        public OrganizerController(ApplicationDbContext context, IEventService eventService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }

        private int GetCurrentUserId()
        {
            if (User.IsInRole("Admin")) return 0; // Admin bypass

            var ci = User?.Identity as System.Security.Claims.ClaimsIdentity;
            if (ci != null)
            {
                var idClaim = ci.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                if (idClaim != null && int.TryParse(idClaim.Value, out var id)) return id;
            }

            var email = User?.Identity?.Name;
            if (string.IsNullOrEmpty(email)) return 0;
            var organizer = _context.EventOrganizers.FirstOrDefault(o => o.Email == email);
            return organizer?.Id ?? 0;
        }

        // ---------- Create Event ----------
        [HttpGet]
        public async Task<ActionResult> CreateEvent()
        {
            var vm = new EventCreateViewModel
            {
                EventDate = DateTime.Now.Date.AddDays(1),
                EventEndDate = DateTime.Now.Date.AddDays(1).AddHours(3),
                // ensure lists exist so view renders input rows
                TicketTypes = new List<TicketTypeCreateViewModel> { new TicketTypeCreateViewModel() },
                Discounts = new List<DiscountCreateViewModel> { new DiscountCreateViewModel { ValidFrom = DateTime.UtcNow.Date, ValidTo = DateTime.UtcNow.Date.AddDays(30) } },
                IsActive = true // default to active so organizer can toggle
            };
            await PopulateVenueSelectList(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> CreateEvent(EventCreateViewModel model)
        {
            await PopulateVenueSelectList(model);

            if (!ModelState.IsValid) return View(model);

            int venueId;
            if (model.UseNewVenue)
            {
                var existing = await _context.Venues.FirstOrDefaultAsync(v => v.VenueName.ToLower() == (model.NewVenueName ?? "").Trim().ToLower());
                if (existing != null) venueId = existing.VenueId;
                else
                {
                    var v = new Venue
                    {
                        VenueName = (model.NewVenueName ?? "").Trim(),
                        Address = model.NewVenueAddress?.Trim()
                    };
                    _context.Venues.Add(v);
                    await _context.SaveChangesAsync();
                    venueId = v.VenueId;
                }
            }
            else if (model.VenueId.HasValue) venueId = model.VenueId.Value;
            else
            {
                ModelState.AddModelError("", "Please select or create a venue.");
                return View(model);
            }

            var organizerId = GetCurrentUserId();

            // Ensure non-nullable domain DateTime values
            var eventDateValue = model.EventDate.GetValueOrDefault(DateTime.UtcNow);
            var eventEndValue = model.EventEndDate ?? eventDateValue;

            var evt = new Event
            {
                EventName = (model.EventName ?? "").Trim(),
                Description = model.Description,
                EventDate = eventDateValue,
                EventEndDate = eventEndValue,
                Category = model.Category,
                ImageUrl = model.ImageUrl,
                TotalSeats = model.TotalSeats, // may be updated later from ticket types
                AvailableSeats = model.TotalSeats,
                IsActive = model.IsActive,
                IsPublished = model.IsPublished,
                PublishedDate = model.IsPublished ? DateTime.UtcNow : (DateTime?)null,
                VenueId = venueId,
                OrganizerId = organizerId
            };

            _context.Events.Add(evt);
            await _context.SaveChangesAsync();

            // Add TicketTypes
            if (model.TicketTypes != null && model.TicketTypes.Any())
            {
                foreach (var tt in model.TicketTypes)
                {
                    if (string.IsNullOrWhiteSpace(tt.TypeName)) continue;
                    _context.TicketTypes.Add(new TicketType
                    {
                        EventId = evt.EventId,
                        TypeName = tt.TypeName.Trim(),
                        Price = tt.Price,
                        AvailableQuantity = tt.AvailableQuantity,
                        Description = tt.Description
                    });
                }
                await _context.SaveChangesAsync();

                // Recalculate totals from ticket types if available
                var sumSeats = await _context.TicketTypes.Where(t => t.EventId == evt.EventId).SumAsync(t => (int?)t.AvailableQuantity) ?? 0;
                if (sumSeats > 0)
                {
                    evt.TotalSeats = sumSeats;
                    evt.AvailableSeats = sumSeats;
                    _context.Entry(evt).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                }
            }

            // Add Discounts
            if (model.Discounts != null && model.Discounts.Any())
            {
                foreach (var d in model.Discounts)
                {
                    if (string.IsNullOrWhiteSpace(d.DiscountCode)) continue;
                    _context.Discounts.Add(new Discount
                    {
                        DiscountCode = d.DiscountCode.Trim(),
                        Description = d.Description,
                        DiscountPercentage = d.DiscountPercentage,
                        MaxDiscountAmount = d.MaxDiscountAmount,
                        ValidFrom = d.ValidFrom,
                        ValidTo = d.ValidTo,
                        IsActive = d.IsActive,
                        EventId = evt.EventId
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Event created successfully.";
            return RedirectToAction("MyEvents");
        }

        // ---------- Edit Event ----------
        [HttpGet]
        public async Task<ActionResult> EditEvent(int id)
        {
            var evt = await _context.Events
                .Include(e => e.TicketTypes)
                .Include(e => e.Discounts)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (evt == null) return HttpNotFound();
            var organizerId = GetCurrentUserId();
            if (!User.IsInRole("Admin") && evt.OrganizerId != organizerId)
                return new HttpUnauthorizedResult();

            var vm = new EventCreateViewModel
            {
                EventId = evt.EventId,
                EventName = evt.EventName,
                Description = evt.Description,
                EventDate = evt.EventDate,
                EventEndDate = evt.EventEndDate,
                Category = evt.Category,
                ImageUrl = evt.ImageUrl,
                TotalSeats = evt.TotalSeats,
                IsActive = evt.IsActive,
                IsPublished = evt.IsPublished,
                PublishedDate = evt.PublishedDate,
                VenueId = evt.VenueId,
                TicketTypes = evt.TicketTypes.Select(tt => new TicketTypeCreateViewModel
                {
                    TypeName = tt.TypeName,
                    Price = tt.Price,
                    AvailableQuantity = tt.AvailableQuantity,
                    Description = tt.Description
                }).ToList(),
                Discounts = evt.Discounts.Select(d => new DiscountCreateViewModel
                {
                    DiscountCode = d.DiscountCode,
                    Description = d.Description,
                    DiscountPercentage = d.DiscountPercentage,
                    MaxDiscountAmount = d.MaxDiscountAmount,
                    ValidFrom = d.ValidFrom,
                    ValidTo = d.ValidTo,
                    IsActive = d.IsActive
                }).ToList()
            };

            if (vm.TicketTypes == null || !vm.TicketTypes.Any()) vm.TicketTypes = new List<TicketTypeCreateViewModel> { new TicketTypeCreateViewModel() };
            if (vm.Discounts == null || !vm.Discounts.Any()) vm.Discounts = new List<DiscountCreateViewModel> { new DiscountCreateViewModel { ValidFrom = DateTime.UtcNow.Date, ValidTo = DateTime.UtcNow.Date.AddDays(30) } };

            await PopulateVenueSelectList(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> EditEvent(EventCreateViewModel model)
        {
            await PopulateVenueSelectList(model);

            // Sanitize incoming collections: remove empty/placeholder rows that cause ModelState to be invalid
            model.TicketTypes = model.TicketTypes?.Where(tt => !string.IsNullOrWhiteSpace(tt?.TypeName)).ToList() ?? new List<TicketTypeCreateViewModel>();
            model.Discounts = model.Discounts?.Where(d => !string.IsNullOrWhiteSpace(d?.DiscountCode)).ToList() ?? new List<DiscountCreateViewModel>();

            // Normalize numeric values to safe defaults
            foreach (var tt in model.TicketTypes)
            {
                if (tt.Price < 0) tt.Price = 0;
                if (tt.AvailableQuantity < 0) tt.AvailableQuantity = 0;
            }
            foreach (var d in model.Discounts)
            {
                if (d.DiscountPercentage < 0) d.DiscountPercentage = 0;
                if (d.MaxDiscountAmount.HasValue && d.MaxDiscountAmount < 0) d.MaxDiscountAmount = 0;
            }

            // Re-validate model after sanitization
            ModelState.Clear();
            TryValidateModel(model);
            if (!ModelState.IsValid)
            {
                // Populate select list again (PopulateVenueSelectList already called above)
                return View(model);
            }

            var evt = await _context.Events
                .Include(e => e.TicketTypes)
                .Include(e => e.Discounts)
                .FirstOrDefaultAsync(e => e.EventId == model.EventId);

            if (evt == null) return HttpNotFound();
            var organizerId = GetCurrentUserId();
            if (!User.IsInRole("Admin") && evt.OrganizerId != organizerId)
                return new HttpUnauthorizedResult();

            int venueId = evt.VenueId;
            if (model.UseNewVenue)
            {
                var existing = await _context.Venues.FirstOrDefaultAsync(v => v.VenueName.ToLower() == (model.NewVenueName ?? "").Trim().ToLower());
                if (existing != null) venueId = existing.VenueId;
                else
                {
                    var v = new Venue
                    {
                        VenueName = (model.NewVenueName ?? "").Trim(),
                        Address = model.NewVenueAddress?.Trim()
                    };
                    _context.Venues.Add(v);
                    await _context.SaveChangesAsync();
                    venueId = v.VenueId;
                }
            }
            else if (model.VenueId.HasValue) venueId = model.VenueId.Value;

            evt.EventName = (model.EventName ?? "").Trim();
            evt.Description = model.Description;
            evt.EventDate = model.EventDate.GetValueOrDefault(evt.EventDate);
            evt.EventEndDate = model.EventEndDate ?? evt.EventEndDate;
            evt.Category = model.Category;
            evt.ImageUrl = model.ImageUrl;

            // compute new total seats from ticket types (preferred)
            int newTotalFromTickets = 0;
            if (model.TicketTypes != null && model.TicketTypes.Any())
            {
                newTotalFromTickets = model.TicketTypes.Sum(t => t.AvailableQuantity);
            }

            var previousTotal = evt.TotalSeats;
            var newTotal = newTotalFromTickets > 0 ? newTotalFromTickets : model.TotalSeats;

            evt.TotalSeats = newTotal;
            // adjust available seats by difference
            evt.AvailableSeats = Math.Max(0, evt.AvailableSeats + (newTotal - previousTotal));
            evt.IsActive = model.IsActive;

            if (!evt.IsPublished && model.IsPublished)
            {
                evt.IsPublished = true;
                evt.PublishedDate = model.PublishedDate ?? DateTime.UtcNow;
            }
            else if (evt.IsPublished && !model.IsPublished)
            {
                evt.IsPublished = false;
                evt.PublishedDate = null;
            }

            evt.VenueId = venueId;

            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    // Replace ticket types: remove existing, add sanitized incoming ones
                    _context.TicketTypes.RemoveRange(evt.TicketTypes);
                    await _context.SaveChangesAsync();

                    if (model.TicketTypes != null && model.TicketTypes.Any())
                    {
                        foreach (var tt in model.TicketTypes)
                        {
                            if (string.IsNullOrWhiteSpace(tt.TypeName)) continue;
                            _context.TicketTypes.Add(new TicketType
                            {
                                EventId = evt.EventId,
                                TypeName = tt.TypeName.Trim(),
                                Price = tt.Price,
                                AvailableQuantity = tt.AvailableQuantity,
                                Description = tt.Description
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    // Replace discounts
                    _context.Discounts.RemoveRange(evt.Discounts);
                    await _context.SaveChangesAsync();

                    if (model.Discounts != null && model.Discounts.Any())
                    {
                        foreach (var d in model.Discounts)
                        {
                            if (string.IsNullOrWhiteSpace(d.DiscountCode)) continue;
                            _context.Discounts.Add(new Discount
                            {
                                DiscountCode = d.DiscountCode.Trim(),
                                Description = d.Description,
                                DiscountPercentage = d.DiscountPercentage,
                                MaxDiscountAmount = d.MaxDiscountAmount,
                                ValidFrom = d.ValidFrom,
                                ValidTo = d.ValidTo,
                                IsActive = d.IsActive,
                                EventId = evt.EventId
                            });
                        }
                        await _context.SaveChangesAsync();
                    }

                    _context.Entry(evt).State = EntityState.Modified;
                    await _context.SaveChangesAsync();
                    tx.Commit();

                    TempData["SuccessMessage"] = "Event updated successfully.";
                    return RedirectToAction("MyEvents");
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    ModelState.AddModelError("", "Failed to update event: " + ex.Message);
                    // repopulate lists for view
                    if (model.TicketTypes == null || !model.TicketTypes.Any()) model.TicketTypes = new List<TicketTypeCreateViewModel> { new TicketTypeCreateViewModel() };
                    if (model.Discounts == null || !model.Discounts.Any()) model.Discounts = new List<DiscountCreateViewModel> { new DiscountCreateViewModel { ValidFrom = DateTime.UtcNow.Date, ValidTo = DateTime.UtcNow.Date.AddDays(30) } };
                    await PopulateVenueSelectList(model);
                    return View(model);
                }
            }
        }

        // ---------- Delete Event ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteEvent(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null) return HttpNotFound();
            if (!User.IsInRole("Admin") && evt.OrganizerId != GetCurrentUserId()) return new HttpUnauthorizedResult();

            try
            {
                _context.TicketTypes.RemoveRange(_context.TicketTypes.Where(tt => tt.EventId == id));
                _context.Discounts.RemoveRange(_context.Discounts.Where(d => d.EventId == id));

                if (_context.Bookings.Any(b => b.EventId == id))
                {
                    TempData["ErrorMessage"] = "Cannot delete event with existing bookings.";
                    return RedirectToAction("MyEvents");
                }

                _context.Events.Remove(evt);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Event deleted successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to delete event: " + ex.Message;
            }

            return RedirectToAction("MyEvents");
        }

        // ---------- Toggle Publish ----------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> TogglePublish(int id)
        {
            var evt = await _context.Events.FindAsync(id);
            if (evt == null) return HttpNotFound();
            if (!User.IsInRole("Admin") && evt.OrganizerId != GetCurrentUserId()) return new HttpUnauthorizedResult();

            try
            {
                evt.IsPublished = !evt.IsPublished;
                evt.PublishedDate = evt.IsPublished ? (DateTime?)DateTime.UtcNow : null;
                _context.Entry(evt).State = EntityState.Modified;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = evt.IsPublished ? "Event published." : "Event unpublished (draft).";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to change publish status: " + ex.Message;
            }

            return RedirectToAction("MyEvents");
        }

        // ---------- Revenue Report ----------
        [HttpGet]
        public async Task<ActionResult> RevenueReport(DateTime? from, DateTime? to)
        {
            var f = from ?? DateTime.UtcNow.Date.AddMonths(-1);
            var t = to ?? DateTime.UtcNow.Date;
            var organizerId = GetCurrentUserId();

            var events = await _context.Events
                .Where(e => organizerId == 0 || e.OrganizerId == organizerId)
                .ToListAsync();

            var eventIds = events.Select(e => e.EventId).ToList();

            var report = await _context.BookingDetails
                .Where(d => d.Booking.BookingDate >= f && d.Booking.BookingDate <= t && eventIds.Contains(d.Booking.EventId))
                .GroupBy(d => d.Booking.Event)
                .Select(g => new
                {
                    EventId = g.Key.EventId,
                    EventName = g.Key.EventName,
                    TicketsSold = g.Sum(x => (int?)x.Quantity) ?? 0,
                    Revenue = g.Sum(x => (decimal?)x.Subtotal) ?? 0m
                })
                .ToListAsync();

            ViewBag.From = f;
            ViewBag.To = t;
            return View(report);
        }

        private async Task PopulateVenueSelectList(EventCreateViewModel vm)
        {
            var venues = await _context.Venues
                .AsNoTracking()
                .OrderBy(v => v.VenueName)
                .Select(v => new { v.VenueId, v.VenueName })
                .ToListAsync();

            vm.AvailableVenues = venues.Select(v => new SelectListItem
            {
                Value = v.VenueId.ToString(),
                Text = v.VenueName,
                Selected = vm.VenueId.HasValue && vm.VenueId.Value == v.VenueId
            }).ToList();
        }

        // ---------- My Events ----------
        [HttpGet]
        public async Task<ActionResult> MyEvents()
        {
            var organizerId = GetCurrentUserId();

            // Admin should see all events
            List<EventViewModel> events;
            if (User.IsInRole("Admin") || organizerId == 0)
            {
                events = await _eventService.ListAllPublishedAsync(); // or ListAsync for upcoming, adjust as needed
            }
            else
            {
                events = await _eventService.GetEventsByOrganizerAsync(organizerId);
            }

            return View(events);
        }
    }
}