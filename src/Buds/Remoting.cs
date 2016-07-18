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

    public class LoadSerializedAssembly : Request, IRequest<CompletionResponse>
    {
        public IReadOnlyList<byte[]> AssemblyData { get; }

        public LoadSerializedAssembly(Guid senderNodeId, IReadOnlyList<byte[]> assemblyData)
            : base(senderNodeId)
        {
            AssemblyData = assemblyData;
        }
    }

    public class RegisterFeedListener : Request, IRequest<CompletionResponse>
    {
        public RegisterFeedListener(Guid senderNodeId, string listenerType, string messageType, string topic)
            : base(senderNodeId)
        {
            ListenerType = listenerType;
            MessageType = messageType;
            Topic = topic;
        }

        public string Topic { get; }
        public string ListenerType { get; set; }
        public string MessageType { get; }

        public static RegisterFeedListener Create<TListener, TMessage>(Guid senderNodeId, string topic = null) where TListener : IListener<TMessage>
        {
            return new RegisterFeedListener(senderNodeId, typeof(TListener).FullName, typeof(TMessage).FullName, topic);
        }
    }

    public class RegisterServiceListener : Request, IRequest<CompletionResponse>
    {
        public RegisterServiceListener(Guid senderNodeId, string listenerType, string requestType, string responseType, string name)
            : base(senderNodeId)
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

        public static RegisterServiceListener Create<TListener, TRequest, TResponse>(Guid senderNodeId, string name)
            where TListener : IListener<TRequest, TResponse> 
            where TRequest : Request, IRequest<TResponse> 
            where TResponse : Response
        {
            return new RegisterServiceListener(senderNodeId, typeof(TListener).FullName, typeof(TRequest).FullName, typeof(TResponse).FullName, name);
        }
    }

    public class RemotingAgent
    {
        public const string ASSM_LOAD = "remote/asm";
        public const string FEED_REGISTRATION = "remote/feed";
        public const string SERVICE_REGISTRATION_PREFIX = "remote/service";

        readonly IDictionary<string, IDisposable> _handlers = new ConcurrentDictionary<string, IDisposable>();
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
                _bus.RegisterService<RegisterServiceListener>(SERVICE_REGISTRATION_PREFIX, HandleServiceRegistration)
            };
        }

        async Task LoadAssemblies(LoadSerializedAssembly msg)
        {
            if (msg.SenderNodeId != _bus.NodeId)
            {
                msg.AssemblyData.DeserializeAndLoadAssemblyGraph();
            }
        }

        async Task HandleFeedRegistration(RegisterFeedListener msg)
        {
            var listenerType = Type.GetType(msg.ListenerType);
            var instance = (dynamic)Activator.CreateInstance(listenerType);

            var feed = (IObservable<dynamic>)typeof(IFeedClient).GetTypeInfo().GetMethod(nameof(IFeedClient.GetFeed)).MakeGenericMethod(Type.GetType(msg.MessageType)).Invoke(_bus, new object[] { msg.Topic });

            IDisposable existing;
            if (_handlers.TryGetValue(msg.Topic, out existing)) existing.Dispose();

            _handlers[msg.Topic] = feed.Subscribe(i => instance.Receive(i));
        }

        async Task HandleServiceRegistration(RegisterServiceListener msg)
        {
            var listenerType = Type.GetType(msg.ListenerType);
            var instance = (IListener<dynamic>)Activator.CreateInstance(listenerType);

            Func<dynamic, dynamic> f = req => instance.Receive(req);
            var registration = (IDisposable)typeof(IServiceRegistry).GetTypeInfo().GetMethod(nameof(IServiceRegistry.RegisterService)).MakeGenericMethod(Type.GetType(msg.RequestType), Type.GetType(msg.ResponseType)).Invoke(_bus, new object[]
            {
                msg.Name,
                f
            });
        }
    }
}
