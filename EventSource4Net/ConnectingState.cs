using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace EventSource4Net
{
    class ConnectingState : IConnectionState
    {
        private ILogger _logger;
        private ILoggerFactory _loggerFactory;

        private Uri mUrl;
        private IWebRequesterFactory mWebRequesterFactory;
        public EventSourceState State { get { return EventSourceState.CONNECTING; } }

        public ConnectingState(Uri url, IWebRequesterFactory webRequesterFactory) : this(url, webRequesterFactory, null) { }


        public ConnectingState(Uri url, IWebRequesterFactory webRequesterFactory, ILoggerFactory loggerFactory)
        {
            if (url == null) throw new ArgumentNullException("Url cant be null");
            if (webRequesterFactory == null) throw new ArgumentNullException("Factory cant be null");
            mUrl = url;
            mWebRequesterFactory = webRequesterFactory;
            _loggerFactory = loggerFactory;
            _logger = _loggerFactory?.CreateLogger<ConnectingState>();
        }

        public async Task<IConnectionState> Run(Action<ServerSentEvent> donothing, CancellationToken cancelToken)
        {
            IWebRequester requester = mWebRequesterFactory.Create();
            var taskResp = await requester.Get(mUrl, cancelToken);

            if (!cancelToken.IsCancellationRequested)
            {
                IServerResponse response = taskResp;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return new ConnectedState(response, mWebRequesterFactory, _loggerFactory);
                }
                else
                {
                    _logger?.LogInformation("Failed to connect to: " + mUrl.ToString() + response ?? (" Http statuscode: " + response.StatusCode));
                }
            }
            else
            {
                taskResp.Dispose();
                taskResp = null;
            }

            return new DisconnectedState(mUrl, mWebRequesterFactory, _loggerFactory);
        }
    }
}
