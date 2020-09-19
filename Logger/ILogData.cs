using System;
using System.Collections.Generic;
using System.Text;
using SQLite;

namespace Vdrio.Diagnostics
{

    public interface ILogData
    {
        [PrimaryKey]
        string Id { get; set; }
        [Ignore]
        Exception Exception { get; set; }
        DateTime Time { get; set; }
        string ExcpetionJSON { get; set; }
        string LongLogMessage { get; set; }
        string ShortLogMessage { get; set; }
        string Message { get; set; }
    }


    public enum LogDataType
    {
        Exception,
        Trace,
    }
    public enum TraceType
    {
        Start,
        InProgress,
        Complete
    }
}
