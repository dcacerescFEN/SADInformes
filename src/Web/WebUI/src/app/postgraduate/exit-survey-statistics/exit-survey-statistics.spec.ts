import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ExitSurveyStatistics } from './exit-survey-statistics';

const BASE = '/api/reports/postgraduate/exit-survey-statistics';
const filters = { success: true, data: { periods: [{value:'2024A',text:'2024'},{value:'Todos',text:'Todos'}], studentTypes: [{value:'Todos',text:'Todos'},{value:'MBA',text:'MBA'}], modalities: [{value:'Todos',text:'Todos'},{value:'PT',text:'PT'}] } };
const sample = { success: true, data: { rows: [{type:'MBA',modality:'PT',sample:10,inProgress:2,answered:8},{type:'MBA GLOBAL',modality:null,sample:0,inProgress:0,answered:0}], totals: {sample:10,inProgress:2,answered:8} } };
const likert = { success: true, data: { section:'malla', title:'Malla Curricular', rows:[{question:'1',description:'Pregunta A',alternative1:3,alternative2:4,alternative3:5,alternative4:6,alternative5:7,average:4.5,total:25}], totals:{alternative1:3,alternative2:4,alternative3:5,alternative4:6,alternative5:7} } };
describe('Exit survey statistics shell', () => {
  let http: HttpTestingController;
  beforeEach(() => { TestBed.configureTestingModule({ imports:[ExitSurveyStatistics], providers:[provideHttpClient(),provideHttpClientTesting()] }); http=TestBed.inject(HttpTestingController); });
  afterEach(() => { http.verify(); TestBed.resetTestingModule(); });
  async function ready(body: any = sample, likertBody: any = likert) {
    const fixture=TestBed.createComponent(ExitSurveyStatistics); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/filters').flush(filters);
    await fixture.whenStable(); fixture.detectChanges(); await Promise.resolve(); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/sample').flush(body);
    await fixture.whenStable(); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/likert').flush(likertBody);
    await fixture.whenStable(); fixture.detectChanges(); return fixture;
  }
  it('loads filters and muestra with legacy totals and tabs', async () => {
    const fixture=await ready(); const body:HTMLElement=fixture.nativeElement;
    expect(fixture.componentInstance.form.getRawValue()).toEqual({period:'2024A',studentType:'Todos',modality:'Todos'});
    expect(body.textContent).toContain('Muestra'); expect(body.textContent).toContain('Incompletas'); expect(body.textContent).toContain('Contestadas');
    expect(body.textContent).toContain('MBA'); expect(body.querySelector('tfoot')?.textContent).toContain('10'); expect(body.querySelector('tfoot')?.textContent).toContain('8');
    expect(body.querySelectorAll('[data-tab]').length).toBe(14);
    expect(body.textContent).toContain('Pregunta A'); expect(body.textContent).toContain('12,0%'); expect(body.textContent).toContain('4,5');
    expect(body.querySelector('[data-panel]')?.textContent).not.toContain('pendiente de migración');
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
    http.expectOne(r=>r.url===BASE+'/likert').flush(likert); await fixture.whenStable(); fixture.detectChanges();
    fixture.destroy();
  });
  it('shows empty sample without fabricating totals', async () => {
    const fixture=await ready({success:true,data:{rows:[],totals:{sample:0,inProgress:0,answered:0}}},{success:true,data:{section:'malla',title:'Malla Curricular',rows:[],totals:{alternative1:0,alternative2:0,alternative3:0,alternative4:0,alternative5:0}}});
    const body:HTMLElement=fixture.nativeElement; expect(body.textContent).toContain('Sin Datos');
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
    http.expectOne(r=>r.url===BASE+'/likert').flush(likert); await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('FRESH');
    fixture.destroy();
  });
  it('loads another Likert section when its tab is selected', async () => {
    const fixture=await ready();
    (fixture.nativeElement.querySelector('[data-tab="cuerpo-academico"]') as HTMLButtonElement).click(); fixture.detectChanges();
    const request=http.expectOne(r=>r.url===BASE+'/likert'); expect(request.request.params.get('section')).toBe('cuerpo-academico');
    request.flush({success:true,data:{section:'cuerpo-academico',title:'Cuerpo Académico',rows:[{question:'2',description:'Pregunta Cuerpo',alternative1:1,alternative2:2,alternative3:3,alternative4:4,alternative5:5,average:4.0,total:15}],totals:{alternative1:1,alternative2:2,alternative3:3,alternative4:4,alternative5:5}}});
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Pregunta Cuerpo'); expect(fixture.nativeElement.querySelector('[data-panel]')?.textContent).not.toContain('pendiente de migración');
    fixture.destroy();
  });

  it('loads Q8 and Q11 through the configurable matrices', async () => {
    const fixture=await ready();
    (fixture.nativeElement.querySelector('[data-tab="catedras-area"]') as HTMLButtonElement).click(); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/catedras-area').flush({success:true,data:{rows:[{code:'C1',description:'Cátedra Uno',alternative1:1,alternative2:1,alternative3:1,alternative4:1,alternative5:1,average:3,total:5}],totals:{alternative1:1,alternative2:1,alternative3:1,alternative4:1,alternative5:1}}});
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Muy Malo'); expect(fixture.nativeElement.textContent).toContain('Muy Bueno'); expect(fixture.nativeElement.textContent).toContain('20,0%');
    (fixture.nativeElement.querySelector('[data-tab="habilidades"]') as HTMLButtonElement).click(); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/skills').flush({success:true,data:{rows:[{question:'Q11',description:'Habilidad Uno',alternative1:1,alternative2:1,alternative3:1,alternative4:1,alternative5:1,alternative6:1,alternative7:1,average:4,total:7}],totals:{alternative1:1,alternative2:1,alternative3:1,alternative4:1,alternative5:1,alternative6:1,alternative7:1}}});
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Deficiente'); expect(fixture.nativeElement.textContent).toContain('Sobresaliente'); expect(fixture.nativeElement.textContent).toContain('Habilidad Uno');
    expect(fixture.nativeElement.textContent).not.toContain('Cátedra Uno');
    fixture.destroy();
  });

  it('loads corrected Q9 roles and Q10 detail', async () => {
    const fixture=await ready();
    (fixture.nativeElement.querySelector('[data-tab="ranking"]') as HTMLButtonElement).click(); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/teachers').flush({success:true,data:{theoretical:[{professor:'Teórico',frequency:5}],practical:[{professor:'Práctico',frequency:4}],formative:[{professor:'Formativo',frequency:3}]}});
    await fixture.whenStable(); fixture.detectChanges();
    const body:HTMLElement=fixture.nativeElement;
    expect(body.textContent).toContain('(1) Sus conocimientos teóricos'); expect(body.textContent).toContain('Práctico'); expect(body.textContent).toContain('Formativo');
    (body.querySelector('[data-tab="insatisfaccion"]') as HTMLButtonElement).click(); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/insatisfaction').flush({success:true,data:{rows:[{professor:'Profesor Insatisfecho',count:3,professorCode:'P9',period:'2024A',studentType:'Todos',modality:'Todos'}]}});
    await fixture.whenStable(); fixture.detectChanges();
    (body.querySelector('[data-detail="P9"]') as HTMLButtonElement).click(); fixture.detectChanges();
    const detail=http.expectOne(r=>r.url===BASE+'/insatisfaction/detail');
    expect(detail.request.params.get('professor')).toBe('P9'); expect(detail.request.params.get('period')).toBe('2024A');
    detail.flush({success:true,data:{professor:'Profesor Insatisfecho',motives:['Motivo uno','Motivo dos']}});
    await fixture.whenStable(); fixture.detectChanges();
    expect(body.querySelector('[data-teacher-detail]')?.textContent).toContain('Motivo uno'); expect(body.textContent).toContain('Ver motivos');
    fixture.destroy();
  });

  it('loads Q17 as a Likert general-evaluation section', async () => {
    const fixture=await ready();
    (fixture.nativeElement.querySelector('[data-tab="evaluacion-general"]') as HTMLButtonElement).click(); fixture.detectChanges();
    const request=http.expectOne(r=>r.url===BASE+'/likert'); expect(request.request.params.get('section')).toBe('evaluacion-general');
    request.flush({success:true,data:{section:'evaluacion-general',title:'Evaluación general',rows:[{question:'Q17',description:'Evaluación general',alternative1:1,alternative2:2,alternative3:3,alternative4:4,alternative5:5,average:3,total:15}],totals:{alternative1:1,alternative2:2,alternative3:3,alternative4:4,alternative5:5}}});
    await fixture.whenStable(); fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Evaluación general'); expect(fixture.nativeElement.querySelector('[data-panel]')?.textContent).not.toContain('pendiente');
    fixture.destroy();
  });

  it('loads the eight CSS chart options and switches charts', async () => {
    const fixture=await ready();
    (fixture.nativeElement.querySelector('[data-tab="graficos"]') as HTMLButtonElement).click(); fixture.detectChanges();
    let request=http.expectOne(r=>r.url===BASE+'/likert'); expect(request.request.params.get('section')).toBe('malla');
    request.flush(likert); await fixture.whenStable(); fixture.detectChanges();
    const body:HTMLElement=fixture.nativeElement;
    expect(body.querySelectorAll('[data-chart]')).toHaveLength(8); expect(body.querySelectorAll('.survey-chart__segment')).toHaveLength(5);
    expect(body.textContent).toContain('Muy en desacuerdo'); expect(body.textContent).toContain('Pregunta A');
    (body.querySelector('[data-chart="evaluacion-general"]') as HTMLButtonElement).click(); fixture.detectChanges();
    request=http.expectOne(r=>r.url===BASE+'/likert'); expect(request.request.params.get('section')).toBe('evaluacion-general');
    request.flush({success:true,data:{section:'evaluacion-general',title:'Evaluación general',rows:[{question:'Q17',description:'Evaluación general',alternative1:1,alternative2:1,alternative3:1,alternative4:1,alternative5:1,average:3,total:5}],totals:{alternative1:1,alternative2:1,alternative3:1,alternative4:1,alternative5:1}}});
    await fixture.whenStable(); fixture.detectChanges(); expect(body.textContent).toContain('Evaluación general');
    fixture.destroy();
  });

  it('loads the summary with N2 percentages and weighted dimension', async () => {
    const fixture=await ready();
    (fixture.nativeElement.querySelector('[data-tab="resumen"]') as HTMLButtonElement).click(); fixture.detectChanges();
    http.expectOne(r=>r.url===BASE+'/summary').flush({success:true,data:{rows:[{section:'malla',title:'Malla Curricular',alternative1:1,alternative2:1,alternative3:1,alternative4:1,alternative5:1,total:5,dimension:3}]}});
    await fixture.whenStable(); fixture.detectChanges();
    const body:HTMLElement=fixture.nativeElement;
    expect(body.textContent).toContain('Dimensiones'); expect(body.textContent).toContain('Malla Curricular'); expect(body.textContent).toContain('20,00%'); expect(body.textContent).toContain('3,00');
    expect(body.querySelector('[data-panel]')?.textContent).not.toContain('pendiente de migración');
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
