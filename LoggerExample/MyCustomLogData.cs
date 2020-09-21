using System;
using System.Collections.Generic;
using System.Text;
using Vdrio.Diagnostics;

namespace LoggerExample
{
    public class MyCustomLogData : BaseLogData
    {
        public MyCustomLogData():base()
        {

        }
        public override void CreateDebugLogData(string message)
        {
            LogType = LogDataType.Debug;
            LongLogMessage = message + "\n===============================================================================\n";
        }

        public override void CreateErrorLogData(ErrorLevel level, string message)
        {
            LogType = LogDataType.Error;

            LongLogMessage = message + "\n===============================================================================\n";
        }

        public override void CreateExceptionLogData(Exception ex, string message)
        {
            LogType = LogDataType.Exception;

            LongLogMessage = message + "\n===============================================================================\n";
        }

        public override void CreateTraceLogData(TraceType type, string message)
        {
            LogType = LogDataType.Trace;

            LongLogMessage = message + "\n===============================================================================\n";
        }

        public override void CreateUserInputLogData(UserInputType type, string message)
        {
            LogType = LogDataType.UserInput;

            LongLogMessage = message + "\n===============================================================================\n";
        }

        public override void CreateWarnLogData(WarningLevel level, string message)
        {
            LogType = LogDataType.Warn;

            LongLogMessage = message + "\n===============================================================================\n";
        }
    }
}
