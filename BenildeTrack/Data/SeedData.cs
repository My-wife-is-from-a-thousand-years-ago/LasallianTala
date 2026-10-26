using BenildeTrack.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BenildeTrack.Data;

// Runs at startup: creates the database tables, the roles, and (in development) demo data.
public static class SeedData
{
    // Demo only. Every demo account uses this password.
    private const string DemoPassword = "Demo@12345";

    public static async Task InitializeAsync(IServiceProvider services, bool addDemoData)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        // Roles are real data, so they are created in every environment.
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AppRoles.Student, AppRoles.Teacher, AppRoles.Admin })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (addDemoData)
            await AddDemoDataAsync(services, db);
    }

    private static async Task AddDemoDataAsync(IServiceProvider services, ApplicationDbContext db)
    {
        // Only fill an empty database.
        if (await db.Users.AnyAsync()) return;

        var users = services.GetRequiredService<UserManager<ApplicationUser>>();
        await CreateUserAsync(users, "admin@demo.test", "Demo Admin", AppRoles.Admin);
        var teacher = await CreateUserAsync(users, "teacher@demo.test", "Prof. Santos", AppRoles.Teacher);
        var ana = await CreateUserAsync(users, "ana@demo.test", "Ana Reyes", AppRoles.Student);
        var ben = await CreateUserAsync(users, "ben@demo.test", "Ben Cruz", AppRoles.Student);

        // Two courses: one with a 9-point limit and one with an 18-point limit.
        var demo101 = new Course { Code = "DEMO101", Subject = "Intro to Demo", MaxPoints = 9, TeacherId = teacher.Id };
        var demo102 = new Course { Code = "DEMO102", Subject = "Demo Practice", MaxPoints = 18, TeacherId = teacher.Id };
        db.Courses.AddRange(demo101, demo102);

        // The shared checklist. The four objectives are the real ones; clearances are placeholders.
        var benire1 = new Requirement { Kind = RequirementKind.Objective, Name = "Benire 1", Guide = "Complete CSBLIFE subject" };
        db.Requirements.AddRange(
            benire1,
            new Requirement { Kind = RequirementKind.Objective, Name = "Benire 2", Guide = "Complete REEXECO subject" },
            new Requirement { Kind = RequirementKind.Objective, Name = "Benire 3", Guide = "Complete [WIP]" },
            new Requirement { Kind = RequirementKind.Objective, Name = "BAWC Interview", Guide = "Go to room 300" },
            new Requirement { Kind = RequirementKind.Clearance, Name = "Library clearance", Guide = "PLACEHOLDER: return all borrowed books" },
            new Requirement { Kind = RequirementKind.Clearance, Name = "Finance clearance", Guide = "PLACEHOLDER: settle any balance" });

        // PLACEHOLDER handbook text. Replace with the real handbook.
        db.HandbookSections.AddRange(
            new HandbookSection { Number = "X.1", Title = "Disruptive noise in class", Text = "PLACEHOLDER. Shouting or loud noise that disrupts a class." },
            new HandbookSection { Number = "X.2", Title = "Academic dishonesty", Text = "PLACEHOLDER. Copying work or using unauthorized help during an exam." },
            new HandbookSection { Number = "X.3", Title = "Dress code", Text = "PLACEHOLDER. Wearing the proper uniform or attire on campus." },
            new HandbookSection { Number = "X.4", Title = "Attendance and dropout points", Text = "PLACEHOLDER. An absence is 3 points and a late is 1 point. Reaching the course limit means dropout." });

        // Save now so the courses get their Ids.
        await db.SaveChangesAsync();

        db.Enrollments.AddRange(
            new Enrollment { StudentId = ana.Id, CourseId = demo101.Id },
            new Enrollment { StudentId = ana.Id, CourseId = demo102.Id },
            new Enrollment { StudentId = ben.Id, CourseId = demo101.Id });

        // Ana: 4 of 9 points in DEMO101 and 1 of 18 in DEMO102. Ben: 7 of 9 in DEMO101.
        var today = DateTime.Today;
        db.AttendanceRecords.AddRange(
            new AttendanceRecord { StudentId = ana.Id, CourseId = demo101.Id, Date = today.AddDays(-14), Type = AttendanceType.Absent },
            new AttendanceRecord { StudentId = ana.Id, CourseId = demo101.Id, Date = today.AddDays(-7), Type = AttendanceType.Late },
            new AttendanceRecord { StudentId = ana.Id, CourseId = demo102.Id, Date = today.AddDays(-3), Type = AttendanceType.Late },
            new AttendanceRecord { StudentId = ben.Id, CourseId = demo101.Id, Date = today.AddDays(-21), Type = AttendanceType.Absent },
            new AttendanceRecord { StudentId = ben.Id, CourseId = demo101.Id, Date = today.AddDays(-14), Type = AttendanceType.Absent },
            new AttendanceRecord { StudentId = ben.Id, CourseId = demo101.Id, Date = today.AddDays(-7), Type = AttendanceType.Late });

        db.Violations.Add(new Violation { StudentId = ana.Id, Title = "Disruptive noise (demo)", Description = "Demo violation: loud disruption during class.", Date = today.AddDays(-5) });
        db.CompletedRequirements.Add(new CompletedRequirement { StudentId = ana.Id, RequirementId = benire1.Id, DateCompleted = today.AddDays(-30) });

        await db.SaveChangesAsync();
    }

    // Creates one account and gives it a role. Throws a readable error if Identity rejects it.
    private static async Task<ApplicationUser> CreateUserAsync(UserManager<ApplicationUser> users, string email, string fullName, string role)
    {
        var user = new ApplicationUser { UserName = email, Email = email, FullName = fullName, EmailConfirmed = true };
        var result = await users.CreateAsync(user, DemoPassword);
        if (!result.Succeeded)
            throw new Exception("Could not create " + email + ": " + string.Join("; ", result.Errors.Select(e => e.Description)));

        await users.AddToRoleAsync(user, role);
        return user;
    }
}
