using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerContribution
{
    public int actorNumber;   // Photon player ID
    public int totalBet;

    public PlayerContribution(int actor, int bet)
    {
        actorNumber = actor;
        totalBet = bet;
    }
}