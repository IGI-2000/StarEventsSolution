using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using StarEvents.Data;
using StarEvents.Models.Domain;
using StarEvents.Models.ViewModels;
using StarEvents.Services.Interfaces;

namespace StarEvents.Services.Implementations
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _db;
        public AdminService(ApplicationDbContext db) { _db = db; }

        public async Task<SalesReportViewModel> GetSalesReportAsync(DateTime from, DateTime to)
        {
            var payments = await _db.Payments
                .AsNoTracking()
                .Where(p => DbFunctions.TruncateTime(p.PaymentDate) >= from.Date && DbFunctions.TruncateTime(p.PaymentDate) <= to.Date)
                .ToListAsync();

            var bookingIds = payments.Select(p => p.BookingId).Distinct().ToList();

            var details = await _db.BookingDetails
                .AsNoTracking()
                .Include(d => d.Booking)
                .Include(d => d.TicketType)
                .Where(d => bookingIds.Contains(d.BookingId))
                .ToListAsync();

            var groups = details
                .GroupBy(d => d.Booking.EventId)
                .ToList();

            var items = new List<SalesReportItemViewModel>();
            foreach (var g in groups)
            {
                var ev = await _db.Events.FindAsync(g.Key);
                items.Add(new SalesReportItemViewModel
                {
                    EventId = g.Key,
                    EventName = ev?.EventName ?? "(deleted)",
                    EventDate = ev?.EventDate ?? DateTime.MinValue,
                    TicketsSold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Subtotal)
                });
            }

            return new SalesReportViewModel
            {
                From = from,
                To = to,
                TotalRevenue = payments.Sum(p => p.Amount),
                TotalTicketsSold = details.Sum(d => d.Quantity),
                Items = items.OrderByDescending(i => i.Revenue).ToList()
            };
        }

        public async Task<List<UserViewModel>> GetUsersAsync()
        {
            var users = new List<UserViewModel>();

            var customers = await _db.Customers.AsNoTracking().ToListAsync();
            users.AddRange(customers.Select(c => new UserViewModel
            {
                Id = c.Id,
                Email = c.Email,
                Name = c.FirstName + " " + c.LastName,
                Role = "Customer",
                IsActive = c.IsActive
            }));

            var organizers = await _db.EventOrganizers.AsNoTracking().ToListAsync();
            users.AddRange(organizers.Select(o => new UserViewModel
            {
                Id = o.Id,
                Email = o.Email,
                Name = o.FirstName + " " + o.LastName,
                Role = "Organizer",
                IsActive = o.IsActive
            }));

            var admins = await _db.Admins.AsNoTracking().ToListAsync();
            users.AddRange(admins.Select(a => new UserViewModel
            {
                Id = a.Id,
                Email = a.Email,
                Name = a.FirstName + " " + a.LastName,
                Role = "Admin",
                IsActive = a.IsActive
            }));

            return users.OrderBy(u => u.Role).ThenBy(u => u.Name).ToList();
        }

        public async Task<List<DiscountViewModel>> GetDiscountsAsync()
        {
            // Map to existing Discount domain property names to avoid schema changes
            return await _db.Discounts
                .AsNoTracking()
                .Select(d => new DiscountViewModel
                {
                    DiscountId = d.DiscountId,
                    Code = d.DiscountCode,
                    Description = d.Description,
                    Percentage = (decimal?)d.DiscountPercentage,
                    Amount = d.MaxDiscountAmount,
                    StartDate = (DateTime?)DbFunctions.TruncateTime(d.ValidFrom),
                    EndDate = (DateTime?)DbFunctions.TruncateTime(d.ValidTo),
                    IsActive = d.IsActive,
                    UsageLimit = d.MaxUsageCount,
                    TimesUsed = d.CurrentUsageCount
                }).ToListAsync();
        }

        public async Task<DiscountViewModel> GetDiscountAsync(int id)
        {
            var d = await _db.Discounts.FindAsync(id);
            if (d == null) return null;
            return new DiscountViewModel
            {
                DiscountId = d.DiscountId,
                Code = d.DiscountCode,
                Description = d.Description,
                Percentage = (decimal?)d.DiscountPercentage,
                Amount = d.MaxDiscountAmount,
                StartDate = d.ValidFrom,
                EndDate = d.ValidTo,
                IsActive = d.IsActive,
                UsageLimit = d.MaxUsageCount,
                TimesUsed = d.CurrentUsageCount
            };
        }

        public async Task<(bool Success, string Error)> CreateOrUpdateDiscountAsync(DiscountViewModel vm)
        {
            if (vm == null) return (false, "Invalid data");
            if (string.IsNullOrWhiteSpace(vm.Code)) return (false, "Code required");

            var existing = vm.DiscountId == 0 ? null : await _db.Discounts.FindAsync(vm.DiscountId);
            if (existing == null)
            {
                existing = new Discount
                {
                    DiscountCode = vm.Code.Trim(),
                    Description = vm.Description,
                    DiscountPercentage = vm.Percentage ?? 0m,
                    MaxDiscountAmount = vm.Amount,
                    ValidFrom = vm.StartDate ?? DateTime.UtcNow,
                    ValidTo = vm.EndDate ?? DateTime.UtcNow.AddYears(1),
                    IsActive = vm.IsActive,
                    MaxUsageCount = vm.UsageLimit,
                    CurrentUsageCount = vm.TimesUsed,
                    Type = vm.Percentage.HasValue ? DiscountType.Percentage : DiscountType.FixedAmount
                };
                _db.Discounts.Add(existing);
            }
            else
            {
                existing.DiscountCode = vm.Code.Trim();
                existing.Description = vm.Description;
                existing.DiscountPercentage = vm.Percentage ?? 0m;
                existing.MaxDiscountAmount = vm.Amount;
                existing.ValidFrom = vm.StartDate ?? existing.ValidFrom;
                existing.ValidTo = vm.EndDate ?? existing.ValidTo;
                existing.IsActive = vm.IsActive;
                existing.MaxUsageCount = vm.UsageLimit;
                // Do not overwrite CurrentUsageCount unless explicitly desired
                _db.Entry(existing).State = EntityState.Modified;
            }

            await _db.SaveChangesAsync();
            return (true, null);
        }

        public async Task<bool> DeleteDiscountAsync(int id)
        {
            var d = await _db.Discounts.FindAsync(id);
            if (d == null) return false;
            _db.Discounts.Remove(d);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<EventAdminViewModel>> GetEventsForAdminAsync()
        {
            var events = await _db.Events
                .AsNoTracking()
                .Include(e => e.Venue)
                .ToListAsync();

            var result = new List<EventAdminViewModel>();
            foreach (var e in events)
            {
                var ticketTypeCount = await _db.TicketTypes.CountAsync(tt => tt.EventId == e.EventId);
                var bookingsCount = await _db.Bookings.CountAsync(b => b.EventId == e.EventId && b.Status != Models.Domain.BookingStatus.Cancelled);

                result.Add(new EventAdminViewModel
                {
                    EventId = e.EventId,
                    EventName = e.EventName,
                    EventDate = e.EventDate,
                    VenueName = e.Venue?.VenueName,
                    TotalTicketTypes = ticketTypeCount,
                    TotalBookings = bookingsCount
                });
            }

            return result.OrderByDescending(x => x.EventDate).ToList();
        }
    }
}