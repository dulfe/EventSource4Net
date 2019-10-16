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
    class ConnectedState : IConnectionState
    {
        private ILogger _logger;
        private ILoggerFactory _loggerFactory;
        private IWebRequesterFactory mWebRequesterFactory;
        private ServerSentEvent mSse = null;
        private string mRemainingText = string.Empty;   // the text that is not ended with a line ending char is saved for next call.
        private IServerResponse mResponse;
        public EventSourceState State { get { return EventSourceState.OPEN; } }

        public ConnectedState(IServerResponse response, IWebRequesterFactory webRequesterFactory) : this(response, webRequesterFactory, null) { }

        public ConnectedState(IServerResponse response, IWebRequesterFactory webRequesterFactory, ILoggerFactory loggerFactory)
        {
            mResponse = response;
            mWebRequesterFactory = webRequesterFactory;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<ConnectedState>();
        }

        public async Task<IConnectionState> Run(Action<ServerSentEvent> msgReceived, CancellationToken cancelToken)
        {
            var stream = await mResponse.GetResponseStream();

            byte[] buffer = new byte[1024 * 8];

            var taskRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancelToken).ConfigureAwait(false);

            if (!cancelToken.IsCancellationRequested)
            {
                int bytesRead = taskRead;
                if (bytesRead > 0) // stream has not reached the end yet
                {
                    //Console.WriteLine("ReadCallback {0} bytesRead", bytesRead);
                    string text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    text = mRemainingText + text;
                    string[] lines = StringSplitter.SplitIntoLines(text, out mRemainingText);
                    foreach (string line in lines)
                    {
                        if (cancelToken.IsCancellationRequested) break;

                        if (string.IsNullOrWhiteSpace(line) && mSse == null)
                        {
                            // Ignore because it is highly likely we started reading the response before creating a "response group"
                            // not an issue.
                            _logger?.LogTrace("Empty line received before starting a response group.");
                        }
                        else if (string.IsNullOrWhiteSpace(line) && mSse != null)
                        {
                            // Dispatch message if empty lne
                            _logger?.LogTrace("Message received");
                            msgReceived(mSse);
                            mSse = null;
                        }
                        else if (line.StartsWith(":"))
                        {
                            // This a comment, just log it.
                            _logger?.LogTrace("A comment was received: " + line);
                        }
                        else
                        {
                            string fieldName = String.Empty;
                            string fieldValue = String.Empty;
                            if (line.Contains(':'))
                            {
                                int index = line.IndexOf(':');
                                fieldName = line.Substring(0, index);
                                fieldValue = line.Substring(index + 1).TrimStart();
                            }
                            else
                                fieldName = line;

                            if (String.Compare(fieldName, "event", true) == 0)
                            {
                                mSse = mSse ?? new ServerSentEvent();
                                mSse.EventType = fieldValue;
                            }
                            else if (String.Compare(fieldName, "data", true) == 0)
                            {
                                mSse = mSse ?? new ServerSentEvent();
                                mSse.Data = fieldValue + '\n';
                            }
                            else if (String.Compare(fieldName, "id", true) == 0)
                            {
                                mSse = mSse ?? new ServerSentEvent();
                                mSse.LastEventId = fieldValue;
                            }
                            else if (String.Compare(fieldName, "retry", true) == 0)
                            {
                                int parsedRetry;
                                if (int.TryParse(fieldValue, out parsedRetry))
                                {
                                    mSse = mSse ?? new ServerSentEvent();
                                    mSse.Retry = parsedRetry;
                                }
                            }
                            else
                            {
                                // Ignore this, just log it
                                _logger?.LogWarning("A unknown line was received: " + line);
                            }
                        }
                    }

                    if (!cancelToken.IsCancellationRequested)
                        return this;
                }
                else // end of the stream reached
                {
                    _logger?.LogTrace("No bytes read. End of stream.");
                }
            }
            else
            {
                // Closing stream
                stream.Dispose();
                stream.Close();

                _logger?.LogTrace(new TaskCanceledException(), "ConnectedState.Run");
            }

            return new DisconnectedState(mResponse.ResponseUri, mWebRequesterFactory, _loggerFactory);

        }
    }
}
