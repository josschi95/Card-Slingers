using UnityEngine;

[CreateAssetMenu(fileName = "New Commander", menuName = "Scriptable Objects/Cards/Commander")]
public class CommanderSO : UnitSO
{
    private void Reset()
    {
        type = CardType.Commander;
    }

    [Header("Commander Properties")]
    [SerializeField] private int _summoningRange = 3;
    [SerializeField] private Deck _deck;

    public int SummonRange => _summoningRange;
    public Deck Deck => _deck;

    [Space]

    [SerializeField] private bool _playerCommander;

    public CommanderController SpawnCommander(GridNode startNode)
    {
        var commanderCard = new Card_Commander(this, _playerCommander); //creates the card
        var commander = commanderCard.OnCommanderCreated(_playerCommander, startNode); //spawns physical body and assigns components
        return commander;
    }
}