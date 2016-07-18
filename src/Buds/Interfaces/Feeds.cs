using System;

namespace Buds.Interfaces
{
    public interface IFeedRegistry : IRepresentNode
    {
        IDisposable RegisterFeed<T>(IObservable<T> feed, string topic = null);
        IDisposable RegisterFeed<T>(IObservable<T> feed, Func<T, string> topicSelector);
    }

    public interface IFeedClient : IRepresentNode
    {
        IObservable<T> GetFeed<T>(string topic = null);
    }

    public interface IFeedBus : IFeedRegistry, IFeedClient { }
}
