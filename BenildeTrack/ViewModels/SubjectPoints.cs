using BenildeTrack.Models;

namespace BenildeTrack.ViewModels;

// One row of the Course Panel: a subject and the student's dropout points in it.
public class SubjectPoints
{
    public Subject Subject { get; set; } = null!;
    public int Absences { get; set; }
    public int Lates { get; set; }

    // Records waiting for a professor's decision, and records that could still get an excuse.
    public int PendingReview { get; set; }
    public int Excusable { get; set; }

    public int Points => Absences * AttendanceRecord.AbsentPoints + Lates * AttendanceRecord.LatePoints;
    public int PointsLeft => Math.Max(0, Subject.MaxPoints - Points);
    public int Percent => PercentOf(Points, Subject.MaxPoints);
    public string Color => ColorOf(Percent);

    // Shared helpers so the student and professor pages color things.
    public static int PercentOf(int points, int max) => max == 0 ? 0 : Math.Min(100, points * 100 / max);
    public static string ColorOf(int percent) => percent >= 80 ? "danger" : percent >= 50 ? "warning" : "success";
}
