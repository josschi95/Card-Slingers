using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Spell", menuName = "Scriptable Objects/Cards/Instants/Spell")]
public class SpellSO : CardSO
{
    [Header("Spell Properties")]
    [SerializeField] private int _range = 2;
    
    public int Range => _range;
}
