using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace EventSource4Net
{
    public class EventSource
    {
        private ILogger _logger;
        private ILoggerFactory _loggerFactory;

        public event EventHandler<StateChangedEventArgs> StateChanged;
        public event EventHandler<ServerSentEventReceivedEventArgs> EventReceived;

        private IWebRequesterFactory _webRequesterFactory;
        private int _timeout = 0;
        public Uri Url { get; private set; }
        //public EventSourceState State { get { return CurrentState.State; } }
        public string LastEventId { get; private set; }
        private IConnectionState mCurrentState = null;
        private CancellationToken mStopToken;
        //private CancellationTokenSource mTokenSource = new CancellationTokenSource();
        private IConnectionState CurrentState
        {
            get { return mCurrentState; }
            set
            {
                if (!value.Equals(mCurrentState))
                {
                    StringBuilder sb = new StringBuilder("State changed from ");
                    sb.Append(mCurrentState == null ? "Unknown" : mCurrentState.State.ToString());
                    sb.Append(" to ");
                    sb.Append(value == null ? "Unknown" : value.State.ToString());
                    _logger?.LogTrace(sb.ToString());
                    mCurrentState = value;
                    OnStateChanged(mCurrentState.State);
                }
            }
        }

        public EventSource(Uri url, int timeout)
        {
            Initialize(url, timeout, null, null);
        }

        public EventSource(Uri url, int timeout, ILoggerFactory loggerFactory)
        {
            Initialize(url, timeout, null, loggerFactory);
        }

        protected EventSource(Uri url, IWebRequesterFactory factory)
        {
            Initialize(url, 0, factory, null);
        }


        ///// <summary>
        ///// Constructor for testing purposes
        ///// </summary>
        ///// <param name="factory">The factory that generates the WebRequester to use.</param>
        //protected EventSource(Uri url, IWebRequesterFactory factory, ILoggerFactory loggerFactory)
        //{
        //    _webRequesterFactory = factory;
        //    Initialize(url, 0, loggerFactory);
        //}

        private void Initialize(Uri url, int timeout, IWebRequesterFactory factory, ILoggerFactory loggerFactory)
        {
            _webRequesterFactory = factory ?? new WebRequesterFactory();
            _timeout = timeout;
            Url = url;
            _loggerFactory = loggerFactory;
            CurrentState = new DisconnectedState(Url, _webRequesterFactory, _loggerFactory);
            _logger = _loggerFactory?.CreateLogger<EventSource>();
            _logger?.LogInformation("EventSource created for " + url.ToString());
        }


        /// <summary>
        /// Start the EventSource. 
        /// </summary>
        /// <param name="stopToken">Cancel this token to stop the EventSource.</param>
        public Task Start(CancellationToken stopToken)
        {
            mStopToken = stopToken;
            return Task.Run(async () =>
            {
                if (CurrentState.State == EventSourceState.CLOSED)
                {
                    while (!stopToken.IsCancellationRequested)
                    {
                        await Run().ConfigureAwait(false);
                    }
                }
            }, mStopToken);
        }

        protected async Task Run()
        {
            if (mStopToken.IsCancellationRequested && CurrentState.State == EventSourceState.CLOSED)
                return;

            CurrentState = await mCurrentState.Run(this.OnEventReceived, mStopToken).ConfigureAwait(false);
        }

        protected void OnEventReceived(ServerSentEvent sse)
        {
            EventReceived?.Invoke(this, new ServerSentEventReceivedEventArgs(sse));
        }

        protected void OnStateChanged(EventSourceState newState)
        {
            StateChanged?.Invoke(this, new StateChangedEventArgs(newState));
        }
    }
}
