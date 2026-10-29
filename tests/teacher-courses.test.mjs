import assert from 'node:assert/strict';
import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
import test from 'node:test';

const root = fileURLToPath(new URL('../', import.meta.url));
const read = path => readFileSync(resolve(root, path), 'utf8');

test('Cursos Profesores Postgrado preserves the catalogue slug and loads its own route', () => {
  const catalogue = read('src/Web/WebUI/src/app/postgraduate/postgraduate-catalogue.ts');
  assert.match(catalogue, /entry\('cursos-profesores'[^)]*CursosProfesoresPostgrado\.aspx', true\)/);
  const routes = read('src/Web/WebUI/src/app/app.routes.ts');
  assert.match(routes, /report\.slug === 'cursos-profesores'[\s\S]*postgraduate\/teacher-courses\/teacher-courses/);
  // The explicit branch must precede the generic available fallback to TeacherSurvey.
  assert.ok(routes.indexOf("report.slug === 'cursos-profesores'") < routes.indexOf("report.available"));
});

test('Report module exposes one handler per action with action-local DTO records', () => {
  const module = 'src/Application/Reports/Postgraduate/TeacherCourses';
  for (const action of ['GetTeacherCourses', 'GetTeacherCoursesFilters']) {
    assert.ok(existsSync(resolve(root, `${module}/Queries/${action}/${action}.cs`)), action);
  }
  const filters = read(`${module}/Queries/GetTeacherCoursesFilters/TeacherCoursesFiltersDTO.cs`);
  assert.match(filters, /public sealed record TeacherCoursesFiltersDTO/);
  const report = read(`${module}/Queries/GetTeacherCourses/TeacherCoursesReportDTO.cs`);
  assert.match(report, /public sealed record TeacherCoursesReportDTO/);
  const row = read(`${module}/Queries/GetTeacherCourses/TeacherCoursesDTO.cs`);
  assert.match(row, /public sealed record TeacherCoursesDTO/);
});

test('Report uses the configured SAD user for the department scope and keeps the legacy columns', () => {
  const filters = read(
    'src/Application/Reports/Postgraduate/TeacherCourses/Queries/GetTeacherCoursesFilters/GetTeacherCoursesFilters.cs');
  assert.match(filters, /SpRec_DocentesDepartamentosAsync\(\s*"NO", settings\.User/);
  assert.match(filters, /SpRec_PeriodosAcademicosXTipoAsync\(\s*"AÑO", "POSTGRADO", "19000"/);
  const report = read('src/Application/Reports/Postgraduate/TeacherCourses/Queries/GetTeacherCourses/GetTeacherCourses.cs');
  assert.match(report, /SpRec_DocentesDepartamentosAsync\(/);
  assert.match(report, /SpRec_EvaPostgradoCursosDocentes2Async\(/);
  assert.ok(report.indexOf('SpRec_DocentesDepartamentosAsync') < report.indexOf('SpRec_EvaPostgradoCursosDocentes2Async'));
  const columns = read('src/Web/WebUI/src/app/postgraduate/teacher-courses/teacher-courses.ts');
  for (const header of ['Cod_Profesor', 'Nombre', '#CatedrasEvaluadas', 'Promedio Ev. Docente Profesor', 'Tipo AQ_PQ_O']) {
    assert.ok(columns.includes(header), header);
  }
  assert.ok(columns.includes("'CursosProfesores_Postgrado.csv'"));
  assert.ok(columns.includes('\\uFEFF'));
  assert.ok(columns.includes('[=+\\-@]'));
});

test('Endpoint requires its own report permission and sanitizes storage failures', () => {
  const endpoint = read('src/Web/WebApi/Endpoints/Reports/Postgraduate/TeacherCoursesEndpoints.cs');
  assert.match(endpoint, /REPORT_VIEW_POSTGRADUATE_TEACHER_COURSES/);
  assert.match(endpoint, /statusCode: 400/);
  assert.match(endpoint, /statusCode: 503/);
  assert.doesNotMatch(endpoint, /private-sql|Exception\.Message/);
  const permission = read('src/Domain/Constants/Permission.cs');
  assert.match(permission, /REPORT_VIEW_POSTGRADUATE_TEACHER_COURSES = "REPORT_VIEW_POSTGRADUATE_TEACHER_COURSES"/);
});

test('Procedure wrappers keep canonical types from the SAD mapping', () => {
  const procedures = read('src/Infrastructure/Data/SadContextProcedures.cs');
  assert.match(procedures, /\[dbo\]\.\[spRec_DocentesDepartamentos\]/);
  assert.match(procedures, /\[dbo\]\.\[spRec_EvaPostgradoCursosDocentes2\]/);
  assert.match(procedures, /ParameterName = "Usuario",\s*Size = 20/);
  assert.match(procedures, /ParameterName = "Año",\s*Size = 4/);
  assert.match(procedures, /ParameterName = "Departamento",\s*Size = 70/);
  const result = read('src/Domain/Entities/Sad/Sp/SpRecEvaPostgradoCursosDocentes2Result.cs');
  assert.match(result, /Column\("#Catedras"\)/);
  assert.match(result, /decimal\(19,6\)/);
});
