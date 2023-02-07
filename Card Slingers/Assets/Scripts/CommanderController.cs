using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CommanderController : MonoBehaviour
{
    private DuelManager duelManager;

    public List<CardSO> cardsInHand;

    private void Start()
    {
        duelManager = DuelManager.instance;
        duelManager.onBeginPhase += delegate { OnBeginPhase(); };
        duelManager.onSummoningPhase += delegate { OnSummoningPhase(); };
        duelManager.onPlanningPhase += delegate { OnPlanningPhase(); };
        duelManager.onResolutionPhase += delegate { OnResolutionPhase(); };
        duelManager.onEndPhase += delegate { OnEndPhase(); };
    }

    public void OnMatchStart()
    {
        //Assign deck
        //Shuffle deck
        //Draw cards
    }

    public void OnBeginPhase()
    {
        //Set max mana
        //Draw Card

        //For each card on the field, invoke an OnBeginPhase event
    }

    public void OnSummoningPhase()
    {

    }

    public void OnPlanningPhase()
    {

    }

    public void OnResolutionPhase()
    {

    }

    public void OnEndPhase()
    {

    }
}
