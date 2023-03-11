using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    private DuelManager duelManager;

    private MonsterEncounter _encounter;

    protected List<Card_Permanent> _permanentsOnField;
    private List<MonsterController> _monsters;
    private bool isTurn;
    protected Quaternion _defaultRotation;

    //gives a weighted chance for the number of monsters in a match to lean towards 4
    //Will likely need a cleaner and more modifiable way to do this in the future
    //I'd say it should depend on the dungeon as well, some value that's held in the dungeon manager
    //take into account... board size, the dungeon, and the level. 2 should be rare in all, uncommon in first floors
    private int[] monsterCount = { 2, 3, 3, 4, 4, 4, 4, 5, 5, 5, 6, 6 };
    public Quaternion DefaultRotation
    {
        get => _defaultRotation;
        set
        {
            _defaultRotation = value;
        }
    }



    private void Start()
    {
        duelManager = DuelManager.instance;
    }

    #region - Match Start -
    public void OnMatchStart(MonsterGroupManager monsters)
    {
        isTurn = false;

        _permanentsOnField = new List<Card_Permanent>();
        _monsters = new List<MonsterController>();

        SubscribeToMatchEvents();

        Debug.LogError("Need to grab monsters");
    }

    public void OnMatchStart(MonsterEncounter encounter, Quaternion defaultRotation)
    {
        isTurn = false;
        _defaultRotation = defaultRotation;

        _permanentsOnField = new List<Card_Permanent>();
        _monsters = new List<MonsterController>();
        SubscribeToMatchEvents();

        _encounter = encounter;
        SelectMonstersFromPool();
    }

    private void SubscribeToMatchEvents()
    {
        duelManager.onNewTurn += OnNewTurn;

        duelManager.onPlayerDefeat += OnPlayerDefeat;
        duelManager.onPlayerVictory += OnPlayerVictory;
    }

    private void SelectMonstersFromPool()
    {
        int num = monsterCount[Random.Range(0, monsterCount.Length)];
        int gold = 0;

        for (int i = 0; i < num; i++)
        {
            //Grab a random card from the pool
            var card = _encounter.MonsterPool[Random.Range(0, _encounter.MonsterPool.Length)];

            gold += card.MaxHealth + card.Attack + card.Defense;

            Card_Unit newCard = new Card_Unit(card, false);
            PlaceMonstersAtStart(newCard);
        }

        duelManager.SetMatchReward(gold);
    }

    private void PlaceMonstersAtStart(Card_Unit unit)
    {
        _permanentsOnField.Add(unit);
        unit.SetCardLocation(CardLocation.OnField);

        //Get node from Battlefield
        var nodes = duelManager.Battlefield.GetSummonableNodes(false);

        var node = GetNodeToSummon(nodes);
        if (node == null) return;

        var info = unit.CardInfo as PermanentSO;
        var summon = Instantiate(info.Prefab, node.Transform.position, _defaultRotation);
        unit.OnSummoned(summon, node);
        unit.onPermanentDestroyed += OnPermanentDestroyed;
        unit.onRemovedFromField += DestroyPermanent;

        var controller = summon.gameObject.AddComponent<MonsterController>();
        controller.AssignCard(unit);
        _monsters.Add(controller);

        //var rot = duelManager.Battlefield.Center.eulerAngles;
        //rot.y -= 180;
        //unit.transform.eulerAngles = rot;
    }

    private GridNode GetNodeToSummon(List<GridNode> nodes)
    {
        for (int i = nodes.Count - 1; i >= 0; i--)
        {
            if (nodes[i].Obstacle != null) nodes.RemoveAt(i);
            else if (nodes[i].Occupant != null) nodes.RemoveAt(i);
        }

        if (nodes.Count == 0) return null;

        return nodes[Random.Range(0, nodes.Count)];
    }
    #endregion

    #region - Monsters Turn -
    private void OnNewTurn(bool isPlayerTurn)
    {
        isTurn = !isPlayerTurn;
        if (isTurn) OnTurnStart();
    }

    private void OnTurnStart()
    {
        //For each card on the field, trigger OnTurnStart effects
        for (int i = 0; i < _permanentsOnField.Count; i++)
        {
            _permanentsOnField[i].OnTurnStart();
        }

        StartCoroutine(HandleTurn());
    }

    private IEnumerator HandleTurn()
    {
        //Wait to proceed until all cards have settled
        while (!duelManager.CanDeclareNewAction()) yield return null;
        yield return new WaitForSeconds(1f);

        yield return StartCoroutine(HandleMonsterActions());

        while (!duelManager.CanDeclareNewAction()) yield return null;
        yield return new WaitForSeconds(1f);
        duelManager.OnEndTurn(); //End Turn
    }

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
            var unit = _monsters[i].Unit;
            if (unit.HasActed && unit.MovesLeft <= 0) continue; //There is nothing more that the unit can do this turn

            _monsters[i].PrioritizeTargets();
            _monsters[i].SelectAction();

            while (!duelManager.CanDeclareNewAction()) yield return null;
            yield return new WaitForSeconds(0.1f);
        }
    }
    #endregion

    private void OnPermanentDestroyed(Card_Permanent permanent)
    {
        permanent.onPermanentDestroyed -= OnPermanentDestroyed;

        //Remove from list
        _permanentsOnField.Remove(permanent);

        if (permanent.Summon.TryGetComponent(out MonsterController monster)) _monsters.Remove(monster);
    }

    private void DestroyPermanent(Card_Permanent card)
    {
        card.onRemovedFromField -= DestroyPermanent;
        Destroy(card.Summon.gameObject);

        //All monsters on the field have been defeated, player victory
        if (_permanentsOnField.Count == 0) DuelManager.instance.onPlayerVictory?.Invoke();
    }

    #region - Match End -
    private void OnPlayerVictory()
    {
        MatcheEnd();
    }

    private void OnPlayerDefeat()
    {
        //pass message to all units to play victory/taunt animation

        MatcheEnd();
    }

    private void MatcheEnd()
    {
        duelManager.onNewTurn -= OnNewTurn;

        duelManager.onPlayerVictory -= OnPlayerVictory;
        duelManager.onPlayerDefeat -= OnPlayerDefeat;
    }

    public void ClearEncounter()
    {
        _encounter = null;

        //Destroy all cards.... 
    }
    #endregion
}