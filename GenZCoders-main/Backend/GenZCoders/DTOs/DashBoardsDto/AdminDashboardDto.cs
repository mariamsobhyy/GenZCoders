namespace GenZCoders.DTOs.DashBoardsDto
{
    public class AdminDashboardDto
    {
        public DashboardSummaryDto Summary { get; set; } = new();
        public List<UsersByRoleDto> UsersByRole { get; set; } = new();
        public List<RecentApplicationDto> RecentApplications { get; set; } = new();
    }

    public class DashboardSummaryDto
    {
        public long TotalUsers { get; set; }
        public long TotalStudents { get; set; }
        public long TotalInstructors { get; set; }
        public long TotalAdmins { get; set; }
        public long ActiveUsers { get; set; }

        public long TotalCourses { get; set; }
        public long TotalCourseRounds { get; set; }

        public long TotalApplications { get; set; }
        public long PendingApplications { get; set; }
        public long TotalEnrollments { get; set; }
        public long RecentRegistrations { get; set; }
    }

    public class UsersByRoleDto
    {
        public string Role { get; set; } = null!;
        public long Count { get; set; }
    }

 public class RecentApplicationDto
{
    public long Id { get; set; }

    public long CourseId { get; set; }

    public string CourseTitle { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public decimal? Price { get; set; }

    public string? InstructorName { get; set; }

    public DateTime ApplicationDate { get; set; }

    public long StatusId { get; set; }

    public string StatusName { get; set; } = null!;
}
}