using System;
using System.Threading;
using System.Diagnostics;
using PSMonitor;
using System.Management.Instrumentation;
using PSMonitor.Powershell;

namespace PSMCmd
{
    class Program : TraceListener
    {
        private static Powershell ps = new Powershell();

        static void Main(string[] args)
        {
            
            Debug.Listeners.Add(new Program());
            
            ps.Reload();
            
            Console.WriteLine("Press a key to exit...");
            
            Console.ReadKey();

            ps.Dispose();

            Console.WriteLine("[Done]");
            Console.ReadKey();
            
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
