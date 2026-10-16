using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.RequestAndResponseModels;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/events")]
    public class EventsController : ApiControllerBase
    {
        private readonly RaceDayContext _context;

        public EventsController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>Get upcoming events. Use categoryId to filter, or includePast=true to show past events too.</summary>
        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List(int? categoryId, bool includePast = false)
        {
            List<RaceEvent> events = await _context.Events.ToListAsync();
            List<RaceEvent> matchingEvents = new List<RaceEvent>();

            // Event dates use South African time, which is UTC plus two hours.
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(2));

            foreach (RaceEvent race in events)
            {
                if (!includePast && race.EventDate < today)
                {
                    continue;
                }

                if (categoryId != null)
                {
                    bool offersCategory = await _context.EventCategories.AnyAsync(c =>
                        c.EventID == race.EventID && c.CategoryID == categoryId);

                    if (!offersCategory)
                    {
                        continue;
                    }
                }

                matchingEvents.Add(race);
            }

            return Ok(matchingEvents.OrderBy(e => e.EventDate).ToList());
        }

        /// <summary>Get one event, its route and available categories. Returns 200 or 404.</summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Get(int id)
        {
            RaceEvent? race = await _context.Events.FindAsync(id);
            if (race == null)
            {
                return NotFound("Event not found.");
            }

            RaceRoute? route = await _context.Routes.FindAsync(race.RouteID);
            List<EventCategory> links = await _context.EventCategories
                .Where(c => c.EventID == id).ToListAsync();
            List<Category> categories = new List<Category>();

            foreach (EventCategory link in links)
            {
                Category? category = await _context.Categories.FindAsync(link.CategoryID);
                if (category != null)
                {
                    categories.Add(category);
                }
            }

            return Ok(new { race, route, categories });
        }

        /// <summary>Create an event with a route and offered categories. Organisers only. Returns 201 or 400.</summary>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create(EventRequest request)
        {
            IActionResult? accessError = CheckAccess("Organiser");
            if (accessError != null)
            {
                return accessError;
            }

            string? detailsError = await CheckEventDetails(request);
            if (detailsError != null)
            {
                return BadRequest(detailsError);
            }

            RaceEvent race = new RaceEvent();
            race.OrganizerID = GetUserId();
            race.EventName = request.EventName.Trim();
            race.Description = request.Description.Trim();
            race.EventType = request.EventType;
            race.EventDate = request.EventDate;
            race.StartTime = request.StartTime;
            race.Location = request.Location.Trim();
            race.Capacity = request.Capacity;
            race.CategoryID = request.CategoryId;
            race.RouteID = request.RouteId;
            race.Status = request.Status;

            _context.Events.Add(race);
            await _context.SaveChangesAsync();

            List<int> categoryIds = GetCategoryIds(request);
            foreach (int categoryId in categoryIds)
            {
                EventCategory link = new EventCategory();
                link.EventID = race.EventID;
                link.CategoryID = categoryId;
                _context.EventCategories.Add(link);
            }

            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = race.EventID }, race);
        }

        /// <summary>Update an event you own. Send all event fields. Returns 200, 400, 403, 404 or 409.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(int id, EventRequest request)
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
                return StatusCode(403, "You can only change your own events.");
            }

            string? detailsError = await CheckEventDetails(request);
            if (detailsError != null)
            {
                return BadRequest(detailsError);
            }

            int enrollmentCount = await _context.Enrollments.CountAsync(e =>
                e.EventID == id && e.Status != "Cancelled");

            if (request.Capacity < enrollmentCount)
            {
                return Conflict("Capacity cannot be lower than the number of enrolments.");
            }

            List<int> categoryIds = GetCategoryIds(request);
            List<Enrollment> enrollments = await _context.Enrollments
                .Where(e => e.EventID == id).ToListAsync();

            foreach (Enrollment enrollment in enrollments)
            {
                if (!categoryIds.Contains(enrollment.CategoryID))
                {
                    return Conflict("You cannot remove a category that an enrolment uses.");
                }
            }

            race.EventName = request.EventName.Trim();
            race.Description = request.Description.Trim();
            race.EventType = request.EventType;
            race.EventDate = request.EventDate;
            race.StartTime = request.StartTime;
            race.Location = request.Location.Trim();
            race.Capacity = request.Capacity;
            race.CategoryID = request.CategoryId;
            race.RouteID = request.RouteId;
            race.Status = request.Status;

            // Replace the list of categories offered by this event.
            List<EventCategory> oldLinks = await _context.EventCategories
                .Where(c => c.EventID == id).ToListAsync();
            _context.EventCategories.RemoveRange(oldLinks);

            foreach (int categoryId in categoryIds)
            {
                EventCategory link = new EventCategory();
                link.EventID = id;
                link.CategoryID = categoryId;
                _context.EventCategories.Add(link);
            }

            await _context.SaveChangesAsync();
            return Ok(race);
        }

        /// <summary>Delete your event and its enrolments and results. Returns 204, 403 or 404.</summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Delete(int id)
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
                return StatusCode(403, "You can only delete your own events.");
            }

            // InMemory does not delete related records for us.
            List<Enrollment> enrollments = await _context.Enrollments
                .Where(e => e.EventID == id).ToListAsync();

            foreach (Enrollment enrollment in enrollments)
            {
                List<RaceResult> results = await _context.Results
                    .Where(r => r.EnrollmentID == enrollment.EnrollmentID).ToListAsync();
                _context.Results.RemoveRange(results);
            }

            List<EventCategory> links = await _context.EventCategories
                .Where(c => c.EventID == id).ToListAsync();

            _context.Enrollments.RemoveRange(enrollments);
            _context.EventCategories.RemoveRange(links);
            _context.Events.Remove(race);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>View enrolments for your event, with participant names and emails. Returns 200, 403 or 404.</summary>
        [HttpGet("{id:int}/enrolments")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Enrolments(int id)
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
                return StatusCode(403, "You can only view enrolments for your own events.");
            }

            List<Enrollment> enrollments = await _context.Enrollments
                .Where(e => e.EventID == id).ToListAsync();
            List<object> details = new List<object>();

            foreach (Enrollment enrollment in enrollments)
            {
                Participant? participant = await _context.Participants.FindAsync(enrollment.ParticipantID);
                if (participant != null)
                {
                    details.Add(new
                    {
                        enrollment.EnrollmentID,
                        enrollment.ParticipantID,
                        participant.FirstName,
                        participant.LastName,
                        participant.Email,
                        enrollment.CategoryID,
                        enrollment.BibNumber,
                        enrollment.Status
                    });
                }
            }

            return Ok(details);
        }

        // These two helpers keep the same checks in one place for POST and PUT.
        private async Task<string?> CheckEventDetails(EventRequest request)
        {
            if (request.EventDate == default)
            {
                return "Please enter an event date.";
            }

            RaceRoute? route = await _context.Routes.FindAsync(request.RouteId);
            if (route == null)
            {
                return "This route does not exist.";
            }

            List<int> categoryIds = GetCategoryIds(request);
            foreach (int categoryId in categoryIds)
            {
                Category? category = await _context.Categories.FindAsync(categoryId);
                if (category == null)
                {
                    return "One of the categories does not exist.";
                }
            }

            return null;
        }

        private List<int> GetCategoryIds(EventRequest request)
        {
            List<int> ids = new List<int>();
            ids.Add(request.CategoryId);

            foreach (int id in request.AdditionalCategoryIds)
            {
                if (!ids.Contains(id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }
    }
}


