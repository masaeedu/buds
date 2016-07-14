using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Text;
using Buds.Messages;
using NetMQ;
using NetMQ.Sockets;

namespace Buds
{
    public class FeedRegistry : IFeedRegistry, IDisposable
    {
        public const int DISCOVERY_PORT = 56001;
        public const int DEFAULT_HEARTBEAT_INTERVAL = 10;

        public Guid NodeId { get; }

        readonly CompositeDisposable _disposal;
        readonly int _pubPort;
        readonly TimeSpan _heartbeatInterval;
        readonly PublisherSocket _pub;
        readonly NetMQBeacon _beacon;
        readonly Subject<Unit> _heartbeatGovernor = new Subject<Unit>();
        readonly object _publishGate = new object();

        FeedRegistry(Guid nodeId, int pubPort, TimeSpan heartbeatInterval, PublisherSocket pub, NetMQBeacon beacon, IDisposable disposal)
        {
            NodeId = nodeId;
            _pubPort = pubPort;
            _heartbeatInterval = heartbeatInterval;
            _pub = pub;
            _beacon = beacon;
            _disposal = new CompositeDisposable() { disposal };

            InitializeHeartbeating();
        }

        void InitializeHeartbeating()
        {
            // Send a heartbeat whenever governor subject receives a value
            var i = 0;
            var beats = _heartbeatGovernor
                .Select(_ => new Heartbeat(NodeId, i++, _pubPort, DateTime.Now));

            // Send heartbeats over UDP beacon broadcasts
            var beaconBinding = beats
                .Synchronize()
                .Subscribe(BroadcastHeartbeat);

            // Also register heartbeats as a PUB feed
            // TODO: Eliminate duplication of heartbeat feed over PUB and get client to just consume beacon feed
            var pubBinding = RegisterFeed(beats, "p2p/hb");

            // Start heartbeating periodically
            var intervalBinding = Observable.Return<long>(0)
                .Merge(Observable.Interval(_heartbeatInterval))
                .Select(_ => Unit.Default)
                .Subscribe(_heartbeatGovernor);

            _disposal.Add(new CompositeDisposable {
                intervalBinding,
                pubBinding,
                beaconBinding
            });
        }

        public void Dispose()
        {
            _disposal.Dispose();
        }

        public IDisposable RegisterFeed<T>(IObservable<T> feed, string topic = null)
        {
            var header = CreateMessageHeader(typeof(T), topic);

            return feed
                .Synchronize(_publishGate)
                .Subscribe(msg => PublishMessage(header, msg));
        }

        public IDisposable RegisterFeed<T>(IObservable<T> feed, Func<T, string> topicSelector)
        {
            return feed
                .Synchronize(_publishGate)
                .Subscribe(msg => PublishMessage(CreateMessageHeader(msg.GetType(), topicSelector(msg)), msg));
        }

        void PublishMessage(string header, object msg)
        {
            _pub.SendMoreFrame(header)
                .SendFrame((string)SerializationExtensions.SerializeAsJson((dynamic)msg));
        }

        void BroadcastHeartbeat(Heartbeat h)
        {
            _beacon.Publish(h.SerializeAsJson(), Encoding.UTF8);
        }

        static IEnumerable<Type> GetTypeHierarchy(Type type)
        {
            var hierarchy = new Stack<Type>();
            while (type != null)
            {
                hierarchy.Push(type);
                type = type.GetTypeInfo().BaseType;
            }

            return hierarchy;
        }

        public static string CreateMessageHeader(Type type = null, string topic = null)
        {
            var topicChunks = GetTypeHierarchy(type)
                .Concat(new object[]
                {
                    topic
                })
                .Where(o => o != null);

            return string.Join("/", topicChunks);
        }

        public static FeedRegistry Create(Guid? id = null, TimeSpan? heartbeatInterval = null)
        {
            id = id ?? Guid.NewGuid();
            heartbeatInterval = heartbeatInterval ?? TimeSpan.FromSeconds(DEFAULT_HEARTBEAT_INTERVAL);

            var pub = new PublisherSocket();
            var port = pub.BindRandomPort("tcp://*");

            var beacon = new NetMQBeacon();
            beacon.ConfigureAllInterfaces(DISCOVERY_PORT);

            var poller = new NetMQPoller() { pub };
            poller.RunAsync();

            var reg = new FeedRegistry(id.Value, port, heartbeatInterval.Value, pub, beacon, new CompositeDisposable
            {
                poller,
                beacon,
                pub
            });

            return reg;
        }
    }
}
