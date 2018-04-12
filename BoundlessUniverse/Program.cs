using System;
using System.Threading;

namespace BoundlessUniverse
{
    /// <summary>
    /// Main entry object
    /// </summary>
    static class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static void Main(string[] args)
        {
            // Run the engine
            new Engine().Run();

            // Wait forever if debugging so we can see the output
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.WriteLine("\r\nProgram Done!");

                using (ManualResetEvent waitForever = new ManualResetEvent(false))
                {
                    waitForever.WaitOne();
                }
            }
        }
    }
}
