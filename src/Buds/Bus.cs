using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Buds.Messages;

namespace Buds
{
    public class Bus : IServiceBus, IDisposable
    {
        public Guid NodeId { get; }
        readonly IFeedClient _client;
        readonly IFeedRegistry _reg;
        readonly CompositeDisposable _disposal;

        Bus(Guid nodeId, IFeedRegistry reg, IFeedClient client, IDisposable disposal)
        {
            NodeId = nodeId;
            _reg = reg;
            _client = client;
            _disposal = disposal != null ? new CompositeDisposable(disposal) : new CompositeDisposable();
        }

        public IDisposable RegisterService<TRequest, TResponse>(string name, Func<TRequest, Task<TResponse>> handler)
            where TRequest : Request, IRequest<TResponse>
            where TResponse : Response
        {
            var repFeed = _client.GetFeed<TRequest>($"p2p/srv/req/{name}")
                .SelectMany<TRequest, Response>(async req =>
                {
                    try
                    {
                        return await handler(req);
                    }
                    catch (Exception ex)
                    {
                        return new ExceptionResponse(NodeId, req.SenderNodeId, req.RequestId, ex);
                    }
                })
                .Publish()
                .RefCount();

            return new CompositeDisposable()
            {
                _reg.RegisterFeed(repFeed.OfType<TResponse>(), rep => $"p2p/srv/rep/{rep.RequestId}"),
                _reg.RegisterFeed(repFeed.OfType<ExceptionResponse>(), rep => $"p2p/srv/exc/{rep.RequestId}")
            };
        }

        public async Task<TResponse> CallService<TRequest, TResponse>(string name, TRequest request)
            where TRequest : Request, IRequest<TResponse>
            where TResponse : Response
        {
            var replies = _client.GetFeed<TResponse>($"p2p/srv/rep/{request.RequestId}");
            var exceptions = _client.GetFeed<ExceptionResponse>($"p2p/srv/exc/{request.RequestId}").SelectMany(exr => Observable.Throw<TResponse>(exr.Exception));

            var result = replies.Merge(exceptions);

            _reg.RegisterFeed(Observable.Return(request), $"p2p/srv/req/{name}");

            return await result.FirstAsync();
        }

        public void Dispose()
        {
            _disposal.Dispose();
        }

        public static Bus Create(Guid? id = null, TimeSpan? heartbeatInterval = null)
        {
            id = id ?? Guid.NewGuid();
            var registry = FeedRegistry.Create(id, heartbeatInterval);
            var client = FeedClient.Create(id);

            return Create(registry, client, new CompositeDisposable()
            {
                registry,
                client
            });
        }

        public static Bus Create(IFeedRegistry reg, IFeedClient client, IDisposable disposal = null)
        {
            if (reg.NodeId != client.NodeId)
                throw new InvalidOperationException("ID of registry and registry client do not match");

            var id = reg.NodeId;

            return new Bus(id, reg, client, disposal);
        }
    }
}
