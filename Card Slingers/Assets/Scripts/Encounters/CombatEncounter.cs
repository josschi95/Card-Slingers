using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Combat Encounter", menuName = "Scriptable Objects/Encounters/Generic")]
public class CombatEncounter : ScriptableObject
{
    [SerializeField] private Vector2Int _battlefieldDimensions;
    public Vector2Int Dimensions => _battlefieldDimensions;

    public virtual void OnCombatTriggered()
    {
        //Meant to be overriden
        //Instantiate all of the enemies, and place them in their appropriate spots
    }
}
