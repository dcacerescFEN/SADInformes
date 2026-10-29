namespace Application.Reports.Postgraduate.TeacherCourses.Queries.GetTeacherCourses;

public sealed record TeacherCoursesDTO(
    string ProfessorCode, string Name, int? EvaluatedCourses, string Average, string Classification);
