using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.RequestAndResponseModels;
using RaceDay.Data;
using RaceDay.Models;

namespace RaceDay.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ApiControllerBase
    {
        private readonly RaceDayContext _context;

        public CategoriesController(RaceDayContext context)
        {
            _context = context;
        }

        /// <summary>Get all categories. Anyone can use this endpoint.</summary>
        [HttpGet]
        [ProducesResponseType(200)]
        public async Task<IActionResult> List()
        {
            List<Category> categories = await _context.Categories.ToListAsync();
            return Ok(categories);
        }

        /// <summary>Create a category using its name and description. Organisers only. Returns 201 or 409 for a duplicate name.</summary>
        [HttpPost]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Create(CategoryRequest request)
        {
            IActionResult? accessError = CheckAccess("Organiser");
            if (accessError != null)
            {
                return accessError;
            }

            string name = request.CategoryName.Trim();
            bool nameExists = await _context.Categories.AnyAsync(c => c.CategoryName.ToLower() == name.ToLower());

            if (nameExists)
            {
                return Conflict("This category already exists.");
            }

            Category category = new Category();
            category.CategoryName = name;
            category.Description = request.Description;

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Created("/api/categories", category);
        }

        /// <summary>Change a category's name and description. Organisers only. Returns 200, 404 or 409.</summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Update(int id, CategoryRequest request)
        {
            IActionResult? accessError = CheckAccess("Organiser");
            if (accessError != null)
            {
                return accessError;
            }

            Category? category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound("Category not found.");
            }

            string name = request.CategoryName.Trim();
            bool nameExists = await _context.Categories.AnyAsync(c =>
                c.CategoryID != id && c.CategoryName.ToLower() == name.ToLower());

            if (nameExists)
            {
                return Conflict("This category already exists.");
            }

            category.CategoryName = name;
            category.Description = request.Description;
            await _context.SaveChangesAsync();

            return Ok(category);
        }

        /// <summary>Delete an unused category. Returns 204, 404, or 409 if it is used by an event or enrolment.</summary>
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

            Category? category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return NotFound("Category not found.");
            }

            bool usedByEvent = await _context.Events.AnyAsync(e => e.CategoryID == id);
            bool offeredByEvent = await _context.EventCategories.AnyAsync(c => c.CategoryID == id);
            bool usedByEnrollment = await _context.Enrollments.AnyAsync(e => e.CategoryID == id);

            if (usedByEvent || offeredByEvent || usedByEnrollment)
            {
                return Conflict("This category is still in use.");
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}


