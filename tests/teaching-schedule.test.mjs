import { readFileSync } from 'node:fs';
import assert from 'node:assert/strict';
import test from 'node:test';
const read = path => readFileSync(new URL('../' + path, import.meta.url), 'utf8');

test('Teaching schedule keeps its actual legacy path and a dedicated lazy route', () => {
  assert.match(read('src/Web/WebUI/src/app/postgraduate/postgraduate-catalogue.ts'),
    /legacyPath: 'Informes\/Inscripcion\/ProgramacionXProgramaModalidad.aspx'/);
  assert.match(read('src/Web/WebUI/src/app/app.routes.ts'), /module.TeachingSchedule/);
});

test('Teaching schedule preserves promotion description binding in its Application query', () => {
  const filters = read('src/Application/Reports/Postgraduate/TeachingSchedule/Queries/GetTeachingScheduleFilters/GetTeachingScheduleFilters.cs');
  assert.match(filters, /TeachingScheduleOptionDTO\(row.Descripcion, row.Promocion\)/);
  assert.doesNotMatch(read('src/Application/Reports/Postgraduate/TeachingSchedule/Queries/GetTeachingSchedule/GetTeachingSchedule.cs'),
    /Substring|\.Take\(|\.Trim\(/);
});
