using System;
using Buds.Extensions;

namespace Buds.Unspecified
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var id = Guid.NewGuid();
            using (var feedReg = FeedRegistry.Create(id))
            using (var feedClient = FeedClient.Create(id))
            {
                var bus = Bus.Create(feedReg, feedClient);
                bus.Debug();
                var remoting = new RemotingAgent(bus);

                using (remoting.Run())
                {
                    Console.ReadLine();
                }
            }
        }
    }
}
