
using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Repos.AdminDashboardRepo
{
    public class AdminDashboardRepo : IAdminDashboardRepo
    {
        private readonly SchoolDbContext _context;

        public AdminDashboardRepo(SchoolDbContext context)
        {
            _context = context;
        }

        // =========================
        // USERS - GenZCoders ONLY
        // =========================

        public async Task<long> GetTotalUsersAsync()
        {
            return await _context.Accounts
                .Where(a => a.Role.BusinessEntity == "GenZCoders")
                .LongCountAsync();
        }

        public async Task<long> GetTotalUsersByRoleAsync(string roleName)
        {
            return await _context.Accounts
                .Where(a =>
                    a.Role.BusinessEntity == "GenZCoders" &&
                    a.Role.RoleName == roleName)
                .LongCountAsync();
        }

        public async Task<long> GetActiveUsersAsync()
        {
            return await _context.Accounts
                .Where(a =>
                    a.Role.BusinessEntity == "GenZCoders" &&
                    a.IsActive)
                .LongCountAsync();
        }

        // =========================
        // COURSES - GenZCoders ONLY
        // =========================

        public async Task<long> GetTotalCoursesAsync()
        {
            return await _context.Courses
                .Where(c => c.BusinessEntity == "GenZCoders")
                .LongCountAsync();
        }

        public async Task<long> GetTotalCourseRoundsAsync()
        {
            return await _context.CourseRounds
                .Where(cr => cr.Course.BusinessEntity == "GenZCoders")
                .LongCountAsync();
        }

        // =========================
        // APPLICATIONS - GenZCoders ONLY
        // =========================

        public async Task<long> GetTotalApplicationsAsync()
        {
            return await _context.Applications
                .Where(a =>
                    a.CourseRound.Course.BusinessEntity == "GenZCoders")
                .LongCountAsync();
        }

        public async Task<long> GetPendingApplicationsAsync()
        {
            return await _context.Applications
                .Where(a =>
                    a.StatusId == 14 &&
                    a.CourseRound.Course.BusinessEntity == "GenZCoders")
                .LongCountAsync();
        }

        // =========================
        // ENROLLMENTS - GenZCoders ONLY
        // =========================

        public async Task<long> GetTotalEnrollmentsAsync()
        {
            return await _context.Applications
                .Where(a =>
                    a.StatusId == 42 &&
                    a.CourseRound.Course.BusinessEntity == "GenZCoders")
                .LongCountAsync();
        }

        // =========================
        // RECENT REGISTRATIONS
        // GenZCoders ONLY
        // =========================

        public async Task<long> GetRecentRegistrationsAsync()
        {
            var fromDate = DateOnly.FromDateTime(
                DateTime.UtcNow.AddDays(-30));

            return await _context.Accounts
                .Where(a =>
                    a.Role.BusinessEntity == "GenZCoders" &&
                    a.CreatedAt >= fromDate)
                .LongCountAsync();
        }

        // =========================
        // USERS BY ROLE
        // GenZCoders ONLY
        // =========================

        public async Task<List<(string Role, long Count)>> GetUsersByRoleAsync()
        {
            var result = await _context.Accounts
                .Where(a => a.Role.BusinessEntity == "GenZCoders")
                .GroupBy(a => a.Role.RoleName)
                .Select(g => new
                {
                    Role = g.Key,
                    Count = g.LongCount()
                })
                .OrderByDescending(x => x.Count)
                .ToListAsync();

            return result
                .Select(x => (x.Role, x.Count))
                .ToList();
        }

        // =========================
        // RECENT APPLICATIONS
        // GenZCoders ONLY
        // =========================

       
   public async Task<List<RecentApplicationData>> GetRecentApplicationsAsync(
    int count = 5)
{
    return await _context.Applications
        .Where(a =>
            a.CourseRound.Course.BusinessEntity == "GenZCoders")
        .Include(a => a.Account)
        .Include(a => a.Status)
        .Include(a => a.CourseRound)
            .ThenInclude(cr => cr.Course)
        .Include(a => a.CourseRound)
            .ThenInclude(cr => cr.CourseRoundInstructors)
                .ThenInclude(cri => cri.Instructor)
        .OrderByDescending(a => a.ApplicationDate)
        .Take(count)
        .Select(a => new RecentApplicationData
        {
            Id = a.Id,

            CourseId = a.CourseRound.CourseId,

            CourseTitle = a.CourseRound.Course.Title,

            FullName = a.Account.FullNameEn,

            Email = a.Account.Email,

            Price = a.CourseRound.Price,

            InstructorName = a.CourseRound.CourseRoundInstructors
                .Select(cri => cri.Instructor.FullNameEn)
                .FirstOrDefault(),

            ApplicationDate = a.ApplicationDate,

            StatusId = a.StatusId,

            StatusName = a.Status.StatusName
        })
        .ToListAsync();

    }
}

}
