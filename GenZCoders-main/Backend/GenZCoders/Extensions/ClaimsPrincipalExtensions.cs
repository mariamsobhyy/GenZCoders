using System.Security.Claims;

namespace GenZCoders.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        // Instructors and managers: course content, weeks, materials, assignments, grading, rosters.
        public const string StaffPolicy = "Staff";

        // Managers (engineer / admin): users, courses, rounds, applications and payment confirmation.
        public const string ManagerPolicy = "Manager";

        // Role IDs the frontend also relies on (see simple-auth-context mapBackendRoleToRole).
        private const string InstructorRoleId = "37";
        private const string EngineerRoleId = "39";

        public static long GetAccountId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue("AccountId");
            return long.TryParse(value, out var id)
                ? id
                : throw new UnauthorizedAccessException("Invalid authentication token.");
        }

        public static bool IsManager(this ClaimsPrincipal user)
        {
            var role = user.GetRoleName();
            return role.Contains("engineer", StringComparison.OrdinalIgnoreCase)
                || role.Contains("admin", StringComparison.OrdinalIgnoreCase)
                || user.FindFirstValue("RoleId") == EngineerRoleId;
        }

        public static bool IsInstructor(this ClaimsPrincipal user)
        {
            var role = user.GetRoleName();
            return role.Contains("instructor", StringComparison.OrdinalIgnoreCase)
                || role.Contains("teacher", StringComparison.OrdinalIgnoreCase)
                || user.FindFirstValue("RoleId") == InstructorRoleId;
        }

        public static bool IsStaff(this ClaimsPrincipal user)
            => user.Identity?.IsAuthenticated == true && (user.IsManager() || user.IsInstructor());

        public static bool IsSelfOrStaff(this ClaimsPrincipal user, long accountId)
            => user.IsStaff() || user.GetAccountId() == accountId;

        private static string GetRoleName(this ClaimsPrincipal user)
            => user.FindFirstValue(ClaimTypes.Role) ?? user.FindFirstValue("role") ?? string.Empty;
    }
}
