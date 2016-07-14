using System;
using System.Reactive;
using System.Threading.Tasks;
using AgentAutomation.PeerCooperation.Messages;

namespace AgentAutomation.PeerCooperation
{
    public interface IRepresentNode
    {
        Guid NodeId { get; }
    }

    public interface IFeedRegistry : IRepresentNode
    {
        IDisposable RegisterFeed<T>(IObservable<T> feed, string topic = null);
        IDisposable RegisterFeed<T>(IObservable<T> feed, Func<T, string> topicSelector);
    }

    public interface IFeedClient : IRepresentNode
    {
        IObservable<T> GetFeed<T>(string topic = null);
    }

    // TODO: Change service registry and client signatures to facilitate reporting progress (perhaps accept an IObserver<IProgress> or something, and forward it qualitative and/or quantitative progress updates)
    public interface IServiceRegistry
    {
        IDisposable RegisterService<TRequest, TResponse>(string name, Func<TRequest, Task<TResponse>> handler)
            where TRequest : Request, IRequest<TResponse>
            where TResponse : Response;
    }

    
    public interface IServiceClient
    {
        Task<TResponse> CallService<TRequest, TResponse>(string name, TRequest request)
            where TRequest : Request, IRequest<TResponse>
            where TResponse : Response;
    }

    public interface IServiceBus : IRepresentNode, IServiceRegistry, IServiceClient
    {
    }

    public interface IMessage
    {
        Guid SenderNodeId { get; }
    }

    public interface IPertainsToRequest
    {
        Guid RequestId { get; }
    }

    public interface IRequest<TResponse> : IMessage, IPertainsToRequest
        where TResponse : Response
    {
    }

    public interface IResponse : IMessage, IPertainsToRequest
    {
        Guid DestinationNodeId { get; }
    }

    public interface IExceptionResponse<out TException> : IResponse
        where TException : Exception
    {
        TException Exception { get; }
    }

    public interface IExceptionResponse : IExceptionResponse<Exception>
    {
    }
}