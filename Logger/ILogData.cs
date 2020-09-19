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
        Debug,
        Warn,
        Error,
        UserInput
    }

    public enum WarningLevel
    {
        Info,
        Moderate,
        Severe
    }

    public enum ErrorLevel
    {
        Low,
        Moderate,
        Severe,
        Critical,
        Fatal
    }
    public enum TraceType
    {
        Start,
        InProgress,
        Complete
    }

    public enum UserInputType
    {
        Click,
        RightClick,
        Navigation,
        Swipe,
        LongPress,
        DoubleClick,
        MultiClick,
        DragAndDrop,
        KeyPress,
        Shortcut,
        Other
    }
}
