using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Data
{
    public class RaceDayContext : DbContext
    {
        public RaceDayContext(DbContextOptions<RaceDayContext> options)
            : base(options)
        {
        }

        public DbSet<Organiser> Organisers { get; set; }
        public DbSet<Participant> Participants { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<RaceRoute> Routes { get; set; }
        public DbSet<RaceEvent> Events { get; set; }
        public DbSet<EventCategory> EventCategories { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<RaceResult> Results { get; set; }
    }
}

