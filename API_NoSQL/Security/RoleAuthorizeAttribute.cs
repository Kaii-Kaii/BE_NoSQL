using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace API_NoSQL.Security
{
    // Minimal role guard for demo: check header X-Role.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class RoleAuthorizeAttribute : Attribute, IAsyncActionFilter
    {
        private readonly string _requiredRole;

        public RoleAuthorizeAttribute(string requiredRole) => _requiredRole = requiredRole;

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var role = context.HttpContext.Request.Headers["X-Role"].ToString();
            if (!string.Equals(role, _requiredRole, StringComparison.OrdinalIgnoreCase))
            {
                // Return 403 directly to avoid requiring ASP.NET authentication schemes
                context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
                return;
            }

            await next();
        }
    }
}