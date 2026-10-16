using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.RequestAndResponseModels;
using RaceDay.Controllers;
using RaceDay.Data;
using RaceDay.Models;
using Xunit;

namespace RaceDay.Tests
{
    public class ControllerUnitTests
    {
        private RaceDayContext CreateDatabase()
        {
            DbContextOptions<RaceDayContext> options =
                new DbContextOptionsBuilder<RaceDayContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new RaceDayContext(options);
        }

        private void SetSession(ControllerBase controller, int userId, string role)
        {
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Session = new TestSession();
            httpContext.Session.SetInt32("UserId", userId);
            httpContext.Session.SetString("Role", role);

            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = httpContext;
        }

        [Fact]
        public async Task GetCategories_ReturnsAllCategories()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Categories.Add(new Category { CategoryName = "5 km" });
            context.Categories.Add(new Category { CategoryName = "10 km" });
            await context.SaveChangesAsync();
            CategoriesController controller = new CategoriesController(context);

            // Act
            IActionResult response = await controller.List();

            // Assert
            OkObjectResult result = Assert.IsType<OkObjectResult>(response);
            List<Category> categories = Assert.IsType<List<Category>>(result.Value);
            Assert.Equal(2, categories.Count);
        }

        [Fact]
        public async Task CreateCategory_SavesCategory()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            CategoriesController controller = new CategoriesController(context);
            SetSession(controller, 1, "Organiser");
            CategoryRequest request = new CategoryRequest();
            request.CategoryName = "Under 20";

            // Act
            IActionResult response = await controller.Create(request);

            // Assert
            Assert.IsType<CreatedResult>(response);
            Category category = await context.Categories.SingleAsync();
            Assert.Equal("Under 20", category.CategoryName);
        }

        [Fact]
        public async Task CreateCategory_RejectsDuplicateName()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Categories.Add(new Category { CategoryName = "Under 20" });
            await context.SaveChangesAsync();
            CategoriesController controller = new CategoriesController(context);
            SetSession(controller, 1, "Organiser");
            CategoryRequest request = new CategoryRequest { CategoryName = "under 20" };

            // Act
            IActionResult response = await controller.Create(request);

            // Assert
            Assert.IsType<ConflictObjectResult>(response);
            Assert.Equal(1, await context.Categories.CountAsync());
        }

        [Fact]
        public async Task UpdateCategory_ChangesName()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Categories.Add(new Category { CategoryID = 1, CategoryName = "Old name" });
            await context.SaveChangesAsync();
            CategoriesController controller = new CategoriesController(context);
            SetSession(controller, 1, "Organiser");

            // Act
            IActionResult response = await controller.Update(1,
                new CategoryRequest { CategoryName = "New name" });

            // Assert
            Assert.IsType<OkObjectResult>(response);
            Assert.Equal("New name", (await context.Categories.FindAsync(1))!.CategoryName);
        }

        [Fact]
        public async Task DeleteCategory_RemovesUnusedCategory()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Categories.Add(new Category { CategoryID = 1, CategoryName = "5 km" });
            await context.SaveChangesAsync();
            CategoriesController controller = new CategoriesController(context);
            SetSession(controller, 1, "Organiser");

            // Act
            IActionResult response = await controller.Delete(1);

            // Assert
            Assert.IsType<NoContentResult>(response);
            Assert.Null(await context.Categories.FindAsync(1));
        }

        [Fact]
        public async Task DeleteCategory_RejectsCategoryInUse()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Categories.Add(new Category { CategoryID = 1, CategoryName = "5 km" });
            context.Events.Add(new RaceEvent { EventID = 1, CategoryID = 1 });
            await context.SaveChangesAsync();
            CategoriesController controller = new CategoriesController(context);
            SetSession(controller, 1, "Organiser");

            // Act
            IActionResult response = await controller.Delete(1);

            // Assert
            Assert.IsType<ConflictObjectResult>(response);
            Assert.NotNull(await context.Categories.FindAsync(1));
        }

        [Fact]
        public async Task GetEvent_ReturnsNotFound_WhenEventIsMissing()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            EventsController controller = new EventsController(context);

            // Act
            IActionResult response = await controller.Get(999);

            // Assert
            Assert.IsType<NotFoundObjectResult>(response);
        }

        [Fact]
        public async Task CreateEvent_SavesEventAndCategories()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Routes.Add(new RaceRoute { RouteID = 1 });
            context.Categories.Add(new Category { CategoryID = 1, CategoryName = "10 km" });
            context.Categories.Add(new Category { CategoryID = 2, CategoryName = "Under 20" });
            await context.SaveChangesAsync();
            EventsController controller = new EventsController(context);
            SetSession(controller, 5, "Organiser");

            EventRequest request = new EventRequest();
            request.EventName = "Spring Run";
            request.Description = "A community run";
            request.Location = "Pretoria";
            request.Capacity = 50;
            request.EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
            request.RouteId = 1;
            request.CategoryId = 1;
            request.AdditionalCategoryIds.Add(2);

            // Act
            IActionResult response = await controller.Create(request);

            // Assert
            Assert.IsType<CreatedAtActionResult>(response);
            RaceEvent race = await context.Events.SingleAsync();
            Assert.Equal(5, race.OrganizerID);
            Assert.Equal("Spring Run", race.EventName);
            Assert.Equal(2, await context.EventCategories.CountAsync());
        }

        [Fact]
        public async Task CreateEvent_RejectsMissingRoute()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            EventsController controller = new EventsController(context);
            SetSession(controller, 1, "Organiser");
            EventRequest request = new EventRequest();
            request.EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
            request.RouteId = 999;

            // Act
            IActionResult response = await controller.Create(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(response);
            Assert.Empty(context.Events);
        }

        [Fact]
        public async Task DeleteEvent_RejectsAnotherOrganiser()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Events.Add(new RaceEvent { EventID = 1, OrganizerID = 5 });
            await context.SaveChangesAsync();
            EventsController controller = new EventsController(context);
            SetSession(controller, 9, "Organiser");

            // Act
            IActionResult response = await controller.Delete(1);

            // Assert
            ObjectResult result = Assert.IsType<ObjectResult>(response);
            Assert.Equal(403, result.StatusCode);
            Assert.NotNull(await context.Events.FindAsync(1));
        }

        [Fact]
        public async Task DeleteEvent_RemovesItsEnrolmentsAndResults()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Events.Add(new RaceEvent { EventID = 1, OrganizerID = 5 });
            context.Enrollments.Add(new Enrollment { EnrollmentID = 1, EventID = 1 });
            context.Results.Add(new RaceResult { EnrollmentID = 1 });
            context.EventCategories.Add(new EventCategory { EventID = 1, CategoryID = 1 });
            await context.SaveChangesAsync();
            EventsController controller = new EventsController(context);
            SetSession(controller, 5, "Organiser");

            // Act
            IActionResult response = await controller.Delete(1);

            // Assert
            Assert.IsType<NoContentResult>(response);
            Assert.Empty(context.Events);
            Assert.Empty(context.Enrollments);
            Assert.Empty(context.Results);
            Assert.Empty(context.EventCategories);
        }

        [Fact]
        public async Task Enrol_SavesParticipantEventAndCategory()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Events.Add(new RaceEvent
            {
                EventID = 1, Capacity = 10,
                EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
            });
            context.EventCategories.Add(new EventCategory { EventID = 1, CategoryID = 2 });
            await context.SaveChangesAsync();
            EnrolmentsController controller = new EnrolmentsController(context);
            SetSession(controller, 7, "Participant");
            EnrolRequest request = new EnrolRequest { CategoryId = 2, BibNumber = 101 };

            // Act
            IActionResult response = await controller.Enrol(1, request);

            // Assert
            Assert.IsType<CreatedAtActionResult>(response);
            Enrollment enrollment = await context.Enrollments.SingleAsync();
            Assert.Equal(7, enrollment.ParticipantID);
            Assert.Equal(1, enrollment.EventID);
            Assert.Equal(2, enrollment.CategoryID);
            Assert.Equal(101, enrollment.BibNumber);
        }

        [Fact]
        public async Task Enrol_RejectsFullEvent()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Events.Add(new RaceEvent
            {
                EventID = 1, Capacity = 1,
                EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10))
            });
            context.EventCategories.Add(new EventCategory { EventID = 1, CategoryID = 1 });
            context.Enrollments.Add(new Enrollment { EventID = 1, ParticipantID = 10 });
            await context.SaveChangesAsync();
            EnrolmentsController controller = new EnrolmentsController(context);
            SetSession(controller, 11, "Participant");

            // Act
            IActionResult response = await controller.Enrol(1, new EnrolRequest { CategoryId = 1 });

            // Assert
            Assert.IsType<ConflictObjectResult>(response);
            Assert.Equal(1, await context.Enrollments.CountAsync());
        }

        [Fact]
        public async Task GetEnrolment_RejectsAnotherParticipant()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Enrollments.Add(new Enrollment { EnrollmentID = 1, ParticipantID = 10 });
            await context.SaveChangesAsync();
            EnrolmentsController controller = new EnrolmentsController(context);
            SetSession(controller, 11, "Participant");

            // Act
            IActionResult response = await controller.Get(1);

            // Assert
            ObjectResult result = Assert.IsType<ObjectResult>(response);
            Assert.Equal(403, result.StatusCode);
        }

        [Fact]
        public async Task CancelEnrolment_ReleasesBibNumber()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Enrollments.Add(new Enrollment
            {
                EnrollmentID = 1, ParticipantID = 10, BibNumber = 101
            });
            await context.SaveChangesAsync();
            EnrolmentsController controller = new EnrolmentsController(context);
            SetSession(controller, 10, "Participant");

            // Act
            IActionResult response = await controller.Cancel(1);

            // Assert
            Assert.IsType<NoContentResult>(response);
            Enrollment enrollment = (await context.Enrollments.FindAsync(1))!;
            Assert.Equal("Cancelled", enrollment.Status);
            Assert.Null(enrollment.BibNumber);
        }

        [Fact]
        public async Task RecordResult_SavesTimeAndPosition()
        {
            // Arrange
            using RaceDayContext context = CreateDatabase();
            context.Events.Add(new RaceEvent
            {
                EventID = 1, OrganizerID = 5,
                EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2))
            });
            context.Enrollments.Add(new Enrollment { EnrollmentID = 1, EventID = 1 });
            await context.SaveChangesAsync();
            ResultsController controller = new ResultsController(context);
            SetSession(controller, 5, "Organiser");
            ResultRequest request = new ResultRequest();
            request.FinishTime = new TimeOnly(1, 2, 34);
            request.Position = 12;

            // Act
            IActionResult response = await controller.Create(1, request);

            // Assert
            Assert.IsType<CreatedResult>(response);
            RaceResult result = await context.Results.SingleAsync();
            Assert.Equal(new TimeOnly(1, 2, 34), result.FinishTime);
            Assert.Equal(12, result.Position);
            Assert.Equal("Completed", (await context.Enrollments.FindAsync(1))!.Status);
        }
    }
}

