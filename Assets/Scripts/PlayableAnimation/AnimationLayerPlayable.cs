using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;
namespace ProjectS
{
	/// <summary>
	/// 一个自定义可播放的动画层
	/// </summary>
	public class AnimationLayerPlayable : PlayableBehaviour
	{
		//动画层的名称
		public string LayerName = "Default";
		//动画层的索引
		public int LayerIndex;

		/// <summary>
		/// 序列播放项
		/// </summary>
		private struct QueuedState
		{
			public AniStateInfo state;
			public float fadeTime;
			public QueuedState(AniStateInfo stateInfo, float fadeTime)
			{
				this.state = stateInfo;
				this.fadeTime = fadeTime;
			}
		}
		LinkedList<QueuedState> m_StateQueue;

		//动画停止时是否保持连接状态
		bool m_KeepStoppedPlayablesConnected = true;
		public bool keepStoppedPlayablesConnected
		{
			get { return m_KeepStoppedPlayablesConnected; }
			set
			{
				if (value != m_KeepStoppedPlayablesConnected)
				{
					m_KeepStoppedPlayablesConnected = value;
				}
			}
		}

		private bool m_Fading; //针对某个图层的淡入淡出
		private float m_FadeSpeed; //淡入淡出的速度
		private float m_TargetWeight;//目标权重
		private float m_Weight;
		private bool m_WeightDirty;
		public bool WeightDirty
		{
			get { return m_WeightDirty; }
			set { m_WeightDirty = value; }
		}

		void UpdateStoppedPlayablesConnections()
		{
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				AniStateInfo stateInfo = m_StateInfos[i];
				if (stateInfo == null)
				{
					continue;
				}
				if (stateInfo.enabled)
					continue;
				if (keepStoppedPlayablesConnected)
				{
					ConnectInput(stateInfo.index);
				}
				else
				{
					DisconnectInput(stateInfo.index);
				}
			}
		}

		protected Playable m_ActualPlayable;
		protected Playable self { get { return m_ActualPlayable; } }
		public Playable playable { get { return self; } }
		protected PlayableGraph graph
		{
			get { return self.GetGraph(); }
		}
		//动画混合器
		AnimationMixerPlayable m_AniMixer;
		//播放完成后的回调函数
		public System.Action onDone = null;
		public AnimationLayerPlayable()
		{
			this.m_StateInfos = new List<AniStateInfo>();
			this.m_StateQueue = new LinkedList<QueuedState>();
		}

		public Playable GetInput(int index)
		{
			if (index >= m_AniMixer.GetInputCount())
				return Playable.Null;

			return m_AniMixer.GetInput(index);
		}

		// Playable 创建后调用此函数
		public override void OnPlayableCreate(Playable playable)
		{
			m_ActualPlayable = playable;

			var mixer = AnimationMixerPlayable.Create(graph, 1, true);
			m_AniMixer = mixer;

			self.SetInputCount(1);
			self.SetInputWeight(0, 1);
			graph.Connect(m_AniMixer, 0, self, 0);
		}

		/// <summary>
		/// 获得所有动画信息集合
		/// </summary>
		/// <returns></returns>
		public List<AniStateInfo> StateInfos
		{
			get
			{
				return m_StateInfos;
			}
		}
		/// <summary>
		/// 根据动画名字获取一个动画信息
		/// </summary>
		/// <param name="name">动画名字</param>
		/// <returns></returns>
		public AniStateInfo GetStateInfo(string name)
		{
			AniStateInfo state = FindState(name);
			if (state == null)
			{
				return null;
			}
			return state;
		}

		//淡入到目标权重
		public void FadeTo(float targetWeight, float time)
		{
			float travel = Mathf.Abs(m_Weight - targetWeight);
			float newSpeed = time != 0f ? travel / time : Mathf.Infinity;

			// If we're fading to the same target as before but slower, assume CrossFade was called multiple times and ignore new speed
			if (m_Fading && Mathf.Approximately(targetWeight, m_TargetWeight) && newSpeed < m_FadeSpeed)
				return;

			m_Fading = Mathf.Abs(newSpeed) > 0f;
			m_FadeSpeed = newSpeed;
			m_TargetWeight = targetWeight;
		}

		//强制更新权重 并关闭淡入淡出
		public void ForceWeight(float weight)
		{
			m_TargetWeight = weight;
			m_Fading = false;
			m_FadeSpeed = 0f;
			SetWeight(weight);
		}

		//设置层的权重
		public void SetWeight(float weight)
		{
			m_Weight = weight;
			WeightDirty = true;
		}

		//层的权重
		public float GetWeight()
		{
			return m_Weight;
		}

		/// <summary>
		/// 创建一个新的动画信息
		/// </summary>
		/// <param name="name"></param>
		/// <param name="clip"></param>
		/// <returns></returns>
		private AniStateInfo DoAddClip(string name, AnimationClip clip)
		{
			AniStateInfo stateInfo = InsertState(); //Start new State
			stateInfo.Initialize(name, clip); 
			int index = stateInfo.index; //Find at which input the state will be connected

			//Increase input count if needed
			if (index == m_AniMixer.GetInputCount())
			{
				m_AniMixer.SetInputCount(index + 1);
			}

			var clipPlayable = AnimationClipPlayable.Create(graph, clip);
			clipPlayable.SetApplyFootIK(false);
			clipPlayable.SetApplyPlayableIK(false);
			
			// 注释该句， 当时间超出clip.length， 让其保持在最后一帧
			// if (clip.isLooping == false)
			// {
			// 	clipPlayable.SetDuration(clip.length); //设置最长的持续时间 
			// }
			stateInfo.SetPlayable(clipPlayable);
			stateInfo.Pause();

			if (keepStoppedPlayablesConnected)
			{
				ConnectInput(stateInfo.index);
			}


			return stateInfo;
		}

		public bool AddClip(AnimationClip clip, string name)
		{
			AniStateInfo state = FindState(name);
			if (state != null)
			{
				return false; //不允许重复添加
			}

			DoAddClip(name, clip);
			UpdateDoneStatus();
			InvalidateStates();

			return true;
		}

		public bool RemoveClip(string name)
		{
			AniStateInfo state = FindState(name);
			if (state == null)
			{
				Debug.LogError(string.Format("Cannot remove state with name {0}, because a state with that name doesn't exist", name));
				return false;
			}

			RemoveClones(state);
			InvalidateStates();
			RemoveState(state.index);
			return true;
		}

		public bool RemoveClip(AnimationClip clip)
		{
			InvalidateStates();
			bool removed = false;
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				AniStateInfo state = m_StateInfos[i];
				if (state != null && state.clip == clip)
				{
					RemoveState(i);
					removed = true;
				}
			}
			return removed;
		}

		public bool Play(string name)
		{
			AniStateInfo state = FindState(name);
			if (state == null)
			{
				Debug.LogError(string.Format("SimpleAnimationPlayable : Cannot play state with name {0} because there is no state with that name", name));
				return false;
			}

			return Play(state.index);
		}

		public bool Play(int index)
		{
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				AniStateInfo stateInfo = m_StateInfos[i];
				if (stateInfo == null)
				{
					continue;
				}
				if (stateInfo.index == index)
				{
					stateInfo.Enable();
					stateInfo.ForceWeight(1.0f);
				}
				else
				{
					DoStop(i);
				}
			}

			return true;
		}


		public bool PlayQueued(string name, QueueMode queueMode)
		{
			AniStateInfo state = FindState(name);
			if (state == null)
			{
				Debug.LogError(string.Format("Cannot queue Play to state with name {0} because there is no state with that name", name));
				return false;
			}

			return PlayQueued(state.index, queueMode);
		}

		bool PlayQueued(int index, QueueMode queueMode)
		{
			AniStateInfo newState = CloneState(index);

			if (queueMode == QueueMode.PlayNow)
			{
				Play(newState.index);
				return true;
			}

			m_StateQueue.AddLast(new QueuedState(newState, 0f));
			return true;
		}

		public void Rewind(string name)
		{
			AniStateInfo state = FindState(name);
			if (state == null)
			{
				Debug.LogError(string.Format("Cannot Rewind state with name {0} because there is no state with that name", name));
				return;
			}

			Rewind(state.index);
		}

		private void Rewind(int index)
		{
			SetStateTime(index, 0f);
		}

		public void Rewind()
		{
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				if (m_StateInfos[i] != null)
				{
					SetStateTime(i, 0f);
				}
			}
		}

		private void RemoveClones(AniStateInfo state)
		{
			var it = m_StateQueue.First;
			while (it != null)
			{
				var next = it.Next;
				AniStateInfo queuedState = m_StateInfos[it.Value.state.index];
				if (queuedState.parentState.index == state.index)
				{
					m_StateQueue.Remove(it);
					DoStop(queuedState.index);
				}
				it = next;
			}
		}

		public bool Stop(string name)
		{
			AniStateInfo state = FindState(name);
			if (state == null)
			{
				Debug.LogError(string.Format("Cannot stop state with name {0} because there is no state with that name", name));
				return false;
			}

			DoStop(state.index);

			UpdateDoneStatus();

			return true;
		}


		private void DoStop(int index)
		{
			AniStateInfo state = m_StateInfos[index];
			if (state == null)
			{
				return;
			}
			StopState(index, state.isClone);
			if (!state.isClone)
			{
				RemoveClones(state);
			}
		}

		public bool StopAll()
		{
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				DoStop(i);
			}
			if(playable.IsValid())
            {
				playable.SetDone(true);
            }

			return true;
		}

		public bool IsPlaying()
		{
			return m_StateInfos.FindIndex(s => s != null && s.enabled) != -1;
		}

		public bool IsPlaying(string stateName)
		{
			AniStateInfo state = FindState(stateName);
			if (state == null)
			{
				return false;
			}

			return state.enabled || IsClonePlaying(state);
		}

		/// <summary>
		/// 是否处于克隆播放
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		private bool IsClonePlaying(AniStateInfo state)
		{
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				AniStateInfo otherState = m_StateInfos[i];
				if (otherState == null)
					continue;

				if (otherState.isClone && otherState.enabled && otherState.parentState.index == state.index)
				{
					return true;
				}
			}

			return false;
		}

		public int GetClipCount()
		{
			int count = 0;
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				if (m_StateInfos[i] != null)
				{
					count++;
				}
			}
			return count;
		}

		private void SetupLerp(AniStateInfo state, float targetWeight, float time)
		{
			float travel = Mathf.Abs(state.weight - targetWeight);
			float newSpeed = time != 0f ? travel / time : Mathf.Infinity;

			// If we're fading to the same target as before but slower, assume CrossFade was called multiple times and ignore new speed
			if (state.fading && Mathf.Approximately(state.targetWeight, targetWeight) && newSpeed < state.fadeSpeed)
				return;

			state.FadeTo(targetWeight, newSpeed);
		}

		private bool Crossfade(int index, float time)
		{
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				AniStateInfo state = m_StateInfos[i];
				if (state == null)
				{
					continue;
				}

				if (state.index == index)
				{
					EnableState(index);   //先让这个动画处于播放状态
				}

				if (state.enabled == false)
					continue;

				float targetWeight = state.index == index ? 1.0f : 0.0f; //让权重从1渐变到0 或者 从0渐变到1
				SetupLerp(state, targetWeight, time);
			}

			return true;
		}

		private AniStateInfo CloneState(int index)
		{
			AniStateInfo original = m_StateInfos[index];
			string newName = original.stateName + "Queued Clone";
			AniStateInfo clone = DoAddClip(newName, original.clip);
			clone.SetAsCloneOf(original);
			return clone;
		}

		private bool OnlyOne(int index)
		{
			int num = 0;
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				AniStateInfo state = m_StateInfos[i];
				if (state == null)
				{
					continue;
				}

				if (state.enabled == false)
				{
					continue;
				}

				if (state.index != index)
				{
					++num;
				}
			}

			if (num > 0)
				return false;
			
			return true;
		}

		public bool Crossfade(string name, float time)
		{
			AniStateInfo state = FindState(name);
			if (state == null)
			{
				Debug.LogError(string.Format("Cannot crossfade to state with name {0} because there is no state with that name", name));
				return false;
			}

			if (time == 0f || OnlyOne(state.index))
			{
				return Play(state.index);
			}
			return Crossfade(state.index, time);
		}

		public bool CrossfadeQueued(string name, float time, QueueMode queueMode)
		{
			AniStateInfo stateInfo = FindState(name);
			if (stateInfo == null)
			{
				Debug.LogError(string.Format("Cannot queue crossfade to state with name {0} because there is no state with that name", name));
				return false;
			}

			return CrossfadeQueued(stateInfo.index, time, queueMode);
		}

		private bool CrossfadeQueued(int index, float time, QueueMode queueMode)
		{
			AniStateInfo newState = CloneState(index);

			if (queueMode == QueueMode.PlayNow)
			{
				Crossfade(newState.index, time);
				return true;
			}

			m_StateQueue.AddLast(new QueuedState(newState, time));
			return true;
		}

		private bool Blend(int index, float targetWeight, float time)
		{
			AniStateInfo state = m_StateInfos[index];
			if (state.enabled == false)
				EnableState(index);

			if (time == 0f)
			{
				state.ForceWeight(targetWeight);
			}
			else
			{
				SetupLerp(state, targetWeight, time);
			}

			return true;
		}

		public bool Blend(string name, float targetWeight, float time)
		{
			AniStateInfo state = FindState(name);
			if (state == null)
			{
				Debug.LogError(string.Format("Cannot blend state with name {0} because there is no state with that name", name));
				return false;
			}

			return Blend(state.index, targetWeight, time);
		}

		public override void OnGraphStop(Playable playable)
		{
			//if the playable is not valid, then we are destroying, and our children won't be valid either
			if (!self.IsValid())
				return;

			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				AniStateInfo state = m_StateInfos[i];
				if (state == null)
					continue;

				if (state.fadeSpeed == 0f && state.targetWeight == 0f)
				{
					Playable input = m_AniMixer.GetInput(state.index);
					if (!input.Equals(Playable.Null))
					{
						input.SetTime(0f);
					}
				}
			}
		}

		private void UpdateDoneStatus()
		{
			if (!IsPlaying())
			{
				bool wasDone = playable.IsDone();
				playable.SetDone(true);
				if (!wasDone && onDone != null)
				{
					onDone();
				}
			}

		}

		//清除所有clone出来的动画
		private void CleanClonedStates()
		{
			for (int i = m_StateInfos.Count - 1; i >= 0; i--)
			{
				AniStateInfo state = m_StateInfos[i];
				if (state == null)
				{
					continue;
				}
				if (state.isReadyForCleanup)
				{
					//Debug.Log("不可爱 Disconnect index:  " + state.index + " time: " + Time.time);
					Playable toDestroy = m_AniMixer.GetInput(state.index);
					graph.Disconnect(m_AniMixer, state.index);
					graph.DestroyPlayable(toDestroy);
					RemoveState(i);
				}
			}
		}

		private void DisconnectInput(int index)
		{
			if (keepStoppedPlayablesConnected)
			{
				m_StateInfos[index].Pause();
			}
			graph.Disconnect(m_AniMixer, index);
		}

		private void ConnectInput(int index)
		{
			AniStateInfo state = m_StateInfos[index];
			graph.Connect(state.playable, 0, m_AniMixer, state.index);
		}

		//更新动画层的权重
		private void UpdateFading(float deltaTime)
		{
			if (m_Fading)
			{
				float weight = Mathf.MoveTowards(m_Weight, m_TargetWeight, m_FadeSpeed * deltaTime);
				SetWeight(weight);
				//如果已达到目标权重相等
				if (Mathf.Approximately(m_Weight, m_TargetWeight))
				{
					ForceWeight(m_TargetWeight);
					if (m_Weight == 0f)
					{
						StopAll(); //权重为0是停止播放
					}
				}

			}
		}
		private void UpdateStates(float deltaTime)
		{
			bool mustUpdateWeights = false;
			float totalWeight = 0f;
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				AniStateInfo stateInfo = m_StateInfos[i];

				//Skip deleted states
				if (stateInfo == null)
				{
					continue;
				}

				//更新淡入淡出 
				if (stateInfo.fading)
				{
					stateInfo.SetWeight(Mathf.MoveTowards(stateInfo.weight, stateInfo.targetWeight, stateInfo.fadeSpeed * deltaTime));
					//如果已达到目标权重相等
					if (Mathf.Approximately(stateInfo.weight, stateInfo.targetWeight))
					{
						stateInfo.ForceWeight(stateInfo.targetWeight);
						if (stateInfo.weight == 0f)
						{
							stateInfo.Stop(); //权重为0是停止播放
						}
					}
				}
				//触发动画的播放停止
				if (stateInfo.enabledDirty)
				{
					if (stateInfo.enabled)
						stateInfo.Play();
					else
						stateInfo.Pause();

					if (false == keepStoppedPlayablesConnected)
					{
						Playable input = m_AniMixer.GetInput(i);
						//if state is disabled but the corresponding input is connected, disconnect it
						if (input.IsValid() && stateInfo.enabled == false)
						{
							DisconnectInput(i);
						}
						else if (stateInfo.enabled && !input.IsValid())
						{
							ConnectInput(stateInfo.index);
						}
					}
				}
				//停止一次性的动画
				if (stateInfo.enabled)
				{
					bool stateIsDone = stateInfo.isDone;
					float speed = stateInfo.speed;
					float time = stateInfo.GetTime();
					float duration = stateInfo.playableDuration;

					stateIsDone |= speed < 0f && time < 0f;
					stateIsDone |= speed >= 0f && time >= duration;
					if (stateIsDone)
					{
						stateInfo.Stop();
						stateInfo.Disable();
						if (!keepStoppedPlayablesConnected)
						{
							DisconnectInput(stateInfo.index);
						}
					}
				}

				totalWeight += stateInfo.weight;
				if (stateInfo.weightDirty)
				{
					mustUpdateWeights = true;
				}
				stateInfo.ResetDirtyFlags();
			}
			//更新权重信息
			if (mustUpdateWeights)
			{
				bool hasAnyWeight = totalWeight > 0.0f;
				for (int i = 0; i < m_StateInfos.Count; i++)
				{
					AniStateInfo state = m_StateInfos[i];
					if (state == null)
						continue;

					float weight = hasAnyWeight ? state.weight / totalWeight : 0.0f;
					m_AniMixer.SetInputWeight(state.index, weight);
				}
			}
		}

		private float CalculateQueueTimes()
		{
			float longestTime = -1f;

			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				AniStateInfo state = m_StateInfos[i];
				//Skip deleted states
				if (state == null || !state.enabled || !state.playable.IsValid())
					continue;

				if (state.isLoop)
				{
					return Mathf.Infinity;
				}

				float speed = state.speed;
				float stateTime = GetStateTime(state.index);
				float remainingTime;
				if (speed > 0)
				{
					remainingTime = (state.clip.length - stateTime) / speed;
				}
				else if (speed < 0)
				{
					remainingTime = (stateTime) / speed;
				}
				else
				{
					remainingTime = Mathf.Infinity;
				}

				if (remainingTime > longestTime)
				{
					longestTime = remainingTime;
				}
			}

			return longestTime;
		}

		private void ClearQueuedStates()
		{
			using (var it = m_StateQueue.GetEnumerator())
			{
				while (it.MoveNext())
				{
					QueuedState queuedState = it.Current;
					StopState(queuedState.state.index, true);
				}
			}
			m_StateQueue.Clear();
		}

		/// <summary>
		/// 更新播放队列
		/// </summary>
		private void UpdateQueuedStates()
		{
			bool mustCalculateQueueTimes = true;
			float remainingTime = -1f;

			var it = m_StateQueue.First;
			while (it != null)
			{
				if (mustCalculateQueueTimes)
				{
					remainingTime = CalculateQueueTimes();
					mustCalculateQueueTimes = false;
				}

				QueuedState queuedState = it.Value;
				//如果此时可以播放了
				if (queuedState.fadeTime >= remainingTime)
				{
					Crossfade(queuedState.state.index, queuedState.fadeTime);
					mustCalculateQueueTimes = true;
					m_StateQueue.RemoveFirst();
					it = m_StateQueue.First;
				}
				else
				{
					//跳过循环
					it = it.Next;
				}

			}
		}

		//设置播放时间的脏标记 
		void InvalidateStateTimes()
		{
			int count = m_StateInfos.Count;
			for (int i = 0; i < count; i++)
			{
				AniStateInfo state = m_StateInfos[i];
				if (state == null)
					continue;

				state.InvalidateTime();
			}
		}

		public override void PrepareFrame(Playable owner, FrameData data)
		{
			InvalidateStateTimes();

			UpdateQueuedStates();

			UpdateFading(data.deltaTime);

			UpdateStates(data.deltaTime);

			//Once everything is calculated, update done status
			UpdateDoneStatus();

			CleanClonedStates();
		}

		public bool ValidateInput(int index, Playable input)
		{
			if (!ValidateIndex(index))
				return false;

			AniStateInfo state = m_StateInfos[index];
			if (state == null || !state.playable.IsValid() || state.playable.GetHandle() != input.GetHandle())
				return false;

			return true;
		}

		public bool ValidateIndex(int index)
		{
			return index >= 0 && index < m_StateInfos.Count;
		}



		/////////////////////////////////////////////////////State区域////////////////////////////////////////////////////////////////
		private int m_StatesVersion = 0;
		private void InvalidateStates()
		{
			m_StatesVersion++;
		}

		//动画状态StateInfo的集合
		private List<AniStateInfo> m_StateInfos;
		private int m_StateCount;
		//动画信息个数
		public int StateCount
		{
			get { return m_StateCount; }
		}

		public AniStateInfo InsertState()
		{
			AniStateInfo state = new AniStateInfo();
			int insertIndex = -1;
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				if (m_StateInfos[i] == null)
				{
					insertIndex = i;
					m_StateInfos[i] = state;
					break;
				}
			}
			if (insertIndex == -1)
			{
				insertIndex = m_StateInfos.Count;
				m_StateInfos.Add(state);
				m_StateCount++;
			}
			state.index = insertIndex;

			return state;
		}

		public void RemoveState(int index)
		{
			AniStateInfo stateInfo = m_StateInfos[index];
			stateInfo.DestroyPlayable();

			m_StateInfos[index] = null;
			m_StateCount = m_StateInfos.Count;
		}


		public AniStateInfo FindState(string name)
		{
			int index = m_StateInfos.FindIndex(s => s != null && s.stateName == name);
			if (index == -1)
				return null;

			return m_StateInfos[index];
		}

		public void EnableState(int index)
		{
			AniStateInfo state = m_StateInfos[index];
			state.Enable();
		}

		public void DisableState(int index)
		{
			AniStateInfo state = m_StateInfos[index];
			state.Disable();
		}

		public void StopState(int index, bool cleanup)
		{
			if (cleanup)
			{
				RemoveState(index);
			}
			else
			{
				m_StateInfos[index].Stop();
			}
		}

		public void SetInputWeight(int index, float weight)
		{
			AniStateInfo state = m_StateInfos[index];
			state.SetWeight(weight);
		}

		public float GetInputWeight(int index)
		{
			return m_StateInfos[index].weight;
		}

		public void SetStateTime(int index, float time)
		{
			AniStateInfo state = m_StateInfos[index];
			state.SetTime(time);
		}

		public float GetStateTime(int index)
		{
			AniStateInfo state = m_StateInfos[index];
			return state.GetTime();
		}

		public void SetStateSpeed(float value)
		{
			for (int i = 0; i < m_StateInfos.Count; i++)
			{
				SetStateSpeed(i, value);
			}
		}

		public float GetStateSpeed(int index)
		{
			return m_StateInfos[index].speed;
		}

		public void SetStateSpeed(int index, float value)
		{
			m_StateInfos[index].speed = value;
		}

		public float GetStateLength(int index)
		{
			AnimationClip clip = m_StateInfos[index].clip;
			if (clip == null)
			{
				return 0f;
			}
			float speed = m_StateInfos[index].speed;
			if (speed == 0f)
				return Mathf.Infinity;

			return clip.length / speed;
		}

		public AnimationClip GetStateClip(int index)
		{
			return m_StateInfos[index].clip;
		}

		public float GetClipLength(int index)
		{
			AnimationClip clip = m_StateInfos[index].clip;
			if (clip == null)
				return 0f;
			return clip.length;
		}


		public float GetStatePlayableDuration(int index)
		{
			return m_StateInfos[index].playableDuration;
		}

		public string GetStateName(int index)
		{
			return m_StateInfos[index].stateName;
		}

		public void SetStateName(int index, string name)
		{
			m_StateInfos[index].stateName = name;
		}

		public bool IsCloneOf(int potentialCloneIndex, int originalIndex)
		{
			AniStateInfo potentialClone = m_StateInfos[potentialCloneIndex];
			return potentialClone.isClone && potentialClone.parentState.index == originalIndex;
		}

	}
}
