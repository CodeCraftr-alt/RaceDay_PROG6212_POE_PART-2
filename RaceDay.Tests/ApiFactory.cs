using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Data;

namespace RaceDay.Tests
{
    // Starts a separate copy of the API for each HTTP test.
    public class ApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            string databaseName = Guid.NewGuid().ToString();

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<RaceDayContext>();
                services.RemoveAll<DbContextOptions<RaceDayContext>>();

                services.AddDbContext<RaceDayContext>(options =>
                    options.UseInMemoryDatabase(databaseName));
            });
        }

        public HttpClient Browser()
        {
            WebApplicationFactoryClientOptions options = new WebApplicationFactoryClientOptions();
            options.BaseAddress = new Uri("https://localhost");
            options.HandleCookies = true;
            options.AllowAutoRedirect = false;

            return CreateClient(options);
        }
    }
}

