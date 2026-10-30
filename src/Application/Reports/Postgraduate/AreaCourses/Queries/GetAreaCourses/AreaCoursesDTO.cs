namespace Application.Reports.Postgraduate.AreaCourses.Queries.GetAreaCourses;

public sealed record AreaCoursesDTO(
    string Id, string Description, string Area, string Period, string CourseCode, string Section,
    string Credits, string TeacherName, string PersonCode, string TeacherType, string Start, string End,
    string Program, string Modality, string Promotion, string CourseName, string CourseType,
    string ScheduleType, string Aqpq, string TeachingCategory, string Confirmed, string DedicatedHours,
    string TotalStudents, string PostgraduateStudents, string UndergraduateStudents,
    string TeachingEvaluationAverage, string CourseObservations, string ObservationUser);
