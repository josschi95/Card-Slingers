using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardSO : ScriptableObject
{
    new public string name;
    public Sprite icon;
    public CardType type;
    public Faction faction;
    public int cost;
    [Space]
    [TextArea(3,5)]
    public string description;
    [TextArea(3, 5)]
    public string flavorText;
}