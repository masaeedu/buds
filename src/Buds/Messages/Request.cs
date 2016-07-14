using System;

namespace AgentAutomation.PeerCooperation.Messages
{
    public abstract class Request : Message, IRequest<Response>
    {
        public Guid RequestId { get; }

        protected Request(Guid senderNodeId, Guid? requestId = null)
            : base(senderNodeId)
        {
            RequestId = requestId ?? Guid.NewGuid();
        }
    }
}
