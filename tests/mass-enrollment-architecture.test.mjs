import { existsSync, readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';
import assert from 'node:assert/strict';
import test from 'node:test';

const root = fileURLToPath(new URL('../', import.meta.url));
const read = path => readFileSync(resolve(root, path), 'utf8');

test('MassEnrollment follows AcademicPeriods command/query folders and dispatcher', () => {
  for (const request of [
    'Commands/EnrollMassEnrollment/EnrollMassEnrollment.cs',
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

test('MassEnrollment SQL is consolidated into SadContext and main procedures', () => {
  for (const partial of [
    'src/Infrastructure/Data/SadContextProcedures.MassEnrollment.cs',
    'src/Application/Common/Interfaces/ISadContextProcedures.MassEnrollment.cs',
  ]) assert.equal(existsSync(resolve(root, partial)), false, partial);
  const procedures = read('src/Infrastructure/Data/SadContextProcedures.cs');
  assert.match(procedures, /SpCre_MovInscAdministrativo_PostgradoAsync/);
  assert.match(procedures, /spCre_MovInscAdministrativo_Postgrado/);
  assert.match(procedures, /SpRec_Alumnos_insc_masiva_catedra_dispAsync/);
  const data = read('src/Infrastructure/Reports/Postgraduate/MassEnrollmentData.cs');
  assert.match(data, /procedures.SpCre_MovInscAdministrativo_PostgradoAsync/);
  assert.doesNotMatch(data, /ExecuteSqlRawAsync|SqlQueryAsync|SqlParameter|SELECT CAST|EXEC dbo/);
  assert.match(read('src/Infrastructure/Data/SadContext.cs'), /ExecuteSqlRawAsync/);
});

test('Custom enrollment queries use LINQ over MappingDB table mappings, not ad-hoc SQL', () => {
  const context = read('src/Infrastructure/Data/SadContext.cs');
  for (const entity of ['Catedra', 'Curso', 'PlanesFormacionCatedra', 'VacantesCurso', 'AlumnosCatedra', 'MovimientosInscripcion']) {
    assert.ok(context.includes('DbSet<' + entity + '>'), entity);
    assert.ok(existsSync(resolve(root, 'src/Domain/Entities/Sad/Tables/dbo/' + entity + '.cs')), entity);
  }
  assert.doesNotMatch(context, /SELECT CAST|SELECT DISTINCT|this.SqlQueryAsync/);
  assert.match(context, /AsNoTracking/);
  assert.match(context, /AnyAsync/);
});
