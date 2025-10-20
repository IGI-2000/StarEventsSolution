using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StarEvents.Models.ViewModels;
using StarEvents.Models.Domain;

namespace StarEvents.Services.Interfaces
{
    public interface IBookingService
    {
        Task<BookingViewModel> GetBookingFormAsync(int eventId);
        Task<(bool Success, string ErrorMessage, int BookingId)> CreateBookingAsync(BookingViewModel model, int customerId);
        Task<Booking> GetBookingEntityAsync(int bookingId);
        Task<(bool Success, string ErrorMessage)> ConfirmBookingPaymentAsync(int bookingId, string transactionId, string cardLast4, string method, decimal amount);

        // New: list bookings for a customer
        Task<List<Booking>> GetCustomerBookingsAsync(int customerId);

        // New: projection helpers for UI (avoid EF proxies)
        Task<List<BookingListItemViewModel>> GetCustomerBookingsForViewAsync(int customerId);
        Task<BookingDetailsViewModel> GetBookingDetailsForViewAsync(int bookingId);
    }
}