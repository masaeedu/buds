using System;
using Buds.Interfaces;

namespace Buds.Messages
{
    public abstract class Request : Message, IRequest
    {
        public Guid RequestId { get; }

        protected Request(Guid senderNodeId, Guid? requestId = null)
            : base(senderNodeId)
        {
            RequestId = requestId ?? Guid.NewGuid();
        }
    }

    public class FireAndForgetRequest : Request, IRequest<CompletionResponse>
    {
        public FireAndForgetRequest(Guid senderNodeId, Guid? requestId = null)
            : base(senderNodeId, requestId)
        {
        }
    }
}
