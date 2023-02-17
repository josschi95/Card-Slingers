using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Trap", menuName = "Scriptable Objects/Cards/Permanents/Trap")]
public class TrapSO : PermanentSO
{
    [Header("Trap Properties")]
    [SerializeField] private EffectHolder[] _trapEffects;
    public EffectHolder[] Effects => _trapEffects;
}
