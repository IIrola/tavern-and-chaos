using System;
using UnityEngine;
namespace TavernChaos
{
    public enum LanAction { Move, Charge, Release, Interact, Drop, Stop, ProposeKick, VoteYes, Reset, StatsRequest, PoolSit, PoolLeave, PoolStart, PoolCall, PoolShoot, PoolPlace, PoolAccept, PoolRerack, PoolClose, PoolWager, PoolReady, ArmSit,ArmLeave,ArmWager,ArmReady,ArmStart,ArmMode,ArmEffort,ArmHold,ArmTap,ArmClose }
    [Serializable] public sealed class LanIntent
    {
        public ulong sequence, target; public LanAction action; public Vector3 move; public float yaw,pitch,power; public bool run,hasLook; public int ball,pocket,page,choice; public long wager; public uint proposal;
    }
    [Serializable] public sealed class LanActorState
    {
        public ulong id; public string name; public Vector3 position, focus,eye; public float yaw, recovery, effect; public bool fallen; public float charge=-1; public int held=-1,resetRevision; public long chips;
        public LanBodyPose[] body; public bool seated; public string table; public int ebriety;
    }
    [Serializable] public sealed class LanBodyPose { public Vector3 position; public Quaternion rotation; }
    public enum LanFeedbackKind { PunchHit, Miss, BottleHit, Drink, Grab, Throw, Slip, PoolContact, PoolPot, PoolShot }
    [Serializable] public sealed class LanFeedback { public ulong id; public LanFeedbackKind kind; public Vector3 position; }
    [Serializable] public sealed class LanBottleState
    {
        public Vector3 position; public Quaternion rotation; public bool held, flying;
    }
    [Serializable] public sealed class LanSnapshot
    {
        public ulong revision; public double serverTime; public float secondsRemaining;
        public LanActorState[] actors; public LanActorState rival; public LanBottleState[] bottles; public PoolRoomView pool;
        public string notice; public bool voteActive; public ulong voteTarget; public int votes,votesNeeded;
        public long drinkPrice;
        public SessionEndingView ending;
        public ArmRoomView arm;
    }
    [Serializable] public sealed class LanHello { public int protocol=7; public string name; }
    [Serializable] public sealed class SessionEndingView { public RoomPhase phase; public RoomEndReason reason; public double phaseStarted; public string reportId; public int pages,participants; }
    [Serializable] public sealed class PoolBallView { public int number; public bool active; public Vector3 position; public Quaternion rotation; }
    [Serializable] public sealed class PoolRoomView
    {
        public TablePhase phase; public EightBallPhase playPhase; public string notice;
        public ulong[] seated,participants,waiting; public int[] seats,groups;
        public bool hasOrganizer,hasTurn,breaking,headOnly,foul; public ulong organizer,turn;
        public int winner=-1,callBall,callPocket=-1,shots; public float absenceRemaining;
        public PoolBallView[] balls;
        public uint proposal; public long stake,openingFee,pot,heldPot; public ulong[] ready; public long[] payouts; public bool settled;
        public string[] participantNames; public MatchReceipt receipt;
        public bool IsSeated(ulong id)=>seated!=null&&Array.IndexOf(seated,id)>=0;
        public bool IsParticipant(ulong id)=>participants!=null&&Array.IndexOf(participants,id)>=0;
    }
    [Serializable] public sealed class MatchReceipt { public string id,winnerName,game="Billar"; public bool hasWinner; public ulong[] players; public long[] payouts; public long pot; }
    [Serializable] public sealed class ArmRoomView
    {
        public TablePhase phase;public ArmPhase gamePhase;public ulong[] seated,participants,ready,waiting;public int[] seats;public string[] names;
        public ulong organizer;public uint proposal;public long openingFee,stake,pot,heldPot;public bool settled;public int winner,position,beat;
        public double start,end;public double[] feedbackTime;public float[] stamina;public long[] payouts;public int[] modes,efforts,feedback,pulses;public bool[] drunk;public string notice;public MatchReceipt receipt;
        public bool IsSeated(ulong id)=>seated!=null&&Array.IndexOf(seated,id)>=0;
        public bool IsParticipant(ulong id)=>participants!=null&&Array.IndexOf(participants,id)>=0;
        public int SeatOf(ulong id){int i=seated==null?-1:Array.IndexOf(seated,id);return i>=0?seats[i]:-1;}
    }
    [Serializable] public sealed class LedgerStatsView
    {
        public ulong id; public string name,won,lost,spent,staked,discarded; public bool connected;
        public long balance; public int wins,draws,wagersWon,falls,drinks;
    }
    [Serializable] public sealed class LanStatsPage
    {
        public int page,pages; public string issued,bankIssued,barRevenue,destroyed,audit,reportId; public bool final; public LedgerStatsView[] players;
    }
}
