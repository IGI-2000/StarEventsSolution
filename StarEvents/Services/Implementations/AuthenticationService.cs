using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Security;
using BCrypt.Net;
using StarEvents.Data;
using StarEvents.Models.Domain;
using StarEvents.Models.ViewModels;
using StarEvents.Services.Interfaces;

namespace StarEvents.Services.Implementations
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly ApplicationDbContext _context;

        public AuthenticationService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<AuthenticationResult> LoginAsync(LoginViewModel model)
        {
            if (model == null) return new AuthenticationResult { Success = false, ErrorMessage = "Invalid login data" };

            // Lookup user across tables - use BaseUser so we can assign any derived type
            BaseUser user = _context.Customers.FirstOrDefault(u => u.Email == model.Email);
            string role = "Customer";

            if (user == null)
            {
                user = _context.EventOrganizers.FirstOrDefault(u => u.Email == model.Email);
                role = user != null ? "Organizer" : null;
            }

            if (user == null)
            {
                user = _context.Admins.FirstOrDefault(u => u.Email == model.Email);
                role = user != null ? "Admin" : role;
            }

            if (user == null)
                return new AuthenticationResult { Success = false, ErrorMessage = "Invalid credentials" };

            var ok = false;
            try
            {
                ok = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
            }
            catch
            {
                ok = false;
            }

            if (!ok)
                return new AuthenticationResult { Success = false, ErrorMessage = "Invalid credentials" };

            // Update last login
            user.LastLoginDate = DateTime.UtcNow;
            _context.Entry(user).State = System.Data.Entity.EntityState.Modified;
            await _context.SaveChangesAsync();

            return new AuthenticationResult
            {
                Success = true,
                UserId = user.Id,
                Role = role
            };
        }

        public async Task<OperationResult> RegisterCustomerAsync(RegisterViewModel model)
        {
            if (model == null) return new OperationResult { Success = false, ErrorMessage = "Invalid data" };

            // Check existing by email across all user tables
            var exists = _context.Set<BaseUser>().Any(u => u.Email == model.Email);
            if (exists) return new OperationResult { Success = false, ErrorMessage = "Email already registered" };

            var customer = new Customer
            {
                Email = model.Email.Trim(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FirstName = model.FirstName?.Trim(),
                LastName = model.LastName?.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                Address = model.Address?.Trim(),
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                Role = UserRole.Customer,
                LoyaltyPoints = 0
            };

            _context.Customers.Add(customer);
            try
            {
                await _context.SaveChangesAsync();
                return new OperationResult { Success = true };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, ErrorMessage = ex.Message };
            }
        }

        public void SignOut()
        {
            try
            {
                FormsAuthentication.SignOut();
                var ctx = System.Web.HttpContext.Current;
                try
                {
                    ctx?.Session?.Clear();
                    ctx?.Session?.Abandon();
                }
                catch { /* ignore session issues */ }
            }
            catch
            {
                // swallow to keep logout resilient
            }
        }
    }
}