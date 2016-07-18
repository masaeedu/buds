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

                var remoteBoxes = bus.GetFeed<Heartbeat>()
                    .Where(hb => hb.SenderNodeId != selfId)
                    .Distinct(hb => hb.SenderNodeId)
                    .Take(1)
                    .ToList()
                    .GetAwaiter()
                    .GetResult();

                // Load assemblies in remote session
                bus.CallService(RemotingAgent.ASSM_LOAD, new LoadSerializedAssembly(bus.NodeId, Guid.NewGuid(), Assembly.GetEntryAssembly().Serialize())).GetAwaiter().GetResult();

                // Register remote listener and send it a message
                Console.WriteLine($"Hosts: {string.Join(", ", remoteBoxes.Select(hb => hb.HostName).ToArray())} now online!");
                bus.CallService(RemotingAgent.FEED_REGISTRATION, RegisterFeedListener.Create<FooListener, Foo>(bus.NodeId, Guid.NewGuid())).GetAwaiter().GetResult();
                bus.Send(new Foo(bus.NodeId));
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
