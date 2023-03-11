using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorParentRootMotion : StateMachineBehaviour
{
    private Transform _transform;
    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.applyRootMotion = true;
        if (_transform == null)
        {
            if (animator.transform.parent != null) _transform = animator.transform.parent;
            else _transform = animator.transform;
        }
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {

    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        animator.applyRootMotion = false;
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        _transform.rotation = animator.rootRotation;
        _transform.position += animator.deltaPosition;

        // Implement code that processes and affects root motion
        /*if (animator.transform.parent != null)
        {
            animator.transform.parent.rotation = animator.rootRotation;
            animator.transform.parent.position += animator.deltaPosition;
        }
        else
        {
            animator.transform.rotation = animator.rootRotation;
            animator.transform.position += animator.deltaPosition;
        }*/
    }

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}
