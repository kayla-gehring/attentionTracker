using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;

using Tobii.EyeX.Client;
using Tobii.Interaction;
using Tobii.EyeX.Framework;

using HWND = System.IntPtr;

namespace AttentionTracker
{
    /// <summary>
    ///  This class holds the majority of the programs which interact with the eyetracker
    /// </summary>
    public class _Tobii
    {
        private static Host _host;
        private static FixationDataStream _fixationDataStream;
        private static GazePointDataStream _gazePointDataStream;

        //starts a fixation data stream (class constructor)
        public _Tobii()
        {
            //create connection to Tobii Eyetracker
            _host = new Host();
            _fixationDataStream = _host.Streams.CreateFixationDataStream();
            _gazePointDataStream = _host.Streams.CreateGazePointDataStream();
            
        }

        //Prints fixation data stream - incorporates the "OpenWindowGetter" class to better describe the fixations 
        //https://github.com/Tobii/CoreSDK/blob/master/samples/Streams/Interaction_Streams_102/Program.cs
        //Used the above as a reference (Example code from Tobii SDK) to write this
        public static string PrintFixations()
        {
            //Variable Declarations
            DateTime fntime = DateTime.Now; //start time
            var tstart = 0d;
            System.Drawing.Point gazePoint = new System.Drawing.Point();
            var csv = new StringBuilder(10000000);
            string data = "";
            var fn = "fixation_Data_" + String.Format("{0:ddMMyyhhmmss}", fntime) + ".csv";
            string path = Application.StartupPath + @"..\..\..\Files\" + fn;
            var t0 = fntime;

            //Analyze data as it's received from the tracker
            _fixationDataStream.Next += (_, fixation) =>
            {
                //BUG: I have some overflow issues but I'm pretty sure the overflow values are coming from not having data and its not worth figuring it out
                gazePoint.X = unchecked((int)fixation.Data.X);
                gazePoint.Y = unchecked((int)fixation.Data.Y);
                
                //Based on the fixation data, do one of the following:
                switch (fixation.Data.EventType)
                {
                    //beginning of a fixation: Record the time and fixation location
                    case Tobii.Interaction.Framework.FixationDataEventType.Begin:
                        t0 = DateTime.Now.ToUniversalTime();
                        tstart = fixation.Data.Timestamp;
                        break;

                    //during a fixation: Not sure - may have to redefine fixation
                    case Tobii.Interaction.Framework.FixationDataEventType.Data:
                        break;

                    //when the fixation ends: Record the window that the fixation was focused on and duration
                    case Tobii.Interaction.Framework.FixationDataEventType.End:
                        var tf = fixation.Data.Timestamp;
                        //Console.WriteLine("Fixation lasted from {0} to {1} (seconds?) on Window: ", t0, tf);
                        List<string> windows = whichWindowWasInteresting(gazePoint); //PROBABLY CAN DELETE THIS
                        windows.ToArray();
                        //Store as csv: Start Timestamp (tobii), start time, end time, window(s) of interest
                        data = t0.ToString() + ';' + tstart.ToString() + ';' + tf.ToString();
                        if (windows.Count() >= 1)
                            data = data + ';' + windows[0];
                        else
                            data = data + ';' + " ";
                        if (windows.Count() >= 2)
                            data = data + ';' + windows[1];
                        else
                            data = data + ';' + " ";
                        data = data + System.Environment.NewLine;
                        
                        if (!File.Exists(path))
                            File.WriteAllText(path, data);
                        else
                            File.AppendAllText(path, data);

                        //TO-DO: refresh screen location data? unclear whether it is live?
                        break;

                    //Catch-all
                    default:
                        //Console.Write("no fixation data available at {0}", fixation.Data.Timestamp);
                        break;
                }
               
            };
            
            return path; //make it easy to retrieve file that was written to

        }

        //this function picks up on whether user is present, says nothing about eyegaze data
        public static bool UserPresence()
        {
            var state = _host.States.GetUserPresenceAsync().Result;

            if (state.IsValid)
            {
                switch (state.Value)
                {
                    case Tobii.Interaction.Framework.UserPresence.Present:
                        //Console.WriteLine("Present");
                        return true;

                    case Tobii.Interaction.Framework.UserPresence.NotPresent:
                        //Console.WriteLine("You disappeared");
                        return false;

                    default:
                        //Console.WriteLine("Tobii cannot tell whether you are present");
                        return false;

                }
            }
            Console.WriteLine("Having problems with _Tobii.UserPresence() function");
            return false; //if not valid, don't do anything
        }

        //Finds what window the gaze/fixation was on
        public static List<string> whichWindowWasInteresting(System.Drawing.Point gazePoint)
        {
            List<string> windows = new List<string>();
            var windowDict = new List<Tuple<HWND, Tuple<string, OpenWindowGetter.WINDOWPLACEMENT>>>();

            //Adapted from same reference in the class "OpenWindowGetter" 
            //Compare gaze data to open window locations
            foreach (KeyValuePair<HWND, Tuple<string, OpenWindowGetter.WINDOWPLACEMENT>> window in OpenWindowGetter.GetOpenWindows())
            {
                //extract values
                IntPtr handle = window.Key;
                string title = window.Value.Item1;
                System.Drawing.Rectangle location = window.Value.Item2.rcNormalPosition;

                //see if gaze location overlaps with an open window 
                bool sameLocation = location.Contains(gazePoint);

                //Add to return list if the gaze/fixation was in the same location as the active window
                if (sameLocation)
                {
                    windows.Add(title);
                    windowDict.Add(new Tuple<HWND, Tuple<string, OpenWindowGetter.WINDOWPLACEMENT>>(handle, window.Value));
                }
            }

            //if there's more than one window, try to narrow down which window is actually visible to the user
            if (windows.Count > 1)
            {
                return OpenWindowGetter.getWindowOfFixation(windowDict);
            }
            return windows;
        }

        //Ends connection to eyetracker and dispose
        public static void closeConnection()
        {
            _host.DisableConnection();
        }
    }
}
