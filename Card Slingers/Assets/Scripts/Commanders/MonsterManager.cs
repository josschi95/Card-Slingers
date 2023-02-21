using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterManager : OpponentCommander
{
    public PermanentSO[] startingPermanents;
    //No CommanderSO _commanderInfo
    //No Card_Commander _commanderCard
    //No int _currentMana
    //No _cardsInHand, _cardsInDeck, _cardsInDiscard, _cardsInExile

    //Yes _permanentsOnField
    //Yes isTurn

    protected override void Start()
    {
        onPermanentDestroyed += OnPermanentDestroyed;
        SubscribeToMatchEvents();
    }

    public override void OnAssignCommander(CommanderSO commanderInfo)
    {
        //Do Nothing, these variables don't exist
    }

    public override void OnMatchStart(CardHolder holder, int startingHandSize = 4, int mana = 4)
    {
        _permanentsOnField = new List<Card_Permanent>();


        Debug.Log("Need to find some way to add all existing mosnters on the field into this list");
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
}
