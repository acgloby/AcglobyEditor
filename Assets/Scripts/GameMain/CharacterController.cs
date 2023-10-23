using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterController : MonoBehaviour
{
    private Animator animator;
    private AniamtorSkill aniamtorSkill;

    private float x;
    private float y;
    private bool isAccelerate;
    private bool isMove;


    void Start()
    {
        animator = GetComponent<Animator>();
        aniamtorSkill = GetComponent<AniamtorSkill>();
    }

    void Update()
    {
        x = Input.GetAxis("Horizontal");
        y = Input.GetAxis("Vertical");
        animator.SetFloat("x", x);
        animator.SetFloat("y", y);

        isMove = Mathf.Abs(x) + Mathf.Abs(y) > 0.01f;
        animator.SetBool("move", isMove);

        if (!isMove && isAccelerate)
        {
            animator.SetFloat("stop", 1);
        }
        else
        {
            animator.SetFloat("stop", 0);
        }


        isAccelerate = Input.GetKey(KeyCode.LeftShift);
        animator.SetFloat("accelerate", isAccelerate ? 1 : 0);
    }
}
