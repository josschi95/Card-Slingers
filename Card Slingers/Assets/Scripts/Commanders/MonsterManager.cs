using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : OpponentCommander
{
    private MonsterEncounter _encounter;

    public PermanentSO[] startingPermanents;

    //gives a weighted chance for the number of monsters in a match to lean towards 4
    //Will likely need a cleaner and more modifiable way to do this in the future
    private int[] monsterCount = { 2, 2, 3, 3, 3, 4, 4, 4, 4, 5, 5, 5, 6, 6 };

    //No CommanderSO _commanderInfo
    //No Card_Commander _commanderCard
    //No int _currentMana
    //No _cardsInHand, _cardsInDeck, _cardsInDiscard, _cardsInExile

    //Yes _permanentsOnField
    //Yes isTurn

    protected override void Start()
    {
        duelManager = DuelManager.instance;
        onPermanentDestroyed += OnPermanentDestroyed;
    }

    public override void OnAssignCommander(CommanderSO commanderInfo)
    {
        //Do Nothing, these variables don't exist
    }

    public void OnNewMatchStart(MonsterEncounter encounter)
    {
        _permanentsOnField = new List<Card_Permanent>();
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
            newCard.AssignCard(card, this);
            PlaceMonstersAtStart(newCard as Card_Unit);
        }
    }

    private void PlaceMonstersAtStart(Card_Unit unit)
    {
        _permanentsOnField.Add(unit);
        unit.SetCardLocation(CardLocation.OnField);

        //Get node from Battlefield
        var nodes = duelManager.Battlefield.GetSummonableNodes(this);


        var node = GetNode(nodes);
        if (node == null) return;

        unit.OnSummoned(node);
        var rot = duelManager.Battlefield.Center.eulerAngles;
        rot.y -= 180;
        unit.transform.eulerAngles = rot;
    }

    private GridNode GetNode(List<GridNode> nodes)
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i].Occupant != null) nodes.RemoveAt(i);
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

    protected override void OnBeginPhase()
    {
        //For each card on the field, invoke an OnBeginPhase event
        onNewPhase?.Invoke(Phase.Begin);

        duelManager.OnCurrentPhaseFinished();
    }

    protected override void OnSummoningPhase()
    {
        duelManager.OnCurrentPhaseFinished();
    }

    protected override void OnAttackPhase()
    {
        if (duelManager.TurnCount == 1) duelManager.OnCurrentPhaseFinished();
        else
        {
            //Run through all permanents on field
        }
    }

    protected override void OnEndPhase()
    {
        duelManager.OnCurrentPhaseFinished();
    }

    private void OnPermanentDestroyed(Card_Permanent permanent)
    {
        //Trigger any exit effects
        permanent.OnRemoveFromField();

        //Remove from list
        _permanentsOnField.Remove(permanent);

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
