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
                Message = message;
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public override void CreateErrorLogData()
        {
            throw new NotImplementedException();
        }

        public override void CreateExceptionLogData(Exception ex, string message)
        {
            Time = DateTime.Now;
            Message = message;
            ShortLogMessage = ex.Message;
            LongLogMessage = ex.Message;
            Debug.WriteLine("Exception Message:" + Message + "\nFull Exception:\n" + ex);
#if DEBUG
            Console.WriteLine("Exception Message:" + Message + "\nFull Exception:\n" + ex);
#endif
        }

        public override void CreateTraceLogData(TraceType type, string message)
        {
            Time = DateTime.Now;
            Message = message;
            ShortLogMessage = type.ToString() + ": " + message;
            LongLogMessage = ShortLogMessage;
        }

        public override void CreateUserInputLogData()
        {
            throw new NotImplementedException();
        }
    }
}
