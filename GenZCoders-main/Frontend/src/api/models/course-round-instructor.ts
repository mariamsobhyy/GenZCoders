export interface InstructorCourseRoundDto {
  instructorId?: number;
  courseRoundId: number;
  courseId: number;
  courseName: string;
  roundNumber: number;
  startDate: string;
  endDate: string;
  price: number;
}

export interface AssignInstructorsRequest {
  courseRoundId: number;
  instructorIds: number[];
}

export interface AssignInstructorsResponse {
  message: string;
}
