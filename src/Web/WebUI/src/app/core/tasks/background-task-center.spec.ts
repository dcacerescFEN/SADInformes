import { TestBed } from '@angular/core/testing';
import { BackgroundTaskCenter } from './background-task-center';
import { BackgroundTaskHost } from './background-task-host';

const task = {
  id: 'job-1',
  kind: 'email',
  title: 'Envío de correo',
  description: 'Alumnos sin fotos',
  status: 'running' as const,
  current: 2,
  total: 4,
  details: [{ label: 'Enviados', value: '2' }],
  terminal: false,
};

describe('BackgroundTaskCenter', () => {
  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({});
  });

  it('does not restore a non-terminal task without a poller', () => {
    sessionStorage.setItem('sadinformes.background-tasks', JSON.stringify({
      tasks: [task], selectedId: task.id, minimized: false,
    }));
    const center = TestBed.inject(BackgroundTaskCenter);
    expect(center.tasks()).toHaveLength(0);
  });

  it('starts, updates, minimizes, expands and dismisses a generic task', () => {
    const center = TestBed.inject(BackgroundTaskCenter);
    center.start(task);
    expect(center.tasks()).toHaveLength(1);
    expect(center.selectedId()).toBe('job-1');

    center.minimize();
    expect(center.minimized()).toBe(true);
    center.expand();
    expect(center.minimized()).toBe(false);

    center.update('job-1', { current: 4, terminal: true, status: 'completed' });
    expect(center.tasks()[0].current).toBe(4);
    expect(center.tasks()[0].terminal).toBe(true);

    center.dismiss('job-1');
    expect(center.tasks()).toHaveLength(0);
  });

  it('renders the generic modal and the minimized pill', () => {
    const fixture = TestBed.createComponent(BackgroundTaskHost);
    const center = TestBed.inject(BackgroundTaskCenter);
    center.start(task);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[role=dialog]')).not.toBeNull();
    expect(root.textContent).toContain('Envío de correo');
    expect(root.textContent).toContain('Enviados');

    const minimize = Array.from(root.querySelectorAll<HTMLButtonElement>('button'))
      .find(button => button.textContent?.includes('Minimizar'));
    minimize?.click();
    fixture.detectChanges();
    expect(root.querySelector('[role=dialog]')).toBeNull();
    expect(root.querySelector('button.task-pill')).not.toBeNull();
  });
});
