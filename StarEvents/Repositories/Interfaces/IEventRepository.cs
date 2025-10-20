using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarEvents.Models.Domain;

namespace StarEvents.Repositories.Interfaces
{
    public interface IEventRepository : IRepository<Event>
    {
        Task<IEnumerable<Event>> SearchEventsAsync(string category, DateTime? date, string location, string keyword = null);
        Task<IEnumerable<Event>> GetUpcomingEventsAsync();
        Task<IEnumerable<Event>> GetEventsByOrganizerAsync(int organizerId);
        Task<IEnumerable<Event>> GetPublishedEventsAsync();
    }
}