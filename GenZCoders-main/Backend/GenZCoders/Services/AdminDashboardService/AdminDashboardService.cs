using GenZCoders.DTOs.DashBoardsDto;
using GenZCoders.Repos.AdminDashboardRepo;

namespace GenZCoders.Services.AdminDashboardService
{
    public class AdminDashboardService : IAdminDashboardService
    {
        private readonly IAdminDashboardRepo _repo;

        public AdminDashboardService(IAdminDashboardRepo repo)
        {
            _repo = repo;
        }

        public async Task<AdminDashboardDto> GetDashboardAsync()
        {
            // =========================
            // USERS - GenZCoders ONLY
            // =========================

            var totalUsers = await _repo.GetTotalUsersAsync();

            // GenZCoders role = Student
            var totalStudents =
                await _repo.GetTotalUsersByRoleAsync("Student");

            // GenZCoders role = co-Instructor
            var totalInstructors =
                await _repo.GetTotalUsersByRoleAsync("co-Instructor");

            // There is currently NO Admin role for GenZCoders
            var totalAdmins =
                await _repo.GetTotalUsersByRoleAsync("Admin");

            var activeUsers =
                await _repo.GetActiveUsersAsync();

            // =========================
            // COURSES
            // =========================

            var totalCourses =
                await _repo.GetTotalCoursesAsync();

            var totalCourseRounds =
                await _repo.GetTotalCourseRoundsAsync();

            // =========================
            // APPLICATIONS
            // =========================

            var totalApplications =
                await _repo.GetTotalApplicationsAsync();

            var pendingApplications =
                await _repo.GetPendingApplicationsAsync();

            // =========================
            // ENROLLMENTS
            // =========================

            var totalEnrollments =
                await _repo.GetTotalEnrollmentsAsync();

            // =========================
            // RECENT REGISTRATIONS
            // =========================

            var recentRegistrations =
                await _repo.GetRecentRegistrationsAsync();

            // =========================
            // USERS BY ROLE
            // =========================

            var usersByRoleRaw =
                await _repo.GetUsersByRoleAsync();

            var usersByRole = usersByRoleRaw
                .Select(x => new UsersByRoleDto
                {
                    Role = x.Role,
                    Count = x.Count
                })
                .ToList();

            // =========================
            // RECENT APPLICATIONS
            // =========================

            var recentApplicationsRaw =
                await _repo.GetRecentApplicationsAsync();
var recentApplications = recentApplicationsRaw
    .Select(x => new RecentApplicationDto
    {
        Id = x.Id,
        CourseId = x.CourseId,
        CourseTitle = x.CourseTitle,
        FullName = x.FullName,
        Email = x.Email,
        Price = x.Price,
        InstructorName = x.InstructorName,
        ApplicationDate = x.ApplicationDate,
        StatusId = x.StatusId,
        StatusName = x.StatusName
    })
    .ToList();
            // =========================
            // RETURN DASHBOARD
            // =========================

            return new AdminDashboardDto
            {
                Summary = new DashboardSummaryDto
                {
                    TotalUsers = totalUsers,
                    TotalStudents = totalStudents,
                    TotalInstructors = totalInstructors,
                    TotalAdmins = totalAdmins,
                    ActiveUsers = activeUsers,

                    TotalCourses = totalCourses,
                    TotalCourseRounds = totalCourseRounds,

                    TotalApplications = totalApplications,
                    PendingApplications = pendingApplications,

                    TotalEnrollments = totalEnrollments,

                    RecentRegistrations = recentRegistrations
                },

                UsersByRole = usersByRole,

                RecentApplications = recentApplications
            };
        }
    }
}