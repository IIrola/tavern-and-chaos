using System;
using System.Collections.Generic;

namespace TavernChaos
{
    public enum BlackjackPhase { PlayerTurns, Settled }
    // Card id 0..51: rank = id % 13 + 1 (1 ace, 11-13 faces), suit = id / 13.
    public static class BlackjackCard
    {
        public static int Rank(int id) => id % 13 + 1;
        public static int Value(int id) => Math.Min(10, Rank(id));
        // Best total not exceeding 21 when possible; soft means an ace is still counted as 11.
        public static int Total(IReadOnlyList<int> cards, out bool soft)
        {
            int total = 0, aces = 0; foreach (int c in cards) { total += Value(c); if (Rank(c) == 1) aces++; }
            soft = false; if (aces > 0 && total + 10 <= 21) { total += 10; soft = true; }
            return total;
        }
        public static bool Natural(IReadOnlyList<int> cards) { return cards.Count == 2 && Total(cards, out _) == 21; }
    }

    [Serializable] public sealed class BlackjackHandView { public ulong id; public int[] cards; public int total; public bool soft, bust, natural, stood; }
    // Per-recipient projection. The hole card id is never present until the dealer plays.
    [Serializable] public sealed class BlackjackView
    {
        public BlackjackPhase phase; public ulong turn; public bool hasTurn;
        public BlackjackHandView[] hands; public int[] dealer; public int dealerHidden, dealerTotal; public bool dealerRevealed;
        public int[] outcomes; public ulong[] players;
    }

    // Pure authority rules: 52 cards, server shuffle, no split/double/insurance/surrender. Dealer hits to 16 and stands on every 17.
    public sealed class BlackjackRound
    {
        public const int MinPlayers = 2, MaxPlayers = 5;
        private sealed class Hand { public ulong id; public readonly List<int> cards = new(); public bool stood; }
        private readonly List<Hand> hands = new();
        private readonly List<int> dealer = new();
        private readonly Stack<int> deck = new();
        private int current;
        public BlackjackPhase Phase { get; private set; }
        public IReadOnlyList<ulong> Players { get { var r = new List<ulong>(); foreach (var h in hands) r.Add(h.id); return r; } }
        public ulong? Turn => Phase == BlackjackPhase.PlayerTurns && current < hands.Count ? hands[current].id : null;
        public int DealerHidden => Phase == BlackjackPhase.Settled ? 0 : 1;
        public static int[] Shuffled(Random random)
        {
            var cards = new int[52]; for (int i = 0; i < 52; i++) cards[i] = i;
            for (int i = 51; i > 0; i--) { int j = random.Next(i + 1); (cards[i], cards[j]) = (cards[j], cards[i]); }
            return cards;
        }
        public BlackjackRound(IReadOnlyList<ulong> players, int[] order)
        {
            if (players == null || players.Count < MinPlayers || players.Count > MaxPlayers) throw new ArgumentException("Blackjack seats 2-5 players");
            if (order == null || order.Length != 52) throw new ArgumentException("A full deck is required");
            var seen = new HashSet<int>(); foreach (int c in order) if (c < 0 || c > 51 || !seen.Add(c)) throw new ArgumentException("Invalid deck");
            var ids = new HashSet<ulong>(); foreach (ulong id in players) if (!ids.Add(id)) throw new ArgumentException("Repeated player");
            for (int i = order.Length - 1; i >= 0; i--) deck.Push(order[i]); // order[0] is dealt first
            foreach (ulong id in players) hands.Add(new Hand { id = id });
            for (int pass = 0; pass < 2; pass++) { foreach (var h in hands) h.cards.Add(deck.Pop()); dealer.Add(deck.Pop()); }
            foreach (var h in hands) if (BlackjackCard.Natural(h.cards)) h.stood = true;
            current = -1; Advance();
        }
        private bool Bust(Hand h) => BlackjackCard.Total(h.cards, out _) > 21;
        private void Advance()
        {
            do current++; while (current < hands.Count && (hands[current].stood || Bust(hands[current])));
            if (current >= hands.Count) PlayDealer();
        }
        private void PlayDealer()
        {
            bool anyLive = false; foreach (var h in hands) if (!Bust(h)) anyLive = true;
            // The dealer does not draw when nobody can still win or tie; the hole card is still revealed.
            while (anyLive && BlackjackCard.Total(dealer, out _) <= 16) dealer.Add(deck.Pop());
            Phase = BlackjackPhase.Settled; current = hands.Count;
        }
        private Hand Active(ulong id) => Phase == BlackjackPhase.PlayerTurns && current < hands.Count && hands[current].id == id ? hands[current] : null;
        public bool Hit(ulong id)
        {
            var h = Active(id); if (h == null) return false;
            h.cards.Add(deck.Pop());
            int total = BlackjackCard.Total(h.cards, out _);
            if (total >= 21) { h.stood = true; Advance(); }
            return true;
        }
        public bool Stand(ulong id) { var h = Active(id); if (h == null) return false; h.stood = true; Advance(); return true; }
        // Absence, expulsion or disconnection: the committed hand stands as it is and resolves normally.
        public bool AutoStand(ulong id) => Stand(id);
        public HouseOutcome? Outcome(ulong id)
        {
            if (Phase != BlackjackPhase.Settled) return null;
            foreach (var h in hands) {
                if (h.id != id) continue;
                int mine = BlackjackCard.Total(h.cards, out _), house = BlackjackCard.Total(dealer, out _);
                bool myNatural = BlackjackCard.Natural(h.cards), houseNatural = BlackjackCard.Natural(dealer);
                if (mine > 21) return HouseOutcome.Loss;
                if (myNatural) return houseNatural ? HouseOutcome.Push : HouseOutcome.Natural;
                if (houseNatural) return HouseOutcome.Loss;
                if (house > 21 || mine > house) return HouseOutcome.Win;
                return mine == house ? HouseOutcome.Push : HouseOutcome.Loss;
            }
            return null;
        }
        public Dictionary<ulong, HouseOutcome> Outcomes()
        {
            if (Phase != BlackjackPhase.Settled) return null;
            var result = new Dictionary<ulong, HouseOutcome>(); foreach (var h in hands) result[h.id] = Outcome(h.id).Value; return result;
        }
        public BlackjackView View()
        {
            bool open = Phase == BlackjackPhase.Settled; var list = new BlackjackHandView[hands.Count]; var ids = new ulong[hands.Count];
            for (int i = 0; i < hands.Count; i++) {
                var h = hands[i]; int t = BlackjackCard.Total(h.cards, out bool soft); ids[i] = h.id;
                list[i] = new BlackjackHandView { id = h.id, cards = h.cards.ToArray(), total = t, soft = soft, bust = t > 21, natural = BlackjackCard.Natural(h.cards), stood = h.stood };
            }
            var view = new BlackjackView { phase = Phase, hasTurn = Turn.HasValue, turn = Turn ?? 0, hands = list, players = ids, dealerRevealed = open };
            if (open) { view.dealer = dealer.ToArray(); view.dealerTotal = BlackjackCard.Total(dealer, out _); }
            else { view.dealer = new[] { dealer[0] }; view.dealerHidden = dealer.Count - 1; view.dealerTotal = BlackjackCard.Total(new[] { dealer[0] }, out _); }
            if (open) { view.outcomes = new int[hands.Count]; for (int i = 0; i < hands.Count; i++) view.outcomes[i] = (int)Outcome(hands[i].id).Value; }
            return view;
        }
        // For conservation checks only: every card is in exactly one of deck, hands or dealer.
        public int CardsAccounted => deck.Count + dealer.Count + CountHands();
        private int CountHands() { int n = 0; foreach (var h in hands) n += h.cards.Count; return n; }
    }
}
