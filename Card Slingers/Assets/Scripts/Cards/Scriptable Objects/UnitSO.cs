using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "Scriptable Objects/Cards/Unit")]
public class UnitSO : PermanentSO
{
    private void Reset()
    {
        type = CardType.Unit;
    }

    [Header("Unit Properties")]
    public int maxHealth;
    public int attack;
    public int defense;
    public int speed;
}
