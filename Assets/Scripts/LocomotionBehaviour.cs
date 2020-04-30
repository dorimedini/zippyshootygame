using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocomotionBehaviour : StateMachineBehaviour
{
    private const float baseWalkPitch = 1;

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<AudioSource>().Play();
        UpdateWalkSound(animator, stateInfo);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        UpdateWalkSound(animator, stateInfo);
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.GetComponent<AudioSource>().Stop();
    }

    void UpdateWalkSound(Animator animator, AnimatorStateInfo stateInfo)
    {
        // Play the walk sound at a speed proportional to the move speed, capped at 1 movespeed (so we don't get a volume burst when going
        // diagonally)
        animator.GetComponent<AudioSource>().transform.position = animator.transform.position;
        float pitch = baseWalkPitch * Mathf.Min(1, (new Vector3(animator.GetFloat("FwdBack"), 0, animator.GetFloat("LeftRight"))).magnitude);
        animator.GetComponent<AudioSource>().pitch = pitch;
    }
}
