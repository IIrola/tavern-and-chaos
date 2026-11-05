using System;
using UnityEngine;
namespace TavernChaos
{
    public enum LanAction { Move, Charge, Release, Interact, Drop, Stop, ProposeKick, VoteYes, Reset, PoolSit, PoolLeave, PoolStart, PoolCall, PoolShoot, PoolPlace, PoolAccept, PoolRerack, PoolClose }
    [Serializable] public sealed class LanIntent
    {
        public ulong sequence, target; public LanAction action; public Vector3 move; public float yaw,pitch,power; public bool run,hasLook; public int ball,pocket;
    }
    [Serializable] public sealed class LanActorState
    {
        public ulong id; public string name; public Vector3 position, focus,eye; public float yaw, recovery, effect; public bool fallen; public float charge=-1; public int held=-1, chips,resetRevision;
        public LanBodyPose[] body; public bool seated;
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
    }
    [Serializable] public sealed class LanHello { public int protocol=4; public string name; }
    [Serializable] public sealed class PoolBallView { public int number; public bool active; public Vector3 position; public Quaternion rotation; }
    [Serializable] public sealed class PoolRoomView
    {
        public TablePhase phase; public EightBallPhase playPhase; public string notice;
        public ulong[] seated,participants,waiting; public int[] seats,groups;
        public bool hasOrganizer,hasTurn,breaking,headOnly,foul; public ulong organizer,turn;
        public int winner=-1,callBall,callPocket=-1,shots; public float absenceRemaining;
        public PoolBallView[] balls;
        public bool IsSeated(ulong id)=>seated!=null&&Array.IndexOf(seated,id)>=0;
        public bool IsParticipant(ulong id)=>participants!=null&&Array.IndexOf(participants,id)>=0;
    }
}
