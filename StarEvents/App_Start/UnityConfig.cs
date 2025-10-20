using System.Web.Mvc;
using Unity;
using Unity.Lifetime;
using Unity.Mvc5;
using StarEvents.Data;
using StarEvents.Repositories.Interfaces;
using StarEvents.Repositories.Implementations;
using StarEvents.Services.Interfaces;
using StarEvents.Services.Implementations;

namespace StarEvents.App_Start
{
    public static class UnityConfig
    {
        private static IUnityContainer _container;

        public static void RegisterComponents()
        {
            _container = new UnityContainer();

            // =====================================================
            // DATABASE CONTEXT
            // =====================================================
            _container.RegisterType<ApplicationDbContext>(new HierarchicalLifetimeManager());

            // =====================================================
            // REPOSITORIES
            // =====================================================
            _container.RegisterType<IEventRepository, EventRepository>(new HierarchicalLifetimeManager());
            // Add other repositories here when implemented
            // Example:
            // _container.RegisterType<IBookingRepository, BookingRepository>(new HierarchicalLifetimeManager());
            // _container.RegisterType<IPaymentRepository, PaymentRepository>(new HierarchicalLifetimeManager());

            // =====================================================
            // CORE APPLICATION SERVICES
            // =====================================================
            _container.RegisterType<IAuthenticationService, AuthenticationService>(new HierarchicalLifetimeManager());
            _container.RegisterType<IEventService, EventService>(new HierarchicalLifetimeManager());
            _container.RegisterType<IBookingService, BookingService>(new HierarchicalLifetimeManager());
            _container.RegisterType<IPaymentService, PaymentService>(new HierarchicalLifetimeManager());
            _container.RegisterType<ITicketService, TicketService>(new HierarchicalLifetimeManager());
            _container.RegisterType<IQRCodeService, QRCodeService>(new HierarchicalLifetimeManager());
            _container.RegisterType<IEmailService, EmailService>(new HierarchicalLifetimeManager());

            // =====================================================
            // ADMIN & REPORT SERVICES
            // =====================================================
            _container.RegisterType<IAdminService, AdminService>(new HierarchicalLifetimeManager());
            _container.RegisterType<IReportService, ReportService>(new HierarchicalLifetimeManager());

            // =====================================================
            // DEPENDENCY RESOLVER SETUP
            // =====================================================
            DependencyResolver.SetResolver(new UnityDependencyResolver(_container));
        }

        public static IUnityContainer GetConfiguredContainer() => _container;
    }
}
