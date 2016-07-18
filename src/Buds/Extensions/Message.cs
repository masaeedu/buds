using System;
using Buds.Interfaces;
using Buds.Messages;

namespace Buds.Extensions
{
    public static class MessageExtensions
    {
        public static CompletionResponse CompletedBy(this IRequest<CompletionResponse> request, Guid responder)
        {
            return new CompletionResponse(responder, request.SenderNodeId, request.RequestId);
        }
    }
}
