using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class ProgramSky : MonoBehaviour
{
    public bool OnAurora = true;
    [Range(0,24)]
    public float m_skyTime = 8;
    public float TimeSpeed = 1;
    public Vector2 CloudDirection;
    private Light sun;
    private Material programSkyMat;

    [SerializeField]
    private List<SkyColor> dayColorList = new List<SkyColor>();
    [SerializeField]
    private List<SkyColor> nightColorList = new List<SkyColor>();

    //每小时的角度
    private const float TIME_STEP = 15f;
    private const float MOON_OFFSET = 0.15f;
    private float MoonOffset = -1;

    private int curDayColorIndex;
    private int curNightColorIndex;
    private List<Color> DayColorDatas = new List<Color>();
    private List<Color> NightColorDatas = new List<Color>();
    private Color curCloudColor;
    private SkyColor curColor;
    private SkyColor nextColor;


    public float SkyTime
    {
        get => m_skyTime;
        set
        {
            m_skyTime = value;
            if (m_skyTime >= 24)
            {
                m_skyTime -= 24;
            }
        }
    }

    void Start()
    {
        sun = RenderSettings.sun;
        programSkyMat = RenderSettings.skybox;

        if(sun)
        {
            float timeSeed = sun.gameObject.transform.eulerAngles.x / 24;
            SkyTime = timeSeed;
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
    }

    void Update()
    {
        if (programSkyMat == null)
        {
            Debug.LogError("未设置Skybox");
            return;
        }
        // 更新时间流逝
        UpdateTime();
        // 更新天空、环境光
        UpdateSkyColor();
        // 更新云颜色、偏移
        UpdateCould();
        // 更新主光源
        UpdateSun();
        // 更新极光
        UpdateAurora();
        // 更新月亮
        UpdateMoon();
    }

    private Color GetCurCloudColor()
    {
        Color cloudColor = Color.white;
        var data = IsNight() ? nightColorList : dayColorList;
        var tempTime = SkyTime >= 0 && SkyTime < 5 ? 24 + SkyTime : SkyTime;
        for (int i = 0; i < data.Count; i++)
        {
            if (data[i].SkyTime <= tempTime)
            {
                cloudColor = data[i].CloudColor;
            }
        }

        return cloudColor;
    }

    private Color GetCurLightColor()
    {
        Color lightColor = Color.white;
        var data = IsNight() ? nightColorList : dayColorList;
        var tempTime = SkyTime >= 0 && SkyTime < 5 ? 24 + SkyTime : SkyTime;
        for (int i = 0; i < data.Count; i++)
        {
            if(data[i].SkyTime <= tempTime)
            {
                lightColor = data[i].LightColor;
            }
        }

        return lightColor;
    }

    private bool IsNight()
    {
        return SkyTime < 5 || SkyTime > 18;
    }

    /// <summary>
    /// 更新主光源
    /// </summary>
    private void UpdateSun()
    {
        if (sun == null) return;
        var timeEuler = SkyTime * TIME_STEP - 90;
        sun.gameObject.transform.eulerAngles = new Vector3(timeEuler, 0, 0);
        programSkyMat.SetMatrix("LToW", sun.transform.localToWorldMatrix);
        sun.color = GetCurLightColor();
    }

    /// <summary>
    /// 更新天空颜色
    /// </summary>
    private void UpdateSkyColor()
    {
        if (dayColorList.Count > 1)
        {
            for (int i = 0; i < dayColorList.Count; i++)
            {
                if (SkyTime >= dayColorList[i].SkyTime)
                {
                    curDayColorIndex = i;
                }
            }

            var nextDayColorIndex = curDayColorIndex == dayColorList.Count - 1 ? 0 : curDayColorIndex + 1;
            curColor = dayColorList[curDayColorIndex];
            nextColor = dayColorList[nextDayColorIndex];
            for (int i = 0; i < 3; i++)
            {
                if (nextColor.SkyTime - SkyTime <= 1 && nextColor.SkyTime - SkyTime > 0)
                {
                    var col = Color.Lerp(nextColor.Color.colorKeys[i].color, curColor.Color.colorKeys[i].color, nextColor.SkyTime - SkyTime);
                    DayColorDatas.Add(col);
                }
                else
                {
                    DayColorDatas.Add(curColor.Color.colorKeys[i].color);
                }
            }
        }
        else if (dayColorList.Count == 1)
        {
            curColor = dayColorList[0];
            for (int i = 0; i < curColor.Color.colorKeys.Length; i++)
            {
                DayColorDatas.Add(curColor.Color.colorKeys[i].color);
            }
        }
        if (IsNight())
        {
            if (nightColorList.Count > 1)
            {
                for (int i = 0; i < nightColorList.Count; i++)
                {
                    if (SkyTime >= nightColorList[i].SkyTime)
                    {
                        curNightColorIndex = i;
                    }
                }
                var nextNightColorIndex = curNightColorIndex == nightColorList.Count - 1 ? 0 : curNightColorIndex + 1;
                curColor = nightColorList[curNightColorIndex];
                nextColor = nightColorList[nextNightColorIndex];
                for (int i = 0; i < 3; i++)
                {
                    if (nextColor.SkyTime - SkyTime <= 1 && nextColor.SkyTime - SkyTime > 0)
                    {
                        var col = Color.Lerp(nextColor.Color.colorKeys[i].color, curColor.Color.colorKeys[i].color, nextColor.SkyTime - SkyTime);
                        NightColorDatas.Add(col);
                    }
                    else
                    {
                        NightColorDatas.Add(curColor.Color.colorKeys[i].color);
                    }
                }
            }
            else if (nightColorList.Count == 1)
            {
                curColor = nightColorList[0];
                for (int i = 0; i < curColor.Color.colorKeys.Length; i++)
                {
                    NightColorDatas.Add(curColor.Color.colorKeys[i].color);
                }
            }
        }

        UpdateAmbient();
    }

    /// <summary>
    /// 更新云
    /// </summary>
    private void UpdateCould()
    {
        programSkyMat.SetVector("CloudDirection", CloudDirection);
        if (!nextColor.Equals(default) && !nextColor.Equals(default) && nextColor.SkyTime - SkyTime <= 1 && nextColor.SkyTime - SkyTime > 0)
        {
            curCloudColor = Color.Lerp(nextColor.CloudColor, curColor.CloudColor, nextColor.SkyTime - SkyTime);
        }
        else
        {
            curCloudColor = curColor.CloudColor;
        }
        programSkyMat.SetColor("_CloudColor", curCloudColor);
    }

    /// <summary>
    /// 开启极光
    /// </summary>
    private void UpdateAurora()
    {
        if (OnAurora)
            programSkyMat.EnableKeyword("_USE_AURORA");
        else
            programSkyMat.DisableKeyword("_USE_AURORA");
    }

    /// <summary>
    /// 更新月亮 TODO
    /// </summary>
    private void UpdateMoon()
    {
        //programSkyMat.SetFloat("_MoonMask", MoonOffset);
    }

    /// <summary>
    /// 更新时间
    /// </summary>
    private void UpdateTime()
    {
        if (Application.isPlaying)
        {
            SkyTime += TimeSpeed * Time.deltaTime;
        }
        programSkyMat.SetFloat("TimeSpeed", TimeSpeed);
    }

    /// <summary>
    /// 更新环境光
    /// </summary>
    private void UpdateAmbient()
    {
        var skyColor = Color.gray;
        var equatorColor = Color.gray;
        var groundColor = Color.gray;
        for (int i = 0; i < DayColorDatas.Count; i++)
        {
            var keyName = $"DayColor_{i}";
            programSkyMat.SetColor(keyName, DayColorDatas[i]);
            switch (i)
            {
                case 0:
                    groundColor = DayColorDatas[0];
                    break;
                case 1:
                    equatorColor = DayColorDatas[1];
                    break;
                case 2:
                    skyColor = DayColorDatas[2];
                    break;
            }
        }
        for (int i = 0; i < NightColorDatas.Count; i++)
        {
            var keyName = $"NightColor_{i}";
            programSkyMat.SetColor(keyName, NightColorDatas[i]);
            switch (i)
            {
                case 0:
                    groundColor = NightColorDatas[0];
                    break;
                case 1:
                    equatorColor = NightColorDatas[1];
                    break;
                case 2:
                    skyColor = NightColorDatas[2];
                    break;
            }
        }

        RenderSettings.ambientSkyColor = skyColor;
        RenderSettings.ambientGroundColor = groundColor;
        RenderSettings.ambientEquatorColor = equatorColor;
        DayColorDatas.Clear();
        NightColorDatas.Clear();
    }
}

[System.Serializable]
public struct SkyColor
{
    public float SkyTime;
    public Gradient Color;
    public Color LightColor;
    public Color CloudColor;
}
