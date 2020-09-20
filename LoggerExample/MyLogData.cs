using System;
using System.Collections.Generic;
using System.Text;
using Vdrio.Diagnostics;

namespace LoggerExample
{
    public class MyLogData:LogData
    {
        public override void CreateExceptionLogData(Exception ex, string message)
        {
            base.CreateExceptionLogData(ex, message);
            LongLogMessage += "teeeeeeeeeeeeeeheeeeeeeeeeeeeeeeeeeee\n======================================================================================================";
        }
    }
}
