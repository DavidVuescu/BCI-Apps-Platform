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

        /* Function for moving the container object
           The container is moved each time the user selects another app
        */
        public void moveSelection(Direction direction)
        {
            if (direction == Direction.RIGHT)
            {
                container.transform.position = new Vector3(container.transform.position.x - 3, 0, 0);
                selectedApp++; Debug.Log("Current selected app is: " + selectedApp);
            }
            if (direction == Direction.LEFT)
            {
                container.transform.position = new Vector3(container.transform.position.x + 3, 0, 0);
                selectedApp--; Debug.Log("Current selected app is: " + selectedApp);
            }
        }
        public void runSelectedApp()
        {
            apps[selectedApp].run();
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
                    Debug.Log($"Received: {message}");

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
            foreach (string folder in folders)
            {
                //Debug.Log(folder);

                app = folder + "\\app.exe";
                icon = folder + "\\icon.png";

                //App newApp = new App(folder, folder + "\\app.exe", folder + "\\icon.png"); 
                App newApp = new App(folder, app, icon);
                newApp.debugPrint();

                apps.Add(newApp);
                appCounter++;
            }

            Debug.Log("MANAGER DEBUG: THERE IS A TOTAL OF " + appCounter + " APPS PRESENT");

            int xpos = 0;
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

                xpos += 3;
            }



            inputFlag = true;

            remoteEndPoint = new IPEndPoint(IPAddress.Any, 1000);
            udpClient = new UdpClient(remoteEndPoint);
            udpClient.Client.ReceiveBufferSize = bufferSize;
            udpClient.EnableBroadcast = true;

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
