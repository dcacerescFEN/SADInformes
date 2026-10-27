namespace Application.Common.Mediator;

// Same request/handler dispatch contract as NewSAD, without an extra scanning package.
public sealed class RequestDispatcher(IServiceProvider services) : IRequestDispatcher
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        dynamic handler = services.GetService(handlerType)
            ?? throw new InvalidOperationException("No se registró el manejador de la consulta.");
        return handler.Handle((dynamic)request, cancellationToken);
    }
}
