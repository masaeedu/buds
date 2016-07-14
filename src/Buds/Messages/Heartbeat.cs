using System;

namespace AgentAutomation.PeerCooperation.Messages
{
    public class Heartbeat : Message
    {
        public Heartbeat(Guid senderNodeId, long sequence, int pubPort, DateTime dispatchTime) : base(senderNodeId)
        {
            PubPort = pubPort;
            DispatchTime = dispatchTime;
            Sequence = sequence;
        }

        public long Sequence { get; }
        public DateTime DispatchTime { get; }
        public int PubPort { get; }
    }
}
