using System;
using Buds.Interfaces;
using System.Reactive.Linq;

namespace Buds.Extensions
{
    public static class BusExtensions
    {
        public static IDisposable Debug(this IBus bus)
        {
            return bus.GetFeed<object>()
                .Select(o => o.SerializeAsJson())
                .Subscribe(Console.WriteLine);
        }
    }
}
