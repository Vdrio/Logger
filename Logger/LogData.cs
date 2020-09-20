using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Vdrio.Diagnostics
{
    public class LogData : BaseLogData
    {
        public LogData()
        {

        }
        public LogData(LogDataType type, params object[] parameters) : base(type, parameters)
        {
            Debug.WriteLine(Message);

#if DEBUG
            Console.WriteLine(Message);
#endif
        }
        public override void CreateDebugLogData(string message)
        {
            try
            {
                Id = Guid.NewGuid().ToString();
                Time = DateTime.Now;
                LogType = LogDataType.Debug;
                Message = message;
                FormatShortString();
                FormatLongString();
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public override void CreateErrorLogData(ErrorLevel level, string message)
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
            LogType = LogDataType.Error;
            ErrorLevel = level;
            Message = message;
            FormatShortString();
            FormatLongString();
        }

        public override void CreateExceptionLogData(Exception ex, string message)
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
            LogType = LogDataType.Exception;
            Message = message;
            LogException = ex;
            FormatShortString();
            FormatLongString();

            Debug.WriteLine("Exception Message:" + Message + "\nFull Exception:\n" + ex);
#if DEBUG
            Console.WriteLine("Exception Message:" + Message + "\nFull Exception:\n" + ex);
#endif
        }

        public override void CreateTraceLogData(TraceType type, string message)
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
            LogType = LogDataType.Trace;
            TraceType = type;
            Message = message;
            FormatShortString();
            FormatLongString();
        }

        public override void CreateUserInputLogData(UserInputType type, string message)
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
            LogType = LogDataType.UserInput;
            UserInputType = type;
            Message = message;
            FormatShortString();
            FormatLongString();
        }


        public override void CreateWarnLogData(WarningLevel level, string message)
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
            LogType = LogDataType.Warn;
            WarningLevel = level;
            Message = message;
            FormatShortString();
            FormatLongString();
        }

        public void FormatShortString()
        {
            if (LogType == LogDataType.Exception)
            {
                ShortLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][Exception]:\n" + Message + "\n" + LogException?.Message + "\n" + LogException?.StackTrace + "\n";
            }
            else if (LogType == LogDataType.Trace)
            {
                ShortLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][Trace][" + TraceType + "]:\n" + Message + "\n";
            }
            else if (LogType == LogDataType.Error)
            {
                ShortLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][Error][" + ErrorLevel + "]:\n" + Message + "\n";
            }
            else if (LogType == LogDataType.Warn)
            {
                ShortLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][Error][" + WarningLevel + "]:\n" + Message + "\n";
            }
            else if (LogType == LogDataType.Debug)
            {
                ShortLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][Debug]:\n" + Message + "\n";
            }
            else if (LogType == LogDataType.UserInput)
            {
                ShortLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][UserInput][" + UserInputType + "]:\n" + Message + "\n";
            }
        }
        public void FormatLongString()
        {
            if (LogType == LogDataType.Exception)
            {
                LongLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][Exception]:\n" + Message + "\n" + LogException?.Message + "\n" + LogException?.StackTrace;
                Exception x = LogException?.InnerException;
                while (x != null)
                {
                    LongLogMessage += x.Message + "\n" + x.StackTrace;
                }
                LongLogMessage += "\n=============================================================================================================================================";
            }
            else if(LogType == LogDataType.Trace)
            {
                LongLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][Trace][" + TraceType +"]:\n"+ Message;
                LongLogMessage += "\n=============================================================================================================================================";
            }
            else if(LogType == LogDataType.Error)
            {
                LongLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][Error][" + ErrorLevel +"]:\n"+ Message;
                LongLogMessage += "\n=============================================================================================================================================";
            }
            else if(LogType == LogDataType.Warn)
            {
                LongLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][Error][" + WarningLevel +"]:\n"+ Message;
                LongLogMessage += "\n=============================================================================================================================================";
            }
            else if(LogType == LogDataType.Debug)
            {
                LongLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][Debug]:\n"+ Message;
                LongLogMessage += "\n=============================================================================================================================================";
            }
            else if(LogType == LogDataType.UserInput)
            {
                LongLogMessage = "[" + Time.ToString("MM/dd/yyyy HH:mm:ss") + "][UserInput][" + UserInputType + "]:\n" + Message;
                LongLogMessage += "\n=============================================================================================================================================";
            }
        }


    }
}
