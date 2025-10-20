using System.Collections.Generic;
using System.Threading.Tasks;
using StarEvents.Models.ViewModels;
using StarEvents.Models.Domain;

namespace StarEvents.Services.Interfaces
{
    public interface IEventService
    {
        Task<List<EventViewModel>> ListAsync(); // upcoming
        Task<List<EventViewModel>> ListAllPublishedAsync(); // new: all published
        Task<EventViewModel> GetByIdAsync(int id);
        Task<(bool Success, string ErrorMessage)> CreateAsync(EventViewModel model, int organizerId);
        Task<(bool Success, string ErrorMessage)> UpdateAsync(EventViewModel model);
        Task<(bool Success, string ErrorMessage)> DeleteAsync(int id);
        Task<List<EventViewModel>> GetEventsByOrganizerAsync(int organizerId);
        Task<List<EventViewModel>> SearchAsync(EventSearchViewModel criteria);

        // New methods for organizer UI
        Task<List<Venue>> ListAllVenuesAsync();

        // Standard organizer create (ViewModel + organizer id)
        Task<(bool IsSuccess, string ErrorMessage)> CreateEventAsync(EventCreateViewModel model, int organizerId);

        // Overload used by EventController (domain Event + ticket types + discounts)
        Task<(bool IsSuccess, string ErrorMessage)> CreateEventAsync(Event ev, List<TicketTypeCreateViewModel> ticketTypes, List<DiscountCreateViewModel> discounts);
    }
}