using System;
using System.Threading.Tasks;

namespace StarEvents.Helpers
{
    public class PaymentRequest
    {
        public decimal Amount { get; set; }
        public string CardNumberLast4 { get; set; }
        public string CustomerEmail { get; set; }
        public string Method { get; set; }
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public string TransactionId { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public static class PaymentHelper
    {
        public static async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            await Task.Delay(1000); // simulate latency
            bool success = new Random().Next(100) < 95;

            return new PaymentResult
            {
                Success = success,
                TransactionId = Guid.NewGuid().ToString("N"),
                Message = success ? "Payment successful" : "Payment failed",
                Timestamp = DateTime.UtcNow
            };
        }
    }
}