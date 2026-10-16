using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.RequestAndResponseModels;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/enrolments")]
    public class EnrolmentsController : ApiControllerBase
    {
        private readonly RaceDayContext _context;

        public EnrolmentsController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>Join an event by choosing a category. Participants only. Returns 201, 400, 404 or 409.</summary>
        [HttpPost("/api/events/{id:int}/enrolments")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Enrol(int id, EnrolRequest request)
        {
            IActionResult? accessError = CheckAccess("Participant");
            if (accessError != null)
            {
                return accessError;
            }

            RaceEvent? race = await _context.Events.FindAsync(id);
            if (race == null)
            {
                return NotFound("Event not found.");
            }

            DateTime eventStart = race.EventDate.ToDateTime(race.StartTime);
            DateTime now = DateTime.UtcNow.AddHours(2);

            if (race.Status != "Open" || eventStart <= now)
            {
                return Conflict("This event is closed or has already started.");
            }

            bool categoryOffered = await _context.EventCategories.AnyAsync(c =>
                c.EventID == id && c.CategoryID == request.CategoryId);

            if (!categoryOffered)
            {
                return BadRequest("Choose a category offered by this event.");
            }

            int participantId = GetUserId();
            Enrollment? enrollment = await _context.Enrollments.FirstOrDefaultAsync(e =>
                e.EventID == id && e.ParticipantID == participantId);

            if (enrollment != null && enrollment.Status != "Cancelled")
            {
                return Conflict("You are already enrolled in this event.");
            }

            int enrollmentCount = await _context.Enrollments.CountAsync(e =>
                e.EventID == id && e.Status != "Cancelled");

            if (enrollmentCount >= race.Capacity)
            {
                return Conflict("This event is full.");
            }

            if (request.BibNumber != null)
            {
                bool bibTaken = await _context.Enrollments.AnyAsync(e =>
                    e.BibNumber == request.BibNumber);

                if (bibTaken)
                {
                    return Conflict("This bib number is already in use.");
                }
            }

            // A cancelled enrolment can be used again instead of creating a duplicate.
            if (enrollment == null)
            {
                enrollment = new Enrollment();
                enrollment.EventID = id;
                enrollment.ParticipantID = participantId;
                _context.Enrollments.Add(enrollment);
            }

            enrollment.CategoryID = request.CategoryId;
            enrollment.BibNumber = request.BibNumber;
            enrollment.Status = "Active";
            enrollment.EnrollmentDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = enrollment.EnrollmentID }, enrollment);
        }

        /// <summary>Get your own enrolments. Participants only. Returns 200, 401 or 403.</summary>
        [HttpGet("me")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Mine()
        {
            IActionResult? accessError = CheckAccess("Participant");
            if (accessError != null)
            {
                return accessError;
            }

            int participantId = GetUserId();
            List<Enrollment> enrollments = await _context.Enrollments
                .Where(e => e.ParticipantID == participantId).ToListAsync();

            return Ok(enrollments);
        }

        /// <summary>Get one of your enrolments. Returns 200, 403 for another person's record, or 404.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Get(int id)
        {
            IActionResult? accessError = CheckAccess("Participant");
            if (accessError != null)
            {
                return accessError;
            }

            Enrollment? enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                return NotFound("Enrolment not found.");
            }

            if (enrollment.ParticipantID != GetUserId())
            {
                return StatusCode(403, "You can only view your own enrolments.");
            }

            return Ok(enrollment);
        }

        /// <summary>Cancel your enrolment. Returns 204, 403, 404, or 409 if a result has been recorded.</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Cancel(int id)
        {
            IActionResult? accessError = CheckAccess("Participant");
            if (accessError != null)
            {
                return accessError;
            }

            Enrollment? enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                return NotFound("Enrolment not found.");
            }

            if (enrollment.ParticipantID != GetUserId())
            {
                return StatusCode(403, "You can only cancel your own enrolments.");
            }

            bool hasResult = await _context.Results.AnyAsync(r => r.EnrollmentID == id);
            if (hasResult)
            {
                return Conflict("You cannot cancel an enrolment that already has a result.");
            }

            enrollment.Status = "Cancelled";
            enrollment.BibNumber = null;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}


