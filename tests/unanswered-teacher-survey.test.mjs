import { readFileSync } from 'node:fs';
import assert from 'node:assert/strict';
import test from 'node:test';
const read = path => readFileSync(new URL('../' + path, import.meta.url), 'utf8');
test('Unanswered survey uses canonical typed SP and Application action folders', () => {
  const path = 'src/Application/Reports/Postgraduate/UnansweredTeacherSurvey/Queries/';
  assert.match(read(path + 'GetUnansweredTeacherSurvey/GetUnansweredTeacherSurvey.cs'), /ISadContextProcedures/);
  assert.match(read(path + 'GetUnansweredTeacherSurveyFilters/GetUnansweredTeacherSurveyFilters.cs'), /ISadContextProcedures/);
  assert.match(read('src/Infrastructure/Data/SadContextProcedures.cs'), /EXEC @returnValue = \[dbo\]\.\[spRec_Alumnos_Sin_Evaluacion_Docente\]/);
});
