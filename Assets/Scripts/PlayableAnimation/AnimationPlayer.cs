using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ProjectS
{
	/// <summary>
	/// 动画播放器
	/// </summary>
	[RequireComponent(typeof(Animator))]
	[ExecuteInEditMode]
	public partial class AnimationPlayer : MonoBehaviour
	{
		[SerializeField]
		protected bool m_PlayAutomatically = true; //自动播放开关
		/// <summary>
		/// 基于物理的动画播放模式标记
		/// </summary>
		[SerializeField]
		protected bool m_AnimatePhysics = false;
		/// <summary>
		/// 动画裁剪模式
		/// </summary>
		[SerializeField]
		protected AnimatorCullingMode m_CullingMode = AnimatorCullingMode.CullUpdateTransforms;
		/// <summary>
		/// 动画结束时采用的重复模式  这个属性暂时不这么用 ，因为实际上每个AnimationClip的wrapmode不尽相同
		/// </summary>
		[SerializeField]
		protected WrapMode m_WrapMode = WrapMode.Default;

		/// <summary>
		/// 动画层级数量  默认是一层
		/// </summary>
		[SerializeField]
		private int m_LayerNum = 1;
		/// <summary>
		/// AvatarMask
		/// </summary>
		[SerializeField]
		private AvatarMask[] m_AvatarMask ;

		protected bool m_Initialized; //初始化标记

		/// <summary>
		/// 图形树实例
		/// </summary>
		protected PlayableGraph m_Graph;

		public List<AnimationLayerPlayable> m_LayerStates;
		public AnimationLayerMixerPlayable m_LayerMixer;

		/// <summary>
		/// transition混合具柄 暂时没用到
		/// </summary>
		protected PlayableHandle m_TransitionMixer;
		// Animator组件
		protected Animator m_Animator;

		//表示整个树是否处于播放状态
		protected bool m_IsRunning;


		public Vector3 deltaPosition
		{
			get
			{
				if (m_Animator != null)
					return m_Animator.deltaPosition;

				return Vector3.zero;
			}
			
		}
		
		public Animator animator
		{
			get
			{
				if (m_Animator == null)
				{
					m_Animator = GetComponent<Animator>();
				}
				return m_Animator;
			}
		}

		public bool animatePhysics
		{
			get { return m_AnimatePhysics; }
			set
			{
				m_AnimatePhysics = value;
				animator.updateMode = m_AnimatePhysics ? AnimatorUpdateMode.AnimatePhysics : AnimatorUpdateMode.Normal; 
			}
		}

		public AnimatorCullingMode cullingMode
		{
			get { return animator.cullingMode; }
			set { m_CullingMode = value; animator.cullingMode = m_CullingMode; }
		}

		/// <summary>
		/// 动画播放器是否正在播放
		/// </summary>
		public bool IsPlaying()
		{
			foreach (var playable in m_LayerStates)
			{
				if (playable.IsPlaying())
				{
					return true;
				}
			}
			return false;
		}

        /// <summary>
        /// 默认动画剪辑 
        /// </summary>
        public AnimationClip clip
        {
            get { return m_Clip; }
            set
            {
				LegacyClipCheck(value);
				m_Clip = value;
            }
        }

        public List<EditorStateManager> StatesList
        {
	        get { return m_StatesList; }
        }
        public bool playAutomatically
		{
			get { return m_PlayAutomatically; }
			set 
			{
				m_PlayAutomatically = value; 
			}
		}

		//尽量避免调用这个函数
		public WrapMode wrapMode
		{
			get { return m_WrapMode; }
			set
			{
				if(m_WrapMode!=value)
				{
					m_WrapMode = value;
				}
			}
		}

		//初始化simpleAnimation组件
		protected virtual void OnEnable()
		{
			Initialize();

#if UNITY_EDITOR
			if (Application.isEditor && !Application.isPlaying)
			{
				if(m_PlayAutomatically)
				{
					Play(); //激活脚本时触发播放
				}
			}
#endif

			Kick();
		}

		protected virtual void Start()
		{
			
		}

		protected virtual void Awake()
		{
			
		}

		protected void Update()
		{
			if(m_Initialized==false)
			{
				return;
			}
			AnimationLayerPlayable layerPlayable = null;
			for (int i = 0; i < m_LayerStates.Count; i++)
			{
				layerPlayable = m_LayerStates[i];
				if (layerPlayable.WeightDirty)
				{
					SetLayerInputWeight(i, layerPlayable.GetWeight());
					layerPlayable.WeightDirty = false;
				}
			}
		}

		public void SetTimeUpdateMode(DirectorUpdateMode updateMode)
		{
			if(m_Graph.GetTimeUpdateMode()==updateMode)
			{
				return;
			}
			m_Graph.SetTimeUpdateMode(updateMode);
		}

		//初始化组件参数
		public void Initialize()
		{
			if (m_Initialized)
			{
				return;
			}

			m_Animator = GetComponent<Animator>();
			m_Animator.updateMode = m_AnimatePhysics ? AnimatorUpdateMode.AnimatePhysics : AnimatorUpdateMode.Normal;
			m_Animator.cullingMode = m_CullingMode;

			m_Graph = PlayableGraph.Create(string.Format("Animation Player Graph  Name: {0}", this.gameObject.name));
			m_Graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

			//动画层混合器
			var layerMixer = AnimationLayerMixerPlayable.Create(m_Graph, 1);
			m_LayerStates = new List<AnimationLayerPlayable>();
			m_LayerMixer = layerMixer;
			AnimationPlayableUtilities.Play(m_Animator, m_LayerMixer, m_Graph);

			InitializeAnimationLayers();

			//初始化所有层的动画信息
			for (int i = 0; i < m_LayerNum; i++)
			{
				AnimationLayerPlayable layerPlayer = m_LayerStates[i];
				if (i>=m_StatesList.Count)
				{
					m_StatesList.Add(new EditorStateManager());
				}
				InitializeEditorStates(ref m_StatesList[i].states, ref layerPlayer);
			}

			//自动播放
			if (m_PlayAutomatically)
			{
				Play();
			}

			m_Initialized = true;
		}

		/// <summary>
		/// 初始化图层
		/// </summary>
		protected void InitializeAnimationLayers()
		{
			if (m_LayerNum < 1)
			{
				Debug.LogError("图层不能小于1");
				return;
			}
			if(m_AvatarMask==null)
			{
				m_AvatarMask = new AvatarMask[1];
			}
			if (m_AvatarMask.Length != m_LayerNum)
			{
				Debug.LogError("Avatar mask数组长度与图层数不一致");
				return;
			}
			for (int i = 0; i < m_LayerNum; i++)
			{
				AnimationLayerPlayable template = new AnimationLayerPlayable();
				var playable = ScriptPlayable<AnimationLayerPlayable>.Create(m_Graph, template, 1).GetBehaviour();
				AddLayerState(playable);
				if (m_AvatarMask[i] != null)
				{
					m_LayerMixer.SetLayerMaskFromAvatarMask((uint)i, m_AvatarMask[i]);
				}
			}
		}

		/// <summary>
		/// 混合播放
		/// </summary>
		/// <param name="layerIndex"></param>
		/// <param name="stateName"></param>
		/// <param name="targetWeight"></param>
		/// <param name="fadeLength"></param>
		public void Blend(int layerIndex, string stateName, float targetWeight, float fadeLength)
		{
			m_Animator.enabled = true;
			Kick();
			var playableLayer = GetPlayableLayer(layerIndex);
			playableLayer.Blend(stateName, targetWeight, fadeLength);
		}

		public void CrossFade(int layerIndex, string stateName, float fadeLength)
		{
			m_Animator.enabled = true;
			Kick();
			var playableLayer = GetPlayableLayer(layerIndex);
			playableLayer.Crossfade(stateName, fadeLength);
		}

		public void CrossFadeQueued(int layerIndex, string stateName, float fadeLength, QueueMode queueMode)
		{
			m_Animator.enabled = true;
			Kick();
			var playableLayer = GetPlayableLayer(layerIndex);
			playableLayer.CrossfadeQueued(stateName, fadeLength, queueMode);
		}

		public AnimationClip GetClip(string name, int layerIndex = 0)
		{
			var playableLayer = GetPlayableLayer(layerIndex);
			AniStateInfo stateInfo = playableLayer.GetStateInfo(name);
			if (stateInfo == null)
			{
				return null;
			}
			return stateInfo.clip;
		}


		/// <summary>
		/// 获取动画剪辑个数
		/// </summary>
		/// <param name="layerIndex"></param>
		/// <returns></returns>
		public int GetClipCount(int layerIndex = 0)
		{
			var playableLayer = GetPlayableLayer(layerIndex);
			return playableLayer.GetClipCount();
		}

		/// <summary>
		/// 是否在播放指定动画
		/// </summary>
		/// <param name="stateName">动画名称</param>
		/// <param name="layerIndex">动画层级 默认为0</param>
		/// <returns></returns>
		public bool IsPlaying(string stateName, int layerIndex = 0)
		{
			var playableLayer = GetPlayableLayer(layerIndex);
			return playableLayer.IsPlaying(stateName);
		}

		/// <summary>
		/// 停止播放所有动画
		/// </summary>
		public void Stop()
		{
			foreach (var state in m_LayerStates)
			{
				state.StopAll();
			}
		}
		/// <summary>
		/// 停止指定的动画
		/// </summary>
		/// <param name="stateName"></param>
		/// <param name="layerIndex"></param>
		public void Stop(string stateName, int layerIndex = 0)
		{
			var playableLayer = GetPlayableLayer(layerIndex);
			playableLayer.Stop(stateName);
		}

		/// <summary>
		/// 停止当前层上的所有动画
		/// </summary>
		/// <param name="layerIndex"></param>
		private void Stop(int layerIndex)
		{
			var playableLayer = GetPlayableLayer(layerIndex);
			playableLayer.StopAll();
		}


		public void Sample()
		{
			m_Graph.Evaluate();
		}
		/// <summary>
		/// 播放图形树
		/// </summary>
		protected void Kick()
		{
			if (!m_IsRunning)
			{
				m_Graph.Play();
				m_IsRunning = true;
			}
		}

		protected void StopRunning()
		{
			if (m_IsRunning)
			{
				if(m_Graph.IsValid())
					m_Graph.Stop();
				m_IsRunning = false;
			}
		}

		/// <summary>
		/// 播放所有层级的动画 
		/// 如果动画参数为空则会停止该动画当前层的所有动画
		/// 参数结构：layer0:aniName , layer1:addAniName ...
		/// </summary>
		/// <param name="aniName">基础动画</param>
		/// <param name="addAniName">附加动画 默认为空 需要上下半身分离动画才会用到</param>
		/// <param name="bFade">是否淡入淡出</param>
		/// <param name="fadeTime">淡入淡出的时间</param>
		/// <returns></returns>
		public bool Play(string aniName, string addAniName=null, bool bFade = false, float fadeTime = 0.2f)
		{
			m_Animator.enabled = true;
			Kick();
			int layerIndex = 0;
			if (aniName == null)
			{
				if (addAniName != null)
				{
					Debug.LogError("AnmationPlayer:Play 基础层的动画必须非空!!");
					return false; //不允许这么调用 基础层必须不为空才行
				}
				Stop(layerIndex); //停止该层所有的动画
			}
			else
			{
				if (bFade)
				{
					CrossFade(layerIndex, aniName, fadeTime);
				}
				else
				{
					Play(layerIndex, aniName);
				}
			}

			//处理第二层动画
			if (m_LayerNum < 2)
			{
				return true;
			}

			layerIndex = 1;
			if (addAniName == null)
			{
				Stop(layerIndex); //停止该层所有的动画
			}
			else
			{
				if (bFade)
				{
					AnimationLayerPlayable playable = GetPlayableLayer(layerIndex);
					//如果第二层动画都处于停止状态，那么第二层动画要做淡入操作，第一层动画会随之淡出
					if (playable.IsPlaying() == false)
					{
						playable.ForceWeight(0f); //
						playable.FadeTo(1.0f, fadeTime);
					}

					CrossFade(layerIndex, addAniName, fadeTime);
				}
				else
				{
					Play(layerIndex, addAniName);
				}
			}

			return true;
		}

		//播放首个动画
		public void Play()
		{
			Kick();
			foreach (var state in m_LayerStates)
			{
				state.Play(0);
			}
		}

		/// <summary>
		/// 在指定的层上播放指定的动画
		/// 注意：绝大多数的情况下，外部避免调用这个接口
		/// </summary>
		/// <param name="layerIndex">层级索引 从0开始</param>
		/// <param name="stateName">动画名字</param>
		/// <returns></returns>
		public bool Play(int layerIndex, string stateName)
		{
			var playable = GetPlayableLayer(layerIndex);
			return playable.Play(stateName);
		}

		/// <summary>
		/// 设置层的权重
		/// </summary>
		/// <param name="layerIndex"></param>
		/// <param name="value"></param>
		public void SetLayerInputWeight(int layerIndex, float value)
		{
			if (m_AvatarMask[layerIndex] != null)
			{
				m_LayerMixer.SetInputWeight(layerIndex, value);
			}
		}
		/// <summary>
		/// 获取某个图层的权重
		/// </summary>
		/// <param name="layerIndex"></param>
		/// <returns></returns>
		public float GetLayerInputWeight(int layerIndex)
		{
			return m_LayerMixer.GetInputWeight(layerIndex);
		}

		/// <summary>
		/// 设置动画播放速度
		/// </summary>
		/// <param name="value"></param>
		public void SetSpeed(float value)
		{
			foreach (var layerState in m_LayerStates)
			{
				layerState.SetStateSpeed(value);
			}
		}

		/// <summary>
		/// 添加新的动画状态
		/// </summary>
		/// <param name="clip">动画剪辑</param>
		/// <param name="name">名称</param>
		/// <param name="layerIndex">层级 默认为0</param>
		public void AddClip(AnimationClip clip, string name = null, int layerIndex = 0)
		{
            if (LegacyClipCheck(clip))
                return;
			Kick();
			var playable = GetPlayableLayer(layerIndex);
			if (name == null)
			{
				name = clip.name;
			}
			if(playable==null)
			{
				Debug.LogError(string.Format("AnimationPlayer:AddClip 找不到对应的动画层 layerIndex: {0} name: {1}", layerIndex, name));
				return;
			}
            if (playable.AddClip(clip, name))
            {
                RebuildStates(layerIndex);
            }
        }

		/// <summary>
		/// 移除一个动画
		/// </summary>
		/// <param name="layerIndex">所在层级 默认为0</param>
		/// <param name="name">名字</param>
		public void RemoveClip(string name, int layerIndex = 0)
		{
			var playable = GetPlayableLayer(layerIndex);
            if (playable.RemoveClip(name))
            {
                RebuildStates(layerIndex);
            }
        }

		/// <summary>
		/// 序列化播放 只支持单层播放
		/// </summary>
		/// <param name="stateName"></param>
		/// <param name="queueMode"></param>
		/// <param name="layerIndex"></param>
		public void PlayQueued(string stateName, QueueMode queueMode)
		{
			m_Animator.enabled = true;
			Kick();
			for (int i = 0; i < m_LayerStates.Count; i++)
			{
				var playable = m_LayerStates[i];
				if (i == 0)
				{
					playable.PlayQueued(stateName, queueMode); //使用第一层
				}
				else
				{
					playable.StopAll(); //停止其它层的动画
				}
			}
		}


		public void CrossFadeQueued(string stateName, float fadeTime, QueueMode queueMode)
		{
			m_Animator.enabled = true;
			Kick();
			for (int i = 0; i < m_LayerStates.Count; i++)
			{
				var playable = m_LayerStates[i];
				if (i == 0)
				{
					playable.CrossfadeQueued(stateName, fadeTime, queueMode); //使用第一层
				}
				else
				{
					playable.StopAll(); //停止其它层的动画
				}
			}
		}



		/// <summary>
		/// 倒回
		/// </summary>
		/// <param name="layerIndex"></param>
		public void Rewind(int layerIndex = 0)
		{
			Kick();
			var playable = GetPlayableLayer(layerIndex);
			playable.Rewind();
		}

		public void Rewind(string stateName, int layerIndex = 0)
		{
			Kick();
			var layerPlayable = GetPlayableLayer(layerIndex);
			layerPlayable.Rewind(stateName);
		}

		/// <summary>
		/// 根据动画名字获取动画状态
		/// </summary>
		/// <param name="stateName"></param>
		/// <returns></returns>
		public AniStateInfo GetStateInfo(string stateName, int layerIndex = 0)
		{
			var layerState = GetPlayableLayer(layerIndex);
			AniStateInfo state = layerState.GetStateInfo(stateName);
			if (state == null)
			{
				return null;
			}
			return state;
		}
		private List<AniStateInfo> m_StateInfos = new List<AniStateInfo>();
		/// <summary>
		/// 获取某一层的所有动画剪辑
		/// </summary>
		/// <param name="layerIndex"></param>
		/// <returns></returns>
		public List<AniStateInfo> GetStateInfos(int layerIndex = 0)
		{
			m_StateInfos.Clear();
			var playable = GetPlayableLayer(layerIndex);
			if(playable==null)
			{
				return m_StateInfos;
			}
			for(int i=0;i< playable.StateInfos.Count; i++)
			{
				if(playable.StateInfos[i]!=null)
				{
					m_StateInfos.Add(playable.StateInfos[i]);
				}
			}
			return m_StateInfos;
		}

		public AniStateInfo this[string name]
		{
			get { return GetStateInfo(name); }
		}

		//-----------------------------------------------------------------------------------------------//
		/// <summary>
		/// 添加新图层
		/// </summary>
		/// <param name="layerState"></param>
		public void AddLayerState(AnimationLayerPlayable layerState)
		{
			foreach (var state in m_LayerStates)
			{
				if (state.LayerName == layerState.LayerName)
				{
					Debug.LogError("图层名称不可重复!");
				}
			}

			layerState.LayerIndex = m_LayerStates.Count;
			layerState.LayerName = string.Format("layer{0}", layerState.LayerIndex);
			m_LayerStates.Add(layerState);

			if (m_LayerMixer.GetInputCount() != m_LayerStates.Count)
			{
				m_LayerMixer.SetInputCount(m_LayerStates.Count);
			}
			m_LayerMixer.ConnectInput(layerState.LayerIndex, layerState.playable, 0, 1.0f);
		}

		/// <summary>
		/// 移除图层
		/// </summary>
		/// <param name="layerIndex"></param>
		public void RemoveLayerState(int layerIndex)
		{
			var layerState = GetPlayableLayer(layerIndex);
			if (layerState == null)
			{
				Debug.LogError(string.Format("RemoveLayerState 找不到对应的图层 index: {0}", layerIndex));
				return;
			}
			m_LayerStates.Remove(layerState);
			for (int i = 0; i < m_LayerStates.Count; i++)
			{
				m_LayerStates[i].LayerIndex = i;  //每移除一个图层 那么剩余所有图层索引可能都会随之改变  这里看日后的需求再做调整吧
				m_LayerStates[i].LayerName = string.Format("layer{0}", i);
			}
			m_LayerMixer.DisconnectInput(m_LayerStates.Count);
			if (m_LayerMixer.GetInputCount() != m_LayerStates.Count)
			{
				m_LayerMixer.SetInputCount(m_LayerStates.Count);
			}
		}

		/// <summary>
		/// 获得可播放的动画层
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public AnimationLayerPlayable GetPlayableLayer(string name)
		{
			foreach (var state in m_LayerStates)
			{
				if (state.LayerName == name)
				{
					return GetPlayableLayer(state.LayerIndex);
				}
			}
			Debug.LogError(string.Format("GetLayerState 没有找到动画图层 name: {0}", name));
			return null;
		}

		/// <summary>
		/// 获得可播放的动画层
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public AnimationLayerPlayable GetPlayableLayer(int index)
		{
			return index >= m_LayerStates.Count ? null : m_LayerStates[index];
		}

		public void SetUpdateMode(DirectorUpdateMode mode)
		{
			m_Graph.SetTimeUpdateMode(mode);
		}

		//脚本禁用时触发
		protected virtual void OnDisable()
		{
			if (m_Initialized)
			{
				//Stop();
				StopRunning();
			}
		}

		protected void OnDestroy()
		{
			if (m_Graph.IsValid())
			{
				m_Graph.Destroy();
			}
		}

		/// <summary>
		/// 节点播放完毕回调函数
		/// 暂时用不到 以后根据需求扩展
		/// </summary>
		private void OnPlayableDone()
		{
			StopRunning();
		}
		
		
		private void OnAnimatorMove()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				transform.position += animator.deltaPosition;
				transform.rotation *= animator.deltaRotation;
			}
#endif
		}
	}

}
