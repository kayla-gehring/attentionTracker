using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Runtime.InteropServices;

using Tobii.EyeX.Client;
using Tobii.Interaction;
using Tobii.EyeX.Framework;


using HWND = System.IntPtr;

namespace AttentionTracker
{
    /// <summary>Contains functionality to get all the open windows.</summary>
    /// https://stackoverflow.com/questions/7268302/get-the-titles-of-all-open-windows
    /// Most of the credit for GetOpenWindows() goes to the discussion above, which itself linked:
    /// http://www.tcx.be/blog/2006/list-open-windows/ (linked in stackoverflow)
    /// Note: specified function was modified to better suit this program.
    public static class OpenWindowGetter
    {
        private delegate bool EnumWindowsProc(HWND hWnd, int lParam);

        [DllImport("USER32.DLL")]
        private static extern bool EnumWindows(EnumWindowsProc enumFunc, int lParam);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowText(HWND hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("USER32.DLL")]
        private static extern int GetWindowTextLength(HWND hWnd);

        [DllImport("USER32.DLL")]
        private static extern bool IsWindowVisible(HWND hWnd);

        [DllImport("USER32.DLL")]
        private static extern IntPtr GetShellWindow();

        [DllImport("USER32.DLL")]
        private static extern bool IsIconic(HWND hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll")]
        static extern IntPtr GetTopWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, EntryPoint = "GetWindow", SetLastError = true)]
        public static extern IntPtr GetNextWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.U4)] int wFlag);

        public struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public System.Drawing.Point ptMinPosition;
            public System.Drawing.Point ptMaxPosition;
            public System.Drawing.Rectangle rcNormalPosition;
        }

        const UInt32 SW_HIDE = 0;
        const UInt32 SW_SHOWNORMAL = 1;
        const UInt32 SW_NORMAL = 1;
        const UInt32 SW_SHOWMINIMIZED = 2;
        const UInt32 SW_SHOWMAXIMIZED = 3;
        const UInt32 SW_SHOWNOACTIVATE = 4;
        const UInt32 SW_SHOW = 5;
        const UInt32 SW_MINIMIZE = 6;
        const UInt32 SW_SHOWMINNOACTIVE = 7;
        const UInt32 SW_SHOWNA = 8;
        const UInt32 SW_RESTORE = 9;

        /// <summary>Returns a dictionary that contains the handle and title of all the open windows.
        /// Modified by Kayla Gehring Apr2018 to contain the position information of the windows as well.</summary>
        /// <returns>A dictionary that contains the handle and title of all the open windows.</returns>
        public static IDictionary<HWND, Tuple<string, WINDOWPLACEMENT>> GetOpenWindows()
        {
            HWND shellWindow = GetShellWindow();
            Dictionary<HWND, Tuple<string, WINDOWPLACEMENT>> windows = new Dictionary<HWND, Tuple<string, WINDOWPLACEMENT>>();

            EnumWindows(delegate (HWND hWnd, int lParam)
            {
            //Check that the window is open and of interest - return true without adding to dictionary if not an active, readable window
            if (hWnd == shellWindow) return true;
            if (!IsWindowVisible(hWnd)) return true; //checks that window has a visible component
            if (IsIconic(hWnd)) return true; //checks if the window is minimized

            int length = GetWindowTextLength(hWnd);
                if (length == 0) return true;

            //If it's open, get the description
            StringBuilder builder = new StringBuilder(length);
                GetWindowText(hWnd, builder, length + 1);

            //Also get the location
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
                GetWindowPlacement(hWnd, ref placement);


                windows[hWnd] = new Tuple<string, WINDOWPLACEMENT>(builder.ToString(), placement);
                return true;

            }, 0);

            return windows;

        }

        /// When given multiple windows, this function will return the window *most likely* to be the one of eye gaze/fixation
        public static List<string> getWindowOfFixation(List<Tuple<HWND, Tuple<string, WINDOWPLACEMENT>>> windows)
        {
            //Check how many windows are in fixation overlap area. If there are more than 2, narrow it down to the top 2 windows
            int indexWindow1 = 0;
            int indexWindow2 = 1;

            if (windows.Count() > 2)
            {
                int j = 0;
                //This loop compares 3 indices and keeps the top 2 windows for each set
                for (int i = 1; i < windows.Count(); i++)
                {
                    //top window of pair takes place of index 1
                    if (topWindow(windows[j].Item1, windows[i].Item1)) //if window-j is above window-i -> true
                    {
                        indexWindow1 = j;
                        //see if i is higher than indexWindow2
                        if ((i != indexWindow2) && (topWindow(windows[i].Item1, windows[indexWindow2].Item1)))
                            indexWindow2 = i;
                    }
                    else
                    {
                        indexWindow1 = i;
                        //see if j is higher than indexWindow2
                        if ((j != indexWindow2) && (topWindow(windows[j].Item1, windows[indexWindow2].Item1)))
                            indexWindow2 = j;
                    }
                    j = indexWindow1;
                }
            }
            System.Drawing.Rectangle window1 = windows[indexWindow1].Item2.Item2.rcNormalPosition;
            System.Drawing.Rectangle window2 = windows[indexWindow2].Item2.Item2.rcNormalPosition;
            
            //Check the overlap percentage: (Area of overlap)/(Area of bottom window)
            var bottomArea = (window2.Height * window2.Width);
            window1.Intersect(window2); //replaces window1 with rectangle of size/location of intersection
            var overlapX = window1.Height * window1.Width;
            overlapX = 100 * overlapX / bottomArea;
            //Console.WriteLine("Overlap: {0}", overlapX);

            //If the percentage is big (>50% - see GlobalVals in Program.CS), assume the top window is the one being looked at
            List<string> returnList = new List<string>();
            returnList.Add(windows[indexWindow1].Item2.Item1);
            //if the overlap area is >= 50%, return only window1. Otherwise return window2 as well (unless I get to TO-DO below)
            if (overlapX < GlobalVals.areaPercent)
                returnList.Add(windows[indexWindow2].Item2.Item1);

            //TO-DO: If it's not >50%, use data from recent fixations to determine it. May not have time to implement this.

            return returnList;

        }

        //Given two windows handlers, see which one is "higher"
        //Returns true if window1 is above window2
        public static bool topWindow(HWND window1, HWND window2)
        {
            //Check GetTopWindow and GetNextWindow until one of the two windows is found
            HWND top = GetTopWindow(IntPtr.Zero);

            //Check if one of the windows is the top window
            if (top == window1)
                return true;
            else if (top == window2)
                return false;
            else //if neither is the top window, start looking through GetNextWindow
            {
                while (1 == 1)
                {
                    top = GetNextWindow(top, 2); //GW_HWNDNEXT has value 2, and returns window below top
                    if (top == window1)
                        return true;
                    else if (top == window2)
                        return false;
                    else if (top == IntPtr.Zero) //if top == NULL
                    {
                        Console.WriteLine("Something's gone wrong with OpenWindowGetter.topWindow() - neither window is on top");
                        return false;
                    }
                }
            }
        }
    }
}
