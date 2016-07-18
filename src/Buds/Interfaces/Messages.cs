using System;

namespace Buds.Interfaces
{
    public interface IMessage
    {
        Guid SenderNodeId { get; }
    }

    public interface IEvent : IMessage
    {
        string Topic { get; }
    }

    public interface IRequest : IMessage, IPertainsToRequest
    {
    }

    public interface IRequest<TResponse> : IRequest
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
