using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "Scriptable Objects/Cards/Permanents/Structure")]
public class StructureSO : PermanentSO
{
    private void Reset()
    {
        type = CardType.Structure;
    }

    [Header("Structure Properties")]
    [SerializeField] private int _maxHealth;
    [SerializeField] private int _defense = 1;
    [SerializeField] private bool _canBeOccupied = true;

    public int MaxHealth => _maxHealth;
    public int Defense => _defense;
    public bool canBeOccupied => _canBeOccupied;
}
