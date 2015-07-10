using System;
using System.Threading;
using System.Diagnostics;
using PSMonitor;
using System.Management.Instrumentation;
namespace PSMCmd
{
    class Program : TraceListener
    {
        private static PSM master = new PSM();

        static void Main(string[] args)
        {
            
            Debug.Listeners.Add(new Program());
            Thread print = new Thread(Print);
            
            master.Reload();
            
            Console.WriteLine("Press Enter key to exit...");

            print.Start();

            Console.ReadLine();

            print.Abort();
            master.Dispose();

            Console.WriteLine("[Done]");
            Console.ReadKey();
            
        }
        
        static void Print()
        {
            
           foreach (Envelope data in master)
            {
               Console.WriteLine(data);
            }

        }

        public override void Write(string message)
        {
            Console.WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            Console.WriteLine(message);
        }
    }
}
