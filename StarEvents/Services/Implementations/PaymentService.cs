using System;
using System.Threading.Tasks;
using StarEvents.Data;
using StarEvents.Models.ViewModels;
using StarEvents.Services.Interfaces;
using StarEvents.Models.Domain;

namespace StarEvents.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;

        public PaymentService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // tokenization simulation
        public async Task<(bool Success, string Token, string Last4, string Brand, string ErrorMessage)> TokenizeCardAsync(CardPaymentViewModel card)
        {
            if (string.IsNullOrWhiteSpace(card.CardNumber) || card.CardNumber.Length < 12)
                return (false, null, null, null, "Invalid card number");

            var cleaned = card.CardNumber.Replace(" ", "");
            var last4 = cleaned.Length >= 4 ? cleaned.Substring(cleaned.Length - 4) : cleaned;
            var token = "tok_" + Guid.NewGuid().ToString("N");
            var brand = cleaned.StartsWith("4") ? "Visa" : cleaned.StartsWith("5") ? "MasterCard" : "Card";
            await Task.CompletedTask;
            return (true, token, last4, brand, null);
        }

        // charge simulation: deterministic failure when card ends with 0000 (for testing)
        public async Task<(bool Success, string TransactionId, string ErrorMessage)> ChargeTokenAsync(string token, decimal amount, string description)
        {
            if (string.IsNullOrEmpty(token)) return (false, null, "Invalid token");

            // simulate a small delay
            await Task.Delay(200);

            // simulate failure for tokens generated from PAN ending with "0000"
            if (token.Contains("0000"))
            {
                return (false, null, "Declined by simulated gateway (card refused).");
            }

            var tx = "simtx_" + Guid.NewGuid().ToString("N");
            return (true, tx, null);
        }

        // persist payment record
        public async Task<Payment> SavePaymentRecordAsync(int bookingId, decimal amount, string transactionId, PaymentStatus status, int? paymentMethodId = null, string last4 = null)
        {
            var p = new Payment
            {
                BookingId = bookingId,
                Amount = amount,
                TransactionId = transactionId,
                PaymentDate = DateTime.UtcNow,
                Status = status,
                Last4 = last4,
                PaymentMethodId = paymentMethodId,
                PaymentGatewayResponse = "Simulated"
            };
            _context.Payments.Add(p);
            await _context.SaveChangesAsync();
            return p;
        }

        // high-level: tokenize, optionally save stored card, charge, persist payment
        public async Task<(bool Success, string TransactionId, string ErrorMessage)> ProcessPaymentAsync(int bookingId, PaymentViewModel model)
        {
            var tokenResult = await TokenizeCardAsync(new CardPaymentViewModel
            {
                NameOnCard = model.NameOnCard,
                CardNumber = model.CardNumber,
                ExpiryMonth = model.ExpiryMonth,
                ExpiryYear = model.ExpiryYear,
                CVV = model.CVV,
                SaveCard = model.SaveCard
            });

            if (!tokenResult.Success) return (false, null, tokenResult.ErrorMessage);

            int? storedId = null;
            if (model.SaveCard)
            {
                var stored = new StoredPaymentMethod
                {
                    Token = tokenResult.Token,
                    Last4 = tokenResult.Last4,
                    ExpiryMonth = model.ExpiryMonth,
                    ExpiryYear = model.ExpiryYear,
                    CardBrand = tokenResult.Brand,
                    CustomerId = 0,
                    DisplayName = $"{tokenResult.Brand} ****{tokenResult.Last4}"
                };
                _context.PaymentMethods.Add(stored);
                await _context.SaveChangesAsync();
                storedId = stored.PaymentMethodId;
            }

            var charge = await ChargeTokenAsync(tokenResult.Token, model.Amount, $"Booking {bookingId}");
            if (!charge.Success) return (false, null, charge.ErrorMessage);

            var saved = await SavePaymentRecordAsync(bookingId, model.Amount, charge.TransactionId, PaymentStatus.Success, storedId, tokenResult.Last4);
            return (true, saved.TransactionId, null);
        }
    }
}