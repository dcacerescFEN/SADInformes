using Application.Reports.Postgraduate.MassEnrollment.Common;

namespace Application.Reports.Postgraduate.MassEnrollment.Commands.CreateMassEnrollment;

public sealed record MassEnrollmentRequestDTO(
    MassEnrollmentSelection? Selection, string[]? StudentCodes, bool Confirmed);
