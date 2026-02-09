using Microsoft.EntityFrameworkCore;
using RunCutWeb.Domain.Entities;
using RouteEntity = RunCutWeb.Domain.Entities.Route;

namespace RunCutWeb.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataSet> DataSets { get; set; }
    public DbSet<RouteEntity> Routes { get; set; }
    public DbSet<Stop> Stops { get; set; }
    public DbSet<StopTiming> StopTimings { get; set; }
    public DbSet<Trip> Trips { get; set; }
    public DbSet<CalendarDate> CalendarDates { get; set; }
    public DbSet<DDate> DDate { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure DataSet
        modelBuilder.Entity<DataSet>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedDate).IsRequired();
        });

        // Configure Route
        modelBuilder.Entity<RouteEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RouteId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => new { e.DataSetId, e.RouteId });
            entity.HasOne(e => e.DataSet)
                .WithMany(d => d.Routes)
                .HasForeignKey(e => e.DataSetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Stop
        modelBuilder.Entity<Stop>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StopId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StopLat).HasPrecision(18, 6);
            entity.Property(e => e.StopLon).HasPrecision(18, 6);
            entity.HasIndex(e => new { e.DataSetId, e.StopId });
            entity.HasOne(e => e.DataSet)
                .WithMany(d => d.Stops)
                .HasForeignKey(e => e.DataSetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure StopTiming
        modelBuilder.Entity<StopTiming>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TripId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StopId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ShapeDistTraveled).HasPrecision(18, 6);
            entity.HasIndex(e => new { e.DataSetId, e.TripId, e.StopSequence });
            entity.HasOne(e => e.DataSet)
                .WithMany(d => d.StopTimings)
                .HasForeignKey(e => e.DataSetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Trip
        modelBuilder.Entity<Trip>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TripId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.RouteId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ServiceId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => new { e.DataSetId, e.TripId });
            entity.HasOne(e => e.DataSet)
                .WithMany(d => d.Trips)
                .HasForeignKey(e => e.DataSetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure CalendarDate
        modelBuilder.Entity<CalendarDate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServiceId).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => new { e.DataSetId, e.ServiceId, e.Date });
            entity.HasOne(e => e.DataSet)
                .WithMany(d => d.CalendarDates)
                .HasForeignKey(e => e.DataSetId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure d_Date (date dimension)
        modelBuilder.Entity<DDate>(entity =>
        {
            entity.HasKey(e => e.DateKey);
            entity.Property(e => e.DayName).HasMaxLength(20);
        });
    }
}
