using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentCommander : CommanderController
{
    private PlayerCommander playerCommander;
    private int[] cardTypesInHand = new int[System.Enum.GetNames(typeof(CardType)).Length];
    CardNodeCombo nulledCombo = new CardNodeCombo(null, null);

    public override void OnMatchStart(int startingHandSize = 4, int mana = 4)
    {
        base.OnMatchStart(startingHandSize, mana);
        playerCommander = duelManager.PlayerController;
    }
    protected override void OnBeginPhase()
    {
        //I don't think anything should change in the Begin phase for the enemy, this should all be the same
        //regain mana, draw cards, next phase
        base.OnBeginPhase();
    }

    protected override void OnSummoningPhase()
    {
        base.OnSummoningPhase();

        StartCoroutine(SummonUnits());

        for (int i = 0; i < cardTypesInHand.Length; i++) cardTypesInHand[i] = 0; //reset array to recalculate it
        
        //Find the total mana cost of all cards in hand to determine how many can be played or if all can be played
        var totalCardManaCost = 0;
        for (int i = 0; i < _cardsInHand.Count; i++)
        {
            totalCardManaCost += _cardsInHand[i].CardInfo.cost;
            cardTypesInHand[(int)_cardsInHand[i].CardInfo.type]++; 
        }

        if (totalCardManaCost <= CurrentMana)
        {
            //can play all cards in hand, and should likely do so
        }
        else
        {
            //will need to decide which cards to play
        }

        //find the nodes that I should be playing a card in
        //this will be 100% of the time, a card in front of their commander, to prevent attacks, 
        //the "vanguard" should be a frontline unit or defensive structure that they play along the front row 

        //I also need to take into account the units which are already on the field

        //1. Ensure the the commander is protected

        //2. Attack the player commander

        //3. Attack invaders

        //4. Invade player

        //5. 


        //If there is not a unit/structure in commander lane, find the tankiest one in hand and play it as the vanguard
        //mark all structures and units as offensive, defensive, or utility

        //each permanent type should be handled separately
        //ignore unoccupied lanes? or if there are some quicker units, place them at the front line to quickly block that lane. 

        //take special note of any unit on this side of field
        //and of course try to deal damage to the enemy commander

        //find some way to compare relative power levels of allies/enemies in the same lane

        //Order all goals, and play in line with those

        //This will probably be done by the Battlefield, or at least kept track there? 
        //But I should dynamically update the power balance of each lane
        //Each non-trap permanent will be given some sort of Power Rating based on its stats (static and dynamic)
        //The advantage for each lane will be calculated by adding the power rating of all player untis, and subtracting power of enemy units
        //lane advantage = TotalPlayerUnitAdvantage - TotalEnemyUnitAdvantage
        //this should also take into account how far forward the permanent is.
            //for instance a building at the back line is less of a concern than one at the front

        //the opponent commander wants their commander to be in the lane with the lowest advantage
        //and they want to decrease the advantage of all lanes above 0

        //Ok so what is the calculation for a permanent's power level
        //Unit - currrentHealth + ATK + DEF 
    }



    protected override void OnDeclarationPhase()
    {
        base.OnDeclarationPhase();

        //if there are spells in the hand, target anyone near their commander

        //Do not attack with whatever guard the commander has
    }

    protected override void OnResolutionPhase()
    {
        //Again, I don't think anything special should happen here
        base.OnResolutionPhase();
    }

    protected override void OnEndPhase()
    {
        //Same, nothing to add for opponents
        base.OnEndPhase();
    }

    #region - Summon Phase 
    private IEnumerator SummonUnits()
    {
        //Continue playing cards until out of mana, or no more cards to play
        while (CurrentMana > 0 && _cardsInHand.Count > 0)
        {
            yield return new WaitForSeconds(2.5f);
            CardNodeCombo combo = nulledCombo;

            //Debug.Log("Checking to Defend Commander.");
            if (CommanderLaneIsWeak()) combo = GetSummonCombo(CommanderCard.OccupiedNode.gridX, CardFocus.Defense);
            if (OnTryValidateSummon(combo)) continue;

            //Debug.Log("Checking to Defend Territory.");
            Debug.Log("Pickup from here," +
                "If there is any unit on this side but the lane is full, it will just keep summoning from the left");
            if (PlayerUnitsInTerritory()) combo = GetSummonCombo(FindInvadedLane(), CardFocus.Offense);
            if (OnTryValidateSummon(combo)) continue;

            //Debug.Log("Checking to Attack Player");
            if (CanSummonInPlayerCommanderLane()) combo = GetSummonCombo(playerCommander.CommanderCard.OccupiedNode.gridX, CardFocus.Defense);
            if (OnTryValidateSummon(combo)) continue;

            //a valid card and node have not been passed, there are no valid summons
            if (combo.card == null || combo.node == null)
            {
                Debug.Log("No More Valid Summons.");
                duelManager.OnCurrentPhaseFinished();
                yield break;
            }

            yield return null;
        }

        duelManager.OnCurrentPhaseFinished();
    }

    private bool OnTryValidateSummon(CardNodeCombo combo)
    {
        if (combo.card == null || combo.node == null) return false;
        OnPermanentPlayed(combo.node, combo.card);
        return true;
    }

    //if true: lane balance of commander is in player's favor
    private bool CommanderLaneIsWeak()
    {
        //the lane balance is greater than the negative value of the commander's power =>
        //there is at least one player unit in this lane with power greater than any allied unit
        int commanderLanePower = DuelManager.instance.Battlefield.LaneBalanceArray[CommanderCard.OccupiedNode.gridX];
        return commanderLanePower > -CommanderCard.PowerLevel;
    }

    //there are open nodes in the player commander's lane
    private bool CanSummonInPlayerCommanderLane()
    {
        var availablesNodes = duelManager.Battlefield.GetControlledNodesInLane
            (this, duelManager.PlayerController.CommanderCard.OccupiedNode.gridX);

        if (availablesNodes.Count > 0) return true;
        return false;
    }

    //there are player units on this side of the field
    private bool PlayerUnitsInTerritory()
    {
        foreach(Card_Permanent card in duelManager.PlayerController.CardsOnField)
        {
            if (card.OccupiedNode.gridZ > (duelManager.Battlefield.Depth - 1) * 0.5f) return true;
        }
        Debug.Log("No Player Units in Territory.");
        return false;
    }

    //Return a lane which is invaded by a player unit, returning the one with the highest threat
    private int FindInvadedLane(bool filterInvalidLanes = true)
    {
        //Generate a score for each lane based on the total power level of each player unit on this side
        int[] laneRating = new int[duelManager.Battlefield.Width - 1];

        foreach (Card_Permanent card in duelManager.PlayerController.CardsOnField)
        {
            if (card.OccupiedNode.gridZ > (duelManager.Battlefield.Depth - 1) * 0.5f)
            {
                laneRating[card.OccupiedNode.gridX] += card.PowerLevel;
            }
        }

        //Don't return any lanes where there are no nodes to summon
        if (filterInvalidLanes)
        {
            for (int i = 0; i < laneRating.Length; i++)
            {
                var availablesNodes = duelManager.Battlefield.GetOpenNodesInLane(this, i);
                if (availablesNodes.Count == 0)
                {
                    //Debug.Log("No Open Nodes in Lane: " + i);
                    laneRating[i] = 0;
                }
            }
        }

        int laneToSelect = 0;
        int highestlaneRating = 0;
        for (int i = 0; i < laneRating.Length; i++)
        {
            if (laneRating[i] > highestlaneRating)
            {
                laneToSelect = i;
                highestlaneRating = laneRating[i];
            }
        }
        //Debug.Log("Invaded Lane: " + laneToSelect);
        return laneToSelect;
    }
    
    //Returns the lane with the highest balance in the player's favor
    private int PreferredLaneToSummon()
    {
        int lane = 0;
        int laneDisadvantage = 0;

        int[] laneBalance = duelManager.Battlefield.LaneBalanceArray;
        for (int i = 0; i < laneBalance.Length; i++)
        {
            if (i == CommanderCard.OccupiedNode.gridX) laneBalance[i] += 3;

            if (laneBalance[i] > laneDisadvantage)
            {
                lane = i;
                laneDisadvantage = laneBalance[i];
            }
        }

        return lane;
    }

    private CardNodeCombo GetSummonCombo(int lane, CardFocus focus)
    {
        var availablesNodes = duelManager.Battlefield.GetControlledNodesInLane(this, lane);

        if (availablesNodes.Count > 0)
        {
            availablesNodes.Reverse(); //work from furthest forward node

            for (int i = 0; i < availablesNodes.Count; i++)
            {
                if (availablesNodes[i].Occupant != null) continue;

                return new CardNodeCombo(GetCard(focus), availablesNodes[i]);
            }
        }
        Debug.Log("Returning Nulled Combo");
        return nulledCombo;
    }
    #endregion

    private Card_Permanent GetCard(CardFocus focus)
    {
        if (focus == CardFocus.Offense) return GetOffensiveCard();
        else return GetDefensiveCard();
    }

    //Returns a unit or structure with the highest Defensive rating : Health + Defense
    private Card_Permanent GetDefensiveCard()
    {
        Card_Permanent card = null;
        int cardPower = 0;
        if (_cardsInHand.Count == 0) return null;

        for (int i = 0; i < _cardsInHand.Count; i++)
        {
            if (_cardsInHand[i] is Card_Unit unit)
            {
                int def = unit.MaxHealth + unit.Defense;
                if (def > cardPower)
                {
                    card = unit;
                    cardPower = def;
                }
            }
            else if (_cardsInHand[i] is Card_Structure structure)
            {
                int def = structure.MaxHealth + structure.Defense;
                if (def > cardPower)
                {
                    card = structure;
                    cardPower = def;
                }
            }
        }

        return card;
    }

    //Returns a unit with the highest power rating
    private Card_Permanent GetOffensiveCard()
    {
        Card_Permanent card = null;
        int cardPower = 0;
        if (_cardsInHand.Count == 0) return null;

        for (int i = 0; i < _cardsInHand.Count; i++)
        {
            if (_cardsInHand[i] is Card_Unit unit)
            {
                if (unit.PowerLevel < cardPower) //returns a negative value. because enemy
                {
                    card = unit;
                    cardPower = unit.PowerLevel;
                }
            }
            /*else if (_cardsInHand[i] is Card_Structure structure)
            {
                int def = structure.MaxHealth + structure.Defense;
                if (def > cardPower)
                {
                    card = structure;
                    cardPower = def;
                }
            }*/
        }
        if (card == null) Debug.Log("Returning Null");
        return card;
    }
}

public struct CardNodeCombo
{
    public Card_Permanent card;
    public GridNode node;

    public CardNodeCombo(Card_Permanent card, GridNode node)
    {
        this.card = card;
        this.node = node;
    }
}

public enum CardFocus { Offense, Defense, Utility }