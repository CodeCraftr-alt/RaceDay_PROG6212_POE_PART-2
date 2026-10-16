# Reading the RaceDay code

Read the files in this order: Category.cs, RaceDayContext.cs, Program.cs, CategoriesController.cs, then the tests. These show the main pattern before you move on to accounts and events.

## The model

Category.cs describes one stored category.

- `int` holds a whole number.
- `string` holds text.
- `string?` means the text can be missing, or null.
- `get; set;` lets the property be read and changed.
- `[Key]` tells EF Core which property identifies the record.
- `= ""` starts a text property with an empty string.

One Category object might hold ID 1 and the name "10 km". The other models work the same way.

## The database context

RaceDayContext gives the controllers access to stored records.

```csharp
public DbSet<Category> Categories { get; set; }
```

Think of a DbSet as the collection of one kind of record managed by EF Core.

The context constructor receives the database settings from Program.cs. `base(options)` passes those settings to the DbContext class that RaceDayContext inherits from.

## Program.cs

This line chooses the temporary database:

```csharp
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseInMemoryDatabase("RaceDayDatabase"));
```

The `options =>` part means "use these settings". The data is stored in memory and is cleared when the application stops.

AddControllers sets up API controllers. AddSwaggerGen prepares the Swagger descriptions. AddSession sets up session storage. UseSession must run before a controller reads a session. MapControllers connects controller methods to their URLs.

## A controller constructor

```csharp
private readonly RaceDayContext _context;

public CategoriesController(RaceDayContext context)
{
    _context = context;
}
```

ASP.NET Core supplies the context when it creates the controller. Private means the field is used inside this class. Readonly means it is assigned when the controller is created.

## GET

```csharp
[HttpGet]
public async Task<IActionResult> List()
{
    List<Category> categories = await _context.Categories.ToListAsync();
    return Ok(categories);
}
```

HttpGet connects this method to GET /api/categories. ToListAsync reads the categories into a list. Await waits for that operation to finish. Ok sends the list as JSON with status 200.

IActionResult allows different HTTP results, such as Ok or NotFound. Task is used because the method runs asynchronously.

## Finding a record

```csharp
Category? category = await _context.Categories.FindAsync(id);

if (category == null)
{
    return NotFound("Category not found.");
}
```

FindAsync searches by the key. Null means no record was found. The if statement returns 404 before the code tries to change a missing record.

## Creating and saving

```csharp
Category category = new Category();
category.CategoryName = request.CategoryName;
category.Description = request.Description;

_context.Categories.Add(category);
await _context.SaveChangesAsync();
```

This creates an object, fills in its properties and saves it. EF Core gives it an ID. Created returns status 201.

PUT finds a record and changes its properties. DELETE finds it and calls Remove. Both call SaveChangesAsync afterwards. A successful delete returns NoContent, which means status 204.

## Request classes

RequestAndResponseModels contains the fields a client is allowed to send. CategoryRequest accepts a name and description, but not the database ID.

Required means a value is needed. StringLength limits text length. Range limits a number.

The ApiController attribute makes ASP.NET Core check these rules for HTTP requests. Invalid input returns 400 before the controller method runs. Calling a controller directly in a unit test does not run this validation, so the HTTP tests check it.

## Sessions and roles

After registration or login, AuthController saves:

```csharp
HttpContext.Session.SetInt32("UserId", profile.UserId);
HttpContext.Session.SetString("Role", profile.Role);
```

The browser keeps a cookie identifying the session. The ID and role stay on the server.

The controllers inherit two small helpers from ApiControllerBase. GetUserId reads the ID. CheckAccess checks login and role.

```csharp
IActionResult? accessError = CheckAccess("Organiser");
if (accessError != null)
{
    return accessError;
}
```

Null means there was no error, so the method continues. Otherwise it returns the error response. Missing login gives 401; the wrong role gives 403.

Ownership is checked separately. An Organiser may only change an event when its OrganizerID matches their own ID. Participants may only access enrolments with their ParticipantID.

The REST guide describes stateless APIs. The assignment specifically asks for sessions, so this implementation keeps authentication state on the server.

## Passwords

Registration uses:

```csharp
string passwordHash = _passwordHasher.HashPassword(email, request.Password);
```

PasswordHasher uses salted PBKDF2 hashing. The salt is random data used during hashing. Only the hash is saved. Login uses VerifyHashedPassword to check the entered password against it; it does not decrypt the hash.

The string type parameter means the hasher accepts a string as its user parameter. This project passes the email. It does not mean normal string manipulation protects the password.

## Events and categories

An event needs a route and at least one category. The route supplies its distance. EventCategory links an event to a category it offers. The default category is always included, and extra categories are optional.

EventsController checks that the route and categories exist. Update also checks that capacity is not lower than current enrolments and that no used category is removed.

## Enrolments

Before saving, EnrolmentsController checks that:

1. The user is a Participant.
2. The event exists, is open and has not started.
3. The selected category belongs to the event.
4. The Participant is not already enrolled.
5. The event has space.
6. The optional bib number is not taken.

The Participant ID comes from the session. A client cannot choose another person's ID in the request.

Cancellation keeps the record with Cancelled status and frees the bib number. The Participant can enter again using that record.

## Short query conditions

```csharp
bool nameExists = await _context.Categories.AnyAsync(c => c.CategoryName == name);
```

Read this as "Is there any category whose name matches this name?" The letter c is the category being checked.

Where filters records. CountAsync counts matches. FirstOrDefaultAsync finds the first match or returns null. OrderBy puts records in order. Foreach works through a list one item at a time.

## Results and deleting events

ResultsController finds an enrolment and its event, checks ownership and the event's start time, then saves a finish time and position. Recording a result marks the enrolment Completed.

Participants only receive results connected to their own enrolments.

Deleting an event explicitly removes its results, enrolments and category links. InMemory does not enforce SQL relationships or cascade deletion.

## Tests

Start with GetCategories_ReturnsAllCategories in ControllerUnitTests.cs.

- Arrange prepares the database, data and controller.
- Act calls the method.
- Assert checks the response and stored records.

Each test uses Guid.NewGuid().ToString() as its database name. This gives it a fresh database.

TestSession supplies a session when a unit test calls a controller directly. ApiFactory starts a test copy of the API. ApiTests sends HTTP requests so it can check real session cookies and request validation.

## How this relates to Part 1

The classes keep the original entities: Organiser, Participant, Category, RaceRoute, RaceEvent, Enrollment and RaceResult. EventCategory allows several categories per event. Event descriptions and types were added for Part 2, and each enrolment stores the selected category.

The original SQL-style names OrganizerID and EnrollmentID remain. URLs use enrolments, as in the endpoint plan. The InMemory version does not reproduce or enforce the original SQL schema.

