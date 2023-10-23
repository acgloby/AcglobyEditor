using System;
using UnityEngine;
using System.Collections.Generic;
using System.Text;
using  UnityEngine.UI;

[ExecuteInEditMode]
public class ProgramSky : MonoBehaviour
{
    [System.Serializable]
    public struct SkyColor
    {
        public float SkyTime;
        public Gradient Color;
        public Color LightColor;
        public Color CloudColor;
    }

    public bool OnAurora = true;
    [Range(0,24)]
    public float m_skyTime = 8;
    public float TimeSpeed = 1;
    public Text TimeShowText;

    public Vector2 CloudDirection;
    private Light sun;
    private Material programSkyMat;

    [SerializeField]
    private List<SkyColor> dayColorList = new List<SkyColor>();
    [SerializeField]
    private List<SkyColor> nightColorList = new List<SkyColor>();

    private const float DAY_SPET = 6;
    private const float NIGHT_SPET = 19;

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


    private  StringBuilder stringBuilder = new StringBuilder();

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

            stringBuilder.AppendFormat("Time ");
            stringBuilder.AppendFormat("{0:d2}", (int)SkyTime);
            stringBuilder.AppendFormat(":");
            stringBuilder.AppendFormat("{0:d2}", (int)((SkyTime - (int)SkyTime) * 60));
            TimeShowText.text = stringBuilder.ToString();
            stringBuilder.Clear();
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
        var tempTime = SkyTime >= 0 && SkyTime < DAY_SPET ? 24 + SkyTime : SkyTime;
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
        if (nextColor.SkyTime - SkyTime <= 1 && nextColor.SkyTime - SkyTime > 0)
        {
            lightColor = Color.Lerp(nextColor.LightColor, curColor.LightColor, nextColor.SkyTime - SkyTime);
        }
        else
        {
            lightColor = curColor.LightColor;
        }
        return lightColor;
    }

    private bool IsNight()
    {
        return SkyTime < DAY_SPET || SkyTime >= NIGHT_SPET;
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
                //夜晚->白天切换过渡
                if (DAY_SPET - SkyTime < 1 && dayColorList.Count > 0)
                {
                    nextColor = dayColorList[0];
                }
                else
                {
                    nextColor = nightColorList[nextNightColorIndex];
                }
                for (int i = 0; i < 2; i++)
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
                //夜晚->白天切换过渡
                if (DAY_SPET - SkyTime < 1 && dayColorList.Count > 0)
                {
                    nextColor = dayColorList[0];
                }
                for (int i = 0; i < curColor.Color.colorKeys.Length; i++)
                {
                    NightColorDatas.Add(curColor.Color.colorKeys[i].color);
                }
            }
        }
        else
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
                //白天->夜晚切换过渡
                if (NIGHT_SPET - SkyTime < 1 && nightColorList.Count > 0)
                {
                    nextColor = nightColorList[0];
                }
                else
                {
                    nextColor = dayColorList[nextDayColorIndex];
                }
                for (int i = 0; i < 2; i++)
                {
                    if (nextColor.SkyTime - SkyTime < 1 && nextColor.SkyTime - SkyTime > 0)
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
        for (var i = 0; i < DayColorDatas.Count; i++)
        {
            var keyName = $"DayColor_{i}";
            programSkyMat.SetColor(keyName, DayColorDatas[i]);
        }
        for (var i = 0; i < NightColorDatas.Count; i++)
        {
            var keyName = $"NightColor_{i}";
            programSkyMat.SetColor(keyName, NightColorDatas[i]);
        }

        Color skyColor, equatorColor, groundColor;
        if (nextColor.SkyTime - SkyTime <= 1 && nextColor.SkyTime - SkyTime > 0)
        {
            groundColor = Color.Lerp(nextColor.Color.colorKeys[1].color, curColor.Color.colorKeys[1].color, nextColor.SkyTime - SkyTime);
            equatorColor = Color.Lerp(nextColor.Color.colorKeys[1].color, curColor.Color.colorKeys[1].color, nextColor.SkyTime - SkyTime);
            skyColor = Color.Lerp(nextColor.Color.colorKeys[0].color, curColor.Color.colorKeys[0].color, nextColor.SkyTime - SkyTime);
        }
        else
        {
            groundColor = curColor.Color.colorKeys[1].color;
            equatorColor = curColor.Color.colorKeys[1].color;
            skyColor = curColor.Color.colorKeys[0].color;
        }
        RenderSettings.ambientSkyColor = skyColor;
        RenderSettings.ambientGroundColor = groundColor;
        RenderSettings.ambientEquatorColor = equatorColor;
        DayColorDatas.Clear();
        NightColorDatas.Clear();
    }
}