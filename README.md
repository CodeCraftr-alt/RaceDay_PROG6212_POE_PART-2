# RaceDay Part 2

RaceDay is an ASP.NET Core Web API for running, walking and cycling events. Organisers manage events. Participants enter events and view their results.

This version uses **EF Core InMemory**, as requested. There is no SQL setup. Accounts, events and other added data disappear when the API stops.

## Run it

Open `RaceDay.sln` in Visual Studio, set RaceDay as the startup project, choose the `http` profile and run it. Open [Swagger](http://localhost:5024/swagger).

Or use a terminal in this folder:

```powershell
dotnet restore RaceDay.sln
dotnet run --project RaceDay/RaceDay.csproj --launch-profile http
```

You need a compatible .NET SDK and the .NET 8 runtime. There are no preset accounts or events. No connection string or migration is needed.

For HTTPS, use the `https` profile and open https://localhost:7150/swagger. If needed, trust the development certificate with `dotnet dev-certs https --trust`.

## Where to start reading

| Folder | What it contains |
| --- | --- |
| Models | One class per file, such as Category and RaceEvent |
| Data | RaceDayContext, which gives access to the stored records |
| RequestAndResponseModels | The fields accepted in requests and returned in a profile |
| Controllers | The methods called by GET, POST, PUT and DELETE |
| RaceDay.Tests | Unit tests and HTTP tests |

Read [the code walkthrough](docs/IMPLEMENTATION.md) for explanations and small examples.

## Roles and login

**Organiser:** manage categories and routes, create events, change their own events, view their event enrolments and record results.

**Participant:** browse events, update their own profile, enrol by choosing a category, cancel their enrolment and view their own results.

A session stores the user's ID and role on the server. Swagger keeps the session cookie automatically. You do not need a bearer token. Logging in as another user switches the session. Logout clears it. An idle session expires after 30 minutes.

Protected controller methods call `CheckAccess`. Ownership checks also compare the logged-in ID with the event's Organiser ID or the enrolment's Participant ID. Passwords are hashed with ASP.NET Core's PasswordHasher; original passwords are never saved.

## Try it in Swagger

### Register an Organiser

Use `POST /api/auth/register`:

```json
{
  "firstName": "Thabo",
  "lastName": "Mokoena",
  "email": "organiser@example.com",
  "password": "RaceDay123!",
  "role": "Organiser"
}
```

### Create a category

Use `POST /api/categories`:

```json
{
  "categoryName": "10 km",
  "description": "10 kilometre road race"
}
```

### Create a route

Use `POST /api/routes`:

```json
{
  "routeName": "Pretoria Central",
  "startLocation": "Church Square",
  "finishLocation": "Church Square",
  "distanceKm": 10,
  "elevationGainM": 45
}
```

### Create an event

Use `POST /api/events`. Use the returned category and route IDs and choose a future date.

```json
{
  "eventName": "Spring Run",
  "description": "A community road race",
  "eventType": "Run",
  "eventDate": "2027-01-24",
  "startTime": "07:00:00",
  "location": "Pretoria",
  "capacity": 100,
  "categoryId": 1,
  "routeId": 1,
  "additionalCategoryIds": [],
  "status": "Open"
}
```

Distance comes from the event's route. To offer more categories, create them and add their IDs to `additionalCategoryIds`.

### Enrol a Participant

Register another account with a different email and `"role": "Participant"`. This switches your session.

Use `POST /api/events/1/enrolments`, replacing 1 with the actual event ID:

```json
{
  "categoryId": 1,
  "bibNumber": 101
}
```

The bib number is optional. Repeat the request to see a 409 duplicate-enrolment response. Check `GET /api/enrolments/me`.

### Record a result

Log back in as the Organiser using `POST /api/auth/login`. View the entries through `GET /api/events/1/enrolments`.

Results can only be entered after an event starts. For a demonstration, update the event with a past date, sending the full body to PUT. Then call `POST /api/enrolments/1/result`, using the actual enrolment ID:

```json
{
  "finishTime": "01:02:34",
  "position": 12
}
```

Log in as the Participant and use `GET /api/results/me` to see the result.

Dates use `yyyy-MM-dd`; times use `HH:mm:ss`. Event dates and start times are South African local time. Audit timestamps use UTC. Finish times must be under 24 hours. PUT needs all required fields.

## Endpoints

| Method | URL | Access |
| --- | --- | --- |
| POST | /api/auth/register, /api/auth/login | Anyone |
| POST | /api/auth/logout | Logged-in users |
| GET, PUT | /api/profile | Own profile |
| GET | /api/categories, /api/routes | Anyone |
| POST | /api/categories, /api/routes | Organiser |
| PUT, DELETE | /api/categories/{id}, /api/routes/{id} | Organiser |
| GET | /api/events, /api/events/{id} | Anyone |
| POST | /api/events | Organiser |
| PUT, DELETE | /api/events/{id} | Event owner |
| GET | /api/events/{id}/enrolments | Event owner |
| POST | /api/events/{id}/enrolments | Participant |
| GET | /api/enrolments/me | Participant |
| GET, DELETE | /api/enrolments/{id} | Enrolment owner |
| POST | /api/enrolments/{id}/result | Event owner |
| GET | /api/results/me | Participant |
| PUT | /api/results/{id} | Event owner |
| GET | /api/events/{id}/results | Event owner |

`GET /api/events?categoryId=1` filters by category. Add `includePast=true` to include past events.

## Tests and GitHub

```powershell
dotnet test RaceDay.sln -m:1
```

In Visual Studio, open Test > Test Explorer and select Run All.

The unit tests use Arrange, Act and Assert, following the supplied guide. Each test gets a fresh InMemory database. The HTTP tests check actual session cookies, validation and role restrictions.

Local verification of this rewrite: **40 tests passed, 0 failed**. This is a local result; the GitHub Actions run has been completed in my repository.

The workflow in `.github/workflows/build-and-test.yml` builds and tests on pushes and pull requests. It expects RaceDay.sln at the repository root. If Part 2 is in a subfolder, place the workflow in the repository's root `.github/workflows` folder and change its three solution paths to the correct subfolder path.

## Storage limits

InMemory is temporary storage for learning and tests. It does not enforce SQL foreign keys or unique indexes. The controllers check references and duplicates themselves and explicitly remove related records when deleting an event. These checks support a local demonstration; simultaneous writes are not protected by SQL transactions.

The original Part 1 files in `docs/part1` are planning references. The app does not run their SQL script. The previous version is backed up in `assignment-reference

## Submission


CI screenshot: **They are part of the video presentation**.

Unlisted YouTube video: **https://youtu.be/LgIe0GqDoaY**.



