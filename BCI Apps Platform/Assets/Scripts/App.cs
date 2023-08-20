using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;

using Debug = UnityEngine.Debug;

namespace AppManager
{
    public class App
    {
        public string name;

        public string exePath;
        public string iconPath;
        public string boardPath;


        public App(string name, string ExecutablePath, string IconPath, string BoardPath)
        {
            this.name = name;

            exePath = ExecutablePath;
            iconPath = IconPath;
            boardPath = BoardPath;
        }


        public Process run()
        {
            Debug.Log("APPLICATION DEBUG: Trying to run app on path: " + exePath);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = "-screen-fullscreen 0 -screen-height 900 -screen-width 1600 -monitor 3"
            };

            return Process.Start(startInfo);
        }
        public void debugPrint()
        {
            Debug.Log("Name: " + name + "   Exe Path: " + exePath + "   Icon Path: " + iconPath + "   Board Path: " + boardPath);
        }
    }
}
