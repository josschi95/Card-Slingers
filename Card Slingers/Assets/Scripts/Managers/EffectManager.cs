using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Effect Manager", menuName = "Scriptable Objects/Effect Manager")]
public class EffectManager : ScriptableObject
{
    public void OnApplyEffect(Card_Permanent target, Effects effect, int magnitude = 1, UnitStat stat = UnitStat.Health)
    {
        switch (effect)
        {
            case Effects.Damage:
                target.OnTakeDamage(magnitude);
                break;
            case Effects.Halt:
                if (target is Card_Unit targetUnit)
                {
                    targetUnit.OnHalt();
                }
                break;
            case Effects.StatModifier:
                if (target is Card_Unit unit)
                {
                    unit.AddModifier(stat, magnitude);
                }
                break;
        }
    }
}

[System.Serializable]
public class EffectHolder
{
    public Effects effect;
    public int magnitude;
    [Space]
    public UnitStat modifiedStat;

    public EffectHolder(Effects effect, int m = 1, UnitStat stat = UnitStat.Health)
    {
        this.effect = effect;
        magnitude = m;

        modifiedStat = stat;
    }
}