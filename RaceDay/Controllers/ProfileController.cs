using Microsoft.AspNetCore.Mvc;
using RaceDay.RequestAndResponseModels;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/profile")]
    public class ProfileController : ApiControllerBase
    {
        private readonly RaceDayContext _context;

        public ProfileController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>Get your own profile. Returns 200, or 401 if not logged in.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ProfileResponse), 200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Get()
        {
            IActionResult? accessError = CheckAccess();
            if (accessError != null)
            {
                return accessError;
            }

            ProfileResponse profile = new ProfileResponse();
            profile.UserId = GetUserId();
            profile.Role = HttpContext.Session.GetString("Role")!;

            if (profile.Role == "Organiser")
            {
                Organiser? organiser = await _context.Organisers.FindAsync(profile.UserId);
                if (organiser == null)
                {
                    HttpContext.Session.Clear();
                    return Unauthorized("Please log in again.");
                }

                profile.FirstName = organiser.FirstName;
                profile.LastName = organiser.LastName;
                profile.Email = organiser.Email;
                profile.Phone = organiser.Phone;
            }
            else
            {
                Participant? participant = await _context.Participants.FindAsync(profile.UserId);
                if (participant == null)
                {
                    HttpContext.Session.Clear();
                    return Unauthorized("Please log in again.");
                }

                profile.FirstName = participant.FirstName;
                profile.LastName = participant.LastName;
                profile.Email = participant.Email;
                profile.Phone = participant.Phone;
            }

            return Ok(profile);
        }

        /// <summary>Change your own first name, last name and phone. Returns 200 or 400 for invalid input.</summary>
        [HttpPut]
        [ProducesResponseType(typeof(ProfileResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Update(ProfileRequest request)
        {
            IActionResult? accessError = CheckAccess();
            if (accessError != null)
            {
                return accessError;
            }

            int userId = GetUserId();
            string? role = HttpContext.Session.GetString("Role");

            if (role == "Organiser")
            {
                Organiser? organiser = await _context.Organisers.FindAsync(userId);
                if (organiser == null)
                {
                    return Unauthorized("Please log in again.");
                }

                organiser.FirstName = request.FirstName.Trim();
                organiser.LastName = request.LastName.Trim();
                organiser.Phone = request.Phone;
            }
            else
            {
                Participant? participant = await _context.Participants.FindAsync(userId);
                if (participant == null)
                {
                    return Unauthorized("Please log in again.");
                }

                participant.FirstName = request.FirstName.Trim();
                participant.LastName = request.LastName.Trim();
                participant.Phone = request.Phone;
            }

            await _context.SaveChangesAsync();

            // Return the same profile format as the GET method.
            return await Get();
        }
    }
}

