using System;
using System.Threading.Tasks;
using Buds.Messages;

namespace Buds.Interfaces
{
    // TODO: Change service registry and client signatures to facilitate reporting progress (perhaps accept an IObserver<IProgress> or something, and forward it qualitative and/or quantitative progress updates)
    public interface IServiceRegistry : IRepresentNode
    {
        IDisposable RegisterService<TRequest>(string name, Func<TRequest, Task> handler)
            where TRequest : Request;
        IDisposable RegisterService<TRequest, TResponse>(string name, Func<TRequest, IObservable<TResponse>> handler)
            where TRequest : Request, IRequest<TResponse>
            where TResponse : Response;
    }

    public interface IServiceClient : IRepresentNode
    {
        IObservable<TResponse> CallService<TRequest, TResponse>(string name, TRequest request)
            where TRequest : Request, IRequest<TResponse>
            where TResponse : Response;
    }

    public interface IServiceBus : IServiceRegistry, IServiceClient
    {
    }
}
