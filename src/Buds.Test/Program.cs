using System;
using System.Collections.Generic;
using Buds.Messages;
using System.Reactive.Linq;
using Buds.Extensions;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using AssemblySerialization;
using Buds.Interfaces;

namespace Buds.Test
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var selfId = Guid.NewGuid();
            using (var feedReg = FeedRegistry.Create(selfId))
            using (var feedClient = FeedClient.Create(selfId))
            {
                IBus bus = Bus.Create(feedReg, feedClient);
                bus.Debug();

                var remoteBoxes = bus.GetFeed<Heartbeat>()
                    .Where(hb => hb.SenderNodeId != selfId)
                    .Distinct(hb => hb.SenderNodeId)
                    .Take(1)
                    .ToList()
                    .GetAwaiter()
                    .GetResult();

                bus.CallService<LoadSerializedAssembly, CompletionResponse>(RemotingAgent.ASSM_LOAD, new LoadSerializedAssembly(bus.NodeId, Assembly.GetEntryAssembly().Serialize())).LastAsync().GetAwaiter().GetResult();
                bus.RegisterFeed(Observable.Interval(TimeSpan.FromSeconds(3))
                    .Select(_ => new Foo(bus.NodeId)));

                Console.WriteLine($"Hostnames: {remoteBoxes.Select(hb => hb.HostName).ToArray()} are now online!");
                bus.CallService<RegisterFeedListener, CompletionResponse>(RemotingAgent.FEED_REGISTRATION, RegisterFeedListener.Create<FooListener, Foo>(bus.NodeId));

                Console.ReadLine();
            }
        }
    }

    public class FooListener : IListener<Foo>
    {
        public void Receive(Foo msg)
        {
            Console.WriteLine(msg);
        }
    }

    public class Foo : Message
    {
        public Foo(Guid senderNodeId)
            : base(senderNodeId)
        {
        }
    }
}
