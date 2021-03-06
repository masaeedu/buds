using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Buds.Interfaces;
using Buds.Messages;
using NetMQ;
using NetMQ.Sockets;

namespace Buds
{
    public class FeedClient : IFeedClient, IDisposable
    {
        readonly NetMQPoller _poller;
        readonly ConcurrentDictionary<Guid, SubscriberSocket> _endpoints = new ConcurrentDictionary<Guid, SubscriberSocket>();
        readonly ConcurrentDictionary<string, Unit> _topics = new ConcurrentDictionary<string, Unit>();
        readonly Subject<Tuple<string, string>> _rawData = new Subject<Tuple<string, string>>();
        readonly CompositeDisposable _disposal;

        FeedClient(Guid nodeId, NetMQBeacon beacon, NetMQPoller poller, IDisposable disposal)
        {
            NodeId = nodeId;
            _poller = poller;
            _disposal = new CompositeDisposable
            {
                disposal
            };

            // TODO: Pre-add beacon to poller in Create function once https://github.com/zeromq/netmq/issues/571 is fixed
            beacon.ReceiveReady += (s, e) => ReceiveHeartbeat(e.Beacon.Receive());
            poller.Add(beacon);
        }

        public void Dispose()
        {
            _disposal.Dispose();
        }

        public Guid NodeId { get; }

        // TODO: Use Observable.Create overload to ensure you don't continue spamming data after subscription is disposed
        public IObservable<T> GetFeed<T>(string topic = null)
        {
            var header = FeedRegistry.CreateMessageHeader(typeof(T), topic);

            _topics[header] = Unit.Default;
            foreach (var socket in _endpoints.Values)
                socket.Subscribe(header);

            return _rawData.AsObservable()
                .Where(t => t.Item1.StartsWith(header, StringComparison.Ordinal))
                .Select(t => t.Item2.DeserializeFromJson<T>());
        }

        // This doesn't need locking IF AND ONLY IF this is never used for anything but the beacon's ReceiveReady handler
        void ReceiveHeartbeat(BeaconMessage msg)
        {
            var heartbeat = msg.String.DeserializeFromJson<Heartbeat>();

            if (_endpoints.ContainsKey(heartbeat.SenderNodeId))
                return;
            _endpoints[heartbeat.SenderNodeId] = PrepareSubscriptionToNewRemote(msg.PeerHost, heartbeat.PubPort);
            _disposal.Add(_endpoints[heartbeat.SenderNodeId]);
        }

        // This doesn't need locking IF AND ONLY IF this is never used for anything but each subscriber's ReceiveReady handler
        void ReceiveMessage(object sender, NetMQSocketEventArgs e)
        {
            var topic = e.Socket.ReceiveFrameString();
            var data = e.Socket.ReceiveFrameString();
            _rawData.OnNext(Tuple.Create(topic, data));
        }

        SubscriberSocket PrepareSubscriptionToNewRemote(string host, int port)
        {
            var socket = new SubscriberSocket($">tcp://{host}:{port}");
            socket.ReceiveReady += ReceiveMessage;

            foreach (var topic in _topics.Keys)
            {
                socket.Subscribe(topic);
            }

            _poller.Add(socket);

            return socket;
        }

        public static FeedClient Create(Guid id)
        {
            var beacon = new NetMQBeacon();
            beacon.ConfigureAllInterfaces(FeedRegistry.DISCOVERY_PORT);
            beacon.Subscribe("");

            var poller = new NetMQPoller();
            poller.RunAsync();

            return new FeedClient(id, beacon, poller, new CompositeDisposable
            {
                poller,
                beacon
            });
        }
    }
}
