using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "Scriptable Objects/Cards/Permanents/Structure")]
public class StructureSO : PermanentSO
{
    private void Reset()
    {
        type = CardType.Structure;
    }

    [Header("Structure Properties")]
    [SerializeField] private CardFocus _focus;
    [SerializeField] private int _maxHealth;
    [SerializeField] private int _defense = 1;
    [Tooltip("if true: an allied unit is able to move into an occupy the structure")]
    [SerializeField] private bool _canBeOccupied = false; //Likely going to remove the ability to occupy structures entirely
    [Tooltip("if true: an allied unit is able to move through the space of this structure")]
    [SerializeField] private bool _canBeTraversed = true;

    public CardFocus Focus => _focus;
    public int MaxHealth => _maxHealth;
    public int Defense => _defense;
    public bool canBeOccupied => _canBeOccupied;
    public bool CanBeTraversed => _canBeTraversed;
}
