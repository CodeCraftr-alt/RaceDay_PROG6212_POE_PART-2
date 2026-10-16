using Microsoft.EntityFrameworkCore;
using RaceDay.Data;

// Set up the services used by the API.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "RaceDay.xml"));
});

// This database is kept in memory. It is cleared when the API stops.
builder.Services.AddDbContext<RaceDayContext>(options =>
    options.UseInMemoryDatabase("RaceDayDatabase"));

// Session stores the logged-in user's ID and role on the server.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

// Session must be ready before a controller tries to read it.
app.UseSession();
app.MapControllers();

app.Run();

// The HTTP tests use this class to start a test copy of the API.
public partial class Program
{
}

