using System;
using UnityEngine;
namespace TavernChaos
{
    public enum LanAction { Move, Charge, Release, Interact, Drop, Stop, ProposeKick, VoteYes }
    [Serializable] public sealed class LanIntent
    {
        public ulong sequence, target; public LanAction action; public Vector3 move; public float yaw; public bool run;
    }
    [Serializable] public sealed class LanActorState
    {
        public ulong id; public string name; public Vector3 position, focus; public float yaw, recovery, effect; public bool fallen; public float charge=-1; public int held=-1, chips;
    }
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
    [Serializable] public sealed class LanHello { public int protocol=1; public string name; }
}
