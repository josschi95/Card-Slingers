using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationEventHandler : MonoBehaviour
{
    private Card_Unit unit;
    private void Start()
    {
        unit = GetComponentInParent<Card_Unit>();
    }

    public void OnAttackAnimationTrigger()
    {
        unit.OnAttackAnimationTrigger();
    }

    public void OnDeathAnimationCompleted()
    {
        unit.OnDeathAnimCompleted();
    }
}
