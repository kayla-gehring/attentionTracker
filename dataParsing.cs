using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AttentionTracker
{
    //This class does all of the primary data parsing
    class dataParsing
    {
        //struct for each data line
        public struct attentionData
        {
            public double t0, tf, totalTime;
            public string time, focus1, focus2;

            public attentionData(string stime, string st0, string stf, string f1, string f2)
            {
                t0 = Convert.ToDouble(st0);
                tf = Convert.ToDouble(stf);
                totalTime = tf - t0;
                time = stime;
                if (f1 == " ")
                    focus1 = "OS background or notification, or off-screen data";
                else
                    focus1 = f1;
                if (f2 == " ")
                    focus2 = String.Empty;
                else
                    focus2 = f2;
            }
        }

        //stores interruptions, time spent on window, etc
        public struct windowData
        {
            public int interruptions; //number of times user looks away from screen
            public double totalTime; //milliseconds
            public List<double> startTimeStamps; //the starting time of each fixation grouping
            public List<Tuple<string, double>> interruptor; //store the window that interrupted focus on a screen, and time of interruption
            public double lastTimeViewed; //just a tracker for interruptions
            public string name; //name of the window

            public windowData(string n, double tTime, double startTime, double lastTime)
            {
                name = n;
                interruptions = 0;
                totalTime = tTime;
                lastTimeViewed = lastTime;
                startTimeStamps = new List<double>();
                startTimeStamps.Add(startTime);
                interruptor = new List<Tuple<string, double>>();
            }
        }


        //puts data collected from Tobii into structs windowData and attentionData
        public static List<windowData> ParseData(string filepath)
        {
            //get attentionData
            attentionData[] allData = FileToAttentionData(filepath);

            //For each window viewed, create a windowData object
            List<windowData> parsedData = new List<windowData>();
            int lastWindowIndex = -1; //index tracker
            for (int i = 0; i < allData.Length; i++)
            {
                var datum = allData[i];
                var lastWindow = new windowData(null, 0, 0, 0);
                if (lastWindowIndex != -1)
                {
                    lastWindow = parsedData[lastWindowIndex];
                }

                //check if the viewed window already has a windowData object
                var windowIndex = parsedData.FindIndex(obj => obj.name == datum.focus1); //TO-DO: maybe rename foundObject to myObject or something
                windowData window;
                if (windowIndex != -1) //if the object already exists, add relevant data to the foundObject and lastWindow objects
                {
                    //check if there has been an interruption by viewing this window
                    // if lastwindow has no value or the same value as foundObject.name, there has been no interruption
                    window = parsedData[windowIndex];
                    if ((lastWindowIndex != -1) && (lastWindow.name != window.name))
                    {
                        //if this window interrupted another, update the lastWindow object
                        lastWindow.interruptions++; //add to number of interruptions
                        lastWindow.interruptor.Add(Tuple.Create(window.name, lastWindow.lastTimeViewed));
                    }

                    //update total time, list of start times, and lastTimeViewed, and "add" window to parsedData since pointers are weird in C
                    window.totalTime = window.totalTime + datum.totalTime; //add to the total time
                    window.lastTimeViewed = datum.tf;
                    window.startTimeStamps.Add(datum.t0);
                    parsedData[windowIndex] = window;
                    parsedData[lastWindowIndex] = lastWindow;
                }
                else //create a new windowData object and add it to the list
                {
                    window = new windowData(datum.focus1, datum.totalTime, datum.t0, datum.tf);
                    parsedData.Add(window);
                }

                //update lastWindowIndex 
                lastWindowIndex = parsedData.FindIndex(obj => obj.name == datum.focus1); //get index of foundObject
            }
            return parsedData;
        }

        //reads a file storing attention data and puts it in the attentionData struct
        public static attentionData[] FileToAttentionData(string filepath)
        {
            //open file and read data
            //CSV contains: Start Time, start timestamp, end timestamp, 0-2 windows of interest
            var file = File.ReadLines(filepath);
            var size = file.Count();
            List<attentionData> allData = new List<attentionData>();

            //Go through each line in the file, each column separated by ';', and put it in the attentionData struct
            foreach (string line in file)
            {
                string[] split = line.Split(';');
                //having problems with the first time stamp being 0 if I'm not already looking at the screen
                if (split[1] != "0")
                {
                    allData.Add(new attentionData(split[0], split[1], split[2], split[3], split[4]));

                }
            }
            return allData.ToArray();
        }

    }
}
