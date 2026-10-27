using System.Data;
using Application.Reports.Postgraduate.TeacherSurvey;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Reports.Postgraduate;

/// <summary>
/// Invoca los procedimientos existentes del SAD sin duplicar su lógica. Abre una
/// conexión por operación y usa parámetros tipados de solo lectura.
/// </summary>
public sealed class SqlTeacherSurveyReader(IConfiguration configuration) : ITeacherSurveyReader
{
    private readonly string _connectionString = configuration.GetConnectionString("Sad")
        ?? throw new InvalidOperationException("Falta la conexión de solo lectura al SAD.");

    public async Task<TeacherSurveyFilters> GetFiltersAsync(string? studentType, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        var periods = await ReadOptionsAsync(connection,
            "dbo.spRec_PeriodosAcademicosProgramacion",
            [("@Limite", "20040"), ("@Modo", 0), ("@ConTodos", "NO"), ("@Tipo", "POSTGRADO")],
            "Periodo", "PeriodoLargo", cancellationToken);
        var types = await ReadOptionsAsync(connection, "dbo.spRec_TiposAlumno",
            [("@IncluirTodos", "NO"), ("@Usuario", "")], "Tipo", "Nombre", cancellationToken);
        var modalities = await ReadModalitiesAsync(connection, studentType ?? string.Empty, cancellationToken);
        return new TeacherSurveyFilters(periods, types, modalities);
    }

    public async Task<IReadOnlyList<TeacherSurveyRow>> GetRowsAsync(
        string period, string studentType, string modality, CancellationToken cancellationToken)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new SqlCommand("dbo.spRec_Encuesta_DocenteXCurso", connection)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 60,
        };
        command.Parameters.Add("@Periodo", SqlDbType.VarChar, 20).Value = period;
        command.Parameters.Add("@Tipo_Alumno", SqlDbType.VarChar, 30).Value = studentType;
        command.Parameters.Add("@Modalidad", SqlDbType.VarChar, 30).Value = modality;

        var rows = new List<TeacherSurveyRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new TeacherSurveyRow(
                Period: Text(reader, "Periodo"),
                Course: Text(reader, "Curso"),
                Name: Text(reader, "Catedra"),
                TeacherId: Text(reader, "Cod_Profesor"),
                TeacherName: Text(reader, "Nombre_Profesor"),
                Assistants: Text(reader, "Ayudantes"),
                A1: Number(reader, "A1"), A2: Number(reader, "A2"), A3: Number(reader, "A3"), A4: Number(reader, "A4"),
                AComments: Text(reader, "AObservaciones"),
                B1: Number(reader, "B1"), B2: Number(reader, "B2"), B3: Number(reader, "B3"), B4: Number(reader, "B4"),
                B5: Number(reader, "B5"), B6: Number(reader, "B6"), B7: Number(reader, "B7"), B8: Number(reader, "B8"),
                B9: Number(reader, "B9"), B10: Number(reader, "B10"), B11: Number(reader, "B11"), B12: Number(reader, "B12"),
                B13: Number(reader, "B13"), BGrade: Number(reader, "BNota"), BComments: Text(reader, "BObservaciones")));
        }
        return rows;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private static async Task<IReadOnlyList<TeacherSurveyOption>> ReadOptionsAsync(
        SqlConnection connection, string procedure, (string Name, object Value)[] parameters,
        string valueColumn, string textColumn, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand(procedure, connection) { CommandType = CommandType.StoredProcedure };
        foreach (var (name, value) in parameters)
            command.Parameters.AddWithValue(name, value);
        var options = new List<TeacherSurveyOption>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var text = Text(reader, textColumn);
            if (text.Length > 0) options.Add(new(Text(reader, valueColumn), text));
        }
        return options;
    }

    private static async Task<IReadOnlyList<string>> ReadModalitiesAsync(
        SqlConnection connection, string studentType, CancellationToken cancellationToken)
    {
        await using var command = new SqlCommand("dbo.spRec_TiposAlumnosModalidades", connection)
        {
            CommandType = CommandType.StoredProcedure,
        };
        // Sin tipo seleccionado se listan todas las modalidades conocidas.
        command.Parameters.Add("@Tipo", SqlDbType.VarChar, 30).Value =
            string.IsNullOrWhiteSpace(studentType) ? "%" : studentType;
        var modalities = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var modality = Text(reader, "Modalidad");
            if (modality.Length > 0 && !modalities.Contains(modality, StringComparer.OrdinalIgnoreCase))
                modalities.Add(modality);
        }
        return modalities;
    }

    private static string Text(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal))?.Trim() ?? string.Empty;
    }

    private static decimal? Number(SqlDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        if (reader.IsDBNull(ordinal)) return null;
        return Convert.ToDecimal(reader.GetValue(ordinal));
    }
}
