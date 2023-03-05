using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//I need to make this not inherit from OpponentCommander nor Commander, there's just so little that it actually uses
//This also means not comparing permanent commanders to determine if they're the same team
//For right now, a simple bool _playerOwned will do
public class MonsterManager : OpponentCommander
{
    private MonsterEncounter _encounter;

    private List<MonsterController> _monsters;

    //gives a weighted chance for the number of monsters in a match to lean towards 4
    //Will likely need a cleaner and more modifiable way to do this in the future
    private int[] monsterCount = { 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 6, 6 };
    //I'd say it should depend on the dungeon as well, some value that's held in the dungeon manager
    //take into account... board size, the dungeon, and the level. 2 should be rare in all, uncommon in first floors

    //No CommanderSO _commanderInfo
    //No Card_Commander _commanderCard
    //No int _currentMana
    //No _cardsInHand, _cardsInDeck, _cardsInDiscard, _cardsInExile

    //Yes _permanentsOnField
    //Yes isTurn

    protected override void Start()
    {
        duelManager = DuelManager.instance;
    }

    #region - Match Start -
    public void OnNewMatchStart(MonsterEncounter encounter)
    {
        currentPhase = Phase.Begin;
        isTurn = false;

        _permanentsOnField = new List<Card_Permanent>();
        _monsters = new List<MonsterController>();
        SubscribeToMatchEvents();

        _encounter = encounter;
        SelectMonstersFromPool();
    }

    private void SelectMonstersFromPool()
    {
        int num = monsterCount[Random.Range(0, monsterCount.Length)];

        for (int i = 0; i < num; i++)
        {
            //Grab a random card from the pool
            var card = _encounter.MonsterPool[Random.Range(0, _encounter.MonsterPool.Length)];

            Card newCard = Instantiate(card.cardPrefab);
            newCard.AssignCardInfo(card, false);
            PlaceMonstersAtStart(newCard as Card_Unit);
        }
    }

    private void PlaceMonstersAtStart(Card_Unit unit)
    {
        _permanentsOnField.Add(unit);
        unit.SetCardLocation(CardLocation.OnField);

        //Get node from Battlefield
        var nodes = duelManager.Battlefield.GetSummonableNodes(false);

        var node = GetNode(nodes);
        if (node == null) return;

        unit.OnSummoned(node);
        unit.onPermanentDestroyed += OnPermanentDestroyed;
        unit.onRemovedFromField += DestroyPermanent;

        var controller = unit.gameObject.AddComponent<MonsterController>();
        _monsters.Add(controller);

        var rot = duelManager.Battlefield.Center.eulerAngles;
        rot.y -= 180;
        unit.transform.eulerAngles = rot;
    }

    private GridNode GetNode(List<GridNode> nodes)
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i].Obstacle != null) nodes.RemoveAt(i);
            else if (nodes[i].Occupant != null) nodes.RemoveAt(i);
        }

        if (nodes.Count == 0) return null;

        //int halfDepth = Mathf.RoundToInt(duelManager.Battlefield.Depth * 0.5f);

        //Right now I'm just going to choose to place monsters in the front row; however, moving forward I should add a "preferred distance" and use that to set the row
        /*for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i].gridZ != halfDepth) nodes.RemoveAt(i);
        }*/

        return nodes[Random.Range(0, nodes.Count)];
    }
    #endregion

    #region - Phases -
    protected override void OnBeginPhase()
    {
        //For each card on the field, invoke an OnBeginPhase event
        for (int i = 0; i < _permanentsOnField.Count; i++)
        {
            _permanentsOnField[i].OnBeginPhase();
        }

        duelManager.OnCurrentPhaseFinished();
    }

    protected override void OnSummoningPhase()
    {
        //Cannot summon, no cards
        duelManager.OnCurrentPhaseFinished();
    }

    protected override void OnAttackPhase()
    {
        if (duelManager.TurnCount == 1) duelManager.OnCurrentPhaseFinished();
        else StartCoroutine(HandleMonsterActions());
    }

    protected override void OnEndPhase()
    {
        //Nothing to do on end phase
        duelManager.OnCurrentPhaseFinished();
    }
    #endregion

    //Down the line I should probably switch this from a single master controller to delegating the decisions to the monsters
    //That will allow for a bit more versatility and variety in how they act
    private IEnumerator HandleMonsterActions()
    {
        for (int i = 0; i < _monsters.Count; i++)
        {
            _monsters[i].PrioritizeTargets();
            _monsters[i].SelectAction();

            while (!duelManager.CanDeclareNewAction()) yield return null;
            //while (_monsters[i].unit.IsActing) yield return null;
            yield return new WaitForSeconds(0.1f);
        }

        while (!duelManager.CanDeclareNewAction()) yield return null;
        //yield return new WaitForSeconds(0.5f);

        //A unit did not decalre an action because all movement was blocked by another unit
        //Circle back to them and try to move again
        for (int i = 0; i < _monsters.Count; i++)
        {
            var unit = _monsters[i].unit;
            if (unit.HasActed && unit.MovesLeft <= 0) continue; //There is nothing more that the unit can do this turn

            _monsters[i].PrioritizeTargets();
            _monsters[i].SelectAction();

            while (!duelManager.CanDeclareNewAction()) yield return null;
            yield return new WaitForSeconds(0.1f);
        }

        duelManager.OnCurrentPhaseFinished(); //End phase
    }

    private void OnPermanentDestroyed(Card_Permanent permanent)
    {
        permanent.onPermanentDestroyed -= OnPermanentDestroyed;

        //Remove from list
        _permanentsOnField.Remove(permanent);

        if (permanent.TryGetComponent(out MonsterController monster)) _monsters.Remove(monster);
    }

    private void DestroyPermanent(Card_Permanent card)
    {
        card.onRemovedFromField -= DestroyPermanent;
        Destroy(card.gameObject);

        //All monsters on the field have been defeated, player victory
        if (_permanentsOnField.Count == 0) DuelManager.instance.onPlayerVictory?.Invoke();
    }

    protected override void OnPlayerVictory()
    {
        //Base OpponentCommander script destroys the gameObject on death. Don't do that.
        MatcheEnd();
    }

    public void ClearEncounter()
    {
        _encounter = null;

        //Destroy all cards.... 
    }
}
