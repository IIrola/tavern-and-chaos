using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TavernChaos
{
    // NGO/UTP is the transport. The application sends intents and public snapshots, never client transforms.
    public sealed class LanSession : MonoBehaviour
    {
        private const string IntentMessage="tavern.intent.1", SnapshotMessage="tavern.snapshot.1";
        private const int PacketLimit=24000;
        private sealed class Controlled
        {
            public TavernActor actor; public Vector3 move; public bool run; public float yaw;
            public double inputAt, chargeAt=-1, cooldown, attackEnd=-1; public float power;
            public ThrowableBottle held;
        }
        public bool Running=>manager&&manager.IsListening;
        public bool IsHost=>Running&&manager.IsServer;
        public bool Connected=>Running&&manager.IsConnectedClient;
        public ulong LocalId=>manager?manager.LocalClientId:0;
        public LanSnapshot View { get; private set; }
        public RoomRoster Roster { get; private set; }
        public string Status { get; private set; }="";
        public string Address { get; private set; }
        public string RoomCode { get; private set; }
        public int SnapshotsReceived { get; private set; }
        public long BytesReceived { get; private set; }
        public int RejectedActions { get; private set; }
        public IReadOnlyDictionary<ulong,TavernActor> RemoteActors=>replicas;
        public TavernActor ActorFor(ulong id) => IsHost ? controlled.TryGetValue(id,out var c)?c.actor:null : replicas.TryGetValue(id,out var a)?a:null;
        private NetworkManager manager;
        private UnityTransport transport;
        private DemoGame g;
        private readonly Dictionary<ulong,Controlled> controlled=new();
        private readonly Dictionary<ulong,TavernActor> replicas=new();
        private readonly Dictionary<ulong,string> approvedNames=new();
        private readonly Queue<(ulong sender,LanIntent intent)> pending=new();
        private ulong nextSequence, nextRevision, lastRevision;
        private double lastSend, lastInputSend, snapshotAt, connectAt, startedAt, endsAt;
        private bool leaving;
        private string serverNotice="";
        private void Awake() { g=GetComponent<DemoGame>(); }
        public bool Host(string name,ushort port=7777,int minutes=30)
        {
            if(Running) return false;
            g.Begin(); Prepare(name,port,"127.0.0.1",true); Address=FindAddress();
            RoomCode=EncodeCode(Address,port); Roster=new RoomRoster(); startedAt=Time.realtimeSinceStartupAsDouble; endsAt=startedAt+Mathf.Clamp(minutes,20,60)*60;
            Status="Abriendo sala…";
            if(!manager.StartHost()) { Fail("No se pudo abrir el puerto de la sala."); return false; }
            RegisterMessages(); Application.runInBackground=true; Status="Sala abierta · "+RoomCode; return true;
        }
        public bool Join(string name,string addressOrCode,ushort port=7777)
        {
            if(Running) return false;
            if(TryDecodeCode(addressOrCode,out string decoded,out ushort codePort)) { addressOrCode=decoded; port=codePort; }
            if(!IPAddress.TryParse(addressOrCode,out var ip)||ip.AddressFamily!=AddressFamily.InterNetwork) { Status="Introduce una dirección IPv4 o un código de sala válido."; return false; }
            g.Begin(); Prepare(name,port,addressOrCode,false); Address=addressOrCode; RoomCode=EncodeCode(Address,port); connectAt=Time.realtimeSinceStartupAsDouble;
            SetClientSimulation(true); Status="Conectando a la sala…"; Application.runInBackground=true;
            if(!manager.StartClient()) { Fail("No se pudo iniciar la conexión."); return false; } RegisterMessages(); return true;
        }
        private void Prepare(string name,ushort port,string address,bool host)
        {
            leaving=false; nextSequence=nextRevision=lastRevision=0; lastSend=snapshotAt=0; View=null; SnapshotsReceived=0; BytesReceived=0; RejectedActions=0;
            var root=new GameObject("LAN transport"); manager=root.AddComponent<NetworkManager>(); transport=root.AddComponent<UnityTransport>();
            manager.NetworkConfig=new NetworkConfig { NetworkTransport=transport, EnableSceneManagement=false, ConnectionApproval=true, TickRate=30, ClientConnectionBufferTimeout=8, ConnectionData=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new LanHello{name=RoomRoster.CleanName(name)})) };
            transport.SetConnectionData(address,port,host?"0.0.0.0":null);
            manager.ConnectionApprovalCallback=Approve;
            manager.OnClientConnectedCallback+=OnConnected; manager.OnClientDisconnectCallback+=OnDisconnected;
        }
        private void RegisterMessages()
        {
            manager.CustomMessagingManager.RegisterNamedMessageHandler(IntentMessage,ReadIntent);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(SnapshotMessage,ReadSnapshot);
        }
        private void Approve(NetworkManager.ConnectionApprovalRequest request,NetworkManager.ConnectionApprovalResponse response)
        {
            string reason=""; LanHello hello=null;
            if(request.Payload==null||request.Payload.Length>256) reason="Solicitud de entrada inválida.";
            else { try { hello=JsonUtility.FromJson<LanHello>(Encoding.UTF8.GetString(request.Payload)); } catch { reason="Solicitud de entrada inválida."; } }
            if(hello==null||hello.protocol!=1) reason="Las versiones del juego no coinciden.";
            if(Roster!=null&&Roster.Members.Count+approvedNames.Count>=RoomRoster.Capacity) reason="Sala llena: máximo seis personas.";
            response.Approved=reason.Length==0; response.CreatePlayerObject=false; response.Pending=false; response.Reason=reason;
            if(response.Approved) approvedNames[request.ClientNetworkId]=RoomRoster.CleanName(hello.name);
        }
        private void OnConnected(ulong id)
        {
            if(!manager.IsServer) { if(id==LocalId) Status="Dentro de la sala"; return; }
            string name=approvedNames.TryGetValue(id,out var approved)?approved:"Anfitrión"; approvedNames.Remove(id);
            string identity=FindAddress();
            if(id!=NetworkManager.ServerClientId) {
                using var bytes=transport.GetEndpoint(id).GetRawAddressBytes();
                identity=bytes.Length==4||bytes.Length==16?new IPAddress(bytes.ToArray()).ToString():"unresolved-endpoint";
            }
            var member=Roster.Add(id,name,identity,Time.realtimeSinceStartupAsDouble);
            if(member==null) { manager.DisconnectClient(id); return; }
            TavernActor actor=id==NetworkManager.ServerClientId?g.Player:g.CreateRoomActor(name,new Vector3(-1.5f+member.Slot*.65f,.08f,-6));
            controlled.Add(id,new Controlled{actor=actor,inputAt=Time.realtimeSinceStartupAsDouble}); serverNotice=name+" ha entrado.";
            Status="Sala abierta · "+RoomCode;
        }
        private void OnDisconnected(ulong id)
        {
            if(leaving||!manager) return;
            if(manager.IsServer) {
                approvedNames.Remove(id);
                if(controlled.TryGetValue(id,out var c)) { if(c.held) c.held.ResetToRack(); if(c.actor!=g.Player) Destroy(c.actor.gameObject); controlled.Remove(id); }
                Roster?.Remove(id); serverNotice="Una persona ha salido de la sala.";
            } else if(id==LocalId||!manager.IsConnectedClient) {
                string reason=manager.DisconnectReason;
                if(g.SmokeMode) Debug.Log("LAN_DISCONNECT: "+reason);
                bool explicitReason=!string.IsNullOrWhiteSpace(reason)&&(reason.Contains("Sala llena")||reason.Contains("Expulsado")||reason.Contains("versiones"));
                Fail(explicitReason?reason:"El anfitrión cerró la sala o se perdió la conexión.");
            }
        }
        public void Leave()
        {
            if(!manager) return; leaving=true;
            if(manager.IsServer) foreach(var c in controlled.Values) { if(c.held) c.held.ResetToRack(); if(c.actor!=g.Player) Destroy(c.actor.gameObject); }
            controlled.Clear(); approvedNames.Clear(); pending.Clear();
            manager.OnClientConnectedCallback-=OnConnected; manager.OnClientDisconnectCallback-=OnDisconnected;
            manager.Shutdown(); Destroy(manager.gameObject); manager=null;
            foreach(var actor in replicas.Values) if(actor&&actor!=g.Player) Destroy(actor.gameObject); replicas.Clear();
            SetClientSimulation(false); g.SetRoomHeld(null); View=null; Roster=null; Application.runInBackground=g.SmokeMode; Status="Fuera de la sala";
        }
        private void Fail(string reason)
        {
            Leave(); Status=reason; g.Menu(); g.Notify(reason);
        }
        private void Update()
        {
            if(!Running) return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(!IsHost&&!Connected&&now-connectAt>10) Fail("La sala no responde. Comprueba el código y la red local.");
            if(!IsHost&&Connected&&(SnapshotsReceived==0?now-connectAt:now-lastSend)>10) Fail("La conexión con el anfitrión se ha interrumpido.");
        }
        public void Send(LanAction action,Vector3 movement=default,bool run=false,float yaw=0,ulong target=0)
        {
            if(!Connected) return;
            var intent=new LanIntent { sequence=++nextSequence,action=action,move=movement,run=run,yaw=yaw,target=target };
            if(IsHost) pending.Enqueue((LocalId,intent));
            else { using var writer=new FastBufferWriter(1024,Allocator.Temp); writer.WriteValueSafe(JsonUtility.ToJson(intent)); manager.CustomMessagingManager.SendNamedMessage(IntentMessage,NetworkManager.ServerClientId,writer,NetworkDelivery.ReliableSequenced); }
        }
        public void LocalMove(Vector3 move,bool run,float yaw)
        {
            double now=Time.realtimeSinceStartupAsDouble; if(now-lastInputSend<.04) return; lastInputSend=now; Send(LanAction.Move,move,run,yaw);
        }
        private void ReadIntent(ulong sender,FastBufferReader reader)
        {
            if(!IsHost||reader.Length>2048) return;
            try { reader.ReadValueSafe(out string json); var intent=JsonUtility.FromJson<LanIntent>(json); if(intent!=null) pending.Enqueue((sender,intent)); } catch { RejectedActions++; }
            if(pending.Count>512) pending.Clear();
        }
        private void ReadSnapshot(ulong sender,FastBufferReader reader)
        {
            if(IsHost||sender!=NetworkManager.ServerClientId||reader.Length>PacketLimit) return;
            try {
                reader.ReadValueSafe(out string json); var s=JsonUtility.FromJson<LanSnapshot>(json);
                if(s==null||s.revision<=lastRevision||s.actors==null||s.actors.Length>6) return;
                lastRevision=s.revision; lastSend=Time.realtimeSinceStartupAsDouble; SnapshotsReceived++; BytesReceived+=reader.Length; View=s; ApplySnapshot(s);
            } catch { RejectedActions++; }
        }
        public void FixedTick()
        {
            if(!IsHost) return; double now=Time.realtimeSinceStartupAsDouble;
            while(pending.Count>0) { var command=pending.Dequeue(); ApplyIntent(command.sender,command.intent,now); }
            foreach(var c in controlled.Values) {
                c.actor.TickRecovery();
                if(c.actor.Fallen) { c.chargeAt=c.attackEnd=-1; if(c.held) { c.held.Release(Vector3.up); c.held=null; } }
                else {
                    c.actor.Move(now-c.inputAt>.3?Vector3.zero:c.move,c.run);
                    if(c.chargeAt>=0) { c.actor.Aim(Yaw(c.yaw)); c.actor.PunchPose(Mathf.Clamp01((float)(now-c.chargeAt))); }
                }
            }
            g.Rival.TickRecovery(); if(!g.Rival.Fallen) g.Rival.Move(Vector3.zero,false);
            ResolveAttacks(now);
            ulong? kick=Roster.ResolveVote(now); if(kick.HasValue) { if(kick==0) { Fail("La votación cerró la sala del anfitrión."); return; } manager.DisconnectClient(kick.Value,"Expulsado por votación de la sala."); }
            g.Pool.FixedTick();
            if(now>=endsAt) { Fail("Ha terminado el tiempo de la sala."); return; }
            if(now>=snapshotAt) { snapshotAt=now+.05; Publish(now); }
        }
        private void ApplyIntent(ulong sender,LanIntent intent,double now)
        {
            if(!Roster.Accept(sender,intent.sequence,now)||!controlled.TryGetValue(sender,out var c)) return;
            if(!Finite(intent.move)||!float.IsFinite(intent.yaw)||Mathf.Abs(intent.yaw)>100000) { RejectedActions++; return; }
            if(intent.action==LanAction.Move) { c.move=Vector3.ClampMagnitude(new Vector3(intent.move.x,0,intent.move.z),1); c.run=intent.run; c.yaw=intent.yaw; c.inputAt=now; return; }
            if(intent.action==LanAction.Stop) { c.move=Vector3.zero; c.chargeAt=-1; return; }
            if(intent.action==LanAction.ProposeKick) { Roster.BeginVote(sender,intent.target,now); return; }
            if(intent.action==LanAction.VoteYes) { Roster.Vote(sender); return; }
            if(c.actor.Fallen) { RejectedActions++; return; }
            switch(intent.action) {
                case LanAction.Charge:
                    if(c.chargeAt<0&&now>=c.cooldown) c.chargeAt=now; else RejectedActions++; break;
                case LanAction.Release:
                    if(c.chargeAt<0||now<c.cooldown) { RejectedActions++; return; }
                    c.power=Mathf.Clamp01((float)(now-c.chargeAt)); c.chargeAt=-1; c.cooldown=now+.6; c.actor.Aim(Yaw(c.yaw));
                    if(c.held) { c.held.Release((Yaw(c.yaw)+Vector3.up*.1f).normalized*Mathf.Lerp(7,15,c.power)); c.held=null; }
                    else { c.attackEnd=now+.25; c.actor.Swing(); } break;
                case LanAction.Drop: c.chargeAt=-1; if(c.held) { c.held.Release(Yaw(c.yaw)*1.5f); c.held=null; } break;
                case LanAction.Interact: Interact(c,sender); break;
                default: RejectedActions++; break;
            }
        }
        private void Interact(Controlled c,ulong sender)
        {
            if(c.held||c.chargeAt>=0||c.attackEnd>=0) return;
            if(Vector3.Distance(c.actor.transform.position,TavernWorld.BarPoint)<2.2f) {
                if(Roster.Spend(sender,20)) c.actor.Drink(); else serverNotice="No tienes fichas suficientes para esa bebida."; return;
            }
            ThrowableBottle best=null; float bestAim=-1, distance=2.05f;
            foreach(var b in g.Bottles) if(!b.Held&&!b.Flying) {
                Vector3 delta=b.transform.position-c.actor.Focus; float d=delta.magnitude, aim=Vector3.Dot(Yaw(c.yaw),delta.normalized);
                bool better=aim>bestAim+.0001f||(Mathf.Abs(aim-bestAim)<.0001f&&d<distance);
                if(d<2.05f&&aim>.25f&&better&&!Physics.Linecast(c.actor.Focus,b.transform.position,1<<0,QueryTriggerInteraction.Ignore)) { best=b; bestAim=aim; distance=d; }
            }
            if(best) { best.Grab(c.actor); c.held=best; }
        }
        private void ResolveAttacks(double now)
        {
            var falls=new List<(TavernActor actor,Vector3 force)>();
            foreach(var c in controlled.Values) if(c.attackEnd>=0&&!c.actor.Fallen) {
                TavernActor target=null; float distance=2.05f; Vector3 direction=Yaw(c.yaw);
                foreach(var candidate in CombatTargets()) if(candidate!=c.actor&&!candidate.Fallen) {
                    Vector3 d=candidate.transform.position-c.actor.transform.position; d.y=0;
                    if(d.magnitude<distance&&Vector3.Dot(direction,d.normalized)>.45f&&!Physics.Linecast(c.actor.Focus,candidate.Focus,1<<0,QueryTriggerInteraction.Ignore)) { target=candidate; distance=d.magnitude; }
                }
                if(target) { falls.Add((target,direction*Mathf.Lerp(3.4f,6,c.power))); c.attackEnd=-1; }
                else if(now>=c.attackEnd) { falls.Add((c.actor,-direction*2.2f)); c.attackEnd=-1; }
            }
            foreach(var fall in falls) fall.actor.Fall(fall.force);
        }
        public IEnumerable<TavernActor> CombatTargets()
        {
            yield return g.Rival; foreach(var c in controlled.Values) yield return c.actor;
        }
        private LanActorState State(ulong id,string name,TavernActor actor,int chips,int held=-1) => new() {
            id=id,name=name,position=actor.transform.position,focus=actor.Focus,yaw=actor.transform.eulerAngles.y,
            fallen=actor.Fallen,recovery=actor.Fallen?Mathf.Max(0,actor.RecoverAt-Time.time):0,effect=actor.EffectRemaining,chips=chips,held=held
        };
        private void Publish(double now)
        {
            var actors=new List<LanActorState>();
            foreach(var pair in controlled) {
                var m=Roster.Members[pair.Key]; var c=pair.Value;
                var state=State(pair.Key,m.Name,c.actor,m.Chips,c.held?g.Bottles.IndexOf(c.held):-1);
                state.charge=c.chargeAt<0?-1:Mathf.Clamp01((float)(now-c.chargeAt)); actors.Add(state);
            }
            var bottles=new LanBottleState[g.Bottles.Count];
            for(int i=0;i<bottles.Length;i++) { var b=g.Bottles[i]; bottles[i]=new(){position=b.transform.position,rotation=b.transform.rotation,held=b.Held,flying=b.Flying}; }
            var snapshot=new LanSnapshot { revision=++nextRevision,serverTime=now,actors=actors.ToArray(),rival=State(ulong.MaxValue,"Bruno",g.Rival,0),bottles=bottles,notice=serverNotice,secondsRemaining=(float)(endsAt-now),voteActive=Roster.VoteTarget.HasValue,voteTarget=Roster.VoteTarget??0,votes=Roster.Votes,votesNeeded=Roster.VotesNeeded };
            View=snapshot; g.SetRoomHeld(controlled.TryGetValue(0,out var local)?local.held:null);
            string json=JsonUtility.ToJson(snapshot); using var writer=new FastBufferWriter(PacketLimit,Allocator.Temp); writer.WriteValueSafe(json);
            foreach(ulong id in manager.ConnectedClientsIds) if(id!=0) manager.CustomMessagingManager.SendNamedMessage(SnapshotMessage,id,writer,NetworkDelivery.ReliableFragmentedSequenced);
        }
        private void ApplySnapshot(LanSnapshot snapshot)
        {
            var alive=new HashSet<ulong>();
            foreach(var state in snapshot.actors) {
                alive.Add(state.id);
                if(!replicas.TryGetValue(state.id,out var actor)) { actor=state.id==LocalId?g.Player:g.CreateRoomActor(state.name,state.position); actor.SetReplica(true); replicas.Add(state.id,actor); }
                actor.ApplyReplica(state.position,state.focus,state.yaw,state.fallen,state.recovery,state.effect); actor.ReplicaCharge=state.charge;
                if(state.id==LocalId) g.SetRoomHeld(state.held>=0&&state.held<g.Bottles.Count?g.Bottles[state.held]:null);
            }
            var removed=new List<ulong>(); foreach(var p in replicas) if(!alive.Contains(p.Key)) removed.Add(p.Key);
            foreach(ulong id in removed) { if(replicas[id]!=g.Player) Destroy(replicas[id].gameObject); replicas.Remove(id); }
            if(snapshot.rival!=null) { var s=snapshot.rival; g.Rival.ApplyReplica(s.position,s.focus,s.yaw,s.fallen,s.recovery,s.effect); }
            if(snapshot.bottles!=null) for(int i=0;i<Math.Min(snapshot.bottles.Length,g.Bottles.Count);i++) g.Bottles[i].ApplyReplica(snapshot.bottles[i]);
        }
        private void SetClientSimulation(bool replica)
        {
            g.Player.SetReplica(replica); g.Rival.SetReplica(replica); foreach(var b in g.Bottles) b.SetReplica(replica);
        }
        public static string FindAddress()
        {
            try { foreach(var a in Dns.GetHostEntry(Dns.GetHostName()).AddressList) if(a.AddressFamily==AddressFamily.InterNetwork&&!IPAddress.IsLoopback(a)) return a.ToString(); } catch { }
            return "127.0.0.1";
        }
        public static string EncodeCode(string address,ushort port)
        {
            if(!IPAddress.TryParse(address,out var ip)||ip.AddressFamily!=AddressFamily.InterNetwork) return "";
            var b=ip.GetAddressBytes(); return $"{b[0]:X2}{b[1]:X2}{b[2]:X2}{b[3]:X2}-{port:X4}";
        }
        public static bool TryDecodeCode(string code,out string address,out ushort port)
        {
            address=""; port=7777; if(code==null||code.Length!=13||code[8]!='-') return false;
            try { var b=new byte[4]; for(int i=0;i<4;i++) b[i]=Convert.ToByte(code.Substring(i*2,2),16); port=Convert.ToUInt16(code.Substring(9,4),16); address=new IPAddress(b).ToString(); return port>0; } catch { return false; }
        }
        private static Vector3 Yaw(float yaw)=>Quaternion.Euler(0,yaw,0)*Vector3.forward;
        private static bool Finite(Vector3 p)=>float.IsFinite(p.x)&&float.IsFinite(p.y)&&float.IsFinite(p.z);
        private void OnDestroy() { Leave(); }
    }
}
