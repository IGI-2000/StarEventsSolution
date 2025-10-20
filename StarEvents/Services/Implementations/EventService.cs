using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Data.Entity;
using StarEvents.Data;
using StarEvents.Models.Domain;
using StarEvents.Models.ViewModels;
using StarEvents.Repositories.Interfaces;
using StarEvents.Services.Interfaces;

namespace StarEvents.Services.Implementations
{
    public class EventService : IEventService
    {
        private readonly IEventRepository _eventRepository;
        private readonly ApplicationDbContext _context;

        public EventService(IEventRepository eventRepository, ApplicationDbContext context)
        {
            _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<List<EventViewModel>> ListAsync()
        {
            var events = await _eventRepository.GetUpcomingEventsAsync();
            return events.Select(MapToViewModel).ToList();
        }

        public async Task<List<EventViewModel>> ListAllPublishedAsync()
        {
            var events = await _eventRepository.GetPublishedEventsAsync();
            return events.Select(MapToViewModel).ToList();
        }

        public async Task<EventViewModel> GetByIdAsync(int id)
        {
            var ev = await _eventRepository.GetByIdAsync(id);
            return ev == null ? null : MapToViewModel(ev);
        }

        public async Task<(bool Success, string ErrorMessage)> CreateAsync(EventViewModel model, int organizerId)
        {
            return (false, "Not implemented");
        }

        public async Task<(bool Success, string ErrorMessage)> UpdateAsync(EventViewModel model)
        {
            return (false, "Not implemented");
        }

        public async Task<(bool Success, string ErrorMessage)> DeleteAsync(int id)
        {
            return (false, "Not implemented");
        }

        public async Task<List<EventViewModel>> GetEventsByOrganizerAsync(int organizerId)
        {
            var events = await _eventRepository.GetEventsByOrganizerAsync(organizerId);
            return events.Select(MapToViewModel).ToList();
        }

        public async Task<List<EventViewModel>> SearchAsync(EventSearchViewModel criteria)
        {
            var events = await _eventRepository.GetPublishedEventsAsync();
            var q = events.AsQueryable();

            if (!string.IsNullOrWhiteSpace(criteria.Category))
                q = q.Where(e => e.Category != null && e.Category.Equals(criteria.Category, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(criteria.SearchKeyword))
            {
                var kw = criteria.SearchKeyword.Trim();
                q = q.Where(e => (e.EventName != null && e.EventName.Contains(kw)) || (e.Description != null && e.Description.Contains(kw)));
            }

            if (!string.IsNullOrWhiteSpace(criteria.Location))
            {
                var loc = criteria.Location.Trim();
                q = q.Where(e => e.Venue != null && (e.Venue.City.Contains(loc) || e.Venue.VenueName.Contains(loc)));
            }

            // single day
            if (criteria.EventDate.HasValue)
            {
                var d = criteria.EventDate.Value.Date;
                q = q.Where(e => DbFunctions.TruncateTime(e.EventDate) == d);
            }

            // range
            if (criteria.StartDate.HasValue)
            {
                q = q.Where(e => DbFunctions.TruncateTime(e.EventDate) >= DbFunctions.TruncateTime(criteria.StartDate.Value));
            }
            if (criteria.EndDate.HasValue)
            {
                q = q.Where(e => DbFunctions.TruncateTime(e.EventDate) <= DbFunctions.TruncateTime(criteria.EndDate.Value));
            }

            return q.Select(MapToViewModel).ToList();
        }

        public async Task<List<Venue>> ListAllVenuesAsync()
        {
            return await _context.Venues.OrderBy(v => v.VenueName).ToListAsync();
        }

        // Existing CreateEventAsync (ViewModel + organizerId)
        public async Task<(bool IsSuccess, string ErrorMessage)> CreateEventAsync(EventCreateViewModel model, int organizerId)
        {
            if (model == null) return (false, "Invalid model");
            if (string.IsNullOrWhiteSpace(model.EventName)) return (false, "Event name required");
            if (model.TotalSeats <= 0) return (false, "Total seats must be > 0");

            if (!model.VenueId.HasValue)
                return (false, "Venue required");

            var venue = await _context.Venues.FindAsync(model.VenueId.Value);
            if (venue == null) return (false, "Venue not found");
            if (model.TotalSeats > venue.Capacity) return (false, $"Total seats cannot exceed venue capacity ({venue.Capacity})");

            // ensure non-nullable DateTime values
            var eventDateValue = model.EventDate.GetValueOrDefault(DateTime.UtcNow);
            var eventEndDateValue = model.EventEndDate ?? eventDateValue;

            var ev = new Event
            {
                EventName = model.EventName?.Trim(),
                Description = model.Description,
                EventDate = eventDateValue,
                EventEndDate = eventEndDateValue,
                Category = model.Category,
                ImageUrl = model.ImageUrl,
                TotalSeats = model.TotalSeats,
                AvailableSeats = model.TotalSeats,
                IsActive = model.IsActive,
                IsPublished = model.IsPublished,
                PublishedDate = model.IsPublished ? (model.PublishedDate ?? DateTime.UtcNow) : (DateTime?)null,
                CreatedDate = DateTime.UtcNow,
                OrganizerId = organizerId,
                VenueId = model.VenueId.Value
            };

            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Events.Add(ev);
                    await _context.SaveChangesAsync();

                    if (model.TicketTypes != null && model.TicketTypes.Any())
                    {
                        foreach (var tt in model.TicketTypes)
                        {
                            if (string.IsNullOrWhiteSpace(tt.TypeName)) continue;
                            var t = new TicketType
                            {
                                EventId = ev.EventId,
                                TypeName = tt.TypeName.Trim(),
                                Price = tt.Price,
                                AvailableQuantity = tt.AvailableQuantity,
                                Description = tt.Description
                            };
                            _context.TicketTypes.Add(t);
                        }
                        await _context.SaveChangesAsync();
                    }

                    if (model.Discounts != null && model.Discounts.Any())
                    {
                        foreach (var d in model.Discounts)
                        {
                            if (string.IsNullOrWhiteSpace(d.DiscountCode)) continue;
                            var disc = new Discount
                            {
                                DiscountCode = d.DiscountCode.Trim(),
                                Description = d.Description,
                                DiscountPercentage = d.DiscountPercentage,
                                MaxDiscountAmount = d.MaxDiscountAmount,
                                ValidFrom = d.ValidFrom,
                                ValidTo = d.ValidTo,
                                IsActive = d.IsActive,
                                EventId = ev.EventId
                            };
                            _context.Discounts.Add(disc);
                        }
                        await _context.SaveChangesAsync();
                    }

                    tx.Commit();
                    return (true, null);
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return (false, ex.Message);
                }
            }
        }

        // New overload: create from domain Event + ticket/disc viewmodels
        public async Task<(bool IsSuccess, string ErrorMessage)> CreateEventAsync(Event ev, List<TicketTypeCreateViewModel> ticketTypes, List<DiscountCreateViewModel> discounts)
        {
            if (ev == null) return (false, "Invalid event");
            if (string.IsNullOrWhiteSpace(ev.EventName)) return (false, "Event name required");
            if (ev.TotalSeats <= 0 && (ticketTypes == null || !ticketTypes.Any()))
                return (false, "Total seats must be > 0 or provide ticket types");

            // If ticketTypes provided, compute total seats from them when TotalSeats is not set or zero
            if ((ev.TotalSeats <= 0) && ticketTypes != null && ticketTypes.Any())
            {
                ev.TotalSeats = ticketTypes.Sum(tt => tt.AvailableQuantity);
                ev.AvailableSeats = ev.TotalSeats;
            }
            else
            {
                ev.AvailableSeats = ev.TotalSeats;
            }

            // ensure EventDate/EventEndDate are valid non-nullable values
            var eventDateValue = ev.EventDate == default(DateTime) ? DateTime.UtcNow : ev.EventDate;
            var eventEndDateValue = ev.EventEndDate == default(DateTime) ? eventDateValue : ev.EventEndDate;

            ev.EventDate = eventDateValue;
            ev.EventEndDate = eventEndDateValue;

            using (var tx = _context.Database.BeginTransaction())
            {
                try
                {
                    _context.Events.Add(ev);
                    await _context.SaveChangesAsync();

                    if (ticketTypes != null && ticketTypes.Any())
                    {
                        foreach (var tt in ticketTypes)
                        {
                            if (string.IsNullOrWhiteSpace(tt.TypeName)) continue;
                            var t = new TicketType
                            {
                                EventId = ev.EventId,
                                TypeName = tt.TypeName.Trim(),
                                Price = tt.Price,
                                AvailableQuantity = tt.AvailableQuantity,
                                Description = tt.Description
                            };
                            _context.TicketTypes.Add(t);
                        }
                        await _context.SaveChangesAsync();
                    }

                    if (discounts != null && discounts.Any())
                    {
                        foreach (var d in discounts)
                        {
                            if (string.IsNullOrWhiteSpace(d.DiscountCode)) continue;
                            var disc = new Discount
                            {
                                DiscountCode = d.DiscountCode.Trim(),
                                Description = d.Description,
                                DiscountPercentage = d.DiscountPercentage,
                                MaxDiscountAmount = d.MaxDiscountAmount,
                                ValidFrom = d.ValidFrom,
                                ValidTo = d.ValidTo,
                                IsActive = d.IsActive,
                                EventId = ev.EventId
                            };
                            _context.Discounts.Add(disc);
                        }
                        await _context.SaveChangesAsync();
                    }

                    tx.Commit();
                    return (true, null);
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    return (false, ex.Message);
                }
            }
        }

        private EventViewModel MapToViewModel(Event e)
        {
            if (e == null) return null;

            var ticketTypeSum = 0;
            try { ticketTypeSum = e.TicketTypes?.Sum(tt => tt.AvailableQuantity) ?? 0; } catch { ticketTypeSum = 0; }

            return new EventViewModel
            {
                EventId = e.EventId,
                EventName = e.EventName,
                Description = e.Description,
                EventDate = e.EventDate,
                EventEndDate = e.EventEndDate,
                Category = e.Category,
                ImageUrl = e.ImageUrl,
                TotalSeats = e.TotalSeats > 0 ? e.TotalSeats : ticketTypeSum,
                AvailableSeats = e.AvailableSeats,
                IsActive = e.IsActive,
                IsPublished = e.IsPublished,
                PublishedDate = e.PublishedDate,
                CreatedDate = e.CreatedDate,
                OrganizerId = e.OrganizerId,
                OrganizerName = e.Organizer != null ? (e.Organizer.CompanyName ?? (e.Organizer.FirstName + " " + e.Organizer.LastName)) : null,
                VenueId = e.VenueId,
                VenueName = e.Venue?.VenueName
            };
        }
    }
}