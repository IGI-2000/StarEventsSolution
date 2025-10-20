using System.Threading.Tasks;
using StarEvents.Models.ViewModels;
using StarEvents.Models.Domain;

namespace StarEvents.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<(bool Success, string Token, string Last4, string Brand, string ErrorMessage)> TokenizeCardAsync(CardPaymentViewModel card);
        Task<(bool Success, string TransactionId, string ErrorMessage)> ChargeTokenAsync(string token, decimal amount, string description);

        // High-level convenience: tokenizes, charges and stores payment record (keeps token handling inside service)
        Task<(bool Success, string TransactionId, string ErrorMessage)> ProcessPaymentAsync(int bookingId, PaymentViewModel model);

        // Persist a payment record separately (if controller/service calls directly)
        Task<Payment> SavePaymentRecordAsync(int bookingId, decimal amount, string transactionId, PaymentStatus status, int? paymentMethodId = null, string last4 = null);
    }
}