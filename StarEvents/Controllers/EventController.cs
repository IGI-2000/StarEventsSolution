using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using StarEvents.Models.ViewModels;
using StarEvents.Services.Interfaces;

namespace StarEvents.Controllers
{
    public class EventController : Controller
    {
        private readonly IEventService _eventService;

        public EventController(IEventService eventService)
        {
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }

        // GET: /Event/  -> show ALL published events
        public async Task<ActionResult> Index()
        {
            var events = await _eventService.ListAllPublishedAsync();
            return View(events);
        }

        // GET: /Event/Details/5
        public async Task<ActionResult> Details(int id)
        {
            var vm = await _eventService.GetByIdAsync(id);
            if (vm == null || !vm.IsPublished || !vm.IsActive) return HttpNotFound();
            return View(vm);
        }

        // GET: /Event/Create
        public async Task<ActionResult> Create()
        {
            var vm = new EventCreateViewModel
            {
                EventDate = DateTime.Now,
                TotalSeats = 100,
                IsActive = true,
                IsPublished = false
            };

            var venues = await _eventService.ListAllVenuesAsync();
            vm.AvailableVenues = venues.Select(v => new SelectListItem
            {
                Value = v.VenueId.ToString(),
                Text = v.VenueName
            }).ToList();

            return View(vm);
        }

        // POST: /Event/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(EventCreateViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                var venues = await _eventService.ListAllVenuesAsync();
                vm.AvailableVenues = venues.Select(v => new SelectListItem
                {
                    Value = v.VenueId.ToString(),
                    Text = v.VenueName
                }).ToList();
                return View(vm);
            }

            // Ensure a venue was selected
            if (!vm.VenueId.HasValue)
            {
                ModelState.AddModelError("", "Please select a venue.");
                var venues = await _eventService.ListAllVenuesAsync();
                vm.AvailableVenues = venues.Select(v => new SelectListItem
                {
                    Value = v.VenueId.ToString(),
                    Text = v.VenueName
                }).ToList();
                return View(vm);
            }

            // ensure non-nullable DateTime assignments
            var eventDateValue = vm.EventDate.GetValueOrDefault(DateTime.UtcNow);
            var eventEndDateValue = vm.EventEndDate ?? eventDateValue;

            var eventToCreate = new StarEvents.Models.Domain.Event
            {
                EventName = vm.EventName,
                Description = vm.Description,
                EventDate = eventDateValue,
                EventEndDate = eventEndDateValue,
                Category = vm.Category,
                ImageUrl = vm.ImageUrl,
                TotalSeats = vm.TotalSeats,
                AvailableSeats = vm.TotalSeats,
                IsActive = vm.IsActive,
                IsPublished = vm.IsPublished,
                PublishedDate = vm.IsPublished ? DateTime.UtcNow : (DateTime?)null,
                VenueId = vm.VenueId.Value,
                OrganizerId = 0 // admin-created or unspecified
            };

            // call overload that accepts domain + lists
            var creationResult = await _eventService.CreateEventAsync(eventToCreate, vm.TicketTypes, vm.Discounts);

            if (creationResult.IsSuccess)
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", "Failed to create event. " + (creationResult.ErrorMessage ?? ""));
            var fallbackVenues = await _eventService.ListAllVenuesAsync();
            vm.AvailableVenues = fallbackVenues.Select(v => new SelectListItem
            {
                Value = v.VenueId.ToString(),
                Text = v.VenueName
            }).ToList();
            return View(vm);
        }

        // GET: /Event/Search
        [HttpGet]
        public async Task<ActionResult> Search(string keyword = null, string category = null, DateTime? date = null, string location = null)
        {
            var criteria = new EventSearchViewModel
            {
                SearchKeyword = keyword,
                Category = category,
                EventDate = date,
                Location = location
            };

            var results = await _eventService.SearchAsync(criteria);
            criteria.Results = results;

            criteria.AvailableCategories = (await _eventService.ListAllPublishedAsync())
                                            .Select(e => e.Category)
                                            .Where(c => !string.IsNullOrWhiteSpace(c))
                                            .Distinct()
                                            .OrderBy(c => c)
                                            .ToList();

            criteria.AvailableLocations = (await _eventService.ListAllPublishedAsync())
                                            .Select(e => e.VenueName)
                                            .Where(v => !string.IsNullOrWhiteSpace(v))
                                            .Distinct()
                                            .OrderBy(v => v)
                                            .ToList();

            return View(criteria);
        }
    }
}