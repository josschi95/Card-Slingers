using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card_Spell : Card
{
    private SpellSO _spellInfo;

    public Card_Spell(SpellSO spell, bool isPlayerCard) : base (spell, isPlayerCard)
    {
        _cardInfo = spell;
        _spellInfo = spell;
        this.isPlayerCard = isPlayerCard;
    }

    public int Range => _spellInfo.Range;
    public Vector3 StartPos => _spellInfo.StartPos;
    public ParticleSystem FX => _spellInfo.SpellFX;
    public EffectHolder[] Effects => _spellInfo.SpellEffects;

}
