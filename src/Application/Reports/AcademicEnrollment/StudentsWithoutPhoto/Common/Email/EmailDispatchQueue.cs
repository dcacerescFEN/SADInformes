using System.Threading.Channels;

namespace Application.Reports.AcademicEnrollment.StudentsWithoutPhoto.Common.Email;

public sealed class EmailDispatchQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true });

    public bool TryEnqueue(Guid jobId) => _channel.Writer.TryWrite(jobId);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
