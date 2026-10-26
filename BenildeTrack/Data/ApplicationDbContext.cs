using BenildeTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Data;

// The gateway to the database. Each DbSet below becomes one table.
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<AttendanceRecord> AttendanceRecords => Set<AttendanceRecord>();
    public DbSet<Violation> Violations => Set<Violation>();
    public DbSet<Requirement> Requirements => Set<Requirement>();
    public DbSet<CompletedRequirement> CompletedRequirements => Set<CompletedRequirement>();
    public DbSet<HandbookSection> HandbookSections => Set<HandbookSection>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Identity needs this line, do not remove it.
        base.OnModelCreating(builder);

        // Safety nets: the database itself refuses duplicates.
        builder.Entity<Enrollment>().HasIndex(e => new { e.StudentId, e.CourseId }).IsUnique();
        builder.Entity<CompletedRequirement>().HasIndex(c => new { c.StudentId, c.RequirementId }).IsUnique();
        builder.Entity<AttendanceRecord>().HasIndex(a => new { a.StudentId, a.CourseId, a.Date }).IsUnique();
    }
}
