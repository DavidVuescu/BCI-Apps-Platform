using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

using Apps;

public enum Direction : ushort
{
    LEFT =  0,
    RIGHT = 1
}

public class Manager : MonoBehaviour
{
    uint appCounter = 0;
    int selectedApp = 0;
    string userPath;
    List<App> apps = new List<App>();

    public GameObject container;
    public GameObject prefab; //Prefab used for instantiating an app

    public static string getUserPath()
    {
        string path;
        path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\.bciapps\\";
        Debug.Log(path);

        return path;
    }
    public void moveContainer(Direction direction)
    {
        if(direction == Direction.RIGHT)
        {
            container.transform.position = new Vector3(container.transform.position.x - 3, 0, 0);
        }
        if(direction == Direction.LEFT)
        {
            container.transform.position = new Vector3(container.transform.position.x + 3, 0, 0);
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
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow) && selectedApp > 0)
        {
            moveContainer(Direction.LEFT);
            selectedApp--; Debug.Log("Current selected app is: " + selectedApp);

        }

        if (Input.GetKeyDown(KeyCode.RightArrow) && selectedApp < appCounter - 1)
        {
            moveContainer(Direction.RIGHT);
            selectedApp++; Debug.Log("Current selected app is: " + selectedApp);
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            apps[selectedApp].run();
        }
    }
}
