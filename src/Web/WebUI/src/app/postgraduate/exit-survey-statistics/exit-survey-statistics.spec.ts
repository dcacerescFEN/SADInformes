import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ExitSurveyStatistics } from './exit-survey-statistics';

const BASE = '/api/reports/postgraduate/exit-survey-statistics';
const filters = { success: true, data: { periods: [{value:'2024A',text:'2024'},{value:'Todos',text:'Todos'}], studentTypes: [{value:'Todos',text:'Todos'},{value:'MBA',text:'MBA'}], modalities: [{value:'Todos',text:'Todos'},{value:'PT',text:'PT'}] } };
const sample = { success: true, data: { rows: [{type:'MBA',modality:'PT',sample:10,inProgress:2,answered:8},{type:'MBA GLOBAL',modality:null,sample:0,inProgress:0,answered:0}], totals: {sample:10,inProgress:2,answered:8} } };
describe('Exit survey statistics shell', () => {
  let http: HttpTestingController;
  beforeEach(() => { TestBed.configureTestingModule({ imports:[ExitSurveyStatistics], providers:[provideHttpClient(),provideHttpClientTesting()] }); http=TestBed.inject(HttpTestingController); });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });
  async function ready(body: any = sample) {
    const fixture=TestBed.createComponent(ExitSurveyStatistics); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/filters').flush(filters);
    await fixture.whenStable(); fixture.detectChanges(); await Promise.resolve(); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/sample').flush(body);
    await fixture.whenStable(); fixture.detectChanges(); return fixture;
  }
  it('loads filters and muestra with legacy totals and tabs', async () => {
    const fixture=await ready(); const body:HTMLElement=fixture.nativeElement;
    expect(fixture.componentInstance.form.getRawValue()).toEqual({period:'2024A',studentType:'Todos',modality:'Todos'});
    expect(body.textContent).toContain('Muestra'); expect(body.textContent).toContain('Incompletas'); expect(body.textContent).toContain('Contestadas');
    expect(body.textContent).toContain('MBA'); expect(body.querySelector('tfoot')?.textContent).toContain('10'); expect(body.querySelector('tfoot')?.textContent).toContain('8');
    expect(body.querySelectorAll('[data-tab]').length).toBe(14);
    expect(body.querySelector('[data-panel]')?.textContent).toContain('pendiente de migración');
    fixture.destroy();
  });
  it('reloads dependent modality and sample on type change', async () => {
    const fixture=await ready();
    fixture.componentInstance.form.controls.studentType.setValue('MBA'); fixture.detectChanges();
    const filter=http.expectOne(r=>r.url===BASE+'/filters'); expect(filter.request.params.get('studentType')).toBe('MBA');
    filter.flush({...filters,data:{...filters.data,modalities:[{value:'PT',text:'PT'}]}}); await fixture.whenStable(); fixture.detectChanges();
    await Promise.resolve(); fixture.detectChanges();
    const request=http.expectOne(r=>r.url===BASE+'/sample'); expect(request.request.params.get('studentType')).toBe('MBA'); expect(request.request.params.get('modality')).toBe('PT');
    request.flush(sample); await fixture.whenStable(); fixture.detectChanges();
    fixture.destroy();
  });
  it('switches placeholder tab and shows empty state without fabricating totals', async () => {
    const fixture=await ready({success:true,data:{rows:[],totals:{sample:0,inProgress:0,answered:0}}});
    const body:HTMLElement=fixture.nativeElement; expect(body.textContent).toContain('Sin Datos');
    (body.querySelector('[data-tab="resumen"]') as HTMLButtonElement).click(); fixture.detectChanges();
    expect(fixture.componentInstance.activeTab()).toBe('resumen'); expect(body.querySelector('[data-panel]')?.textContent).toContain('pendiente de migración');
    fixture.destroy();
  });
  it('cancels an older sample when filters change before it resolves', async () => {
    const fixture=await ready();
    fixture.componentInstance.form.controls.period.setValue('Todos'); fixture.detectChanges();
    const stale=http.expectOne(r=>r.url===BASE+'/sample');
    fixture.componentInstance.form.controls.modality.setValue('PT'); fixture.detectChanges();
    expect(stale.cancelled).toBe(true);
    const fresh=http.expectOne(r=>r.url===BASE+'/sample');
    fresh.flush({...sample,data:{...sample.data,rows:[{...sample.data.rows[0],type:'FRESH'}]}}); await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('FRESH');
    fixture.destroy();
  });
  it('ignores stale catalogs and sanitizes failures', async () => {
    const fixture=TestBed.createComponent(ExitSurveyStatistics); fixture.detectChanges();
    const stale=http.expectOne(r=>r.url===BASE+'/filters');
    fixture.componentInstance.form.controls.studentType.setValue('MBA');
    const fresh=http.expectOne(r=>r.url===BASE+'/filters'); fresh.flush(filters); await fixture.whenStable(); fixture.detectChanges(); await Promise.resolve(); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/sample').flush('private',{status:503,statusText:'private'}); await fixture.whenStable(); fixture.detectChanges();
    stale.flush(filters); await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No se pudo cargar la muestra.');
    expect(fixture.nativeElement.textContent).not.toContain('private');
    fixture.destroy();
  });
});
