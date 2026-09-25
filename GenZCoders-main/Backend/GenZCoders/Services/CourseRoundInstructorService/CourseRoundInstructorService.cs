using GenZCoders.DTOs.CourseRoundInstructor;
using GenZCoders.Models;
using GenZCoders.Repos.CourseRoundInstructorRepo;

namespace GenZCoders.Services.CourseRoundInstructorService
{
    public class CourseRoundInstructorService : ICourseRoundInstructorService
    {
        private readonly ICourseRoundInstructorRepository _repository;
        private readonly SchoolDbContext _db;

        public CourseRoundInstructorService(ICourseRoundInstructorRepository repository, SchoolDbContext db)
        {
            _repository = repository;
            _db = db;
        }

        public async Task AssignInstructorsAsync(AssignInstructorsDto dto)
        {
            // Replace the round's instructors in one transaction, so a failed insert can't leave it with none.
            await using var transaction = await _db.Database.BeginTransactionAsync();

            await _repository.RemoveByCourseRoundAsync(dto.CourseRoundId);

            var entities = dto.InstructorIds.Distinct().Select(id => new CourseRoundInstructor
            {
                CourseRoundId = dto.CourseRoundId,
                InstructorId = id,
                AssignedDate = DateTime.UtcNow
            }).ToList();

            await _repository.AddRangeAsync(entities);
            await transaction.CommitAsync();
        }

        public async Task<List<InstructorCourseRoundDto>> GetInstructorCourseRoundsAsync(long instructorId)
        {
            return await _repository.GetByInstructorAsync(instructorId);
        }
    }

}
