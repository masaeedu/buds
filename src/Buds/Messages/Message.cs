using System;

namespace Buds.Messages
{
    public abstract class Message : IMessage
    {
        protected Message(Guid senderNodeId)
        {
            SenderNodeId = senderNodeId;
        }
        public Guid SenderNodeId { get; }
    }
}
