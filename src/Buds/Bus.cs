using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Buds.Interfaces;
using Buds.Messages;

namespace Buds
{
    public class Bus : IBus
    {
        public const string REQUEST_TOPIC_PREFIX = "p2p/srv/req";
        public const string RESPONSE_TOPIC_PREFIX = "p2p/srv/rep";
        public const string EXCEPTION_TOPIC_PREFIX = "p2p/srv/exc";
        public const string COMPLETION_TOPIC_PREFIX = "p2p/srv/cmp";

        readonly IFeedClient _client;
        readonly IFeedRegistry _reg;

        Bus(Guid nodeId, IFeedRegistry reg, IFeedClient client)
        {
            NodeId = nodeId;
            _reg = reg;
            _client = client;
        }

        public Guid NodeId { get; }

        public IObservable<T> GetFeed<T>(string topic = null)
        {
            return _client.GetFeed<T>(topic);
        }

        IDisposable IServiceRegistry.RegisterService<TRequest>(string name, Func<TRequest, Task> handler)
        {
            return RegisterService<TRequest, CompletionResponse>(name, req =>
            {
                handler(req).GetAwaiter().GetResult();
                return Observable.Empty<CompletionResponse>();
            });
        }

        IDisposable IServiceRegistry.RegisterService<TRequest, TResponse>(string name, Func<TRequest, IObservable<TResponse>> handler)
        {
            return RegisterService(name, handler);
        }

        async Task IServiceClient.CallService<TRequest>(string name, TRequest request)
        {
            await CallService<TRequest, Unit>(name, request).ToList();
        }

        IObservable<TResponse> IServiceClient.CallService<TRequest, TResponse>(string name, TRequest request)
        {
            return CallService<TRequest, TResponse>(name, request);
        }

        IObservable<TResponse> CallService<TRequest, TResponse>(string name, TRequest request)
            where TRequest : Request, IRequest<TResponse>
        {
            var replies = _client.GetFeed<TResponse>($"{RESPONSE_TOPIC_PREFIX}/{request.RequestId}");
            var completion = _client.GetFeed<CompletionResponse>($"{COMPLETION_TOPIC_PREFIX}/{request.RequestId}");
            var exceptions = _client.GetFeed<ExceptionResponse>($"{EXCEPTION_TOPIC_PREFIX}/{request.RequestId}")
                .SelectMany(exr => Observable.Throw<TResponse>(exr.Exception));

            var result = replies.Merge(exceptions)
                .TakeUntil(completion);

            _reg.RegisterFeed(Observable.Return(request), $"{REQUEST_TOPIC_PREFIX}/{name}");

            return result;
        }

        public IDisposable RegisterFeed<T>(IObservable<T> feed, string topic = null)
        {
            return _reg.RegisterFeed(feed, topic);
        }

        public IDisposable RegisterFeed<T>(IObservable<T> feed, Func<T, string> topicSelector)
        {
            return _reg.RegisterFeed(feed, topicSelector);
        }

        IDisposable RegisterService<TRequest, TResponse>(string name, Func<TRequest, IObservable<TResponse>> handler) where TRequest : Request where TResponse : class
        {
            var requests = _client.GetFeed<TRequest>($"{REQUEST_TOPIC_PREFIX}/{name}");

            var processing = requests.Select(req => new
                {
                    Request = req,
                    Result = handler(req)
                })
                .Publish()
                .RefCount();

            var disposal = new CompositeDisposable();

            processing.Subscribe(x =>
            {
                var req = x.Request;

                var responses = x.Result;
                var errors = responses.Catch<object, Exception>(ex => Observable.Return(new ExceptionResponse(NodeId, req.SenderNodeId, req.RequestId, ex)))
                    .OfType<ExceptionResponse>();
                var completion = responses.LastOrDefaultAsync();

                var reqFeeds = new CompositeDisposable
                {
                    _reg.RegisterFeed(responses, rep => $"{RESPONSE_TOPIC_PREFIX}/{req.RequestId}"),
                    _reg.RegisterFeed(errors, rep => $"{EXCEPTION_TOPIC_PREFIX}/{req.RequestId}"),
                    _reg.RegisterFeed(completion.Select(_ => new CompletionResponse(NodeId, req.SenderNodeId, req.RequestId)), rep => $"{COMPLETION_TOPIC_PREFIX}/{req.RequestId}")
                };

                disposal.Add(reqFeeds);
            });

            return disposal;
        }

        public static Bus Create(IFeedRegistry reg, IFeedClient client)
        {
            if (reg.NodeId != client.NodeId)
                throw new InvalidOperationException("ID of registry and registry client do not match");

            var id = reg.NodeId;

            return new Bus(id, reg, client);
        }
    }
}
