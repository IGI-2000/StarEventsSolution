using System;

namespace StarEvents.Models.Domain
{
    public enum UserRole { Admin, Customer, Organizer }
    public enum BookingStatus { Pending, Confirmed, Cancelled, Completed }
    public enum PaymentMethod { CreditCard, DebitCard, OnlineBanking, Wallet }
    public enum PaymentStatus { Pending, Success, Failed, Refunded }
    public enum DiscountType { Percentage, FixedAmount }
}