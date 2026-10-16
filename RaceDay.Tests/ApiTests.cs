using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.RequestAndResponseModels;
using RaceDay.Data;
using RaceDay.Models;
using Xunit;

namespace RaceDay.Tests
{
    // These tests send real HTTP requests to check cookies, sessions and validation.
    public class ApiTests
    {
        private async Task<HttpResponseMessage> Register(HttpClient client,
            string role, string email = "anele@example.com")
        {
            RegisterRequest request = new RegisterRequest();
            request.FirstName = "Anele";
            request.LastName = "Mokoena";
            request.Email = email;
            request.Password = "RaceDay123!";
            request.Role = role;

            return await client.PostAsJsonAsync("/api/auth/register", request);
        }

        private EventRequest EventDetails(int categoryId, int routeId)
        {
            EventRequest request = new EventRequest();
            request.EventName = "Spring Run";
            request.Description = "A community race";
            request.Location = "Pretoria";
            request.EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(10));
            request.StartTime = new TimeOnly(7, 0);
            request.Capacity = 10;
            request.CategoryId = categoryId;
            request.RouteId = routeId;
            return request;
        }

        // Make the category and route first because an event needs both IDs.
        private async Task<RaceEvent> AddEvent(HttpClient organiser)
        {
            CategoryRequest categoryRequest = new CategoryRequest { CategoryName = "10 km" };
            HttpResponseMessage categoryResponse =
                await organiser.PostAsJsonAsync("/api/categories", categoryRequest);
            Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);
            Category category = (await categoryResponse.Content.ReadFromJsonAsync<Category>())!;

            RouteRequest routeRequest = new RouteRequest();
            routeRequest.RouteName = "Central route";
            routeRequest.StartLocation = "Square";
            routeRequest.FinishLocation = "Square";
            routeRequest.DistanceKm = 10;

            HttpResponseMessage routeResponse =
                await organiser.PostAsJsonAsync("/api/routes", routeRequest);
            Assert.Equal(HttpStatusCode.Created, routeResponse.StatusCode);
            RaceRoute route = (await routeResponse.Content.ReadFromJsonAsync<RaceRoute>())!;

            EventRequest request = EventDetails(category.CategoryID, route.RouteID);
            HttpResponseMessage response = await organiser.PostAsJsonAsync("/api/events", request);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            return (await response.Content.ReadFromJsonAsync<RaceEvent>())!;
        }

        [Theory]
        [InlineData("Organiser")]
        [InlineData("Participant")]
        public async Task Register_StartsSessionAndHashesPassword(string role)
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient client = factory.Browser();

            // Act
            HttpResponseMessage response = await Register(client, role);
            HttpResponseMessage profileResponse = await client.GetAsync("/api/profile");

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(HttpStatusCode.OK, profileResponse.StatusCode);
            ProfileResponse profile = (await profileResponse.Content.ReadFromJsonAsync<ProfileResponse>())!;
            Assert.Equal(role, profile.Role);

            string responseText = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("passwordHash", responseText);

            using IServiceScope scope = factory.Services.CreateScope();
            RaceDayContext context = scope.ServiceProvider.GetRequiredService<RaceDayContext>();
            string storedHash;

            if (role == "Organiser")
            {
                storedHash = (await context.Organisers.SingleAsync()).PasswordHash;
            }
            else
            {
                storedHash = (await context.Participants.SingleAsync()).PasswordHash;
            }

            Assert.NotEqual("RaceDay123!", storedHash);
            PasswordHasher<string> hasher = new PasswordHasher<string>();
            PasswordVerificationResult check =
                hasher.VerifyHashedPassword("anele@example.com", storedHash, "RaceDay123!");
            Assert.Equal(PasswordVerificationResult.Success, check);
        }

        [Theory]
        [InlineData("Organiser")]
        [InlineData("Participant")]
        public async Task LoginAndLogout_KeepAndClearTheSession(string role)
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient client = factory.Browser();
            await Register(client, role);

            // Act
            HttpResponseMessage logout = await client.PostAsync("/api/auth/logout", null);
            HttpResponseMessage loggedOutProfile = await client.GetAsync("/api/profile");

            LoginRequest request = new LoginRequest();
            request.Email = "ANELE@example.com";
            request.Password = "RaceDay123!";
            HttpResponseMessage login = await client.PostAsJsonAsync("/api/auth/login", request);
            HttpResponseMessage loggedInProfile = await client.GetAsync("/api/profile");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, loggedOutProfile.StatusCode);
            Assert.Equal(HttpStatusCode.OK, login.StatusCode);
            Assert.Equal(HttpStatusCode.OK, loggedInProfile.StatusCode);
        }

        [Fact]
        public async Task Login_RejectsWrongPassword()
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient client = factory.Browser();
            await Register(client, "Participant");
            await client.PostAsync("/api/auth/logout", null);
            LoginRequest request = new LoginRequest();
            request.Email = "anele@example.com";
            request.Password = "WrongPassword";

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/login", request);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/profile")).StatusCode);
        }

        [Fact]
        public async Task Register_RejectsAnEmailAlreadyUsedByTheOtherRole()
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient first = factory.Browser();
            using HttpClient second = factory.Browser();
            await Register(first, "Participant");

            // Act
            HttpResponseMessage response = await Register(second, "Organiser", "ANELE@example.com");

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Theory]
        [InlineData("Admin", "RaceDay123!", "anele@example.com")]
        [InlineData("Participant", "short", "anele@example.com")]
        [InlineData("Participant", "RaceDay123!", "not-an-email")]
        public async Task Register_RejectsInvalidDetails(string role, string password, string email)
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient client = factory.Browser();
            RegisterRequest request = new RegisterRequest();
            request.FirstName = "Anele";
            request.LastName = "Mokoena";
            request.Role = role;
            request.Email = email;
            request.Password = password;

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/auth/register", request);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/profile")]
        [InlineData("/api/enrolments/me")]
        [InlineData("/api/enrolments/1")]
        [InlineData("/api/results/me")]
        [InlineData("/api/events/1/enrolments")]
        [InlineData("/api/events/1/results")]
        public async Task ProtectedEndpoint_RejectsLoggedOutUser(string path)
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient client = factory.Browser();

            // Act
            HttpResponseMessage response = await client.GetAsync(path);

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Participant_CannotCreateAnEvent()
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient client = factory.Browser();
            await Register(client, "Participant");
            EventRequest request = EventDetails(1, 1);

            // Act
            HttpResponseMessage response = await client.PostAsJsonAsync("/api/events", request);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("/api/enrolments/me")]
        [InlineData("/api/results/me")]
        public async Task Organiser_CannotUseParticipantFeatures(string path)
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient client = factory.Browser();
            await Register(client, "Organiser");

            // Act
            HttpResponseMessage response = await client.GetAsync(path);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ProfileUpdate_OnlyChangesTheCurrentUser()
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient first = factory.Browser();
            using HttpClient second = factory.Browser();
            await Register(first, "Participant");
            await Register(second, "Participant", "second@example.com");
            ProfileRequest request = new ProfileRequest { FirstName = "Changed", LastName = "Name" };

            // Act
            HttpResponseMessage response = await first.PutAsJsonAsync("/api/profile", request);
            ProfileResponse firstProfile =
                (await first.GetFromJsonAsync<ProfileResponse>("/api/profile"))!;
            ProfileResponse secondProfile =
                (await second.GetFromJsonAsync<ProfileResponse>("/api/profile"))!;

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Changed", firstProfile.FirstName);
            Assert.Equal("Anele", secondProfile.FirstName);
        }

        [Fact]
        public async Task Organiser_CanCreateReadUpdateAndDeleteAnEvent()
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient organiser = factory.Browser();
            await Register(organiser, "Organiser");
            RaceEvent race = await AddEvent(organiser);

            // Act
            HttpResponseMessage get = await organiser.GetAsync("/api/events/" + race.EventID);
            EventRequest changes = EventDetails(race.CategoryID, race.RouteID);
            changes.EventName = "Updated run";
            HttpResponseMessage update =
                await organiser.PutAsJsonAsync("/api/events/" + race.EventID, changes);
            HttpResponseMessage delete = await organiser.DeleteAsync("/api/events/" + race.EventID);
            HttpResponseMessage missing = await organiser.GetAsync("/api/events/" + race.EventID);

            // Assert
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            Assert.Equal(HttpStatusCode.OK, update.StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        }

        [Fact]
        public async Task AnotherOrganiser_CannotChangeOrInspectYourEvent()
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient owner = factory.Browser();
            using HttpClient other = factory.Browser();
            await Register(owner, "Organiser");
            await Register(other, "Organiser", "other@example.com");
            RaceEvent race = await AddEvent(owner);

            // Act
            HttpResponseMessage update = await other.PutAsJsonAsync("/api/events/" + race.EventID,
                EventDetails(race.CategoryID, race.RouteID));
            HttpResponseMessage delete = await other.DeleteAsync("/api/events/" + race.EventID);
            HttpResponseMessage enrolments =
                await other.GetAsync("/api/events/" + race.EventID + "/enrolments");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, enrolments.StatusCode);
        }

        [Fact]
        public async Task Participant_CanEnrolButCannotEnrolTwice()
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient organiser = factory.Browser();
            using HttpClient participant = factory.Browser();
            await Register(organiser, "Organiser");
            await Register(participant, "Participant", "participant@example.com");
            RaceEvent race = await AddEvent(organiser);
            EnrolRequest request = new EnrolRequest { CategoryId = race.CategoryID };

            // Act
            HttpResponseMessage first = await participant.PostAsJsonAsync(
                "/api/events/" + race.EventID + "/enrolments", request);
            HttpResponseMessage duplicate = await participant.PostAsJsonAsync(
                "/api/events/" + race.EventID + "/enrolments", request);
            List<Enrollment> enrollments =
                (await participant.GetFromJsonAsync<List<Enrollment>>("/api/enrolments/me"))!;

            // Assert
            Assert.Equal(HttpStatusCode.Created, first.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
            Assert.Single(enrollments);
            Assert.Equal(race.CategoryID, enrollments[0].CategoryID);
        }

        [Fact]
        public async Task Result_IsOnlyShownToItsParticipant()
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient organiser = factory.Browser();
            using HttpClient participant = factory.Browser();
            using HttpClient other = factory.Browser();
            await Register(organiser, "Organiser");
            await Register(participant, "Participant", "participant@example.com");
            await Register(other, "Participant", "other@example.com");
            RaceEvent race = await AddEvent(organiser);
            HttpResponseMessage enrollmentResponse = await participant.PostAsJsonAsync(
                "/api/events/" + race.EventID + "/enrolments",
                new EnrolRequest { CategoryId = race.CategoryID });
            Enrollment enrollment = (await enrollmentResponse.Content.ReadFromJsonAsync<Enrollment>())!;

            EventRequest changes = EventDetails(race.CategoryID, race.RouteID);
            changes.EventDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2));
            await organiser.PutAsJsonAsync("/api/events/" + race.EventID, changes);

            ResultRequest request = new ResultRequest();
            request.FinishTime = new TimeOnly(1, 2, 34);
            request.Position = 12;

            // Act
            HttpResponseMessage response = await organiser.PostAsJsonAsync(
                "/api/enrolments/" + enrollment.EnrollmentID + "/result", request);
            List<RaceResult> ownResults =
                (await participant.GetFromJsonAsync<List<RaceResult>>("/api/results/me"))!;
            List<RaceResult> otherResults =
                (await other.GetFromJsonAsync<List<RaceResult>>("/api/results/me"))!;

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Single(ownResults);
            Assert.Equal(12, ownResults[0].Position);
            Assert.Empty(otherResults);
        }

        [Fact]
        public async Task Swagger_ShowsTheRaceDayEndpoints()
        {
            // Arrange
            using ApiFactory factory = new ApiFactory();
            using HttpClient client = factory.Browser();

            // Act
            HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument document = JsonDocument.Parse(json);
            JsonElement paths = document.RootElement.GetProperty("paths");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(paths.TryGetProperty("/api/auth/register", out _));
            Assert.True(paths.TryGetProperty("/api/events", out _));
            Assert.True(paths.TryGetProperty("/api/events/{id}/enrolments", out _));
            Assert.True(paths.TryGetProperty("/api/results/me", out _));
        }
    }
}

