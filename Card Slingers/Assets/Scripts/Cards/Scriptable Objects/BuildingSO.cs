using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "Scriptable Objects/Cards/Building")]
public class BuildingSO : CardSO
{
    private void Reset()
    {
        type = CardType.Building;
    }

    [Header("Building Properties")]
    public int maxHealth;
    public bool canBeOccupied = true;
}
