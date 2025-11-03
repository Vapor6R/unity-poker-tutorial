using System.Collections.Generic;
using System.Linq;

public class HandComparer : IComparer<PlayerManager>
{
    public int Compare(PlayerManager x, PlayerManager y)
    {
        if (x == null || y == null)
            return 0;

        // ✅ Step 1: Compare hand rank (higher is better)
        int handRankComparison = ((int)x.currentHandRank).CompareTo((int)y.currentHandRank);
        if (handRankComparison != 0)
            return handRankComparison; // higher rank wins

        // ✅ Step 2: If ranks are equal, compare best cards (highest to lowest)
        var xCards = x.currentBestCards?.OrderByDescending(c => (int)c.rank).ToList();
        var yCards = y.currentBestCards?.OrderByDescending(c => (int)c.rank).ToList();

        if (xCards == null || yCards == null)
            return 0;

        int cardCount = System.Math.Min(xCards.Count, yCards.Count);

        for (int i = 0; i < cardCount; i++)
        {
            int cardComparison = ((int)xCards[i].rank).CompareTo((int)yCards[i].rank);
            if (cardComparison != 0)
                return cardComparison; // higher card wins
        }

        // ✅ Step 3: Completely tied — keep deterministic order
        return string.Compare(x.PlayerName, y.PlayerName, System.StringComparison.Ordinal);
    }
}
