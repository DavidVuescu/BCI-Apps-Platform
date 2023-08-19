using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

using System.Text;
using System.Text.RegularExpressions;

using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AppManager
{
    // Short enum for making container movement more readable
    public enum Direction : ushort
    {
        LEFT = 0,
        RIGHT = 1
    }

    public class Manager : MonoBehaviour
    {
        uint appCounter = 0;                // Number of apps present
        int selectedApp = 0;                // The selected app
        string userPath;                    // Path to the user's Documents folder
        List<App> apps = new List<App>();   // List for storing all apps 
        // TODO rename app list to smtg more appropriate
        private Dictionary<string, System.Diagnostics.Process> runningProcesses = new Dictionary<string, System.Diagnostics.Process>();   // Dictionary for storing running app processes

        public GameObject container;        // Container in which the app objects are generated and stored
        public GameObject prefab;           // Prefab used for instantiating an app

        
        // Variables for BCI connectivity
        public bool inputFlag;

        private UdpClient udpClient;
        private IPEndPoint remoteEndPoint;
        private Thread receiveThread;
        private int bufferSize = 32768;

        private string[] navigationCommands = new string[] { "LEFT", "RIGHT", "SELECT", "ALTF4" };

        bool shouldMoveLeft = false;
        bool shouldMoveRight = false;



        /* Function for initializing the user's Documents folder
           The .bciapps folder is used for storing the applications
        */
        public static string getUserPath()
        {
            string path;
            path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\.bciapps\\";
            Debug.Log(path);

            return path;
        }

        /* Function for resizing applications inside the app container
         * The app in the center of the screen gets bigger 
         * The other apps get smaller as they get closer to the screen edges
         */
        void AdjustScaleOfApps()
        {
            float screenCenterX = 0f; // This is the world X coordinate of the screen's center
            float maxScale = 1.0f;  // Center app remains at its original size
            float minScale = 0.7f;  // Apps away from center become smaller
            float maxDistance = 5f;  // The distance over which the scaling effect takes place

            foreach (Transform child in container.transform)
            {
                // Determine the distance from the center of the screen
                float distanceFromCenter = Mathf.Abs(child.position.x - screenCenterX);

                // Calculate a scale factor based on the distance from the center
                float scaleValue = Mathf.Lerp(minScale, maxScale, 1f - (distanceFromCenter / maxDistance));

                // Apply the scale value
                child.localScale = new Vector3(scaleValue, scaleValue, scaleValue);
            }
        }
        /* Function for moving the container object
           The container is moved each time the user selects another app
        */
        public void moveSelection(Direction direction)
        {
            float moveDistance = 2.5f;

            if (direction == Direction.RIGHT)
            {
                container.transform.position = new Vector3(container.transform.position.x - moveDistance, 0, 0);
                selectedApp++; Debug.Log("Current selected app is: " + selectedApp);
            }
            if (direction == Direction.LEFT)
            {
                container.transform.position = new Vector3(container.transform.position.x + moveDistance, 0, 0);
                selectedApp--; Debug.Log("Current selected app is: " + selectedApp);
            }
            AdjustScaleOfApps();
        }


        void runBoardSelected()
        {
            string batFilePath = System.IO.Path.Combine(Application.dataPath, "Boards", "runIntendix.bat");
            string boardPath = apps[selectedApp].boardPath;

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = batFilePath,
                Arguments = $"\"{boardPath}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = startInfo };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string errorOutput = process.StandardError.ReadToEnd();
            int exitCode = process.ExitCode;

            //process.WaitForExit();

            UnityEngine.Debug.Log("Executing BATCH COMMAND: " + batFilePath + " with argument: " + boardPath);
            UnityEngine.Debug.Log("Output: " + output);
            UnityEngine.Debug.Log("Error Output: " + errorOutput);
        }
        void runBoardDefault()
        {
            string batFilePath = System.IO.Path.Combine(Application.dataPath, "Boards", "runIntendix.bat");
            string boardPath = System.IO.Path.Combine(Application.dataPath, "Boards", "PlatformBoard.ibc");

            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = batFilePath,
                Arguments = $"\"{boardPath}\"",
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            System.Diagnostics.Process process = new System.Diagnostics.Process { StartInfo = startInfo };
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string errorOutput = process.StandardError.ReadToEnd();
            int exitCode = process.ExitCode;

            //process.WaitForExit();

            UnityEngine.Debug.Log("Executing BATCH COMMAND: " + batFilePath + " with argument: " + boardPath);
            UnityEngine.Debug.Log("Output: " + output);
            UnityEngine.Debug.Log("Error Output: " + errorOutput);
        }


        public void runSelectedApp()
        {
            System.Diagnostics.Process process = apps[selectedApp].run();
            runningProcesses[apps[selectedApp].name] = process;
            // runBoardSelected();
        }
        public void killSelectedApp()
        {
            if (runningProcesses.ContainsKey(apps[selectedApp].name))
            {
                runningProcesses[apps[selectedApp].name].Kill();
                runningProcesses.Remove(apps[selectedApp].name);
            }
        }


        // Method to relay data to application listeners on port 1000
        private void UDPRelay(byte[] rawData)
        {
            UdpClient relayClient = null; // Declare outside to ensure we can access it in the finally block.

            try
            {
                relayClient = new UdpClient();
                IPEndPoint targetEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1000);

                Debug.Log($"Relaying data: {rawData} to {targetEndPoint.Address}:{targetEndPoint.Port}");
                relayClient.Send(rawData, rawData.Length, targetEndPoint);
            }
            catch (SocketException socketEx)
            {
                // Handle specific socket exceptions here, like issues with binding, addressing, etc.
                Debug.LogError($"Socket Exception in UDPRelay: {socketEx.Message}");
            }
            catch (Exception e)
            {
                // General exception handler to catch any unexpected errors.
                Debug.LogError($"Exception in UDPRelay: {e.Message}");
            }
            finally
            {
                // Ensure resources are properly cleaned up to prevent potential resource leaks.
                relayClient?.Close();
            }
        }


        // UDPListener function for running in the background
        private async void UDPListener()
        {
            while (inputFlag == true)
            {
                try
                {
                    var receivedResult = await udpClient.ReceiveAsync();

                    string message = Encoding.UTF8.GetString(receivedResult.Buffer);
                    Debug.Log($"Received: {message}");  // Debug the received data
                    Debug.Log($"Received data of length: {receivedResult.Buffer.Length} bytes"); // Debug the length of the received data
                    UDPRelay(receivedResult.Buffer); // Relay the raw data

                    foreach (var navCmd in navigationCommands)
                    {
                        if (message.Contains(navCmd))
                        {
                            switch (navCmd)
                            {
                                case "LEFT":
                                    {
                                        Debug.Log($"The given command is: {navCmd}");
                                        if (selectedApp > 0) shouldMoveLeft = true;
                                    }
                                    break;
                                case "RIGHT":
                                    {
                                        Debug.Log($"The given command is: {navCmd}");
                                        if (selectedApp < appCounter - 1) shouldMoveRight = true;
                                    }
                                    break;
                                case "SELECT":
                                    {
                                        Debug.Log($"The given command is: {navCmd}");
                                        runSelectedApp();
                                    }
                                    break;
                                case "ALTF4":
                                    {
                                        Debug.Log($"The given command is: {navCmd}");
                                        killSelectedApp();
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                }
                catch (Exception e)
                {
                    Debug.LogError($"Exception: {e}");
                }

            }
        }


        // Start is called before the first frame update
        void Start()
        {
            userPath = getUserPath();
            string[] folders = Directory.GetDirectories(userPath);

            string app;
            string icon;
            string board;
            foreach (string folder in folders)
            {
                //Debug.Log(folder);

                app = folder + "\\app.exe";
                icon = folder + "\\icon.png";
                board = folder + "\\board.ibc";

                //App newApp = new App(folder, folder + "\\app.exe", folder + "\\icon.png"); 
                App newApp = new App(folder, app, icon, board);
                newApp.debugPrint();

                apps.Add(newApp);
                appCounter++;
            }

            Debug.Log("MANAGER DEBUG: THERE IS A TOTAL OF " + appCounter + " APPS PRESENT");

            float xpos = 0f;
            float spacing = 2.5f;
            foreach (App i in apps)
            {
                var newObj = GameObject.Instantiate(prefab, new Vector3(xpos, 0, 0), Quaternion.identity);


                byte[] fileData;

                if (File.Exists(i.iconPath))
                {
                    fileData = File.ReadAllBytes(i.iconPath);
                    Texture2D texture = new Texture2D(1, 1);
                    texture.LoadImage(fileData); //..this will auto-resize the texture dimensions.

                    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    newObj.GetComponent<SpriteRenderer>().sprite = sprite;
                }

                newObj.transform.parent = container.transform;

                xpos += spacing;
            }

            AdjustScaleOfApps();
            //runBoardDefault();


            inputFlag = true;

            remoteEndPoint = new IPEndPoint(IPAddress.Any, 1001);
            udpClient = new UdpClient(remoteEndPoint);
            udpClient.Client.ReceiveBufferSize = bufferSize;
            udpClient.EnableBroadcast = true;

            // Start the UDPListener thread
            receiveThread = new Thread(new ThreadStart(UDPListener));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        // Update is called once per frame
        void Update()
        {
            ////////////////////
            ///KEYBOARD INPUT
            ///TESTING ONLY
            ////////////////////
            if (Input.GetKeyDown(KeyCode.LeftArrow) && selectedApp > 0)
            {
                moveSelection(Direction.LEFT);
            }

            if (Input.GetKeyDown(KeyCode.RightArrow) && selectedApp < appCounter - 1)
            {
                moveSelection(Direction.RIGHT);
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                runSelectedApp();
            }


            ////////////////////
            ///BCI INPUT
            ///Moves based on flags
            ///I hate this, but unity wouldn't let me have a more delicate solution
            ////////////////////
            if(shouldMoveLeft) { moveSelection(Direction.LEFT); shouldMoveLeft=false; }
            if(shouldMoveRight) { moveSelection(Direction.RIGHT); shouldMoveRight = false; }
        }

    }
}
