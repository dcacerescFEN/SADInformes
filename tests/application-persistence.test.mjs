import { readFileSync, existsSync } from 'node:fs';
import assert from 'node:assert/strict';
import test from 'node:test';
const read = path => readFileSync(new URL('../' + path, import.meta.url), 'utf8');

test('Application context exposes mapped tables through read-only DbSet getters', () => {
  const source = read('src/Application/Common/Interfaces/ISadContext.cs');
  for (const name of ['Catedras', 'Cursos', 'PlanesFormacionCatedras', 'VacantesCursos', 'AlumnosCatedras', 'MovimientosInscripcions'])
    assert.match(source, new RegExp('DbSet<[^>]+> ' + name + ' \\{ get; \\}'));
});

test('SadContext owns generic transaction plumbing and modules own enrollment LINQ', () => {
  const context = read('src/Infrastructure/Data/SadContext.cs');
  assert.match(context, /BeginTransactionAsync\(/);
  assert.match(context, /SET XACT_ABORT ON/);
  assert.doesNotMatch(context, /EnrollmentTransaction|MassEnrollment/);
  const lookup = read('src/Application/Reports/Postgraduate/MassEnrollment/MassEnrollmentLookup.cs');
  assert.match(lookup, /ISadContext context/);
  assert.match(lookup, /AsNoTracking/);
  assert.match(lookup, /AnyAsync/);
  assert.equal(existsSync(new URL('../src/Infrastructure/Reports/Postgraduate/MassEnrollmentData.cs', import.meta.url)), false);
});

test('Enrollment SP retains MappingDB output parameter and non-query execution', () => {
  const source = read('src/Infrastructure/Data/SadContextProcedures.cs');
  const write = source.slice(source.indexOf('Task<int> SpCre_MovInscAdministrativo_PostgradoAsync'));
  assert.match(write, /OutputParameter<int>\? returnValue = null/);
  assert.match(write, /EXEC @returnValue = \[dbo\]\.\[spCre_MovInscAdministrativo_Postgrado\]/);
  assert.match(write, /returnValue\?\.SetValue/);
  assert.match(write, /ExecuteSqlRawAsync/);
});

test('Report handlers consume typed procedures directly, without infrastructure readers', () => {
  for (const [module, action] of [
    ['CoursesByStudents', 'GetCoursesByStudents'], ['CoursesByStudents', 'GetCoursesByStudentsFilters'],
    ['TeacherSurvey', 'GetTeacherSurvey'], ['TeacherSurvey', 'GetTeacherSurveyFilters'],
  ]) {
    const source = read('src/Application/Reports/Postgraduate/' + module + '/Queries/' + action + '/' + action + '.cs');
    assert.match(source, /ISadContextProcedures procedures/);
    assert.doesNotMatch(source, /Reader reader|using Infrastructure/);
  }
  for (const name of ['CoursesByStudentsReader', 'TeacherSurveyReader'])
    assert.equal(existsSync(new URL('../src/Infrastructure/Reports/Postgraduate/' + name + '.cs', import.meta.url)), false);
});
