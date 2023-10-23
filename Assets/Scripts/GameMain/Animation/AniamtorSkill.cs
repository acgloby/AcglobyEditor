using System;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class AniamtorSkill : MonoBehaviour
{
    [Serializable]
    public class ActionData
    {
        public string Name;
        public AnimationClip Clip;
        public float FateValue;
    }

    public ActionData[] ActionDatas;
    private Animator animator;
    private string[] stateNames = new[] {"Action1", "Action2"};
    private int curStateIndex = 0;

    private AnimatorOverrideController overrideController;

    void Start()
    {
        animator = GetComponent<Animator>();
        overrideController = new AnimatorOverrideController();
        overrideController.runtimeAnimatorController = animator.runtimeAnimatorController;
    }

    void Update()
    {

    }

    public void Play(string actionName)
    {
        var actionData = FindAciton(actionName);
        if(actionData == null)
            return;
        var stateName = stateNames[curStateIndex];
        overrideController[stateName] = actionData.Clip;
        animator.runtimeAnimatorController = overrideController;
        RefreshIndex();
        animator.SetLayerWeight(1, 1);
        animator.CrossFade(stateName, actionData.FateValue, 1);
    }

    public void RefreshIndex()
    {
        curStateIndex = curStateIndex == 1 ? 0 : 1;
    }

    public ActionData FindAciton(string actionName)
    {
        for (int i = 0; i < ActionDatas.Length; i++)
        {
            if(ActionDatas[i].Name != actionName)
                continue;
            return ActionDatas[i];
        }

        return null;
    }

    public void OnGUI()
    {
        for (int i = 0; i < stateNames.Length; i++)
        {
            var dis = 10 + i * 55;
            var click = GUI.Button(new Rect(new Vector2(Screen.width - 150, dis), new Vector2(150, 50)), ActionDatas[i].Name);
            if (click)
            {
                Play(ActionDatas[i].Name);
            }
        }
    }

}

