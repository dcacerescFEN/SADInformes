import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { resolve, join, basename, dirname } from 'node:path';
import assert from 'node:assert/strict';
import test from 'node:test';

const root = fileURLToPath(new URL('../', import.meta.url));
const read = path => readFileSync(resolve(root, path), 'utf8');
const modules = [
  'src/Application/Config',
  'src/Application/Reports/Postgraduate/TeacherSurvey',
  'src/Application/Reports/Postgraduate/CoursesByStudents',
  'src/Application/Reports/Postgraduate/MassEnrollment',
];
const commandActions = ['Create', 'Update', 'Delete', 'Upload', 'Download'];

function files(directory) {
  if (!existsSync(directory)) return [];
  return readdirSync(directory).flatMap(name => {
    const path = join(directory, name);
    return statSync(path).isDirectory() ? files(path) : path.endsWith('.cs') ? [path] : [];
  });
}

test('Application modules use action-named Commands and Get* Queries folders', () => {
  for (const module of modules) {
    const folder = resolve(root, module);
    for (const kind of ['Commands', 'Queries']) {
      const path = join(folder, kind);
      if (!existsSync(path)) continue;
      for (const entry of readdirSync(path)) {
        const action = join(path, entry);
        if (!statSync(action).isDirectory()) {
          assert.fail(module + '/' + kind + '/' + entry + ' must be an action folder, not a bundled file');
        }
        assert.ok(existsSync(join(action, entry + '.cs')), action + '/' + entry + '.cs');
        if (kind === 'Queries') assert.match(entry, /^Get[A-Z]/, entry);
        else assert.ok(commandActions.some(prefix => entry.startsWith(prefix) && /^[A-Z]/.test(entry.slice(prefix.length))), entry);
      }
    }
  }
});

test('Every DTO is a sealed record in its own DTO file inside the action folder', () => {
  for (const module of modules) {
    for (const file of files(resolve(root, module))) {
      const name = basename(file);
      const source = readFileSync(file, 'utf8');
      if (name.endsWith('DTO.cs')) {
        const type = name.slice(0, -3);
        assert.match(source, new RegExp('public sealed record ' + type), file);
        const parent = basename(dirname(file));
        assert.match(parent, /^(Get|Create|Update|Delete|Upload|Download)[A-Z]/, file + ' must live in an action folder');
      }
      assert.doesNotMatch(source, /public (?:partial )?(?:class|record) \w+Dto\b/, file + ' must use DTO suffix and sealed record');
    }
  }
});

test('SadContext is only the database definition', () => {
  const context = read('src/Infrastructure/Data/SadContext.cs');
  assert.doesNotMatch(context,
    /FindMassEnrollment|IsEnrollmentConfirmed|ExecuteSqlRawAsync|BeginTransactionAsync|SqlQueryAsync|SqlParameter/);
  assert.match(context, /DbSet</);
  assert.match(context, /OnModelCreating/);
  assert.match(read('src/Application/Common/Interfaces/ISadContext.cs'), /^namespace [\s\S]*public partial interface ISadContext;\s*$/m);
});

test('AGENTS.md documents backend and frontend conventions', () => {
  const agents = read('AGENTS.md');
  for (const heading of ['## Backend', '### Application modules', '### DTOs', '### Database contexts and queries', '## Frontend']) {
    assert.ok(agents.includes(heading), heading);
  }
  for (const rule of ['Commands', 'Queries', 'Create', 'Update', 'Delete', 'Upload', 'Download', 'Get', 'sealed record', 'LINQ', 'SadContext']) {
    assert.ok(agents.includes(rule), rule);
  }
});
