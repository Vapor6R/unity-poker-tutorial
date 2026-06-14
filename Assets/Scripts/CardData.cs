// CardData.cs
// Enums and data class for a playing card.
// Sprite naming convention: {Rank}{Suit}.png  e.g. "AceSpades.png", "10Hearts.png"

using System;

namespace Poker
{
    public enum Rank
    {
        Two   = 2,
        Three = 3,
        Four  = 4,
        Five  = 5,
        Six   = 6,
        Seven = 7,
        Eight = 8,
        Nine  = 9,
        Ten   = 10,
        Jack  = 11,
        Queen = 12,
        King  = 13,
        Ace   = 14   // High ace; handle low-ace (A-2-3-4-5) in hand evaluator
    }

    public enum Suit
    {
        Clubs    = 0,
        Diamonds = 1,
        Hearts   = 2,
        Spades   = 3
    }

    /// <summary>
    /// Immutable value-type representing a single playing card.
    /// Implements IComparable so cards can be sorted by rank.
    /// </summary>
    [Serializable]
    public readonly struct CardData : IComparable<CardData>, IEquatable<CardData>
    {
        public readonly Rank Rank;
        public readonly Suit Suit;

        public CardData(Rank rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
        }

        // ── Sprite name ──────────────────────────────────────────────────────────
        /// <summary>
        /// Returns the expected sprite filename (without extension).
        /// e.g. "AceSpades", "10Hearts", "KingDiamonds"
        /// </summary>
        public string SpriteName => $"{Rank}{Suit}";

        // ── Helpers for hand evaluation ──────────────────────────────────────────
        public int RankValue  => (int)Rank;
        public int SuitValue  => (int)Suit;

        // ── Comparison / equality ────────────────────────────────────────────────
        public int CompareTo(CardData other) => RankValue.CompareTo(other.RankValue);

        public bool Equals(CardData other)   => Rank == other.Rank && Suit == other.Suit;
        public override bool Equals(object obj) => obj is CardData c && Equals(c);
        public override int GetHashCode()    => HashCode.Combine(Rank, Suit);

        public override string ToString()    => $"{Rank} of {Suit}";

        public static bool operator ==(CardData a, CardData b) => a.Equals(b);
        public static bool operator !=(CardData a, CardData b) => !a.Equals(b);
        public static bool operator  >(CardData a, CardData b) => a.RankValue > b.RankValue;
        public static bool operator  <(CardData a, CardData b) => a.RankValue < b.RankValue;
    }
}
