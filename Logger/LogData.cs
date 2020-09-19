using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Vdrio.Diagnostics
{
    public class LogData : BaseLogData
    {
        public LogData(string message) : base(message)
        {
            Debug.WriteLine(Message);

#if DEBUG
            Console.WriteLine(Message);
#endif
        }

        public LogData(Exception ex, string message) : base(ex, message)
        {
            Debug.WriteLine("Exception Message:" + Message + "\nFull Exception:\n" + ex);
#if DEBUG
            Console.WriteLine("Exception Message:" + Message + "\nFull Exception:\n" + ex);
#endif
        }

        public LogData(TraceType type, string message) : base(type, message)
        {
            Debug.WriteLine(Message);
#if DEBUG
            Console.WriteLine(Message);
#endif
        }
    }
}
