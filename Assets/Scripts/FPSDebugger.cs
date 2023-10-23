using UnityEngine;
using UnityGameFramework.Runtime;

public class FPSDebugger : MonoBehaviour
{
    public Font Font;

    private float interval = 0.5f;
    private int frameCount;
    private float timeCount;
    private float fps;
    private float timeLeft;

    void Start()
    {
        DontDestroyOnLoad(this);
        Application.targetFrameRate = 120;
    }

    void Update()
    {
        frameCount++;
        timeCount += Time.unscaledDeltaTime;
        timeLeft -= Time.unscaledDeltaTime;
        if (timeLeft <= 0)
        {
            fps = timeCount > 0 ? frameCount / timeCount : 0;
            frameCount = 0;
            timeCount = 0;
            timeLeft += interval;
        }
    }


    private void OnGUI()
    {
        GUIStyle guiStyle = new GUIStyle();
        guiStyle.fontSize = 34;
        guiStyle.normal.textColor = Color.white;
        guiStyle.font = Font;
        GUI.Label(new Rect(0 + 20f,0,80,50), "FPS:" + (int)fps, guiStyle);
    }
}
