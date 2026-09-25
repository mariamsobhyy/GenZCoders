namespace GenZCoders.Repos.AdminDashboardRepo
{
    public interface IAdminDashboardRepo
    {
        Task<long> GetTotalUsersAsync();
        Task<long> GetTotalUsersByRoleAsync(string roleName);
        Task<long> GetActiveUsersAsync();

        Task<long> GetTotalCoursesAsync();
        Task<long> GetTotalCourseRoundsAsync();

        Task<long> GetTotalApplicationsAsync();
        Task<long> GetPendingApplicationsAsync();
        Task<long> GetTotalEnrollmentsAsync();
        Task<long> GetRecentRegistrationsAsync();

        Task<List<(string Role, long Count)>> GetUsersByRoleAsync();

        Task<List<RecentApplicationData>> GetRecentApplicationsAsync(int count = 5);
    }

  public class RecentApplicationData
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