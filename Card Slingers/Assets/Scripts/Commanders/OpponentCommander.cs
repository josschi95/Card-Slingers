using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* NOTE: I'm probably better off just breaking up this script into a state machine
 That's going to prevent this script from reaching 800+ lines and should hopefully allow for some more modulation*/

public class OpponentCommander : CommanderController
{
    private PlayerCommander playerCommander;
    private int[] cardTypesInHand = new int[System.Enum.GetNames(typeof(CardType)).Length];
    CardNodeCombo nulledCombo = new CardNodeCombo(null, null);
    [SerializeField] private bool[] invadesLanes; //true if invaded

    public override void OnMatchStart(int startingHandSize = 4, int mana = 4)
    {
        base.OnMatchStart(startingHandSize, mana);
        playerCommander = duelManager.PlayerController;
        invadesLanes = new bool[duelManager.Battlefield.Width];
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

        FindInvadedLanes();

        StartCoroutine(HandleSummonUnits());

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

    protected override void OnAttackPhase()
    {
        base.OnAttackPhase();

        StartCoroutine(HandleUnitAttacks());
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
    /// <summary>
    /// Updates array to determine which lanes are invaded
    /// </summary>
    private void FindInvadedLanes()
    {
        //Reset all to false
        for (int i = 0; i < invadesLanes.Length; i++) invadesLanes[i] = false;

        foreach (Card_Permanent card in duelManager.PlayerController.CardsOnField)
        {
            if (card.OccupiedNode.gridZ > (duelManager.Battlefield.Depth - 1) * 0.5f)
            {
                invadesLanes[card.OccupiedNode.gridX] = true;
            }
        }
    }

    private IEnumerator HandleSummonUnits()
    {
        //Continue playing cards until out of mana, or no more cards to play
        while (CurrentMana > 0 && _cardsInHand.Count > 0)
        {
            yield return new WaitForSeconds(1f);
            CardNodeCombo combo = nulledCombo;

            if (CommanderLaneIsWeak()) combo = GetSummonCombo(CommanderCard.OccupiedNode.gridX, CardFocus.Defense);
            if (OnTryValidateSummon(combo)) continue;

            if (CanAndNeedToDefendTerritory()) combo = GetSummonCombo(HighestThreatLane(), CardFocus.Offense);
            if (OnTryValidateSummon(combo)) continue;

            //Debug.Log("Checking to Attack Player");
            var playerX = playerCommander.CommanderCard.OccupiedNode.gridX;
            if (OpenNodesInLane(playerX)) combo = GetSummonCombo(playerX, CardFocus.Defense);
            if (OnTryValidateSummon(combo)) continue;

            //a valid card and node have not been passed, there are no valid summons
            if (combo.card == null || combo.node == null)
            {
                //Debug.Log("No More Valid Summons. Ending Phase");
                duelManager.OnCurrentPhaseFinished();
                yield break;
            }

            yield return null;
        }

        //Debug.Log("No More Mana or Cards In Hand. Ending Phase");
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

    /// <summary>
    /// if true: There are player units in territory and open nodes to summon allies in those lanes.
    /// </summary>
    private bool CanAndNeedToDefendTerritory()
    {
        for (int i = 0; i < invadesLanes.Length; i++)
        {
            if (invadesLanes[i] == true) break;
            if (i == invadesLanes.Length - 1) return false;
        }

        //Debug.Log("Invaders in Territory.");
        for (int i = 0; i < invadesLanes.Length; i++)
        {
            if (invadesLanes[i] == true && OpenNodesInLane(i)) return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the highest threat lane which is invaded by a player unit.
    /// Unit's forward position is taken into account.
    /// </summary>
    private int HighestThreatLane(bool filterInvalidLanes = true)
    {
        //Generate a score for each lane based on the total power level of each player unit on this side
        int[] laneRating = new int[duelManager.Battlefield.Width];

        foreach (Card_Permanent card in duelManager.PlayerController.CardsOnField)
        {
            if (card.OccupiedNode.gridZ > (duelManager.Battlefield.Depth - 1) * 0.5f)
            {
                float gridZ = card.OccupiedNode.gridZ;
                int modifiedScore = Mathf.RoundToInt(card.PowerLevel + gridZ * (1 + (gridZ / duelManager.Battlefield.Depth)));
                laneRating[card.OccupiedNode.gridX] += modifiedScore;
            }
        }

        //Don't return any lanes where there are no nodes to summon
        if (filterInvalidLanes)
        {
            for (int i = 0; i < laneRating.Length; i++)
            {
                if (OpenNodesInLane(i) == false) laneRating[i] = 0;
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
    
    /// <summary>
    /// if true: there is at least one open node in the given lanel
    /// </summary>
    private bool OpenNodesInLane(int lane)
    {
        if (duelManager.Battlefield.GetOpenNodesInLane(this, lane).Count > 0) return true;
        return false;
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
        //Debug.Log("Returning Nulled Combo");
        return nulledCombo;
    }
    #endregion

    #region - Attack Phase -
    private IEnumerator HandleUnitAttacks()
    {
        //Debug.Log("Starting Attack Declaration");
        var unitsToAct = new List<Card_Unit>();
        foreach(Card_Unit unit in _permanentsOnField) if (unit.CanAct) unitsToAct.Add(unit);

        while (unitsToAct.Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
            //Debug.Log("Remaining Units to Act: " + unitsToAct.Count);
            var unit = unitsToAct[0];
            var availableNodes = duelManager.Battlefield.GetAllNodesInLane(unit.OccupiedNode.gridX);
            var validNodes = duelManager.GetValidNodes(unit, availableNodes);


            if (UnitCanAttack(unit, validNodes.attackNodes))
            {
                //Do nothing?
            }
            else if (UnitCanMove(unit, validNodes.nodesToOccupy))
            {
                //Also do nothing?
            }

            unitsToAct.Remove(unit);
            yield return null;
        }

        //Debug.Log("Ending Attack Declaration");
        duelManager.OnCurrentPhaseFinished();
    }

    private bool UnitCanAttack(Card_Unit unit, List<GridNode> attackNodes)
    {
        //Debug.Log("Checking if unit can attack");
        if (attackNodes.Count == 0) return false;

        for (int i = 0; i < attackNodes.Count; i++)
        {
            //Can attack without needing to move
            if (Mathf.Abs(attackNodes[i].gridZ - unit.OccupiedNode.gridZ) <= unit.Range)
            {
                //Debug.Log("Unit located at " + unit.OccupiedNode.gridX + "," + unit.OccupiedNode.gridZ + " Can attack without moving");
                duelManager.OnAttackActionConfirmed(unit, attackNodes[i]);
                return true;
            }
            //need to make sure that I can get within range to the target
            else if (duelManager.Battlefield.GetUnoccupiedNodeInRange(unit.OccupiedNode, attackNodes[i], unit.Range) != null)
            {
                //Debug.Log("Unit located at " + unit.OccupiedNode.gridX + "," + unit.OccupiedNode.gridZ + " Can attack after moving to valid node");
                duelManager.OnAttackActionConfirmed(unit, attackNodes[i]);
                return true;
            }
            //else Debug.Log("Unit located at " + unitsToAct[0].OccupiedNode.gridX + "," + unit.OccupiedNode.gridZ + " Cannot get within range to attack");
        }
        return false;
    }

    private bool UnitCanMove(Card_Unit unit, List<GridNode> nodesToOccupy, bool moveForward = true)
    {
        
        if (nodesToOccupy.Count == 0) return false;
        if (moveForward)
        {
            for (int i = nodesToOccupy.Count - 1; i >= 0; i--)
            {
                if (nodesToOccupy[i].gridZ > unit.OccupiedNode.gridZ) nodesToOccupy.RemoveAt(i);
            }
        }
        if (nodesToOccupy.Count == 0) return false;
        duelManager.OnMoveActionConfirmed(unit, nodesToOccupy[nodesToOccupy.Count - 1]);
        return true;
    }

    #endregion

    #region - Card Sorting -
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
        //Debug.Log("Getting Offensive Card");
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
        //if (card == null) Debug.Log("Returning Null");
        return card;
    }
    #endregion
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