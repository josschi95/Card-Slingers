using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Spell : Card
{
    public int Range
    {
        get
        {
            var info = CardInfo as SpellSO;
            return info.Range;
        }
    }

    public EffectHolder[] Effects
    {
        get
        {
            var info = CardInfo as SpellSO;
            return info.SpellEffects;
        }
    }

    public ParticleSystem FX
    {
        get
        {
            var info = CardInfo as SpellSO;
            return info.SpellFX;
        }
    }

    public Vector3 StartPos
    {
        get
        {
            var info = CardInfo as SpellSO;
            return info.StartPos;
        }
    }
}
