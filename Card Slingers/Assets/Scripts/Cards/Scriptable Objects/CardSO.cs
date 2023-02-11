using UnityEngine;

public class CardSO : ScriptableObject
{
    new public string name;
    public GameObject cardPrefab; //this will possibly later change to a string for the objectPooler
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