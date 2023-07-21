using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Debugger : MonoBehaviour
{
    public Font Font;

    private float interval = 0.5f;
    private int frameCount;
    private float timeCount;
    private float fps;

    void Start()
    {
        DontDestroyOnLoad(this);
    }

    void Update()
    {
        frameCount++;
        timeCount += Time.unscaledDeltaTime;
        if (timeCount >= interval)
        {
            fps = frameCount / timeCount;
            frameCount = 0;
            timeCount -= interval;
        }
    }


    private void OnGUI()
    {
        GUIStyle guiStyle = new GUIStyle();
        guiStyle.fontSize = 34;
        guiStyle.normal.textColor = Color.white;
        guiStyle.font = Font;
        GUI.Label(new Rect(0,0,80,50), "FPS:" + (int)fps, guiStyle);
    }
}
