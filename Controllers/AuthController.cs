using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.RequestAndResponseModels;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ApiControllerBase
    {
        private readonly RaceDayContext _context;
        private readonly PasswordHasher<string> _passwordHasher;

        public AuthController(RaceDayContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<string>();
        }

        /// <summary>Register as a Participant or Organiser. Returns 201, or 409 if the email is taken.</summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ProfileResponse), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            string email = request.Email.Trim().ToLower();

            bool organiserExists = await _context.Organisers.AnyAsync(o => o.Email == email);
            bool participantExists = await _context.Participants.AnyAsync(p => p.Email == email);

            if (organiserExists || participantExists)
            {
                return Conflict("This email is already registered.");
            }

            // Store a hash, never the original password.
            string passwordHash = _passwordHasher.HashPassword(email, request.Password);
            ProfileResponse profile = new ProfileResponse();
            profile.Role = request.Role;
            profile.FirstName = request.FirstName.Trim();
            profile.LastName = request.LastName.Trim();
            profile.Email = email;
            profile.Phone = request.Phone;

            if (request.Role == "Organiser")
            {
                Organiser organiser = new Organiser();
                organiser.FirstName = profile.FirstName;
                organiser.LastName = profile.LastName;
                organiser.Email = email;
                organiser.PasswordHash = passwordHash;
                organiser.Phone = request.Phone;

                _context.Organisers.Add(organiser);
                await _context.SaveChangesAsync();
                profile.UserId = organiser.OrganiserID;
            }
            else
            {
                Participant participant = new Participant();
                participant.FirstName = profile.FirstName;
                participant.LastName = profile.LastName;
                participant.Email = email;
                participant.PasswordHash = passwordHash;
                participant.Phone = request.Phone;

                _context.Participants.Add(participant);
                await _context.SaveChangesAsync();
                profile.UserId = participant.ParticipantID;
            }

            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", profile.UserId);
            HttpContext.Session.SetString("Role", profile.Role);

            return Created("/api/profile", profile);
        }

        /// <summary>Log in with an email and password. Returns 200 on success or 401 for incorrect details.</summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ProfileResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            string email = request.Email.Trim().ToLower();
            string passwordHash;
            ProfileResponse profile = new ProfileResponse();

            Organiser? organiser = await _context.Organisers.FirstOrDefaultAsync(o => o.Email == email);

            if (organiser != null)
            {
                profile.UserId = organiser.OrganiserID;
                profile.Role = "Organiser";
                profile.FirstName = organiser.FirstName;
                profile.LastName = organiser.LastName;
                profile.Email = organiser.Email;
                profile.Phone = organiser.Phone;
                passwordHash = organiser.PasswordHash;
            }
            else
            {
                Participant? participant = await _context.Participants.FirstOrDefaultAsync(p => p.Email == email);

                if (participant == null)
                {
                    return Unauthorized("Incorrect email or password.");
                }

                profile.UserId = participant.ParticipantID;
                profile.Role = "Participant";
                profile.FirstName = participant.FirstName;
                profile.LastName = participant.LastName;
                profile.Email = participant.Email;
                profile.Phone = participant.Phone;
                passwordHash = participant.PasswordHash;
            }

            PasswordVerificationResult result =
                _passwordHasher.VerifyHashedPassword(email, passwordHash, request.Password);

            if (result == PasswordVerificationResult.Failed)
            {
                return Unauthorized("Incorrect email or password.");
            }

            HttpContext.Session.Clear();
            HttpContext.Session.SetInt32("UserId", profile.UserId);
            HttpContext.Session.SetString("Role", profile.Role);

            return Ok(profile);
        }

        /// <summary>Log out by clearing the current session. Returns 204, or 401 if not logged in.</summary>
        [HttpPost("logout")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        public IActionResult Logout()
        {
            IActionResult? accessError = CheckAccess();
            if (accessError != null)
            {
                return accessError;
            }

            HttpContext.Session.Clear();
            return NoContent();
        }
    }
}

