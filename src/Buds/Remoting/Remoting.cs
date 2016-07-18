using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Buds.Interfaces;
using Buds.Messages;
using AssemblySerialization;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Reactive.Linq;

namespace Buds
{
    public interface IListener<in TMessage>
    {
        void Receive(TMessage msg);
    }

    public interface IListener<in TRequest, out TResponse>
        where TRequest : Request, IRequest<TResponse>
        where TResponse : Response
    {
        IObservable<TResponse> Receive(TRequest req);
    }

    public enum ListenerType
    {
        Feed,
        Service
    }

    public class LoadSerializedAssembly : Request, ICommand
    {
        public IReadOnlyList<byte[]> AssemblyData { get; }

        public LoadSerializedAssembly(Guid senderNodeId, Guid requestId, IReadOnlyList<byte[]> assemblyData)
            : base(senderNodeId, requestId)
        {
            AssemblyData = assemblyData;
        }
    }

    public class RegisterFeedListener : Request, ICommand
    {
        public RegisterFeedListener(Guid senderNodeId, Guid requestId, string listenerType, string messageType, string topic)
            : base(senderNodeId, requestId)
        {
            ListenerType = listenerType;
            MessageType = messageType;
            Topic = topic;
        }

        public string Topic { get; }
        public string ListenerType { get; set; }
        public string MessageType { get; }

        public static RegisterFeedListener Create<TListener, TMessage>(Guid senderNodeId, Guid requestId, string topic = null) where TListener : IListener<TMessage>
        {
            return new RegisterFeedListener(senderNodeId, requestId, typeof(TListener).FullName, typeof(TMessage).FullName, topic);
        }
    }

    public class RegisterServiceListener : Request, ICommand
    {
        public RegisterServiceListener(Guid senderNodeId, Guid requestId, string listenerType, string requestType, string responseType, string name)
            : base(senderNodeId, requestId)
        {
            ListenerType = listenerType;
            Name = name;
            RequestType = requestType;
            ResponseType = responseType;
        }

        public string Name { get; }
        public string ListenerType { get; }
        public string RequestType { get; }
        public string ResponseType { get; }

        public static RegisterServiceListener Create<TListener, TRequest, TResponse>(Guid senderNodeId, Guid requestId, string name)
            where TListener : IListener<TRequest, TResponse> 
            where TRequest : Request, IRequest<TResponse> 
            where TResponse : Response
        {
            return new RegisterServiceListener(senderNodeId, requestId, typeof(TListener).FullName, typeof(TRequest).FullName, typeof(TResponse).FullName, name);
        }
    }

    public class RemotingAgent
    {
        public const string ASSM_LOAD = "remote/asm";
        public const string FEED_REGISTRATION = "remote/feed";
        public const string SERVICE_REGISTRATION = "remote/service";

        readonly IDictionary<string, IDisposable> _subscriptions = new ConcurrentDictionary<string, IDisposable>();
        readonly ConcurrentBag<Assembly> _assemblies = new ConcurrentBag<Assembly>();
        readonly IBus _bus;

        // ReSharper disable once SuggestBaseTypeForParameter
        public RemotingAgent(IBus bus)
        {
            _bus = bus;
        }

        public IDisposable Run()
        {
            return new CompositeDisposable()
            {
                _bus.RegisterService<LoadSerializedAssembly>(ASSM_LOAD, LoadAssemblies),
                _bus.RegisterService<RegisterFeedListener>(FEED_REGISTRATION, HandleFeedRegistration),
                _bus.RegisterService<RegisterServiceListener>(SERVICE_REGISTRATION, HandleServiceRegistration)
            };
        }

        async Task LoadAssemblies(LoadSerializedAssembly msg)
        {
            if (msg.SenderNodeId != _bus.NodeId)
            {
                _assemblies.Add(msg.AssemblyData.DeserializeAndLoadAssemblyGraph().Last());
            }
        }

        Type FindTypeInLoadedAssemblies(string typeName)
        {
            return _assemblies.Select(a => a.GetType(typeName)).Single(t => t != null);
        }

        async Task HandleFeedRegistration(RegisterFeedListener msg)
        {
            var topic = msg.Topic;
            var listenerType = FindTypeInLoadedAssemblies(msg.ListenerType);
            var instance = (dynamic)Activator.CreateInstance(listenerType);

            var feed = (IObservable<dynamic>)typeof(IFeedClient).GetTypeInfo().GetMethod(nameof(IFeedClient.GetFeed)).MakeGenericMethod(FindTypeInLoadedAssemblies(msg.MessageType)).Invoke(_bus, new object[] { topic });

            IDisposable existing;
            if (_subscriptions.TryGetValue(topic ?? "", out existing)) existing.Dispose();

            _subscriptions[topic ?? ""] = feed.Subscribe(i => instance.Receive(i));
        }

        async Task HandleServiceRegistration(RegisterServiceListener msg)
        {
            var listenerType = FindTypeInLoadedAssemblies(msg.ListenerType);
            var instance = (IListener<dynamic>)Activator.CreateInstance(listenerType);

            Func<dynamic, dynamic> f = req => instance.Receive(req);
            var registration = (IDisposable)typeof(IServiceRegistry).GetTypeInfo().GetMethod(nameof(IServiceRegistry.RegisterService)).MakeGenericMethod(FindTypeInLoadedAssemblies(msg.RequestType), FindTypeInLoadedAssemblies(msg.ResponseType)).Invoke(_bus, new object[]
            {
                msg.Name,
                f
            });
        }
    }
}
