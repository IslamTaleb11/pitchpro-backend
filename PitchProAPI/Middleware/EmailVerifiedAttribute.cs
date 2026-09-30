using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PitchProAPI.Middleware
{
    // Blocks requests from authenticated users whose email is not verified.
    // Apply with [EmailVerified] at the controller (or action) level so the
    // check is scoped to business endpoints that require a verified account.
    // Reads the "email_verified" claim embedded in the JWT at login / refresh
    // time, so it requires NO database call per request.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class EmailVerifiedAttribute : Attribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var user = context.HttpContext.User;

            // Skip when there is no authenticated identity.
            if (user.Identity?.IsAuthenticated != true)
                return;

            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            // Only enforce verification for the President role.
            if (!string.Equals(role, "President", StringComparison.OrdinalIgnoreCase))
                return;

            bool emailVerified =
                bool.TryParse(user.FindFirst("email_verified")?.Value, out bool verified)
                && verified;

            if (!emailVerified)
            {
                context.Result = new ObjectResult(new
                {
                    message = "Email not verified. Please verify your email address before continuing.",
                    emailNotVerified = true
                })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}