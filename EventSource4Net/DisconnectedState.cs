using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventSource4Net
{
    class DisconnectedState : IConnectionState
    {
        private readonly ILoggerFactory _loggerFactory;
        private Uri mUrl;
        private IWebRequesterFactory mWebRequesterFactory;
        public EventSourceState State
        {
            get { return EventSourceState.CLOSED; }
        }

        public DisconnectedState(Uri url, IWebRequesterFactory webRequesterFactory, ILoggerFactory loggerFactory)
        {
            if (url == null) throw new ArgumentNullException("Url cant be null");
            mUrl = url;
            mWebRequesterFactory = webRequesterFactory;
            _loggerFactory = loggerFactory;
        }

        public async Task<IConnectionState> Run(Action<ServerSentEvent> donothing, CancellationToken cancelToken)
        {
            if (cancelToken.IsCancellationRequested)
                return await Task.FromResult<IConnectionState>(new DisconnectedState(mUrl, mWebRequesterFactory, _loggerFactory)).ConfigureAwait(false);
            else
                return await Task.FromResult<IConnectionState>(new ConnectingState(mUrl, mWebRequesterFactory, _loggerFactory)).ConfigureAwait(false);
        }
    }
}
