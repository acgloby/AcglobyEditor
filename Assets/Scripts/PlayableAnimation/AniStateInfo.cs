using UnityEngine;
using UnityEngine.Playables;
namespace ProjectS
{
	/// <summary>
	/// 动画状态信息
	/// 被Playable包裹
	/// </summary>
	public class AniStateInfo
	{
		private Playable m_Playable;
		private AnimationClip m_Clip;

		public AniStateInfo m_ParentState;

		private bool m_Enabled;
		private bool m_EnabledDirty;

		private int m_Index;
		private string m_StateName;


		private float m_Time; //从0开始已播放的时长
		private bool m_TimeIsUpToDate; //标记时间值是否是最新的

		private bool m_Fading;
		float m_FadeSpeed;

		float m_Weight;
		private float m_TargetWeight;
		private bool m_WeightDirty;

		//是否准备好了被清理
		private bool m_ReadyForCleanup;

		public void Initialize(string name, AnimationClip clip)
		{
			m_StateName = name;
			m_Clip = clip;
        }

		public float GetTime()
		{
			if (m_TimeIsUpToDate)
			{
				return m_Time;
			}
			m_Time = (float)m_Playable.GetTime();
			m_TimeIsUpToDate = true;
			return m_Time;
		}

		public float time
		{
			set
			{
				SetTime(value);
			}
			get
			{
				return GetTime(); 
			}
		}

		public float length
		{
			get { return m_Clip.length; }
		}

		//倒回
		public void Rewind()
		{
			SetTime(0f);
		}
		public void SetTime(float newTime)
		{
			m_Time = newTime;
			if (m_Playable.IsValid())
			{
				m_Playable.SetTime(m_Time);
				m_Playable.SetDone(m_Time >= m_Playable.GetDuration());
			}
		}

		/// <summary>
		/// 归一时间
		/// </summary>
		public float normalizedTime
		{
			get
			{
				float length = m_Clip.length;
				if (length == 0f)
					length = 1f;
				return GetTime() / length;
			}
			set
			{
				float length = m_Clip.length;
				if (length == 0f)
				{
					length = 1f;
				}
				SetTime(value *= length);
			}
		}

		public void Enable()
		{
			if (m_Enabled)
				return;

			m_EnabledDirty = true;
			m_Enabled = true;
		}

		public void Disable()
		{
			if (m_Enabled == false)
				return;

			m_EnabledDirty = true;
			m_Enabled = false;
		}

		public void Pause()
		{
			m_Playable.Pause();
		}

		public void Play()
		{
			if (!clip.isLooping)
			{
				Rewind(); //倒回处理
			}
			m_Playable.Play();
		}

		public void Stop()
		{
			m_FadeSpeed = 0f;
			ForceWeight(0.0f);
			Disable();
			SetTime(0.0f);
			
			if(m_Playable.IsValid())
				m_Playable.SetDone(false);
			
			if (isClone)
			{
				m_ReadyForCleanup = true;
			}
		}

		public void ForceWeight(float weight)
		{
			m_TargetWeight = weight;
			m_Fading = false;
			m_FadeSpeed = 0f;
			SetWeight(weight);
		}

		public void SetWeight(float weight)
		{
			m_Weight = weight;
			m_WeightDirty = true;
		}

		//渐变到目标权重
		public void FadeTo(float weight, float speed)
		{
			m_Fading = Mathf.Abs(speed) > 0f;
			m_FadeSpeed = speed;
			m_TargetWeight = weight;
		}

		public void DestroyPlayable()
		{
			if (m_Playable.IsValid())
			{
				m_Playable.GetGraph().DestroySubgraph(m_Playable);
			}
		}

		public void SetAsCloneOf(AniStateInfo pStateInfo)
		{
			m_ParentState = pStateInfo;
			m_IsClone = true;
		}

		public bool enabled
		{
			get { return m_Enabled; }
		}

		public int index
		{
			get { return m_Index; }
			set
			{
				Debug.Assert(m_Index == 0, "Should never reassign Index");
				m_Index = value;
			}
		}

		public string stateName
		{
			get { return m_StateName; }
			set { m_StateName = value; }
		}

		public bool fading
		{
			get { return m_Fading; }
		}

		public float targetWeight
		{
			get { return m_TargetWeight; }
		}

		public float weight
		{
			get { return m_Weight; }
		}

		public float fadeSpeed
		{
			get { return m_FadeSpeed; }
		}

		public float speed
		{
			get { return (float)m_Playable.GetSpeed(); }
			set { m_Playable.SetSpeed(value); }
		}

		public float playableDuration
		{
			get { return (float)m_Playable.GetDuration(); }
		}

		public AnimationClip clip
		{
			get { return m_Clip; }
		}



		public void SetPlayable(Playable playable)
		{
			m_Playable = playable;
		}

		public bool isDone { get { return m_Playable.IsDone(); } }

		public Playable playable
		{
			get { return m_Playable; }
		}

		public bool isLoop
		{
			get
			{
				if (m_Clip != null)
					return m_Clip.isLooping;

				return false;
			}
		}

		//Clone information
		public bool isClone
		{
			get { return m_IsClone; }
		}
		//克隆 主要用于序列化播放的地方
		private bool m_IsClone;

		/// <summary>
		/// 是否要被清理掉
		/// </summary>
		public bool isReadyForCleanup
		{
			get { return m_ReadyForCleanup; }
		}

		public AniStateInfo parentState
		{
			get { return m_ParentState; }
		}

		public bool enabledDirty
		{
			get { return m_EnabledDirty; }
		}
		public bool weightDirty
		{
			get { return m_WeightDirty; }
		}

		public void ResetDirtyFlags()
		{
			m_EnabledDirty = false;
			m_WeightDirty = false;
		}

		//更新时间 脏标记 有用
		public void InvalidateTime() { m_TimeIsUpToDate = false; }

	}

}
