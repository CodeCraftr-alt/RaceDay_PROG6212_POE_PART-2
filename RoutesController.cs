using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.RequestAndResponseModels;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/routes")]
    public class RoutesController : ApiControllerBase
    {
        private readonly RaceDayContext _context;

        public RoutesController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>Get all routes, including their distances in kilometres. Anyone can view them.</summary>
        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List()
        {
            List<RaceRoute> routes = await _context.Routes.ToListAsync();
            return Ok(routes);
        }

        /// <summary>Create a route with start and finish locations, distance and elevation. Organisers only. Returns 201.</summary>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create(RouteRequest request)
        {
            IActionResult? accessError = CheckAccess("Organiser");
            if (accessError != null)
            {
                return accessError;
            }

            RaceRoute route = new RaceRoute();
            route.RouteName = request.RouteName.Trim();
            route.StartLocation = request.StartLocation.Trim();
            route.FinishLocation = request.FinishLocation.Trim();
            route.DistanceKm = request.DistanceKm;
            route.ElevationGainM = request.ElevationGainM;

            _context.Routes.Add(route);
            await _context.SaveChangesAsync();

            return Created("/api/routes", route);
        }

        /// <summary>Change a route's details. Organisers only. Returns 200 or 404.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(int id, RouteRequest request)
        {
            IActionResult? accessError = CheckAccess("Organiser");
            if (accessError != null)
            {
                return accessError;
            }

            RaceRoute? route = await _context.Routes.FindAsync(id);
            if (route == null)
            {
                return NotFound("Route not found.");
            }

            route.RouteName = request.RouteName.Trim();
            route.StartLocation = request.StartLocation.Trim();
            route.FinishLocation = request.FinishLocation.Trim();
            route.DistanceKm = request.DistanceKm;
            route.ElevationGainM = request.ElevationGainM;
            await _context.SaveChangesAsync();

            return Ok(route);
        }

        /// <summary>Delete an unused route. Returns 204, 404, or 409 if an event uses it.</summary>
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

            RaceRoute? route = await _context.Routes.FindAsync(id);
            if (route == null)
            {
                return NotFound("Route not found.");
            }

            bool routeInUse = await _context.Events.AnyAsync(e => e.RouteID == id);
            if (routeInUse)
            {
                return Conflict("This route is still used by an event.");
            }

            _context.Routes.Remove(route);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}


