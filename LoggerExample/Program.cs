using System;
using System.IO;
using System.Threading.Tasks;
using Vdrio.Diagnostics;

namespace LoggerExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            MyCustomLogDataImplementation();
            //DefaultLogDataImplementation();
            //MyLogDataImplementation();

            Console.Read();
        }

        static void DefaultLogDataImplementation()
        {
            Logger.Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VdrioLogger", "MyCustomLogFileStuff"), true, TimeSpan.FromMinutes(1), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VdrioLoggerArchive"), TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(10));
            Logger.Trace(TraceType.Start, "Started testing logger");
            Exception ex = new Exception();
            ex.Log();
            ex.Log("testing");
            ex.Log("testing{0}", "params");
            StartDefaultLogDataTesting();
            Logger.LogFileArchived += Logger_LogFileArchived;
            Logger.LogFileCompleted += Logger_LogFileCompleted;
        }

        static void MyLogDataImplementation()
        {
            Logger<MyLogData>.Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VdrioLogger", "MyCustomLogFileStuff"), true, TimeSpan.FromMinutes(1), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VdrioLoggerArchive"), TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(10));
            Logger<MyLogData>.Trace(TraceType.Start, "Started testing logger");
            Exception ex = new Exception();
            ex.Log<MyLogData>();
            ex.Log<MyLogData>("testing");
            ex.Log<MyLogData>("testing{0}", "params");
            StartMyLogDataTesting();
            Logger<MyLogData>.LogFileArchived += Logger_LogFileArchived;
            Logger<MyLogData>.LogFileCompleted += Logger_LogFileCompleted;
        }

        static void MyCustomLogDataImplementation()
        {
            Logger<MyCustomLogData>.Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VdrioLogger", "MyCustomLogFileStuff"), true, TimeSpan.FromMinutes(1), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VdrioLoggerArchive"), TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(10));
            Logger<MyCustomLogData>.Trace(TraceType.Start, "Started testing logger");
            Exception ex = new Exception();
            ex.Log<MyCustomLogData>();
            ex.Log<MyCustomLogData>("testing");
            ex.Log<MyCustomLogData>("testing{0}", "params");
            Logger<MyCustomLogData>.LogFileArchived += Logger_LogFileArchived;
            Logger<MyCustomLogData>.LogFileCompleted += Logger_LogFileCompleted;
            StartCustomLogDataTesting();
        }

        private static void Logger_LogFileCompleted(string oldLogFilePath)
        {
            Console.WriteLine("=======================================LogFileCompleted\n" + oldLogFilePath + "\n========================================================");
        }

        private static void Logger_LogFileArchived(string archiveLogFilePath)
        {
            Console.WriteLine("=======================================LogFileArchived\n" + archiveLogFilePath + "\n========================================================");
        }

        static int count = 1;
        async static void StartCustomLogDataTesting()
        {
            Logger<MyCustomLogData>.Trace(TraceType.Start, $"Started testing loop, iteration {count}");
            await Task.Delay(TimeSpan.FromMinutes(.05));
            CauseMyCustomLogDataException();
            Logger<MyCustomLogData>.Trace(TraceType.InProgress, "Testing loop caused exception");
            Logger<MyCustomLogData>.Warn(WarningLevel.Severe, "Severe Warning Test");
            Logger<MyCustomLogData>.Error(ErrorLevel.Critical, "Critical Error Test");
            Logger<MyCustomLogData>.Debug("Just a debug message");
            Logger<MyCustomLogData>.UserInput(UserInputType.Navigation, "User navigation test");
            Exception ex = new Exception();
            ex.Log<MyCustomLogData>(Guid.NewGuid().ToString());
            count++;
            Logger<MyCustomLogData>.Trace(TraceType.Complete, "Finished testing loop");
            StartCustomLogDataTesting();
        }
        async static void StartDefaultLogDataTesting()
        {
            Logger.Trace(TraceType.Start, $"Started testing loop, iteration {count}");
            await Task.Delay(TimeSpan.FromMinutes(.05));
            CauseDefaultLogDataException();
            Logger.Trace(TraceType.InProgress, "Testing loop caused exception");
            Logger.Warn(WarningLevel.Severe, "Severe Warning Test");
            Logger.Error(ErrorLevel.Critical, "Critical Error Test");
            Logger.Debug("Just a debug message");
            Logger.UserInput(UserInputType.Navigation, "User navigation test");
            Exception ex = new Exception();
            ex.Log(Guid.NewGuid().ToString());
            count++;
            Logger.Trace(TraceType.Complete, "Finished testing loop");
            StartCustomLogDataTesting();
        }
        async static void StartMyLogDataTesting()
        {
            Logger<MyLogData>.Trace(TraceType.Start, $"Started testing loop, iteration {count}");
            await Task.Delay(TimeSpan.FromMinutes(.05));
            CauseMyLogDataException();
            Logger<MyLogData>.Trace(TraceType.InProgress, "Testing loop caused exception");
            Logger<MyLogData>.Warn(WarningLevel.Severe, "Severe Warning Test");
            Logger<MyLogData>.Error(ErrorLevel.Critical, "Critical Error Test");
            Logger<MyLogData>.Debug("Just a debug message");
            Logger<MyLogData>.UserInput(UserInputType.Navigation, "User navigation test");
            Exception ex = new Exception();
            ex.Log<MyLogData>(Guid.NewGuid().ToString());
            count++;
            Logger<MyLogData>.Trace(TraceType.Complete, "Finished testing loop");
            StartMyLogDataTesting();
        }

        private static void CauseMyLogDataException()
        {
            try
            {
                CauseInnerException();
                string s = null;
                if (s.Contains(' '))
                {

                }
            }
            catch (Exception x)
            {
                x.Log<MyLogData>();
            }
        }
        private static void CauseMyCustomLogDataException()
        {
            try
            {
                CauseInnerException();
                string s = null;
                if (s.Contains(' '))
                {

                }
            }
            catch (Exception x)
            {
                x.Log<MyCustomLogData>();
            }
        }
        private static void CauseDefaultLogDataException()
        {
            try
            {
                CauseInnerException();
                string s = null;
                if (s.Contains(' '))
                {

                }
            }
            catch (Exception x)
            {
                x.Log();
            }
        }
        private static void CauseInnerException()
        {

                string s = null;
                if (s.Contains(' '))
                {

                }

        }
    }
}
