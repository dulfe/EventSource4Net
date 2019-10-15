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

        private Uri mUrl;
        private IWebRequesterFactory mWebRequesterFactory;
        public EventSourceState State { get { return EventSourceState.CONNECTING; } }

        public ConnectingState(Uri url, IWebRequesterFactory webRequesterFactory) : this(url, webRequesterFactory, null) { }


        public ConnectingState(Uri url, IWebRequesterFactory webRequesterFactory, ILogger logger)
        {
            if (url == null) throw new ArgumentNullException("Url cant be null");
            if (webRequesterFactory == null) throw new ArgumentNullException("Factory cant be null");
            mUrl = url;
            mWebRequesterFactory = webRequesterFactory;
            _logger = logger;
        }

        public Task<IConnectionState> Run(Action<ServerSentEvent> donothing, CancellationToken cancelToken)
        {
            IWebRequester requester = mWebRequesterFactory.Create();
            var taskResp = requester.Get(mUrl);

            return taskResp.ContinueWith<IConnectionState>(tsk =>
            {
                if (tsk.Status == TaskStatus.RanToCompletion && !cancelToken.IsCancellationRequested)
                {
                    IServerResponse response = tsk.Result;
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        return new ConnectedState(response, mWebRequesterFactory);
                    }
                    else
                    {
                        _logger?.LogInformation("Failed to connect to: " + mUrl.ToString() + response ?? (" Http statuscode: " + response.StatusCode));
                    }
                }

                return new DisconnectedState(mUrl, mWebRequesterFactory);
            });
        }
    }
}
