using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Extensions
{
    public static class EnrollmentExtensions
    {
        public const long AcceptedStatusId = 17;

        // An application in this status means payment was confirmed and the student is enrolled.
        public const long PaidStatusId = 42;

        public static Task<List<long>> GetEnrolledRoundIdsAsync(this SchoolDbContext db, long accountId)
            => db.Applications
                .Where(a => a.AccountId == accountId && a.StatusId == PaidStatusId)
                .Select(a => a.CourseRoundId)
                .Distinct()
                .ToListAsync();

        public static Task<bool> IsEnrolledAsync(this SchoolDbContext db, long accountId, long courseRoundId)
            => db.Applications.AnyAsync(a =>
                a.AccountId == accountId && a.CourseRoundId == courseRoundId && a.StatusId == PaidStatusId);
    }
}
