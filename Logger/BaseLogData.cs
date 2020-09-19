using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Vdrio.Diagnostics
{
    public abstract class BaseLogData
    {
        public BaseLogData(string message)
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
            Message = message;
            ShortLogMessage = message;
            LongLogMessage = message;
        }

        public BaseLogData(Exception ex, string message)
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
            Message = message;
            ShortLogMessage = ex.Message;
            LongLogMessage = ex.Message;
        }

        public BaseLogData(TraceType type, string message)
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
            Message = message;
            ShortLogMessage = type.ToString() + ": " + message;
            LongLogMessage = ShortLogMessage;
        }

        [Ignore]
        public Exception Exception { get; set; }
        public string JSONException { get; set; }
        public string LongLogMessage { get; set; }
        public string ShortLogMessage { get; set; }
        public DateTime Time { get; set; }
        public string Message { get; set; }
        [PrimaryKey]
        public string Id { get; set; }
    }
}
