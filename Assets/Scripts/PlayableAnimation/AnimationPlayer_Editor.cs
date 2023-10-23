using System;
using System.Collections.Generic;
using GameFramework;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
namespace ProjectS
{
	[RequireComponent(typeof(Animator))]
	public partial class AnimationPlayer : MonoBehaviour, IAnimationClipSource
	{
		/// <summary>
		/// 动画片段的编辑状态
		/// </summary>
		[System.Serializable]
		public class EditorState
		{
			public AnimationClip clip;
			public string name;
			public EditorState()
			{

			}
		}

		[System.Serializable]
		public class EditorStateManager
		{
			public EditorState[] states;
		}

		/// <summary>
		/// 默认动画片段 一般在编辑器下才用到
		/// </summary>
		[SerializeField]

		protected AnimationClip m_Clip;


		/// <summary>
		/// 动画片段编辑状态数组
		/// </summary>
		[SerializeField]
		private List<EditorStateManager> m_StatesList = new List<EditorStateManager>();

		/// <summary>
		/// 初始化EditorStates
		/// </summary>
		/// <param name="states"></param>
		protected void InitializeEditorStates(ref EditorState[] editorStates, ref AnimationLayerPlayable playable)
		{
			if (editorStates != null)
			{
				foreach (var editorState in editorStates)
				{
					if (editorState.clip)
					{
						playable.AddClip(editorState.clip, editorState.name);
					}
				}
			}
		}

		/*private void EnsureDefaultStateExists()
		{
			if ( m_Playable != null && m_Clip != null && m_Playable.GetState(kDefaultStateName) == null )
			{
				m_Playable.AddClip(m_Clip, kDefaultStateName);
				Kick();
			}
		}*/

		//更新editorstates
		private void RebuildStates(int layerIndex = 0)
		{
#if !UNITY_EDITOR
			return;
#endif
			var playableStates = GetStateInfos(layerIndex);
			var list = new List<EditorState>();
			foreach (var stateInfo in playableStates)
			{
				if(stateInfo==null)
				{
					continue;
				}
				var newState = new EditorState();
				newState.clip = stateInfo.clip;
				newState.name = stateInfo.stateName;
				list.Add(newState);
			}
			m_StatesList[layerIndex].states = list.ToArray();
		}

        EditorState CreateDefaultEditorState()
        {
            var defaultState = new EditorState();
            defaultState.name = m_Clip.name;
            defaultState.clip = m_Clip;
            return defaultState;
        }

        protected bool LegacyClipCheck(AnimationClip clip)
		{
			if (clip && clip.legacy)
			{
				string objName = "null";
				if(gameObject!=null)
				{
					objName = gameObject.transform.parent == null ? gameObject.name : gameObject.transform.parent.name;
				}
				throw new Exception(string.Format("LegacyClip: {0} cannot be used in AnimationPlayer  GameObject: {1}", clip.name, objName));
				return true;
			}
			return false;
		}

		void InvalidLegacyClipError(string clipName, string stateName)
		{
			Debug.LogErrorFormat(this.gameObject, "Animation clip {0} in state {1} is Legacy. Set clip.legacy to false, or reimport as Generic to use it with SimpleAnimationComponent", clipName, stateName);
		}

		private void OnValidate()
		{
			//Don't mess with runtime data
			if (Application.isPlaying || m_StatesList == null)
			{
				return;
			}

			//         if (m_Clip && m_Clip.legacy)
			//         {
			//             Debug.LogErrorFormat(this.gameObject,"Animation clip {0} is Legacy. Set clip.legacy to false, or reimport as Generic to use it with SimpleAnimationComponent", m_Clip.name);
			//             m_Clip = null;
			//         }


			if (m_LayerStates != null)
			{
				for (int i = 0; i < m_LayerNum; i++)
				{
					AnimationLayerPlayable layerPlayer = m_LayerStates[i];
					if (m_StatesList.Count > i)
					{
						UpdateEditorStates(ref m_StatesList[i].states, i);
					}
				
					InitializeEditorStates(ref m_StatesList[i].states, ref layerPlayer);
				}
			}

			m_Animator = GetComponent<Animator>();
			m_Animator.updateMode = m_AnimatePhysics ? AnimatorUpdateMode.AnimatePhysics : AnimatorUpdateMode.Normal;
			m_Animator.cullingMode = m_CullingMode;
		}

		public void UpdateEditorStates(ref EditorState[] editorStates, int layer)
		{
			if(editorStates==null)
			{
				return;
			}
			if (layer == 0)
			{
				if (m_Clip != null && LegacyClipCheck(m_Clip))
				{
					m_Clip = null;
				}
					if (m_Clip != null )
				{

					bool isExist = false;
					for (int j = 0; j < editorStates.Length; j++)
					{
						if (editorStates[j] != null && editorStates[j].clip.Equals(m_Clip))
						{
							isExist = true;
							break;
						}
					}
					if (!isExist)
					{
						var oldArray = editorStates;
						var len = oldArray.Length + 1;

						editorStates = new EditorState[len];
						Array.Copy(oldArray, editorStates, len - 1);
						editorStates[len - 1] = CreateDefaultEditorState();
					}
				}
			}
		
			//Ensure state names are unique
			int stateCount = editorStates.Length;
			string[] names = new string[stateCount];

			for (int j = 0; j < stateCount; j++)
			{
				EditorState editorState = editorStates[j];
				if (editorState == null)
				{
					continue;
				}

				if (editorState.name == "" && editorState.clip)
				{
					editorState.name = editorState.clip.name;
				}

#if UNITY_EDITOR
				editorState.name = ObjectNames.GetUniqueName(names, editorState.name); //防止重名
#endif
				names[j] = editorState.name;

				if (editorState.clip && editorState.clip.legacy)
				{
					InvalidLegacyClipError(editorState.clip.name, editorState.name);
					editorState.clip = null;
				}
			}
		}

		public void GetAnimationClips(List<AnimationClip> results)
		{
			for (int i = 0; i < m_StatesList.Count; i++)
			{
				var m_States = m_StatesList[i].states;
				foreach (var state in m_States)
				{
					if (state.clip != null)
					{
						results.Add(state.clip);
					}
				}
			}
		}
	}

}
