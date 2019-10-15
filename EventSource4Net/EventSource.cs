﻿using System;
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

        public event EventHandler<StateChangedEventArgs> StateChanged;
        public event EventHandler<ServerSentEventReceivedEventArgs> EventReceived;

        private readonly IWebRequesterFactory _webRequesterFactory = new WebRequesterFactory();
        private int _timeout = 0;
        public Uri Url { get; private set; }
        public EventSourceState State { get { return CurrentState.State; } }
        public string LastEventId { get; private set; }
        private IConnectionState mCurrentState = null;
        private CancellationToken mStopToken;
        private CancellationTokenSource mTokenSource = new CancellationTokenSource();
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
            Initialize(url, timeout, null);
        }

        public EventSource(Uri url, int timeout, ILogger logger)
        {
            Initialize(url, timeout, logger);
        }

        protected EventSource(Uri url, IWebRequesterFactory factory)
        {
            _webRequesterFactory = factory;
            Initialize(url, 0, null);
        }

        /// <summary>
        /// Constructor for testing purposes
        /// </summary>
        /// <param name="factory">The factory that generates the WebRequester to use.</param>
        protected EventSource(Uri url, IWebRequesterFactory factory, ILogger logger)
        {
            _webRequesterFactory = factory;
            Initialize(url, 0, logger);
        }

        private void Initialize(Uri url, int timeout, ILogger logger)
        {
            _timeout = timeout;
            Url = url;
            CurrentState = new DisconnectedState(Url, _webRequesterFactory);
            _logger = logger;
            _logger?.LogInformation("EventSource created for " + url.ToString());
        }


        /// <summary>
        /// Start the EventSource. 
        /// </summary>
        /// <param name="stopToken">Cancel this token to stop the EventSource.</param>
        public void Start(CancellationToken stopToken)
        {
            if (State == EventSourceState.CLOSED)
            {
                mStopToken = stopToken;
                mTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stopToken);
                Run();
            }
        }

        protected void Run()
        {
            if (mTokenSource.IsCancellationRequested && CurrentState.State == EventSourceState.CLOSED)
                return;

            mCurrentState.Run(this.OnEventReceived, mTokenSource.Token).ContinueWith(cs =>
            {
                CurrentState = cs.Result;
                Run();
            });
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
