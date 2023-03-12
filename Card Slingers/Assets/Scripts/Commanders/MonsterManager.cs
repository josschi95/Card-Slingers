using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : MonoBehaviour
{
    private DuelManager duelManager;

    protected List<Card_Permanent> _permanentsOnField;
    private List<MonsterController> _monsters;
    private bool isTurn;

    protected Quaternion _defaultRotation;
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
    public void OnMatchStart(EnemyGroupManager monsters, Quaternion defaultRotation)
    {
        SubscribeToMatchEvents();

        isTurn = false;
        int gold = 0;
        _defaultRotation = defaultRotation;

        _monsters = new List<MonsterController>(monsters.Monsters);
        _permanentsOnField = new List<Card_Permanent>();

        if (monsters.Commander != null)
        {
            _permanentsOnField.Add(monsters.Commander.CommanderCard);
            monsters.Commander.CommanderCard.onPermanentDestroyed += OnPermanentDestroyed;
            monsters.Commander.CommanderCard.onRemovedFromField += DestroyPermanent;
            gold += 200;
        }

        for (int i = 0; i < _monsters.Count; i++)
        {
            _permanentsOnField.Add(_monsters[i].Unit);
            _monsters[i].Unit.onPermanentDestroyed += OnPermanentDestroyed;
            _monsters[i].Unit.onRemovedFromField += DestroyPermanent;
            gold += _monsters[i].Unit.MaxHealth + _monsters[i].Unit.Attack + _monsters[i].Unit.Defense;
        }

        duelManager.SetMatchReward(gold);
    }

    private void SubscribeToMatchEvents()
    {
        duelManager.onNewTurn += OnNewTurn;

        duelManager.onPlayerDefeat += OnPlayerDefeat;
        duelManager.onPlayerVictory += OnPlayerVictory;
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

    #region - Monster Culling -
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
    #endregion

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
        //Destroy all cards.... 
    }
    #endregion
}