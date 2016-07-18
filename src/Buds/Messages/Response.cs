using System;
using Buds.Interfaces;

namespace Buds.Messages
{
    public abstract class Response : Message, IResponse
    {
        protected Response(Guid senderNodeId, Guid destinationNodeId, Guid requestId)
            : base(senderNodeId)
        {
            DestinationNodeId = destinationNodeId;
            RequestId = requestId;
        }

        public Guid RequestId { get; }
        public Guid DestinationNodeId { get; }
    }

    public class CompletionResponse : Response
    {
        public CompletionResponse(Guid senderNodeId, Guid destinationNodeId, Guid requestId)
            : base(senderNodeId, destinationNodeId, requestId)
        {
        }
    }

    public class ExceptionResponse : Response, IExceptionResponse
    {
        public ExceptionResponse(Guid senderNodeId, Guid destinationNodeId, Guid requestId, Exception exception)
            : base(senderNodeId, destinationNodeId, requestId)
        {
            Exception = exception;
        }

        public Exception Exception { get; }
    }
}
