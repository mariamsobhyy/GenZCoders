namespace GenZCoders.DTOs.CourseMaterialDto
{
    public class CreatePdfCourseMaterialDto
    {
        public long CourseRoundId { get; set; }
        public long? WeekId { get; set; }
        public long? ParentMaterialId { get; set; }
        public long? MaterialTypeStatusId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public IFormFile? File { get; set; }
    }
}
