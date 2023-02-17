using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Scriptable Objects/Cards/Instants/Spell")]
public class SpellSO : CardSO
{
    [Header("Spell Properties")]
    [SerializeField] private ParticleSystem _spellFX;
    [SerializeField] private int _range = 2;
    [Space]
    [SerializeField] private EffectHolder[] _spellEffects;

    public ParticleSystem SpellFX => _spellFX;
    public int Range => _range;
    public EffectHolder[] SpellEffects => _spellEffects;
}
