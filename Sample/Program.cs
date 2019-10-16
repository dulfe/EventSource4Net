using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EventSource4Net;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                //builder.SetMinimumLevel(LogLevel.Trace);
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddDebug();
                builder.AddConsole();
            }
            );

            EventSource es = new EventSource(new Uri(@"http://ssetest.apphb.com/api/sse"), 50000, loggerFactory);
            es.StateChanged += new EventHandler<StateChangedEventArgs>((o, e) => { Console.WriteLine("New state: " + e.State.ToString()); });
            es.EventReceived += new EventHandler<ServerSentEventReceivedEventArgs>((o, e) => { Console.WriteLine("--------- Msg received -----------\n" + e.Message.ToString()); });
            es.Start(cts.Token);
            Console.WriteLine("EventSource started");

            ConsoleKey key;
            while ((key = Console.ReadKey(true).Key) != ConsoleKey.X)
            {
                if (key == ConsoleKey.C)
                {
                    cts.Cancel();
                    Console.WriteLine("Eventsource is cancelled.");
                }
                else if (key == ConsoleKey.S)
                {
                    cts = new CancellationTokenSource();
                    es.Start(cts.Token);
                }
            }
        }

    }
}
