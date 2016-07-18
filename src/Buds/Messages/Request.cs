using System;
using Buds.Interfaces;

namespace Buds.Messages
{
    public abstract class Request : Message, IRequest
    {
        public Guid RequestId { get; }

        protected Request(Guid senderNodeId, Guid requestId)
            : base(senderNodeId)
        {
            RequestId = requestId;
        }
    }
}
