using System.Reactive.Linq;
using Buds.Interfaces;

namespace Buds.Extensions
{
    public static class FeedExtensions
    {
        public static void Send<T>(this IFeedRegistry reg, T msg, string topic = null)
        {
            reg.RegisterFeed(Observable.Return(msg), topic);
        }
    }
}
