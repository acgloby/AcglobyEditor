using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

[RequireComponent(typeof(Animator))]
public class AnimationPlayer : MonoBehaviour
{
    public ActionData[] ActionDatas;

    private List<AnimationLayerPlayable> m_animationLayerPlayableList = new List<AnimationLayerPlayable>();

    private PlayableGraph m_graph;

    [SerializeField]
    private Animator m_animator;
    private AnimationLayerMixerPlayable m_layerMixer;
    private int m_layerNum = 1;

    public bool IsPlaying = false;

    void OnEnable()
    {
        Initialize();
        Play();
    }


    public void Initialize()
    {
        m_graph = PlayableGraph.Create($"AnimationGraphName[{gameObject.name}]");
        m_graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        m_layerMixer = AnimationLayerMixerPlayable.Create(m_graph, 1);

        if(m_animator == null)
            m_animator = GetComponent<Animator>();

        AnimationPlayableUtilities.Play(m_animator, m_layerMixer, m_graph);

        InitializeAnimationLayer();
        InitializeClip();
    }

    private void InitializeAnimationLayer()
    {
        for (var i = 0; i < m_layerNum; i++)
        {
            var layerPlayable = new AnimationLayerPlayable();
            var playable = ScriptPlayable<AnimationLayerPlayable>.Create(m_graph, layerPlayable, 1).GetBehaviour();
            AddLayerState(playable);
        }
    }

    private void InitializeClip()
    {
        foreach (var layer in m_animationLayerPlayableList)
        {
            foreach (var actionData in ActionDatas)
            {
                layer.AddClip(actionData.Name, actionData.Clip);
            }
        }
    }

    public void AddLayerState(AnimationLayerPlayable layerPlayable)
    {
        foreach (var layer in m_animationLayerPlayableList)
        {
            if (layerPlayable.LayerName != layer.LayerName) 
                continue;
            Debug.LogError($"图层不可重复添加：{layer.LayerName}");
            break;
        }

        layerPlayable.LayerIndex = m_animationLayerPlayableList.Count;
        layerPlayable.LayerName = $"Layer{layerPlayable.LayerIndex}";
        m_animationLayerPlayableList.Add(layerPlayable);
        if (m_layerMixer.GetInputCount() != m_animationLayerPlayableList.Count)
        {
            m_layerMixer.SetInputCount(m_animationLayerPlayableList.Count);
        }

        m_layerMixer.ConnectInput(layerPlayable.LayerIndex, layerPlayable.Playable, 0, 1.0f);
    }

    public void Play()
    {
        if (!IsPlaying)
        {
            m_graph.Play();
            IsPlaying = true;
        }
        foreach (var layer in m_animationLayerPlayableList)
        {
            layer.Play(0);
        }
    }

    public void Play(string actionName)
    {
        if (!IsPlaying)
        {
            m_graph.Play();
            IsPlaying = true;
        }
        foreach (var layer in m_animationLayerPlayableList)
        {
            layer.Play(actionName);
        }
    }

    public void PauseAll()
    {
        foreach (var layer in m_animationLayerPlayableList)
        {
            layer.PauseAll();
        }
    }


    public void OnGUI()
    {
        for (int i = 0; i < ActionDatas.Length; i++)
        {
            var dis = 10 + i * 55;
            var click = GUI.Button(new Rect(new Vector2(Screen.width - 150, dis), new Vector2(150, 50)), ActionDatas[i].Name);
            if (click)
            {
                Play(ActionDatas[i].Name);
            }
        }
        var pause = GUI.Button(new Rect(new Vector2(Screen.width - 310, 10), new Vector2(150, 50)), "Pause");
        if (pause)
        {
            PauseAll();
        }
    }

    [Serializable]
    public class ActionData
    {
        public string Name;
        public AnimationClip Clip;
    }
}
