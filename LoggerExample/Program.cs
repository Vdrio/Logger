using System;
using Vdrio.Diagnostics;

namespace LoggerExample
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Logger.Trace(TraceType.Start, "Started testing logger");
            Exception ex = new Exception();
            ex.Log();
            ex.Log("testing");
            ex.Log("testing{0}", "params");

            Console.Read();
        }
    }
}
