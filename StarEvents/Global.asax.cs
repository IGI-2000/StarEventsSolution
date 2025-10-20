using System;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using StarEvents.App_Start;
using StarEvents.Data;
using StarEvents.Migrations;

namespace StarEvents
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // register Unity (must run before any controller resolution)
            UnityConfig.RegisterComponents();

            // Tell AntiForgery which claim to use as the unique identifier
            // This avoids the error when the NameIdentifier claim is missing.
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

            // Apply any pending Code-First migrations at startup (development only).
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<ApplicationDbContext, Configuration>());

            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        /// <summary>
        /// Rehydrates the IPrincipal/IIdentity on each request from the forms auth cookie.
        /// Sets a ClaimsPrincipal with Name, NameIdentifier and Role claims so controllers
        /// that look for ClaimTypes.NameIdentifier or User.IsInRole() will work.
        /// </summary>
        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {
            try
            {
                var authCookie = Request.Cookies[FormsAuthentication.FormsCookieName];
                if (authCookie == null || string.IsNullOrEmpty(authCookie.Value)) return;

                var ticket = FormsAuthentication.Decrypt(authCookie.Value);
                if (ticket == null) return;

                // If ticket is expired, do not rehydrate the identity
                if (ticket.Expiration < DateTime.Now) return;

                // userData format: "Role;UserId"
                var userData = ticket.UserData ?? string.Empty;
                var parts = userData.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                string role = parts.Length > 0 ? parts[0] : null;
                string userId = parts.Length > 1 ? parts[1] : null;

                var claims = new System.Collections.Generic.List<System.Security.Claims.Claim>();

                if (!string.IsNullOrEmpty(ticket.Name))
                    claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, ticket.Name));

                if (!string.IsNullOrEmpty(role))
                    claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, role));

                // Ensure a NameIdentifier claim is present for AntiForgery and identity uniqueness
                if (!string.IsNullOrEmpty(userId))
                    claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, userId));
                else if (!string.IsNullOrEmpty(ticket.Name))
                    claims.Add(new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, ticket.Name));

                var ci = new System.Security.Claims.ClaimsIdentity(claims, "Forms");
                var principal = new System.Security.Claims.ClaimsPrincipal(ci);

                HttpContext.Current.User = principal;
                System.Threading.Thread.CurrentPrincipal = principal;
            }
            catch
            {
                // don't break request pipeline on auth errors
                HttpContext.Current.User = new System.Security.Principal.GenericPrincipal(new System.Security.Principal.GenericIdentity(string.Empty), new string[] { });
                System.Threading.Thread.CurrentPrincipal = HttpContext.Current.User;
            }
        }
    }
}
