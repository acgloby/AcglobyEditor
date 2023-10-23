using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class ActionInfo
{
    private string m_actionName;
    private AnimationClip m_clip;
    private Playable m_playable;
    private float m_time;
    private int m_frame;


    public Playable Playable
    {
        get { return m_playable; }
    }

    public float Time
    {
        get { return m_time; }
    }

    public int Frame
    {
        get { return m_frame; }
    }

    public string ActionName
    {
        get { return m_actionName; }
    }

    public int Index { get; set; }

    public void Initialize(string actionName, AnimationClip clip)
    {
        m_actionName = actionName;
        m_clip = clip;
    }

    public void SetPlayable(Playable playable)
    {
        m_playable = playable;
    }

    public void Play()
    {
        if (!m_clip.isLooping)
        {
            SetTime(0);
        }
        m_playable.Play();
    }

    public void Pause()
    {
        m_playable.Pause();
    }

    public void Stop()
    {
        SetTime(0);
        if (m_playable.IsValid())
            m_playable.SetDone(false);
    }

    public void SetTime(float newTime)
    {
        m_time = newTime;
        if (m_playable.IsValid())
        {
            m_playable.SetTime(m_time);
            m_playable.SetDone(m_time >= m_playable.GetDuration());
        }
    }

    public void DestroyPlayable()
    {
        if (m_playable.IsValid())
        {
            m_playable.GetGraph().DestroySubgraph(m_playable);
        }
    }
}
