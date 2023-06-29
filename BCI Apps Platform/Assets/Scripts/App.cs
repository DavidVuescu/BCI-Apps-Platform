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


        public App(string name, string ExecutablePath, string IconPath)
        {
            this.name = name;

            exePath = ExecutablePath;
            iconPath = IconPath;
        }


        public void run()
        {
            Debug.Log("APPLICATION DEBUG: Trying to run app on path: " + exePath);
            Process.Start(exePath);
        }
        public void debugPrint()
        {
            Debug.Log("Name: " + name + "   Exe Path: " + exePath + "   Icon Path: " + iconPath);
        }
    }
}
