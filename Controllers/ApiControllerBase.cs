using Microsoft.AspNetCore.Mvc;

namespace RaceDay.Controllers
{
    // The other controllers share these two session checks.
    public abstract class ApiControllerBase : ControllerBase
    {
        protected int GetUserId()
        {
            return HttpContext.Session.GetInt32("UserId").GetValueOrDefault();
        }

        protected IActionResult? CheckAccess(string requiredRole = "")
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            string? role = HttpContext.Session.GetString("Role");

            if (userId == null || (role != "Organiser" && role != "Participant"))
            {
                return Unauthorized("Please log in first.");
            }

            if (requiredRole != "" && role != requiredRole)
            {
                return StatusCode(403, "Your role cannot use this feature.");
            }

            // No error means the user is allowed to continue.
            return null;
        }
    }
}

