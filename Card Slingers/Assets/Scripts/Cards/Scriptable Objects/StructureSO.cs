using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "Scriptable Objects/Cards/Structure")]
public class StructureSO : CardSO
{
    private void Reset()
    {
        type = CardType.Structure;
    }

    [Header("Structure Properties")]
    public int maxHealth;
    public bool canBeOccupied = true;
}
