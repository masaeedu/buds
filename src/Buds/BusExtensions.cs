using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AgentAutomation.PeerCooperation.Messages;

namespace AgentAutomation.PeerCooperation
{
    public static class BusExtensions
    {
        public static CompletionResponse CompletedBy(this IRequest<CompletionResponse> request, Guid responder)
        {
            return new CompletionResponse(responder, request.SenderNodeId, request.RequestId);
        }
    }
}
