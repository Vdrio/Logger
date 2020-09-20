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
        Exception LogException { get; set; }
        DateTime Time { get; set; }
        string LongLogMessage { get; set; }
        string ShortLogMessage { get; set; }
        string Message { get; set; }
        LogDataType LogType { get; set; }
        ErrorLevel ErrorLevel { get; set; }
        WarningLevel WarningLevel { get; set; }
        TraceType TraceType { get; set; }
        UserInputType UserInputType { get; set; }

        void CreateDebugLogData(string message);
        void CreateErrorLogData(ErrorLevel level, string message);
        void CreateWarnLogData(WarningLevel level, string message);
        void CreateExceptionLogData(Exception ex, string messsage);
        void CreateTraceLogData(TraceType type, string message);
        public abstract void CreateUserInputLogData(UserInputType type, string message);



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

    public delegate void LoggerEventArgs(BaseLogData logData);

    public delegate void LogFileCompletedEventArgs(string oldLogFilePath);
    public delegate void LogFileArchivedEventArgs(string archiveLogFilePath);

}
