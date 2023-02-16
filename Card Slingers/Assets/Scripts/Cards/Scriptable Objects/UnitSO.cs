using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "Scriptable Objects/Cards/Permanents/Unit")]
public class UnitSO : PermanentSO
{
    private void Reset()
    {
        type = CardType.Unit;
    }

    [Header("Unit Properties")]
    [SerializeField] private int _maxHealth = 5;
    [SerializeField] private int _attack = 1;
    [SerializeField] private int _range = 1;
    [SerializeField] private int _defense = 0;
    [SerializeField] private int _speed = 1;
    
    public int MaxHealth => _maxHealth;
    public int Attack => _attack;
    public int Range => _range;
    public int Defense => _defense;
    public int Speed => _speed;
}
