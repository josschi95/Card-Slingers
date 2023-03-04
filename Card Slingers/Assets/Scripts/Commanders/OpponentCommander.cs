using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/* NOTE: I'm probably better off just breaking up this script into a state machine
 That's going to prevent this script from reaching 800+ lines and should hopefully allow for some more modulation*/

public class OpponentCommander : CommanderController
{
    [SerializeField] private bool instantiateOnStart;
    [Space]

    private PlayerCommander playerCommander;
    private int[] cardTypesInHand = new int[System.Enum.GetNames(typeof(CardType)).Length];
    CardNodeCombo nulledCombo = new CardNodeCombo(null, null);
    //[SerializeField] 
    private bool[] invadesLanes; //true if invaded

    [SerializeField] private List<Card_Permanent> threats = new List<Card_Permanent>();

    public override void OnMatchStart(CardHolder holder, int startingHandSize = 4, int mana = 4)
    {
        base.OnMatchStart(holder, startingHandSize, mana);
        playerCommander = duelManager.Player_Commander;
        invadesLanes = new bool[duelManager.Battlefield.Width];
    }

    protected override void OnNewTurn(bool isPlayerTurn)
    {
        isTurn = !isPlayerTurn;
    }

    protected override void OnPlayerVictory()
    {
        base.OnPlayerVictory();
        Destroy(gameObject, 5f); //later also include a method for sinking beneath the groudn
    }

    protected override void OnPlayerDefeat()
    {
        base.OnPlayerDefeat();
    }

    protected override void OnSummoningPhase()
    {
        base.OnSummoningPhase();

        FindInvadedLanes();

        SortThreats();

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

    /// <summary>
    /// Updates array to determine which lanes are invaded
    /// </summary>
    private void FindInvadedLanes()
    {
        //Reset all to false
        for (int i = 0; i < invadesLanes.Length; i++) invadesLanes[i] = false;

        foreach (Card_Permanent card in duelManager.Player_Commander.CardsOnField)
        {
            if (card.Node.gridZ > (duelManager.Battlefield.Depth - 1) * 0.5f)
            {
                invadesLanes[card.Node.gridX] = true;
            }
        }

        for (int i = 0; i < threats.Count; i++)
        {

        }
    }

    private void SortThreats()
    {
        threats.Clear();

        foreach (Card_Permanent card in duelManager.Player_Commander.CardsOnField)
        {
            if (card is Card_Trap) continue; //The opponent should not be aware of player traps

            float gridZ = card.Node.gridZ;
            float modifiedScore = Mathf.RoundToInt(card.ThreatLevel + gridZ * (1 + (gridZ / duelManager.Battlefield.Depth)));
            
            //Defensive structures are not considered major threats,
            if (card is Card_Structure structure && structure.Focus == CardFocus.Defense) modifiedScore -= 10;
            //Place a higher priority on cards that are within movement range of the commnader
            else if (card is Card_Unit unit && duelManager.Battlefield.GetDistanceInNodes(card.Node, CommanderCard.Node) <= unit.Speed) modifiedScore += 5;
            
            card.ModifiedThreatLevel = modifiedScore;

            
            for (int i = 0; i < threats.Count; i++)
            {
                if (card.ModifiedThreatLevel > threats[i].ModifiedThreatLevel)
                {
                    //threats.Remove(card);
                    threats.Insert(i, card);
                    break;
                }
                else if (i == threats.Count - 1) threats.Add(card);
            }
        }
    }

    protected override void OnAttackPhase()
    {
        base.OnAttackPhase();

        if (duelManager.TurnCount == 1) duelManager.OnCurrentPhaseFinished();
        else StartCoroutine(HandleUnitAttacks());
        //if there are spells in the hand, target anyone near their commander

        //Do not attack with whatever guard the commander has
    }

    protected override void OnEndPhase()
    {
        //Same, nothing to add for opponents
        base.OnEndPhase();
        if (_cardsInHand.Count == 0 || CurrentMana == 0) duelManager.OnCurrentPhaseFinished();
    }

    #region - Summon Phase 


    private IEnumerator HandleSummonUnits()
    {
        //Continue playing cards until out of mana, or no more cards to play
        while (CurrentMana > 0 && _cardsInHand.Count > 0)
        {
            yield return new WaitForSeconds(1f);
            CardNodeCombo combo = nulledCombo;

            //So the rework that is going to be done here should be as follows
            //Find each threat on the board and order them accordingly. Will need to make a lot of changes since lane locking is no longer a thing
            //Find an appropriate response to each threat and summon it, obviously starting with the highest threat first


            if (CommanderLaneIsWeak()) combo = GetSummonCombo(CommanderCard.Node.gridX, CardFocus.Defense);
            if (OnTryValidateSummon(combo)) continue;

            if (CanAndNeedToDefendTerritory()) combo = GetSummonCombo(HighestThreatLane(), CardFocus.Offense);
            if (OnTryValidateSummon(combo)) continue;

            //Debug.Log("Checking to Attack Player");
            var playerX = playerCommander.CommanderCard.Node.gridX;
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
        OnCardPlayed(combo.card, combo.node);
        return true;
    }

    //if true: lane balance of commander is in player's favor
    private bool CommanderLaneIsWeak()
    {
        //the lane balance is greater than the negative value of the commander's power =>
        //there is at least one player unit in this lane with power greater than any allied unit
        int commanderLanePower = DuelManager.instance.Battlefield.LaneThreatArray[CommanderCard.Node.gridX];
        return commanderLanePower > -CommanderCard.ThreatLevel;
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

        foreach (Card_Permanent card in duelManager.Player_Commander.CardsOnField)
        {
            if (card.Node.gridZ > (duelManager.Battlefield.Depth - 1) * 0.5f)
            {
                float gridZ = card.Node.gridZ;
                int modifiedScore = Mathf.RoundToInt(card.ThreatLevel + gridZ * (1 + (gridZ / duelManager.Battlefield.Depth)));
                laneRating[card.Node.gridX] += modifiedScore;
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

        for (int i = 0; i < _permanentsOnField.Count; i++)
        {
            if (_permanentsOnField[i] is Card_Unit unit && unit.CanAct)
                unitsToAct.Add(unit);
        }

        while (unitsToAct.Count > 0)
        {
            while (!duelManager.canDeclareNewAction) yield return null;
            yield return new WaitForSeconds(0.5f);

            var unit = unitsToAct[0];
            var walkNodes = duelManager.Battlefield.FindReachableNodes(unit);
            var attackNodes = duelManager.Battlefield.FindTargetableNodes(unit, unit.Range);

            if (UnitCanAttack(unit, attackNodes))
            {
                //Do nothing?
            }
            else if (UnitCanMove(unit, walkNodes))
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
            if (duelManager.Battlefield.GetDistanceInNodes(unit.Node, attackNodes[i]) <= unit.Range)
            {
                //Debug.Log("Unit located at " + unit.OccupiedNode.gridX + "," + unit.OccupiedNode.gridZ + " Can attack without moving");
                duelManager.OnAttackActionConfirmed(unit, attackNodes[i]);
                return true;
            }
            else
            {
                var nodePath = duelManager.Battlefield.FindNodePath(unit, attackNodes[i], true, true);
                if (nodePath != null)
                {
                    duelManager.OnAttackActionConfirmed(unit, attackNodes[i]);
                    return true;
                }
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
                if (nodesToOccupy[i].gridZ > unit.Node.gridZ) nodesToOccupy.RemoveAt(i);
            }
        }
        if (nodesToOccupy.Count == 0) return false;
        
        GridNode node = nodesToOccupy[0];
        for (int i = 0; i < nodesToOccupy.Count; i++)
        {
            if (nodesToOccupy[i].gridZ < node.gridZ)
                node = nodesToOccupy[i];
        }

        duelManager.OnMoveActionConfirmed(unit, node);
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
                if (structure.Focus == CardFocus.Defense) def += 10;
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
                if (unit.ThreatLevel < cardPower) //returns a negative value. because enemy
                {
                    card = unit;
                    cardPower = unit.ThreatLevel;
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