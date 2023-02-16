using UnityEngine;

public class CardSO : ScriptableObject
{
    [Header("Base Card Properties")]
    new public string name;
    public Card cardPrefab;
    [Space]
    public int cost;
    public Sprite icon;
    public CardType type;
    public Faction faction;
    [Space]
    [TextArea(3,5)]
    public string description;
    [TextArea(3, 5)]
    public string flavorText;
}