using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventSource4Net
{
    public class ServerSentEvent
    {
        public string LastEventId { get; set; }
        public string EventType { get; set; }
        public string Data { get; set; }
        public int? Retry { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (!String.IsNullOrWhiteSpace(EventType))
                sb.Append("EventType: ").Append(EventType);

            if (!String.IsNullOrWhiteSpace(Data))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("Data: ").Append(Data.Remove(Data.Length-1));
            }

            if (!String.IsNullOrWhiteSpace(LastEventId))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("LastEventId: ").Append(LastEventId);
            }

            if (Retry.HasValue)
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append("Retry: ").Append(Retry.Value.ToString());
            }

            return sb.ToString();
        }
    }
}
