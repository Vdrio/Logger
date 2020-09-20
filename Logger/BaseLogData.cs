using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace Vdrio.Diagnostics
{
    public abstract class BaseLogData:ILogData
    {
        public BaseLogData()
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
        }
        public BaseLogData(LogDataType type, params object[] parameters)
        {
            Id = Guid.NewGuid().ToString();
            Time = DateTime.Now;
            LogType = type;

            //Setting parameters protects us from improper implementation of BaseLogData
            if (parameters?.Length <= 0)
            {
                parameters = new object[2] { new object(), new object() };
            }


            try
            {
                switch (type)
                {
                    case LogDataType.Exception:
                        CreateExceptionLogData(parameters[0] as Exception, parameters[1] as string);
                        break;
                    case LogDataType.Trace:
                        Enum.TryParse(parameters[0].ToString(), out TraceType traceType);
                        CreateTraceLogData(traceType, parameters[1] as string);
                        break;
                    case LogDataType.Error:
                        Enum.TryParse(parameters[0].ToString(), out ErrorLevel errorLevel);
                        CreateErrorLogData(errorLevel, parameters[1] as string);
                        break;
                    case LogDataType.Warn:
                        Enum.TryParse(parameters[0].ToString(), out WarningLevel warnLevel);
                        CreateWarnLogData(warnLevel, parameters[1] as string);
                        break;
                    case LogDataType.Debug:
                        CreateDebugLogData(parameters[0] as string);
                        break;
                    case LogDataType.UserInput:
                        Enum.TryParse(parameters[0].ToString(), out UserInputType userInputType);
                        CreateUserInputLogData(userInputType, parameters[1] as string);
                        break;
                    default:
                        Message = parameters[0] as string;
                        ShortLogMessage = parameters[0] as string;
                        LongLogMessage = parameters[0] as string;
                        break;
                }
            }
            catch
            {
                throw new NotImplementedException("A class that inherits BaseLogData should have a constructor as follows: MyLogData(LogDataType type, params object[] parameters):base(type, parameters)");
            }
            
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
        public Exception LogException { get; set; }
        public string JSONException { get; set; }
        public string LongLogMessage { get; set; }
        public string ShortLogMessage { get; set; }
        public DateTime Time { get; set; }
        public string Message { get; set; }
        [PrimaryKey]
        public string Id { get; set; }
        [Ignore]
        public LogDataType LogType { get; set; }
        public ErrorLevel ErrorLevel { get; set; }
        public WarningLevel WarningLevel { get; set; }
        public TraceType TraceType { get; set; }
        public UserInputType UserInputType { get; set; }

        public abstract void CreateDebugLogData(string message);
        public abstract void CreateErrorLogData(ErrorLevel level, string message);
        public abstract void CreateExceptionLogData(Exception ex, string messsage);
        public abstract void CreateTraceLogData(TraceType type, string message);
        public abstract void CreateUserInputLogData(UserInputType type, string message);

        public abstract void CreateWarnLogData(WarningLevel level, string message);
    }
}
