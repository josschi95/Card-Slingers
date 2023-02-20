using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCommander : CommanderController
{
    public override void OnMatchStart(CardHolder holder, int startingHandSize = 4, int mana = 4)
    {
        base.OnMatchStart(holder, startingHandSize, mana);
        
        isTurn = true;
    }
}
