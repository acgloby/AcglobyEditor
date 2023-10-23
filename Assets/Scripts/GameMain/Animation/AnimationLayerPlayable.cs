using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationLayerPlayable : PlayableBehaviour
{
    public int ActionCount;
    public string LayerName;
    public int LayerIndex;

    private Playable m_playable;
    private AnimationMixerPlayable m_animationMixer;
    private int m_currentPlayIndex;

    private List<ActionInfo>  m_actionInfos = new List<ActionInfo>();


    public Playable Playable
    {
        get { return m_playable; }
    }
    
    protected PlayableGraph graph
    {
        get { return m_playable.GetGraph(); }
    }

    public void Init(AnimationPlayer go)
    {
    }

    public override void OnPlayableCreate(Playable playable)
    {
        m_playable = playable;

        m_animationMixer = AnimationMixerPlayable.Create(graph, 1);

        m_playable.SetInputCount(1);
        m_playable.SetInputWeight(0, 1);
        graph.Connect(m_animationMixer, 0, m_playable, 0);
    }

    public override void PrepareFrame(Playable owner, FrameData data)
    {
        for (int i = 0; i < m_actionInfos.Count; i++)
        {
            m_animationMixer.SetInputWeight(m_actionInfos[i].Index,
                m_actionInfos[i].Index == m_currentPlayIndex ? 1 : 0);
        }
    }

    public void AddClip(string actionName, AnimationClip clip)
    {
        var actionInfo = FindAction(actionName);
        if (actionInfo != null)
        {
            return;
        }

        DoAddClip(actionName, clip);
    }

    public void DoAddClip(string actionName, AnimationClip clip)
    {
        var actionInfo = InsertActionInfo();
        actionInfo.Initialize(actionName, clip);
        var index = actionInfo.Index;

        if (index == m_animationMixer.GetInputCount())
        {
            m_animationMixer.SetInputCount(index + 1);
        }

        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        clipPlayable.SetApplyFootIK(false);
        clipPlayable.SetApplyPlayableIK(false);

        actionInfo.SetPlayable(clipPlayable);
        actionInfo.Pause();

        ConnectInput(actionInfo.Index);
    }

    public void Play(int index)
    {
        var action = m_actionInfos.Find(a => a != null & a.Index == index);
        m_currentPlayIndex = action.Index;
        action.Play();
    }

    public void Play(string name)
    {
        PauseAll();
        var action = m_actionInfos.Find(a => a != null & a.ActionName == name);
        m_currentPlayIndex = action.Index;
        action.Play();
    }

    public void PauseAll()
    {
        foreach (var actionInfo in m_actionInfos)
        {
            actionInfo.Pause();
        }
    }


    private ActionInfo InsertActionInfo()
    {
        var actionInfo = new ActionInfo();
        var insertIndex = -1;
        for (var i = 0; i < m_actionInfos.Count; i++)
        {
            if (m_actionInfos[i] != null) continue;

            insertIndex = i;
            m_actionInfos[i] = actionInfo;
        }

        if (insertIndex == -1)
        {
            insertIndex = m_actionInfos.Count;
            m_actionInfos.Add(actionInfo);
            ActionCount++;
        }

        actionInfo.Index = insertIndex;
        return actionInfo;
    }

    private void ConnectInput(int index)
    {
        var action = m_actionInfos[index];
        graph.Connect(action.Playable, 0, m_animationMixer, action.Index);
    }


    private ActionInfo FindAction(string actionName)
    {
        var actionInfo = m_actionInfos.Find(a => a != null && a.ActionName == actionName);
        
        return actionInfo;
    }
}
