using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds methods to be called by Events in the animator
/// </summary>
public class AnimationEventHandler : MonoBehaviour
{
    private Card_Unit unit;

    private void Start()
    {
        unit = GetComponentInParent<Card_Unit>();
    }

    public void OnAttackAnimationTrigger() => unit.OnAttackAnimationTrigger();
   
    public void OnAbilityAnimationTrigger() => unit.onAbilityAnimation?.Invoke();

    public void OnDeathAnimationCompleted() => unit.OnUnitDeathAnimationComplete();

    public void OnUnsummon() => GameManager.instance.GetUnsummonParticles(transform.position);
}
