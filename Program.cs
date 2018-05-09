using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

using System.Runtime.InteropServices;




using HWND = System.IntPtr;

namespace AttentionTracker
{
    //Some "global variables" that are likely to be changed with experimentation and/or user preference
    public static class GlobalVals
    {
        //percent one window must cover another before being considered the active window
        //changing this may help if the program says you are fixating on window A, but actually are fixating
        //  on window B which is behind window A   (see _Tobii.getTopWindow())
        public static readonly int areaPercent = 33; //percent
    }

    //General Functions to actually classify attention
    class AttentionTracking
    {
        public static void Main(string[] args)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            
             //Start the EyeTracker
            _Tobii eyeTracker = new _Tobii(); //start stream
            var fp = _Tobii.PrintFixations();

            Console.WriteLine("Eyegaze Data is Being Recorded.");
            Console.Write("Press <Enter> to exit...");

            //check that the user has pressed enter or is no longer present - exit if either is true
            while ((Console.ReadKey().Key != ConsoleKey.Enter) && (_Tobii.UserPresence() == true)) { }

            //close connection when while loop above exits
            _Tobii.closeConnection();
            stopWatch.Stop();

            List<dataParsing.windowData> summary = dataParsing.ParseData(fp);
            Console.WriteLine("Total time elapsed: {0}", stopWatch.Elapsed);
            ClassifyTypes(summary);

            Console.ReadKey(); //lets you see what the console is doing
        }


        //classifies each attention type and removes data that isn't interesting
        public static void ClassifyTypes(List<dataParsing.windowData> data)
        {
            double totalTime = 0;

            foreach (dataParsing.windowData window in data)
            {
                //basic filtering
                totalTime += window.totalTime;
                var interruptions = window.interruptions;
                if (interruptions > 0) interruptions--;
                //interruptions minus one because it's not really an interruption if you didn't look back
              //  if (WasFocus(window))
                //{
                    
                Console.WriteLine("Spent {0} tobii timestamps on window '{1}'. There were {2} interruptions while viewing this window.", window.totalTime, window.name, interruptions);

                   /* for (int j = 0; j < window.interruptor.Count - 1; j++)
                    {

                    Console.WriteLine("Interrupted by \t {0} for {1} tobii timestamps", window.interruptor[j].Item1, window.interruptor[j].Item2);
                        /*var interruptDuration = window.startTimeStamps[j + 1] - window.interruptor[j].Item2;
                        if ((interruptions == 1))
                        {
                            if (WasDistraction(window.interruptor[j], window)) Console.WriteLine("Distracted by \t {0} for {1} tobii timestamps", window.interruptor[j].Item1, interruptDuration);
                            else Console.WriteLine("Irrelevant Data Caught");
                        }
                        else
                        {
                            if (!WasDistraction(window.interruptor[j], window))
                            Console.WriteLine("You may have been multitasking on window {0}", window.interruptor[j].Item1);
                        }*/
                    }*/
                    Console.WriteLine("\n");
                //}
            }
            Console.WriteLine("There were a total of {0} tobii timestamps elapsed (need to convert this to actual seconds above)", totalTime);
        }

        //checks if an interruption was a distraction
        public static bool WasDistraction(Tuple<string, double> t, dataParsing.windowData window)
        {
            if ((t.Item2 > 0.08) && (t.Item2 < 0.2*window.totalTime))  return true;
            else return false;
        }
        //checks if multiple interruptions were from same source - tries to identify multi-tasking or multi-modal tasking
        public static bool WasMultitasking()
            {
                return true; //TO-DO: tried to rewrite this and ran out of time
            }
         //checks for long periods of very few interruptions - identifies focused behavior
         public static bool WasFocus(dataParsing.windowData window)
         {
                double ttime = 0;
                foreach (var t in window.interruptor)
                {
                    ttime += t.Item2;
                }
                if (ttime < .75 * window.totalTime) return true;
                else return false;
         }



        //prints summary of fixation data after filtering data to interesting observations 
            public static void PrintFixationData(List<dataParsing.windowData> data)
        {
            /* Console.WriteLine("Spent {0} min, {1} sec on window '{2}'. There were {3} interruptions while viewing this window:", window.totalTime, tsec, window.name, window.interruptions);
                for (int j = 0; j < window.interruptor.Count; j++)
                {
                    Console.WriteLine("\t {0} at {1}", window.interruptor[j].Item1, window.interruptor[j].Item2);
                }
                */
        }
    }
}
