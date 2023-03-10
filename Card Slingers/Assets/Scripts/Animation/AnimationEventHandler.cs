using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Holds methods to be called by Events in the animator
/// </summary>
public class AnimationEventHandler : MonoBehaviour
{
    private Card_Unit _unit;
    public Card_Unit Unit
    {
        get => _unit;
        set
        {
            _unit = value;
        }
    }

    public void OnAttackAnimationTrigger() => _unit.OnAttackAnimationTrigger();
   
    public void OnAbilityAnimationTrigger() => _unit.onAbilityAnimation?.Invoke();

    public void OnDeathAnimationCompleted()
    {
        if (_unit is Card_Commander comm) comm.OnCommanderDeath();
        else StartCoroutine(_unit.OnRemoveUnit());
    }

    public void OnUnsummon()
    {
        GameManager.instance.GetUnsummonParticles(transform.position);
        StartCoroutine(_unit.OnRemoveUnit(false));
    }
}
