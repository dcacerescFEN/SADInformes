using System.Threading.Channels;

namespace Application.Reports.AcademicEnrollment.TeacherAvailability.Common.Email;

public sealed class TeacherAvailabilityEmailQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });
    public bool TryEnqueue(Guid id) => _channel.Writer.TryWrite(id);
    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
