using System;
using UnityEngine;
namespace TavernChaos
{
    public enum LanAction { Move, Charge, Release, Interact, Drop, Stop, ProposeKick, VoteYes, Reset }
    [Serializable] public sealed class LanIntent
    {
        public ulong sequence, target; public LanAction action; public Vector3 move; public float yaw,pitch; public bool run,hasLook;
    }
    [Serializable] public sealed class LanActorState
    {
        public ulong id; public string name; public Vector3 position, focus,eye; public float yaw, recovery, effect; public bool fallen; public float charge=-1; public int held=-1, chips,resetRevision;
        public LanBodyPose[] body;
    }
    [Serializable] public sealed class LanBodyPose { public Vector3 position; public Quaternion rotation; }
    public enum LanFeedbackKind { PunchHit, Miss, BottleHit, Drink, Grab, Throw, Slip }
    [Serializable] public sealed class LanFeedback { public ulong id; public LanFeedbackKind kind; public Vector3 position; }
    [Serializable] public sealed class LanBottleState
    {
        public Vector3 position; public Quaternion rotation; public bool held, flying;
    }
    [Serializable] public sealed class LanSnapshot
    {
        public ulong revision; public double serverTime; public float secondsRemaining;
        public LanActorState[] actors; public LanActorState rival; public LanBottleState[] bottles;
        public string notice; public bool voteActive; public ulong voteTarget; public int votes,votesNeeded;
    }
    [Serializable] public sealed class LanHello { public int protocol=3; public string name; }
}
