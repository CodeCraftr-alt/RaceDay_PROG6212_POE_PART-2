using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.RequestAndResponseModels;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/results")]
    public class ResultsController : ApiControllerBase
    {
        private readonly RaceDayContext _context;

        public ResultsController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>Get your own results with event names. Participants only. Returns 200, 401 or 403.</summary>
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
            List<object> details = new List<object>();

            foreach (Enrollment enrollment in enrollments)
            {
                RaceResult? result = await _context.Results
                    .FirstOrDefaultAsync(r => r.EnrollmentID == enrollment.EnrollmentID);
                RaceEvent? race = await _context.Events.FindAsync(enrollment.EventID);

                if (result != null && race != null)
                {
                    details.Add(new
                    {
                        result.ResultID,
                        result.EnrollmentID,
                        enrollment.EventID,
                        race.EventName,
                        race.EventDate,
                        enrollment.CategoryID,
                        result.FinishTime,
                        result.Position
                    });
                }
            }

            return Ok(details);
        }

        /// <summary>Record a finish time (HH:mm:ss) and position for your event. Returns 201, 403, 404 or 409.</summary>
        [HttpPost("/api/enrolments/{id:int}/result")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create(int id, ResultRequest request)
        {
            IActionResult? accessError = CheckAccess("Organiser");
            if (accessError != null)
            {
                return accessError;
            }

            Enrollment? enrollment = await _context.Enrollments.FindAsync(id);
            if (enrollment == null)
            {
                return NotFound("Enrolment not found.");
            }

            RaceEvent? race = await _context.Events.FindAsync(enrollment.EventID);
            if (race == null)
            {
                return NotFound("Event not found.");
            }

            if (race.OrganizerID != GetUserId())
            {
                return StatusCode(403, "You can only record results for your own events.");
            }

            DateTime eventStart = race.EventDate.ToDateTime(race.StartTime);
            if (eventStart > DateTime.UtcNow.AddHours(2))
            {
                return Conflict("The event has not started yet.");
            }

            if (enrollment.Status == "Cancelled" || race.Status == "Cancelled")
            {
                return Conflict("The event or enrolment has been cancelled.");
            }

            bool resultExists = await _context.Results.AnyAsync(r => r.EnrollmentID == id);
            if (resultExists)
            {
                return Conflict("A result already exists. Use PUT to update it.");
            }

            RaceResult result = new RaceResult();
            result.EnrollmentID = id;
            result.FinishTime = request.FinishTime;
            result.Position = request.Position;

            _context.Results.Add(result);
            enrollment.Status = "Completed";
            await _context.SaveChangesAsync();

            return Created("/api/events/" + race.EventID + "/results", result);
        }

        /// <summary>Correct a finish time and position for your event. Returns 200, 403 or 404.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(int id, ResultRequest request)
        {
            IActionResult? accessError = CheckAccess("Organiser");
            if (accessError != null)
            {
                return accessError;
            }

            RaceResult? result = await _context.Results.FindAsync(id);
            if (result == null)
            {
                return NotFound("Result not found.");
            }

            Enrollment? enrollment = await _context.Enrollments.FindAsync(result.EnrollmentID);
            if (enrollment == null)
            {
                return NotFound("Enrolment not found.");
            }

            RaceEvent? race = await _context.Events.FindAsync(enrollment.EventID);
            if (race == null)
            {
                return NotFound("Event not found.");
            }

            if (race.OrganizerID != GetUserId())
            {
                return StatusCode(403, "You can only change results for your own events.");
            }

            result.FinishTime = request.FinishTime;
            result.Position = request.Position;
            await _context.SaveChangesAsync();

            return Ok(result);
        }

        /// <summary>Get results for your event in finishing order. Organisers only. Returns 200, 403 or 404.</summary>
        [HttpGet("/api/events/{id:int}/results")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> EventResults(int id)
        {
            IActionResult? accessError = CheckAccess("Organiser");
            if (accessError != null)
            {
                return accessError;
            }

            RaceEvent? race = await _context.Events.FindAsync(id);
            if (race == null)
            {
                return NotFound("Event not found.");
            }

            if (race.OrganizerID != GetUserId())
            {
                return StatusCode(403, "You can only view results for your own events.");
            }

            List<Enrollment> enrollments = await _context.Enrollments
                .Where(e => e.EventID == id).ToListAsync();
            List<RaceResult> results = new List<RaceResult>();

            foreach (Enrollment enrollment in enrollments)
            {
                RaceResult? result = await _context.Results
                    .FirstOrDefaultAsync(r => r.EnrollmentID == enrollment.EnrollmentID);

                if (result != null)
                {
                    results.Add(result);
                }
            }

            return Ok(results.OrderBy(r => r.Position).ToList());
        }
    }
}


