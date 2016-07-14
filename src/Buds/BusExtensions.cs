using System;
using Buds.Messages;

namespace Buds
{
    public static class BusExtensions
    {
        public static CompletionResponse CompletedBy(this IRequest<CompletionResponse> request, Guid responder)
        {
            return new CompletionResponse(responder, request.SenderNodeId, request.RequestId);
        }
    }
}
