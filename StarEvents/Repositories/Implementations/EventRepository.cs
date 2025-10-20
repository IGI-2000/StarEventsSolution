using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using StarEvents.Data;
using StarEvents.Models.Domain;
using StarEvents.Repositories.Interfaces;

namespace StarEvents.Repositories.Implementations
{
    public class EventRepository : BaseRepository<Event>, IEventRepository
    {
        public EventRepository(ApplicationDbContext context) : base(context) { }

        protected override IQueryable<Event> IncludeNavigationProperties(IQueryable<Event> query)
        {
            return query
                .Include(e => e.Venue)
                .Include(e => e.Organizer)
                .Include(e => e.TicketTypes)
                .Include(e => e.Bookings);
        }

        public override async Task<Event> GetByIdAsync(int id)
        {
            IQueryable<Event> query = _dbSet.AsQueryable().Where(e => e.EventId == id);
            query = IncludeNavigationProperties(query);
            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Event>> GetUpcomingEventsAsync()
        {
            var today = DateTime.Now.Date;
            IQueryable<Event> query = _dbSet
                .AsNoTracking()
                .Where(e => e.IsActive && e.IsPublished && DbFunctions.TruncateTime(e.EventDate) >= today)
                .OrderBy(e => e.EventDate);

            query = IncludeNavigationProperties(query);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetPublishedEventsAsync()
        {
            IQueryable<Event> query = _dbSet
                .AsNoTracking()
                .Where(e => e.IsActive && e.IsPublished)
                .OrderBy(e => e.EventDate);

            query = IncludeNavigationProperties(query);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Event>> SearchEventsAsync(string category, DateTime? date, string location, string keyword = null)
        {
            IQueryable<Event> query = _dbSet.AsQueryable();

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(e => e.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

            if (date.HasValue)
            {
                var d = date.Value.Date;
                query = query.Where(e => DbFunctions.TruncateTime(e.EventDate) == d);
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(e => e.Venue.City.Contains(location) || e.Venue.VenueName.Contains(location));
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.Trim();
                query = query.Where(e => e.EventName.Contains(kw) || e.Description.Contains(kw));
            }

            query = IncludeNavigationProperties(query);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(int organizerId)
        {
            IQueryable<Event> query = _dbSet.AsNoTracking()
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.EventDate);

            query = IncludeNavigationProperties(query);
            return await query.ToListAsync();
        }
    }
}