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
            Logger<MyLogData>.Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"VdrioLogger", "MyLogFileStuff") ,true, TimeSpan.FromMinutes(.5), Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "VdrioLoggerArchive"), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(3));
            Logger<MyLogData>.Trace(TraceType.Start, "Started testing logger");
            Exception ex = new Exception();
            ex.Log<MyLogData>();
            ex.Log<MyLogData>("testing");
            ex.Log<MyLogData>("testing{0}", "params");
            StartTesting();
            Console.Read();
        }

        static int count = 1;
        async static void StartTesting()
        {
            Logger<MyLogData>.Trace(TraceType.Start, $"Started testing loop, iteration {count}");
            await Task.Delay(TimeSpan.FromMinutes(.05));
            CauseException();
            Logger<MyLogData>.Trace(TraceType.InProgress, "Testing loop caused exception");
            Logger<MyLogData>.Warn(WarningLevel.Severe, "Severe Warning Test");
            Logger<MyLogData>.Error(ErrorLevel.Critical, "Critical Error Test");
            Logger<MyLogData>.Debug("Just a debug message");
            Logger<MyLogData>.UserInput(UserInputType.Navigation, "User navigation test");
            Exception ex = new Exception();
            ex.Log<MyLogData>(Guid.NewGuid().ToString());
            count++;
            Logger<MyLogData>.Trace(TraceType.Complete, "Finished testing loop");
            StartTesting();
        }

        private static void CauseException()
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
        private static void CauseInnerException()
        {

                string s = null;
                if (s.Contains(' '))
                {

                }

        }
    }
}
