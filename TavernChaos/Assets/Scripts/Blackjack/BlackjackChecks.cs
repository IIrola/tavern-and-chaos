using System;
using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    public static class BlackjackChecks
    {
        // Rank r (1=A..13=K) of suit 0 => id r-1. Builds a deck whose first cards are the given ranks, rest filled arbitrarily.
        private static int[] Rigged(params int[] ranks)
        {
            var used = new HashSet<int>(); var order = new List<int>(); var suit = new int[14];
            foreach (int r in ranks) { int id = suit[r]++ * 13 + (r - 1); order.Add(id); used.Add(id); }
            for (int i = 0; i < 52; i++) if (used.Add(i)) order.Add(i);
            return order.ToArray();
        }
        // Deal order: p0,dealer? No: pass 1 gives each player a card then the dealer; pass 2 the same.
        // Two players: p0a,p1a,Da,p0b,p1b,Db then draws.
        public static void Run(Action<bool, string> check)
        {
            var ids = new ulong[] { 10, 11 };
            var shuffled = BlackjackRound.Shuffled(new System.Random(7)); var set = new HashSet<int>(shuffled);
            check(shuffled.Length == 52 && set.Count == 52, "Blackjack: server shuffle yields exactly the 52 distinct cards");
            check(BlackjackCard.Total(new[] { 0, 12 }, out bool s1) == 21 && s1 && BlackjackCard.Total(new[] { 0, 5, 9 }, out bool s2) == 17 && !s2, "Blackjack: ace counts 11 only while it does not bust; faces count ten");
            check(BlackjackCard.Total(new[] { 0, 5 }, out bool soft) == 17 && soft, "Blackjack: A+6 is soft 17");
            check(!BlackjackCard.Natural(new[] { 6, 5, 7 }) && BlackjackCard.Natural(new[] { 0, 9 }), "Blackjack: only a two-card 21 is a natural");
            bool threw = false; try { new BlackjackRound(new ulong[] { 1 }, shuffled); } catch (ArgumentException) { threw = true; }
            check(threw, "Blackjack: fewer than two players is rejected");
            threw = false; try { new BlackjackRound(new ulong[] { 1, 2, 3, 4, 5, 6 }, shuffled); } catch (ArgumentException) { threw = true; }
            check(threw, "Blackjack: more than five players is rejected");
            var round = new BlackjackRound(ids, shuffled);
            check(round.CardsAccounted == 52 && round.View().hands[0].cards.Length == 2 && round.View().dealer.Length == 1 && round.View().dealerHidden == 1, "Blackjack: everyone gets two cards and the dealer shows one");

            // Privacy: the hole card id is absent from every projection until the dealer plays.
            round = new BlackjackRound(ids, Rigged(10, 9, 5, 8, 7, 12)); // p0:10,8 =18; p1:9,7=16; dealer:5(up),Q(hole)=15
            string json = JsonUtility.ToJson(round.View()); int hole = Rigged(10, 9, 5, 8, 7, 12)[5];
            var view = round.View(); bool leaked = Array.IndexOf(view.dealer, hole) >= 0;
            check(!leaked && !view.dealerRevealed && view.dealerTotal == 5, "Blackjack: hole card is not in the projected dealer cards before settlement");
            check(!round.Hit(11) && !round.Stand(11) && round.Turn == 10, "Blackjack: only the player whose turn it is can act");
            check(round.Stand(10) && round.Turn == 11 && !round.Stand(10), "Blackjack: standing passes the turn and cannot be repeated");
            round.Stand(11);
            check(round.Phase == BlackjackPhase.Settled && round.View().dealerRevealed && round.View().dealer.Length >= 2, "Blackjack: dealer plays and reveals after the last player");
            check(round.CardsAccounted == 52, "Blackjack: cards are conserved through play");

            // Dealer rules: hit on 16, stand on soft 17.
            round = new BlackjackRound(ids, Rigged(10, 10, 6, 8, 8, 10, 2)); // dealer 6+10=16 hits, draws 2 => 18
            round.Stand(10); round.Stand(11);
            check(round.View().dealerTotal == 18 && round.View().dealer.Length == 3, "Blackjack: dealer draws on 16");
            round = new BlackjackRound(ids, Rigged(10, 10, 1, 8, 8, 6, 2)); // dealer A+6 = soft 17 stands
            round.Stand(10); round.Stand(11);
            check(round.View().dealerTotal == 17 && round.View().dealer.Length == 2, "Blackjack: dealer stands on soft 17");

            // Outcomes.
            round = new BlackjackRound(ids, Rigged(10, 9, 10, 8, 7, 7)); // p0 18, p1 16, dealer 17
            round.Stand(10); round.Stand(11);
            check(round.Outcome(10) == HouseOutcome.Win && round.Outcome(11) == HouseOutcome.Loss, "Blackjack: higher total wins and lower total loses");
            round = new BlackjackRound(ids, Rigged(10, 9, 10, 7, 8, 7)); // p0 17, p1 17, dealer 17
            round.Stand(10); round.Stand(11);
            check(round.Outcome(10) == HouseOutcome.Push && round.Outcome(11) == HouseOutcome.Push, "Blackjack: equal totals push");
            round = new BlackjackRound(ids, Rigged(1, 10, 9, 13, 6, 8)); // p0 natural, p1 10+6 = 16, dealer 9+8 = 17
            check(round.Turn == 11, "Blackjack: a natural stands automatically and play moves on");
            round.Stand(11);
            check(round.Outcome(10) == HouseOutcome.Natural && round.Outcome(11) == HouseOutcome.Loss, "Blackjack: natural pays as a natural");
            round = new BlackjackRound(ids, Rigged(1, 1, 1, 13, 13, 13)); // p0 natural, p1 natural, dealer natural
            check(round.Phase == BlackjackPhase.Settled && round.Outcome(10) == HouseOutcome.Push && round.Outcome(11) == HouseOutcome.Push, "Blackjack: simultaneous naturals push");
            round = new BlackjackRound(ids, Rigged(1, 5, 9, 13, 6, 9, 10)); // p0 natural; p1 5+6=11; dealer 9+9=18
            round.Hit(11); // p1 draws a 10 => 21 with three cards, auto-stands
            check(round.Outcome(11) == HouseOutcome.Win && round.Outcome(10) == HouseOutcome.Natural, "Blackjack: three-card 21 beats the dealer but does not equal a natural");
            round = new BlackjackRound(ids, Rigged(5, 5, 1, 6, 6, 13)); // p0 11, p1 11, dealer natural
            round.Hit(10); round.Stand(10); round.Stand(11);
            check(round.Outcome(10) == HouseOutcome.Loss && round.Outcome(11) == HouseOutcome.Loss, "Blackjack: dealer natural beats non-natural hands");
            round = new BlackjackRound(ids, Rigged(1, 1, 10, 13, 13, 10)); // p0 A+K natural, p1 A+K natural, dealer 10+10=20
            check(round.Phase == BlackjackPhase.Settled && round.Outcome(10) == HouseOutcome.Natural && round.Outcome(11) == HouseOutcome.Natural, "Blackjack: all players with naturals settle immediately");
            round = new BlackjackRound(ids, Rigged(10, 10, 5, 6, 6, 4, 13, 13, 13)); // p0 16, p1 16, dealer 9 (draws if any live)
            round.Hit(10); // p0 draws K => 26 bust
            round.Hit(11); // p1 draws K => 26 bust
            var all = round.View();
            check(round.Phase == BlackjackPhase.Settled && all.dealer.Length == 2 && round.Outcome(10) == HouseOutcome.Loss && round.Outcome(11) == HouseOutcome.Loss, "Blackjack: dealer does not draw when every player busted");
            round = new BlackjackRound(ids, Rigged(10, 10, 5, 6, 6, 4, 10)); // dealer 5+4=9 must draw => 19
            round.Stand(10); round.AutoStand(11);
            check(round.Phase == BlackjackPhase.Settled && round.View().dealer.Length == 3, "Blackjack: an absent player's hand auto-stands and the round completes");
            check(!round.Hit(10) && !round.Stand(11) && round.Turn == null, "Blackjack: no action is accepted after settlement");

            // Ledger integration: stake 26 is even; natural pays 3:2 from the infinite bank.
            var ledger = new SessionLedger(1000); ledger.Join(10, "A", "a"); ledger.Join(11, "B", "b");
            var round2 = new BlackjackRound(ids, Rigged(1, 10, 9, 13, 6, 8));
            check(ledger.ReserveOpening("bj", "blackjack", 10, ids, 10, 26, againstHouse: true) == LedgerResult.Applied && ledger.Activate("bj") == LedgerResult.Applied, "Blackjack: opening fee and stakes reserve against the house");
            round2.Stand(11);
            var outcomes = round2.Outcomes();
            check(ledger.SettleHouse("bj", outcomes) == LedgerResult.Applied && ledger.Balance(10) == 1000 - 10 + 39 && ledger.Balance(11) == 1000 - 26 && ledger.AuditDelta == 0,
                "Blackjack: natural pays 3:2, loser's stake goes to the bar, books balance");
            check(ledger.SettleHouse("bj", outcomes) == LedgerResult.Replayed && ledger.Balance(10) == 1029, "Blackjack: replaying settlement does not pay twice");
        }
    }
}
