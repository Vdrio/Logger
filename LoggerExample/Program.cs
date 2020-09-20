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
            Logger.Initialize(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"VdrioLogger", "MyLogFile") ,true, TimeSpan.FromMinutes(10), TimeSpan.FromHours(3));
            Logger.Trace(TraceType.Start, "Started testing logger");
            Exception ex = new Exception();
            ex.Log();
            ex.Log("testing");
            ex.Log("testing{0}", "params");
            StartTesting();
            Console.Read();
        }

        static int count = 1;
        async static void StartTesting()
        {
            Logger.Trace(TraceType.Start, $"Started testing loop, iteration {count}");
            await Task.Delay(TimeSpan.FromMinutes(.1));
            CauseException();
            Logger.Trace(TraceType.InProgress, "Testing loop caused exception");
            Logger.Warn(WarningLevel.Severe, "Severe Warning Test");
            Logger.Error(ErrorLevel.Critical, "Critical Error Test");
            Logger.Debug("Just a debug message");
            Logger.UserInput(UserInputType.Navigation, "User navigation test");
            Exception ex = new Exception();
            ex.Log(Guid.NewGuid().ToString());
            count++;
            Logger.Trace(TraceType.Complete, "Finished testing loop");
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
