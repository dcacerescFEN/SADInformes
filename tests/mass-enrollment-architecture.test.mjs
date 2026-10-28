import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';
import test from 'node:test';

const root = fileURLToPath(new URL('../', import.meta.url));
const read = path => readFileSync(resolve(root, path), 'utf8');

test('MassEnrollment follows AcademicPeriods command/query folders and dispatcher', () => {
  for (const request of [
    'Commands/CreateMassEnrollment/CreateMassEnrollment.cs',
    'Queries/GetMassEnrollmentFilters/GetMassEnrollmentFilters.cs',
    'Queries/GetMassEnrollmentCandidates/GetMassEnrollmentCandidates.cs',
  ]) {
    const path = 'src/Application/Reports/Postgraduate/MassEnrollment/' + request;
    assert.ok(existsSync(resolve(root, path)), path);
    assert.match(read(path), /IRequestHandler</);
  }
  const endpoint = read('src/Web/WebApi/Endpoints/Reports/Postgraduate/MassEnrollmentEndpoints.cs');
  assert.match(endpoint, /IRequestDispatcher/);
  assert.match(endpoint, /sender.Send/);
  assert.doesNotMatch(endpoint, /MassEnrollmentService/);
  assert.equal(existsSync(resolve(root, 'src/Application/Reports/Postgraduate/MassEnrollment/MassEnrollmentService.cs')), false);
});

test('MassEnrollment SP execution lives in the main procedures file', () => {
  for (const partial of [
    'src/Infrastructure/Data/SadContextProcedures.MassEnrollment.cs',
    'src/Application/Common/Interfaces/ISadContextProcedures.MassEnrollment.cs',
  ]) assert.equal(existsSync(resolve(root, partial)), false, partial);
  const procedures = read('src/Infrastructure/Data/SadContextProcedures.cs');
  assert.match(procedures, /SpCre_MovInscAdministrativo_PostgradoAsync/);
  assert.match(procedures, /spCre_MovInscAdministrativo_Postgrado/);
  assert.match(procedures, /SpRec_Alumnos_insc_masiva_catedra_dispAsync/);
  const command = read('src/Application/Reports/Postgraduate/MassEnrollment/Commands/CreateMassEnrollment/CreateMassEnrollment.cs');
  assert.match(command, /procedures.SpCre_MovInscAdministrativo_PostgradoAsync/);
  assert.doesNotMatch(command, /SqlQueryAsync|SqlParameter|SELECT |EXEC dbo/);
  assert.match(procedures, /EXEC @returnValue = \[dbo\]\.\[spCre_MovInscAdministrativo_Postgrado\]/);
});

test('Custom enrollment queries use LINQ over MappingDB table mappings, not ad-hoc SQL', () => {
  const context = read('src/Infrastructure/Data/SadContext.cs');
  for (const entity of ['Catedra', 'Curso', 'PlanesFormacionCatedra', 'VacantesCurso', 'AlumnosCatedra', 'MovimientosInscripcion']) {
    assert.ok(context.includes('DbSet<' + entity + '>'), entity);
    assert.ok(existsSync(resolve(root, 'src/Domain/Entities/Sad/Tables/dbo/' + entity + '.cs')), entity);
  }
  assert.doesNotMatch(context, /AsNoTracking|AnyAsync|FirstOrDefaultAsync|SELECT /);
  const data = read('src/Application/Reports/Postgraduate/MassEnrollment/MassEnrollmentLookup.cs');
  assert.match(data, /AsNoTracking/);
  assert.match(data, /AnyAsync/);
  assert.match(data, /FindCourseAsync/);
  assert.match(data, /IsEnrolledAsync/);
  assert.match(context, /Database.BeginTransactionAsync/);
});
