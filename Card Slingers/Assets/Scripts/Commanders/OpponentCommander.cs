using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpponentCommander : CommanderController
{
    private int[] cardTypesInHand = new int[System.Enum.GetNames(typeof(CardType)).Length];
    protected override void OnBeginPhase()
    {
        //I don't think anything should change in the Begin phase for the enemy, this should all be the same
        //regain mana, draw cards, next phase
        base.OnBeginPhase();
    }

    protected override void OnSummoningPhase()
    {
        base.OnSummoningPhase();

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
        for (int i = 0; i < _permanentsOnField.Count; i++)
        {
            if (_permanentsOnField[i].OccupiedNode.gridX == CommanderCard.OccupiedNode.gridX)
            {
                //There is a unit in the same lane as the commander, this is good
                //could double check that their health is doing okay though
            }
        }

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

        /* 1. Protect Commander
         * 2. Attack Player Commander
         * 3. Remove Invaders
         * 4. Invade Other Half
         * 
         */
        //Protect commander
        //attack enemy commander
        //protect own side
        //

        //Ok so what is the calculation for a permanent's power level
        //Unit - currrentHealth + ATK + DEF 
    }

    protected override void OnDeclarationPhase()
    {
        base.OnDeclarationPhase();

        //if there are spells in the hand, target anyone near their commander
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
}
