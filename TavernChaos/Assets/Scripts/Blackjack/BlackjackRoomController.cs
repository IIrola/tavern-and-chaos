using System;
using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    // Authority controller for the blackjack table. Hands and the dealer's face-up card are public; the hole card never leaves BlackjackRound.
    public sealed class BlackjackRoomController : IRoomTable
    {
        public const long MinStake = 26;
        public static readonly long[] Stakes = { 26, 50, 100, 250 };
        public static readonly Vector3 Center = new(10f, 0, -5.8f);
        public TableSession Table { get; } = new("bj-1", 5);
        public string Notice { get; private set; } = "2–5 personas contra la banca. Apuesta mínima 26, apertura 10.";
        private readonly RoomRoster roster; private readonly Func<ulong, TavernActor> actor; private readonly Action<ulong, bool, int> seat; private readonly long fee;
        private readonly HashSet<ulong> ready = new(), gone = new();
        private BlackjackRound round; private ulong[] players; private string key; private long stake = MinStake; private uint revision = 1; private ulong serial; private double now; private bool closing;

        public static Vector3 SeatPosition(int i) { float phi = (-60 + 30 * i) * Mathf.Deg2Rad; return Center + new Vector3(-1.95f * Mathf.Cos(phi), .08f, 1.95f * Mathf.Sin(phi)); }
        public static float SeatYaw(int i) { float phi = (-60 + 30 * i) * Mathf.Deg2Rad; return Mathf.Atan2(Mathf.Cos(phi), -Mathf.Sin(phi)) * Mathf.Rad2Deg; }
        public static bool Near(Vector3 p) { for (int i = 0; i < 5; i++) if (Vector3.Distance(p, SeatPosition(i)) < 1.5f) return true; return false; }
        public BlackjackRoomController(RoomRoster roster, Func<ulong, TavernActor> actor, Action<ulong, bool, int> seat, EconomySettings prices)
        { this.roster = roster; this.actor = actor; this.seat = seat; fee = prices.Opening(TavernActivity.Blackjack); }
        private void Invalidate() { ready.Clear(); revision = revision == uint.MaxValue ? 1 : revision + 1; }

        public bool Sit(ulong id, bool eligible, double time)
        {
            now = time; var a = actor(id); bool old = Table.SeatOf(id) >= 0; bool can = eligible && a && !a.Fallen && Near(a.transform.position); int preferred = -1;
            if (can && !old && (Table.Phase != TablePhase.Playing && Table.Phase != TablePhase.Result || Table.Participates(id))) {
                float best = 1.5f;
                for (int i = 0; i < 5; i++) if (!Table.Occupant(i).HasValue) {
                    float d = Vector3.Distance(a.transform.position, SeatPosition(i));
                    if (d < best && TavernActor.TryFindStandingPosition(SeatPosition(i), a, out var free) && Vector3.Distance(free, SeatPosition(i)) < .05f) { best = d; preferred = i; }
                }
                can = preferred >= 0;
            }
            var result = Table.Sit(id, can, now, preferred);
            if (result == SeatReply.Seated && !old) { seat(id, true, Table.SeatOf(id)); if (Table.Phase == TablePhase.Preparation) Invalidate(); gone.Remove(id); }
            if (result == SeatReply.Waiting) Notice = "En espera para la siguiente mano. No se cobra por esperar.";
            return result != SeatReply.Rejected;
        }
        public void Leave(ulong id, double time)
        {
            now = time; if (Table.LeaveSeat(id, now)) { seat(id, false, -1); if (Table.Phase == TablePhase.Preparation) Invalidate(); }
        }
        public void Disconnect(ulong id, double time)
        {
            Leave(id, time); Table.Remove(id, time);
            if (round != null && Table.Phase == TablePhase.Playing && Table.Participates(id)) { gone.Add(id); Drive(time); }
        }
        public bool Handle(ulong id, LanIntent intent, double time)
        {
            now = time; int seated = Table.SeatOf(id);
            switch (intent.action) {
                case LanAction.BjLeave: Leave(id, now); return true;
                case LanAction.BjClose:
                    if (Table.Phase != TablePhase.Result || Table.Organizer != id || key == null || !roster.Ledger.IsSettled(key)) return false;
                    Reset(); Table.Close(); return true;
                case LanAction.BjWager:
                    if (Table.Phase != TablePhase.Preparation || Table.Organizer != id || intent.proposal != revision || intent.wager < MinStake || intent.wager % 2 != 0 || intent.wager > 1000000) return false;
                    if (stake != intent.wager) { stake = intent.wager; Invalidate(); } return true;
                case LanAction.BjReady: {
                    if (Table.Phase != TablePhase.Preparation || seated < 0 || intent.proposal != revision || !actor(id) || actor(id).Fallen) return false;
                    if (ready.Remove(id)) return true;
                    long cost = stake + (Table.Organizer == id ? fee : 0);
                    if (roster.Ledger.Balance(id) < cost) { Notice = "Saldo insuficiente para la apuesta. No se ha cobrado nada."; return false; }
                    ready.Add(id); Notice = "Confirmado. El organizador reparte cuando todos estén listos."; return true;
                }
                case LanAction.BjStart: return Start(id, intent.proposal);
                case LanAction.BjHit: case LanAction.BjStand: {
                    if (round == null || Table.Phase != TablePhase.Playing || seated < 0 || !actor(id) || actor(id).Fallen) return false;
                    bool ok = intent.action == LanAction.BjHit ? round.Hit(id) : round.Stand(id);
                    if (ok) Drive(now); return ok;
                }
            }
            return false;
        }
        private bool Start(ulong id, uint proposal)
        {
            if (!Table.CanStart(id, 2) || proposal != revision) return false;
            var list = new List<ulong>(); for (int i = 0; i < 5; i++) if (Table.Occupant(i).HasValue) list.Add(Table.Occupant(i).Value);
            foreach (ulong p in list) if (!ready.Contains(p) || !actor(p) || actor(p).Fallen) return false;
            string k = Table.Id + "/" + (++serial);
            if (roster.Ledger.ReserveOpening(k, "blackjack", id, list, fee, stake, againstHouse: true) != LedgerResult.Applied) { Notice = "No se pudo reservar la apuesta. Nadie ha pagado."; return false; }
            try {
                var fresh = new BlackjackRound(list, BlackjackRound.Shuffled(new System.Random(Guid.NewGuid().GetHashCode())));
                if (!Table.Start(id, 2)) throw new InvalidOperationException("Blackjack table changed during reservation");
                round = fresh; players = list.ToArray(); key = k; closing = false; gone.Clear();
                if (roster.Ledger.Activate(k) != LedgerResult.Applied) throw new InvalidOperationException("Blackjack reservation could not activate");
                Notice = "Cartas repartidas. Pedir o plantarse en tu turno."; Drive(now); return true;
            } catch (Exception error) { roster.Ledger.AbortOpening(k); Table.AbortStart(); Reset(); Invalidate(); Debug.LogException(error); return false; }
        }
        private void Reset() { round = null; players = null; key = null; gone.Clear(); Invalidate(); }
        // Moves play forward: absent hands stand, then the table settles once the dealer has played.
        private void Drive(double time)
        {
            if (round == null) return;
            int guard = 0;
            while (round.Phase == BlackjackPhase.PlayerTurns && round.Turn is { } turn && guard++ < 16) {
                bool away = gone.Contains(turn) || !roster.Ledger.IsConnected(turn);
                if (away || closing) round.AutoStand(turn); else break;
            }
            if (round.Phase == BlackjackPhase.Settled) { if (Table.Phase == TablePhase.Playing) Table.Finish(); Settle(); }
            else if (round.Turn is { } t) Table.SetTurn(t, time);
        }
        private void Settle()
        {
            if (round == null || round.Phase != BlackjackPhase.Settled || roster.Ledger.IsSettled(key)) return;
            if (roster.Ledger.SettleHouse(key, round.Outcomes()) == LedgerResult.Rejected) return;
            int win = 0, lose = 0, push = 0; foreach (var o in round.Outcomes().Values) { if (o == HouseOutcome.Loss) lose++; else if (o == HouseOutcome.Push) push++; else win++; }
            Notice = "Mano liquidada: " + win + " ganan, " + push + " empatan, " + lose + " pierden.";
        }
        public void Tick(double time)
        {
            now = time;
            if (round != null && Table.Phase == TablePhase.Playing) {
                if (Table.ExpiredAbsence(now, false) is { } absent) { Notice = roster.Ledger.Find(absent)?.Name + " estuvo ausente: su mano se planta."; gone.Add(absent); }
                Drive(now);
            }
            Settle();
        }
        public bool FinishSession(double time)
        {
            now = time; closing = true; Drive(now); Settle();
            if (round != null && !roster.Ledger.IsSettled(key)) roster.Ledger.CancelMatch(key);
            for (int i = 0; i < 5; i++) if (Table.Occupant(i).HasValue) Leave(Table.Occupant(i).Value, now);
            Reset(); Table.Close(); return true;
        }
        public BlackjackRoomView Capture()
        {
            var occupants = new List<ulong>(); var slots = new List<int>(); for (int i = 0; i < 5; i++) if (Table.Occupant(i).HasValue) { occupants.Add(Table.Occupant(i).Value); slots.Add(i); }
            var names = new List<string>(); if (players != null) foreach (ulong id in players) names.Add(roster.Ledger.Find(id)?.Name ?? "Persona");
            var pay = new long[players?.Length ?? 0]; for (int i = 0; i < pay.Length; i++) pay[i] = roster.Ledger.Payout(key, players[i]);
            return new BlackjackRoomView {
                phase = Table.Phase, seated = occupants.ToArray(), seats = slots.ToArray(), participants = players ?? Array.Empty<ulong>(), names = names.ToArray(),
                ready = new List<ulong>(ready).ToArray(), waiting = new List<ulong>(Table.Waiting).ToArray(), hasOrganizer = Table.Organizer.HasValue, organizer = Table.Organizer ?? 0,
                proposal = revision, openingFee = fee, stake = stake, minStake = MinStake, settled = key != null && roster.Ledger.IsSettled(key), payouts = pay, notice = Notice,
                absenceRemaining = Table.AbsenceRemaining(now), round = round?.View()
            };
        }
        public void Dispose() { if (key != null && !roster.Ledger.IsSettled(key)) roster.Ledger.CancelMatch(key); }
    }
}
