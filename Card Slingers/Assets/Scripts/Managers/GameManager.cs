using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region - Singleton -
    public static GameManager instance;
    private void Awake()
    {
        instance = this;
    }
    #endregion
}
public enum Phase { Begin, Summoning, Declaration, Resolution, End } 
public enum CardType { Unit, Building, Trap, Equipment, Terrain, Spell }
public enum Faction { Arcane, Kingdom, Goblins, Coven, Undead, Demons}