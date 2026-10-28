using Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudents;
using Application.Reports.Postgraduate.CoursesByStudents.Queries.GetCoursesByStudentsFilters;
using Domain.Common;
using Domain.Entities.Sad.Sp;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace WebApi.Tests;

public sealed class CoursesByStudentsReaderTests
{
    [Fact]
    public async Task Catalogues_exclude_Todos_and_scope_modalities_to_selected_type()
    {
        using var context = new SadContext(new DbContextOptionsBuilder<SadContext>().Options);
        var procedures = new CatalogueProcedures(context);
        var responses = new ApiResponseService();
        var filters = new GetCoursesByStudentsFiltersHandler(procedures, responses);
        var report = new GetCoursesByStudentsHandler(procedures, responses);
        var initial = (await filters.Handle(new GetCoursesByStudentsFilters(null), default)).Data!;
        Assert.Equal("NO", procedures.IncludeAll);
        Assert.Equal(("20040", 0, "NO", "POSTGRADO"), procedures.PeriodArguments);
        Assert.Empty(initial.Modalities);
        Assert.Null(procedures.SelectedType);
        Assert.Equal("MBA", Assert.Single(initial.StudentTypes).Value);
        var selected = (await filters.Handle(new GetCoursesByStudentsFilters("MBA"), default)).Data!;
        Assert.Equal("MBA", procedures.SelectedType);
        Assert.Equal(new[] { "PT" }, selected.Modalities);
        var rows = (await report.Handle(new GetCoursesByStudents("2023A", "MBA", "PT"), default)).Data!.Rows;
        Assert.Equal(("2023A", "MBA", "PT"), procedures.ReportArguments);
        Assert.Equal(new CoursesByStudentsDTO("2024B", "MBA101/1", "", "A001", "", "2023A"), Assert.Single(rows));
    }

    private sealed class CatalogueProcedures(SadContext context) : SadContextProcedures(context)
    {
        public string? IncludeAll { get; private set; }
        public string? SelectedType { get; private set; }
        public (string?, int?, string?, string?) PeriodArguments { get; private set; }
        public (string?, string?, string?) ReportArguments { get; private set; }

        public override Task<List<SpRecPeriodosAcademicosProgramacionResult>?> SpRec_PeriodosAcademicosProgramacionAsync(
            string? limite, int? modo, string? conTodos, string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            PeriodArguments = (limite, modo, conTodos, tipo);
            return Task.FromResult<List<SpRecPeriodosAcademicosProgramacionResult>?>([new() { Periodo = "2023A" }]);
        }

        public override Task<List<SpRecTiposAlumnoResult>?> SpRec_TiposAlumnoAsync(string? incluirTodos, string? usuario,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            IncludeAll = incluirTodos;
            return Task.FromResult<List<SpRecTiposAlumnoResult>?>([new() { Tipo = "MBA", Nombre = "MBA" }]);
        }

        public override Task<List<SpRecTiposAlumnosModalidadesResult>?> SpRec_TiposAlumnosModalidadesAsync(string? tipo,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            SelectedType = tipo;
            return Task.FromResult<List<SpRecTiposAlumnosModalidadesResult>?>([new() { Modalidad = " PT " }, new() { Modalidad = "PT" }]);
        }

        public override Task<List<SpRecAlumnosXPeriodoXTipoAlumnoXModalidadResult>?> SpRec_AlumnosXPeriodoXTipo_AlumnoXModalidadAsync(
            string? periodo, string? tipo_Alumno, string? modalidad,
            OutputParameter<int>? returnValue = null, CancellationToken? cancellationToken = default)
        {
            ReportArguments = (periodo, tipo_Alumno, modalidad);
            return Task.FromResult<List<SpRecAlumnosXPeriodoXTipoAlumnoXModalidadResult>?>(
                [new() { Periodo = "2024B ", Curso = "MBA101/1 ", Cod_Alumno = "A001 ", SemestreIngreso = "2023A" }]);
        }
    }
}
