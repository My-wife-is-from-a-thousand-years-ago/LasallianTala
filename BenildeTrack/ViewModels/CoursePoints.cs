using BenildeTrack.Models;

namespace BenildeTrack.ViewModels;

// One row of the Course Panel: a course and the student's dropout points in it.
public class CoursePoints
{
    public Course Course { get; set; } = null!;
    public int Absences { get; set; }
    public int Lates { get; set; }

    public int Points => Absences * AttendanceRecord.AbsentPoints + Lates * AttendanceRecord.LatePoints;
    public int PointsLeft => Math.Max(0, Course.MaxPoints - Points);
    public int Percent => Course.MaxPoints == 0 ? 0 : Math.Min(100, Points * 100 / Course.MaxPoints);

    // Bootstrap color for the progress bar. These cut-offs are my guess, so change them freely.
    public string Color => Percent >= 80 ? "danger" : Percent >= 50 ? "warning" : "success";
}
