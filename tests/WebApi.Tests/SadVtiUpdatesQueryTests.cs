using System.Linq.Expressions;
using Application.Reports.Postgraduate.SadVtiUpdates.Queries.GetSadVtiUpdates;
using Application.Reports.Postgraduate.SadVtiUpdates.Queries.GetSadVtiUpdateFilters;
using Domain.Entities.Sad.Tables.dbo;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace WebApi.Tests;

public sealed class SadVtiUpdatesQueryTests
{
    private static readonly InMemoryCollation Collation = new();
    internal static DbContextOptions<SadContext> Options() => new DbContextOptionsBuilder<SadContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).AddInterceptors(Collation).Options;

    internal static async Task Seed(SadContext context)
    {
        context.AudiEstadoPostgradoAlumnosGuia.AddRange(
            new AudiEstadoPostgradoAlumnosGuia { Id = 1, Estado = "OK", Descripcion = "Actualizado" },
            new AudiEstadoPostgradoAlumnosGuia { Id = 2, Estado = "ERR", Descripcion = "Error" });
        context.Personas.Add(new Persona { CodPersona = "P1", Nombre1 = "Ana", Apellido1 = "Pérez", Apellido2 = "López" });
        context.Alumnos.Add(new Alumno { CodAlumno = "NEW", CodPersona = "P1", Matricula = "M1", SemIngresoDecreto = "2026A", TipoAlumno = "MBA", Modalidad = "PT" });
        context.Alumnos.Add(new Alumno { CodAlumno = "OLD", CodPersona = "P1", Matricula = "M2", SemIngresoDecreto = "2026A", TipoAlumno = "MBA" });
        var date = new DateTime(2026, 10, 1);
        context.AudiPostgradoAlumnosGuia.AddRange(
            new AudiPostgradoAlumnosGuia { Id = 1, Estado = "OK", CodPersona = "AUDIT-PERSON", CodAlumnoAntiguo = "OLD", CodAlumnoNuevo = "NEW", FechaRegistro = date, Matricula = "AUDIT-M", CodSitAcademica = "ACTIVO", MensajeError = "=SUM(1,2)<&\"" },
            new AudiPostgradoAlumnosGuia { Id = 2, Estado = "ERR", CodAlumnoAntiguo = "OLD", FechaRegistro = date.AddDays(1).AddTicks(-1) },
            new AudiPostgradoAlumnosGuia { Id = 3, Estado = "OK", CodAlumnoAntiguo = "OLD", FechaRegistro = date.AddTicks(-1) },
            new AudiPostgradoAlumnosGuia { Id = 4, Estado = "OK", CodAlumnoAntiguo = "OLD", FechaRegistro = date.AddDays(1) },
            new AudiPostgradoAlumnosGuia { Id = 5, Estado = "OK", CodAlumnoAntiguo = "OLD", CodAlumnoNuevo = "", FechaRegistro = date },
            new AudiPostgradoAlumnosGuia { Id = 6, Estado = "UNKNOWN", CodAlumnoAntiguo = "OLD", FechaRegistro = date });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task Inclusive_dates_coalesce_inner_joins_and_all_12_fields_match_legacy()
    {
        await using var context = new SadContext(Options()); await Seed(context);
        var result = await new GetSadVtiUpdatesHandler(context, new ApiResponseService())
            .Handle(new GetSadVtiUpdates("-1", "2026-10-01", "2026-10-01"), default);
        Assert.True(result.Success);
        Assert.Equal(2, result.Data!.Rows.Length);
        var row = result.Data.Rows[0];
        Assert.Equal(("OK", "Actualizado", "P1", "OLD", "NEW", "Ana Pérez López", "MBA", "PT", "ACTIVO", "AUDIT-M"),
            (row.Estado, row.Descripcion, row.CodPersona, row.CodAlumnoAnterior, row.CodAlumno, row.NombreCompleto, row.TipoAlumno, row.Modalidad, row.CodSitAcademica, row.Matricula));
        Assert.Equal(new DateTime(2026, 10, 1), row.FechaRegistro);
        Assert.Equal("=SUM(1,2)<&\"", row.MensajeError);
        Assert.Equal("OLD", result.Data.Rows[1].CodAlumno);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Selected_state_filters_and_null_name_parts_keep_legacy_null_result()
    {
        await using var context = new SadContext(Options()); await Seed(context);
        var person = await context.Personas.SingleAsync(); person.Apellido2 = null; await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        var result = await new GetSadVtiUpdatesHandler(context, new ApiResponseService())
            .Handle(new GetSadVtiUpdates("ERR", "2026-10-01", "2026-10-01"), default);
        Assert.Single(result.Data!.Rows);
        Assert.Equal("ERR", result.Data.Rows[0].Estado);
        Assert.Null(result.Data.Rows[0].NombreCompleto);
        Assert.Null(result.Data.Rows[0].MensajeError);
    }

    [Fact]
    public async Task State_catalog_starts_with_minus_one_Todos_and_preserves_descriptions()
    {
        await using var context = new SadContext(Options()); await Seed(context);
        var result = await new GetSadVtiUpdateFiltersHandler(context, new ApiResponseService())
            .Handle(new GetSadVtiUpdateFilters(), default);
        Assert.Equal("-1", result.Data!.States[0].Value);
        Assert.Equal("Todos", result.Data.States[0].Text);
        Assert.Contains(result.Data.States, row => row.Value == "OK" && row.Text == "Actualizado");
        Assert.Equal(3, result.Data.States.Length);
    }

    [Fact]
    public void SqlServer_translation_preserves_collation_coalesce_and_calendar_date_comparison()
    {
        using var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true;TrustServerCertificate=true").Options);
        var sql = GetSadVtiUpdatesHandler.CreateQuery(context, "-1", new DateTime(2026, 10, 1), new DateTime(2026, 10, 1)).ToQueryString();
        Assert.Contains("COLLATE Modern_Spanish_CI_AS", sql);
        Assert.Contains("COALESCE", sql);
        Assert.Contains("CONVERT(date,", sql);
        Assert.Contains("INNER JOIN [dbo].[Alumnos]", sql);
        Assert.Contains("INNER JOIN [dbo].[Personas]", sql);
    }

    [Fact]
    public void Student_mapping_keeps_the_canonical_composite_key_and_lengths()
    {
        using var context = new SadContext(new DbContextOptionsBuilder<SadContext>()
            .UseSqlServer("Server=unused;Database=unused;Integrated Security=true").Options);
        var student = context.Model.FindEntityType(typeof(Alumno))!;
        Assert.Equal(new[] { "Matricula", "SemIngresoDecreto" }, student.FindPrimaryKey()!.Properties.Select(p => p.Name));
        Assert.Equal(10, student.FindProperty("Matricula")!.GetMaxLength());
        Assert.Equal(5, student.FindProperty("SemIngresoDecreto")!.GetMaxLength());
        Assert.Equal("Alumnos", student.GetTableName());
    }

    // Test adapter only: EF-InMemory has no SQL collation engine. Real SQL translation is pinned separately.
    private sealed class InMemoryCollation : ExpressionVisitor, IQueryExpressionInterceptor
    {
        public Expression QueryCompilationStarting(Expression queryExpression, QueryExpressionEventData eventData) => Visit(queryExpression);
        protected override Expression VisitMethodCall(MethodCallExpression node) =>
            node.Method.Name == "Collate" && node.Method.DeclaringType == typeof(RelationalDbFunctionsExtensions)
                ? Visit(node.Arguments[1]) : base.VisitMethodCall(node);
    }
}
