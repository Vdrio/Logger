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
        [Ignore]
        LogDataType LogType { get; set; }
        int LogTypeInt { get; set; }

        void CreateDebugLogData(string message);
        void CreateErrorLogData();
        void CreateExceptionLogData(Exception ex, string messsage);
        void CreateTraceLogData(TraceType type, string message);
        public abstract void CreateUserInputLogData();

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
