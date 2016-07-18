using System;

namespace Buds.Interfaces
{
    public interface IRepresentNode
    {
        Guid NodeId { get; }
    }

    public interface IPertainsToRequest
    {
        Guid RequestId { get; }
    }

    public interface IBus : IFeedBus, IServiceBus
    {
    }
}