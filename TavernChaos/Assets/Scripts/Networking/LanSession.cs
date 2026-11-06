using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Globalization;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TavernChaos
{
    // NGO/UTP is the transport. The application sends intents and public snapshots, never client transforms.
    public sealed class LanSession : MonoBehaviour
    {
        private const string IntentMessage="tavern.intent.8", SnapshotMessage="tavern.snapshot.8", FeedbackMessage="tavern.feedback.8", StatsMessage="tavern.stats.8";
        private const int PacketLimit=24000;
        private sealed class Controlled
        {
            public TavernActor actor; public Vector3 move,attackDirection; public bool run; public float yaw,pitch;
            public int resetRevision;
            public double inputAt, chargeAt=-1, cooldown, attackEnd=-1; public float power;
            public double statsAt=-1;
            public ThrowableBottle held;
            public string table="";
            public Vector3 returnPosition; public Quaternion returnRotation;
        }
        public bool Running=>manager&&manager.IsListening;
        public bool IsHost=>Running&&manager.IsServer;
        public bool Connected=>Running&&manager.IsConnectedClient;
        public ulong LocalId=>manager?manager.LocalClientId:0;
        public LanSnapshot View { get; private set; }
        public RoomRoster Roster { get; private set; }
        public PoolRoomController PoolTable { get; private set; }
        public ArmRoomController ArmTable {get;private set;}
        private readonly List<IRoomTable> tables=new();
        private readonly HashSet<IRoomTable> drainedTables=new();
        public bool LocalArmSeated=>IsHost?ArmTable?.Table.SeatOf(LocalId)>=0:View?.arm?.IsSeated(LocalId)==true;
        public LanStatsPage StatsPage { get; private set; }
        public SessionEndingView Ending { get; private set; }
        public RoomLifecycle Lifecycle { get; private set; }
        public bool BlocksPlay=>Ending!=null&&Ending.phase!=RoomPhase.Open;
        public bool ResultsDisconnected { get; private set; }
        public int FinalPagesReceived=>finalPages.Count;
        public string LocalName { get; private set; }
        public ushort Port { get; private set; }
        public int Minutes { get; private set; }=30;
        public bool CanReadStatsPage(int page)=>finalPages.ContainsKey(page)||Running;
        public double EstimatedServerTime=>IsHost?Time.realtimeSinceStartupAsDouble:serverTimeAtReceipt+Time.realtimeSinceStartupAsDouble-endingReceivedAt;
        private readonly Dictionary<int,LanStatsPage> finalPages=new();
        private string reportId;
        private double serverTimeAtReceipt,endingReceivedAt,nextFinalPageRequest;
        private long minimumPurchase;
        private bool finalFrozen,tableDrained;
        private int desiredStatsPage;
        private int finalParticipants;
        internal void ExpireForCheck() { if(g.SmokeMode&&IsHost) Lifecycle.Expire(Time.realtimeSinceStartupAsDouble); }
        public bool CanLocalPoolAct=>Connected&&!BlocksPlay&&View?.pool is PoolRoomView table&&table.phase==TablePhase.Playing&&table.hasTurn&&table.turn==LocalId&&table.IsSeated(LocalId);
        public string Status { get; private set; }="";
        public string Address { get; private set; }
        public string RoomCode { get; private set; }
        public int SnapshotsReceived { get; private set; }
        public long BytesReceived { get; private set; }
        public int RejectedActions { get; private set; }
        public int FeedbackPresented { get; private set; }
        public ulong RoundTripMilliseconds => Connected&&!IsHost?transport.GetCurrentRtt(NetworkManager.ServerClientId):0;
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
        private ulong nextFeedback,lastFeedback;
        private ulong fallSerial;
        private long drinkPrice;
        private readonly HashSet<string> seenReceipts=new();
        private readonly Dictionary<LanFeedbackKind,int> feedbackCounts=new();
        public int FeedbackCount(LanFeedbackKind kind)=>feedbackCounts.TryGetValue(kind,out var count)?count:0;
        private int localResetRevision=-1;
        private double lastSend, lastInputSend, snapshotAt, connectAt, startedAt, endsAt;
        private bool leaving;
        private string serverNotice="";
        private void Awake() { g=GetComponent<DemoGame>(); }
        public bool Host(string name,ushort port=7777,int minutes=30)
        {
            if(manager) return false;
            if(!RoomLifecycle.ValidMinutes(minutes)) { Status="Duración: elige 20, 30, 45 o 60 minutos."; return false; }
            Minutes=minutes;
            g.Begin(); Prepare(name,port,"127.0.0.1",true); Address=FindAddress();
            RoomCode=EncodeCode(Address,port); Roster=new RoomRoster(new SessionLedger(g.Economy.StartingChips)); startedAt=Time.realtimeSinceStartupAsDouble; endsAt=startedAt+minutes*60; Lifecycle=new RoomLifecycle(endsAt);
            PoolTable=new PoolRoomController(g.Pool,Roster,ActorFor,ChangePoolSeat,g.Economy);
            ArmTable=new ArmRoomController(Roster,ActorFor,ChangeArmSeat,g.Economy);tables.Add(PoolTable);tables.Add(ArmTable);
            drinkPrice=g.Economy.Drink;
            minimumPurchase=g.Economy.MinimumPurchase;
            Status="Abriendo sala…";
            if(!manager.StartHost()) { Fail("No se pudo abrir el puerto de la sala."); return false; }
            RegisterMessages(); Application.runInBackground=true; Status="Sala abierta · "+RoomCode; return true;
        }
        public bool Join(string name,string addressOrCode,ushort port=7777)
        {
            if(manager) return false;
            if(TryDecodeCode(addressOrCode,out string decoded,out ushort codePort)) { addressOrCode=decoded; port=codePort; }
            if(!IPAddress.TryParse(addressOrCode,out var ip)||ip.AddressFamily!=AddressFamily.InterNetwork) { Status="Introduce una dirección IPv4 o un código de sala válido."; return false; }
            g.Begin(); Prepare(name,port,addressOrCode,false); Address=addressOrCode; RoomCode=EncodeCode(Address,port); connectAt=Time.realtimeSinceStartupAsDouble;
            SetClientSimulation(true); Status="Conectando a la sala…"; Application.runInBackground=true;
            if(!manager.StartClient()) { Fail("No se pudo iniciar la conexión."); return false; } RegisterMessages(); return true;
        }
        private void Prepare(string name,ushort port,string address,bool host)
        {
            ClearEnding(); reportId=Guid.NewGuid().ToString("N");
            LocalName=RoomRoster.CleanName(name); Port=port;
            leaving=false; nextSequence=nextRevision=lastRevision=nextFeedback=lastFeedback=0; feedbackCounts.Clear(); FeedbackPresented=0; lastSend=lastInputSend=snapshotAt=0; localResetRevision=-1; View=null; SnapshotsReceived=0; BytesReceived=0; RejectedActions=0;
            StatsPage=null; fallSerial=0; seenReceipts.Clear();
            g.Notify("Busca una mesa, la barra o las botellas. Acércate y pulsa E / A.");
            var root=new GameObject("LAN transport"); manager=root.AddComponent<NetworkManager>(); transport=root.AddComponent<UnityTransport>();
            manager.NetworkConfig=new NetworkConfig { NetworkTransport=transport, EnableSceneManagement=false, ConnectionApproval=true, TickRate=30, ClientConnectionBufferTimeout=8, ConnectionData=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new LanHello{name=RoomRoster.CleanName(name)})) };
            transport.SetConnectionData(address,port,host?(g.SmokeMode?"127.0.0.1":"0.0.0.0"):null);
            manager.ConnectionApprovalCallback=Approve;
            manager.OnClientConnectedCallback+=OnConnected; manager.OnClientDisconnectCallback+=OnDisconnected;
        }
        private void RegisterMessages()
        {
            manager.CustomMessagingManager.RegisterNamedMessageHandler(IntentMessage,ReadIntent);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(SnapshotMessage,ReadSnapshot);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(FeedbackMessage,ReadFeedback);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(StatsMessage,ReadStats);
        }
        private void Approve(NetworkManager.ConnectionApprovalRequest request,NetworkManager.ConnectionApprovalResponse response)
        {
            string reason=""; LanHello hello=null;
            if(request.Payload==null||request.Payload.Length>256) reason="Solicitud de entrada inválida.";
            else { try { hello=JsonUtility.FromJson<LanHello>(Encoding.UTF8.GetString(request.Payload)); } catch { reason="Solicitud de entrada inválida."; } }
            if(hello==null||hello.protocol!=8) reason="Las versiones del juego no coinciden.";
            if(Roster!=null&&Roster.Members.Count+approvedNames.Count>=RoomRoster.Capacity) reason="Sala llena: máximo seis personas.";
            if(Lifecycle!=null&&!Lifecycle.Accepting(Time.realtimeSinceStartupAsDouble)) reason="La sala está terminando; no admite nuevas entradas.";
            response.Approved=reason.Length==0; response.CreatePlayerObject=false; response.Pending=false; response.Reason=reason;
            if(response.Approved) approvedNames[request.ClientNetworkId]=RoomRoster.CleanName(hello.name);
        }
        private void OnConnected(ulong id)
        {
            if(!manager.IsServer) { if(id==LocalId) Status="Dentro de la sala"; return; }
            if(!Lifecycle.Accepting(Time.realtimeSinceStartupAsDouble)) { approvedNames.Remove(id); manager.DisconnectClient(id,"La sala está terminando; no admite nuevas entradas."); return; }
            string name=approvedNames.TryGetValue(id,out var approved)?approved:"Anfitrión"; approvedNames.Remove(id);
            string identity=FindAddress();
            if(id!=NetworkManager.ServerClientId) {
                using var bytes=transport.GetEndpoint(id).GetRawAddressBytes();
                identity=bytes.Length==4||bytes.Length==16?new IPAddress(bytes.ToArray()).ToString():"unresolved-endpoint";
            }
            int slot=Roster.NextFreeSlot;
            if(slot<0) { manager.DisconnectClient(id); return; }
            Vector3 preferred=SpawnPoint(slot);
            if(!TavernActor.TryFindStandingPosition(preferred,id==0?g.Player:null,out var spawn)) { manager.DisconnectClient(id,"No hay un punto libre para aparecer."); return; }
            var member=Roster.Add(id,name,identity,Time.realtimeSinceStartupAsDouble);
            if(member==null) { manager.DisconnectClient(id); return; }
            TavernActor actor=id==NetworkManager.ServerClientId?g.Player:g.CreateRoomActor(name,spawn);
            actor.Teleport(spawn,Quaternion.identity);
            controlled.Add(id,new Controlled{actor=actor,inputAt=Time.realtimeSinceStartupAsDouble}); serverNotice=name+" ha entrado.";
            Status="Sala abierta · "+RoomCode;
        }
        private void OnDisconnected(ulong id)
        {
            if(leaving||!manager) return;
            if(manager.IsServer) {
                foreach(var table in tables)table.Disconnect(id,Time.realtimeSinceStartupAsDouble);
                approvedNames.Remove(id);
                if(controlled.TryGetValue(id,out var c)) { if(c.held) c.held.ResetToRack(); if(c.actor!=g.Player) Destroy(c.actor.gameObject); controlled.Remove(id); }
                Roster?.Remove(id); serverNotice="Una persona ha salido de la sala.";
            } else if(id==LocalId||!manager.IsConnectedClient) {
                string reason=manager.DisconnectReason;
                if(BlocksPlay&&finalPages.Count>0) {
                    Leave(); ResultsDisconnected=true; Status="El anfitrión salió. Se conservan los resultados recibidos."; g.RefreshInputContext(); return;
                }
                if(g.SmokeMode) Debug.Log("LAN_DISCONNECT: "+reason);
                bool explicitReason=!string.IsNullOrWhiteSpace(reason)&&(reason.Contains("Sala llena")||reason.Contains("Expulsado")||reason.Contains("versiones")||reason.Contains("terminando"));
                Fail(explicitReason?reason:"El anfitrión cerró la sala o se perdió la conexión.");
            }
        }
        public void Leave()
        {
            if(!manager) return; leaving=true;
            foreach(var table in tables)table.Dispose();tables.Clear();PoolTable=null;ArmTable=null; Roster?.Ledger.Close(); g.Pool.EndMatch();
            foreach(var c in controlled.Values) c.actor.SetSeated(false);
            if(manager.IsServer) foreach(var c in controlled.Values) { if(c.held) c.held.ResetToRack(); if(c.actor!=g.Player) Destroy(c.actor.gameObject); }
            controlled.Clear(); approvedNames.Clear(); pending.Clear();
            manager.OnClientConnectedCallback-=OnConnected; manager.OnClientDisconnectCallback-=OnDisconnected;
            manager.Shutdown(); Destroy(manager.gameObject); manager=null;
            foreach(var actor in replicas.Values) if(actor&&actor!=g.Player) Destroy(actor.gameObject); replicas.Clear();
            SetClientSimulation(false); g.SetRoomHeld(null); g.Player.ClearAllEffects();g.Player.ClearIntoxication(); g.Rival.ClearAllEffects();g.Rival.ClearIntoxication(); View=null; Roster=null; localResetRevision=-1; Application.runInBackground=g.SmokeMode; Status="Fuera de la sala";
        }
        private void Fail(string reason)
        {
            Leave(); Status=reason; g.Menu(); g.Notify(reason);
        }
        private void Update()
        {
            if(ResultsDisconnected&&Ending?.phase==RoomPhase.Ejecting&&EstimatedServerTime-Ending.phaseStarted>=RoomLifecycle.EjectionSeconds) Ending.phase=RoomPhase.Results;
            if(!Running) return;
            double now=Time.realtimeSinceStartupAsDouble;
            if(!IsHost&&BlocksPlay&&Ending.pages>finalPages.Count&&now>=nextFinalPageRequest) {
                nextFinalPageRequest=now+.35;
                for(int page=0;page<Ending.pages;page++) if(!finalPages.ContainsKey(page)) { Send(LanAction.StatsRequest,page:page); break; }
            }
            if(!IsHost&&!Connected&&now-connectAt>10) Fail("La sala no responde. Comprueba el código y la red local.");
            if(!IsHost&&Connected&&(SnapshotsReceived==0?now-connectAt:now-lastSend)>10) Fail("La conexión con el anfitrión se ha interrumpido.");
        }
        public void Send(LanAction action,Vector3 movement=default,bool run=false,float? yaw=null,ulong target=0,float? pitch=null,int ball=0,int pocket=-1,float power=0,long wager=0,uint proposal=0,int page=0,int choice=0)
        {
            if(!Connected) return;
            var intent=new LanIntent { sequence=++nextSequence,action=action,move=movement,run=run,yaw=yaw??0,pitch=pitch??0,hasLook=yaw.HasValue||pitch.HasValue,target=target,ball=ball,pocket=pocket,power=power,wager=wager,proposal=proposal,page=page,choice=choice };
            if(IsHost) pending.Enqueue((LocalId,intent));
            else { using var writer=new FastBufferWriter(1024,Allocator.Temp); writer.WriteValueSafe(JsonUtility.ToJson(intent)); manager.CustomMessagingManager.SendNamedMessage(IntentMessage,NetworkManager.ServerClientId,writer,NetworkDelivery.ReliableSequenced); }
        }
        public void LocalMove(Vector3 move,bool run,float yaw,float pitch=0)
        {
            double now=Time.realtimeSinceStartupAsDouble; if(now-lastInputSend<.04) return; lastInputSend=now; Send(LanAction.Move,move,run,yaw,pitch:pitch);
        }
        public void CyclePoolCall(bool pocket)
        {
            var table=View?.pool; if(!CanLocalPoolAct||table.breaking||table.playPhase!=EightBallPhase.Aiming) return;
            int ball=table.callBall, calledPocket=Mathf.Max(0,table.callPocket);
            if(pocket) calledPocket=(calledPocket+1)%6;
            if(!PoolTargetAllowed(table,ball)||!pocket) for(int n=1;n<=15;n++) { int candidate=(Mathf.Max(0,ball)+n-1)%15+1; if(PoolTargetAllowed(table,candidate)) { ball=candidate; break; } }
            Send(LanAction.PoolCall,ball:ball,pocket:calledPocket);
        }
        public void ConfirmPoolSetup()
        {
            var t=View?.pool; if(t==null||t.phase!=TablePhase.Preparation) return;
            bool all=t.seated.Length==2&&t.ready!=null&&t.ready.Length==2;
            Send(all&&t.organizer==LocalId?LanAction.PoolStart:LanAction.PoolReady,proposal:t.proposal);
        }
        public void CyclePoolWager(bool previous=false)
        {
            var t=View?.pool; if(t==null||t.phase!=TablePhase.Preparation||t.organizer!=LocalId) return;
            long[] choices={0,10,25,50,100,250}; int i=Array.IndexOf(choices,t.stake); i=(Mathf.Max(0,i)+(previous?-1:1)+choices.Length)%choices.Length;
            Send(LanAction.PoolWager,wager:choices[i],proposal:t.proposal);
        }
        public void ConfirmArmSetup()
        {
            var t=View?.arm;if(t==null)return;
            if(t.phase==TablePhase.Result){Send(LanAction.ArmClose);return;}
            if(t.phase!=TablePhase.Preparation)return;
            Send(t.organizer==LocalId&&t.ready.Length==2?LanAction.ArmStart:LanAction.ArmReady,proposal:t.proposal);
        }
        public void CycleArmWager()
        {
            var t=View?.arm;if(t==null||t.phase!=TablePhase.Preparation||t.organizer!=LocalId)return;
            long[] choices={0,10,25,50,100,250};int i=Array.IndexOf(choices,t.stake);Send(LanAction.ArmWager,wager:choices[(Math.Max(i,0)+1)%choices.Length],proposal:t.proposal);
        }
        public void ClearEnding() { Ending=null; Lifecycle=null; finalPages.Clear();drainedTables.Clear(); StatsPage=null; finalFrozen=ResultsDisconnected=tableDrained=false; nextFinalPageRequest=0; desiredStatsPage=finalParticipants=0; }
        public void RequestStats(int page=0)
        {
            desiredStatsPage=Math.Max(0,page);
            if(finalPages.TryGetValue(desiredStatsPage,out var cached)) StatsPage=cached;
            else Send(LanAction.StatsRequest,page:page);
        }
        private LanStatsPage MakeStatsPage(int page)
        {
            var list=new List<SessionLedger.Account>(Roster.Ledger.Accounts); list.Sort((a,b)=>a.Id.CompareTo(b.Id));
            int pages=Mathf.Max(1,(list.Count+5)/6); page=Mathf.Clamp(page,0,pages-1); var rows=new List<LedgerStatsView>();
            string Number(decimal number)=>number.ToString("0",CultureInfo.InvariantCulture);
            for(int i=page*6;i<Mathf.Min(list.Count,(page+1)*6);i++) {
                var a=list[i]; rows.Add(new LedgerStatsView{id=a.Id,name=a.Name,connected=a.Connected,balance=a.Balance,wins=a.Wins,draws=a.Draws,wagersWon=a.WagersWon,falls=a.Falls,drinks=a.Drinks,won=Number(a.Won),lost=Number(a.Lost),spent=Number(a.Services),staked=Number(a.Staked),discarded=Number(a.Discarded)});
            }
            var ledger=Roster.Ledger;
            return new LanStatsPage{page=page,pages=pages,players=rows.ToArray(),issued=Number(ledger.InitialIssued),bankIssued=Number(ledger.BankIssued),barRevenue=Number(ledger.BarRevenue),destroyed=Number(ledger.Destroyed),audit=Number(ledger.AuditDelta)};
        }
        private void PublishStats(ulong recipient,int page,double now,bool force=false)
        {
            if(!controlled.TryGetValue(recipient,out var c)||!force&&now-c.statsAt<.25) return; c.statsAt=now;
            LanStatsPage result=finalFrozen?finalPages[Mathf.Clamp(page,0,finalPages.Count-1)]:MakeStatsPage(page);
            if(recipient==LocalId) StatsPage=result;
            else { using var writer=new FastBufferWriter(12000,Allocator.Temp); writer.WriteValueSafe(JsonUtility.ToJson(result)); manager.CustomMessagingManager.SendNamedMessage(StatsMessage,recipient,writer,NetworkDelivery.ReliableFragmentedSequenced); }
        }
        private void FreezeFinalReport(double now)
        {
            if(finalFrozen) return;
            if(!tableDrained) tableDrained=FinishTables(now);
            if(!tableDrained||Roster.Ledger.HasPending) return;
            Roster.CancelVote();
            foreach(var participant in controlled.Values)participant.actor.ClearAllEffects();
            g.Rival.ClearAllEffects();
            var first=MakeStatsPage(0); finalPages[0]=first;
            for(int page=1;page<first.pages;page++) finalPages[page]=MakeStatsPage(page);
            Roster.Ledger.Close();
            foreach(var page in finalPages.Values) { page.final=true; page.reportId=reportId; page.audit=Roster.Ledger.AuditDelta.ToString("0",CultureInfo.InvariantCulture); page.destroyed=Roster.Ledger.Destroyed.ToString("0",CultureInfo.InvariantCulture); }
            finalFrozen=true; StatsPage=first;
            finalParticipants=controlled.Count;
            foreach(ulong id in manager.ConnectedClientsIds) PublishStats(id,0,now,true);
        }
        private void ReadStats(ulong sender,FastBufferReader reader)
        {
            if(IsHost||sender!=NetworkManager.ServerClientId||reader.Length>12000) return;
            try {
                reader.ReadValueSafe(out string json); var result=JsonUtility.FromJson<LanStatsPage>(json);
                if(result?.players==null||result.players.Length>6||result.pages<1||result.page<0||result.page>=result.pages) return;
                if(result.final) {
                    if(string.IsNullOrEmpty(result.reportId)||Ending!=null&&Ending.reportId!=result.reportId) return;
                    finalPages[result.page]=result;
                    if(StatsPage==null||!StatsPage.final||result.page==desiredStatsPage) StatsPage=result;
                } else if(!BlocksPlay) StatsPage=result;
            } catch { RejectedActions++; }
        }
        public bool PoolTargetAllowed(PoolRoomView table,int ball)
        {
            if(ball<1||ball>15||table.balls==null||!table.balls[ball].active) return false;
            int side=Array.IndexOf(table.participants,LocalId); if(side<0||table.groups.Length<=side) return false;
            BallGroup group=(BallGroup)table.groups[side];
            if(group==BallGroup.Open) return ball!=8;
            bool left=false; for(int i=1;i<16;i++) if(EightBallRules.GroupOf(i)==group&&table.balls[i].active) left=true;
            return left?EightBallRules.GroupOf(ball)==group:ball==8;
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
        public void EmitFeedback(LanFeedbackKind kind,Vector3 position)
        {
            if(!IsHost) return;
            var feedback=new LanFeedback { id=++nextFeedback,kind=kind,position=position };
            PresentFeedback(feedback);
            using var writer=new FastBufferWriter(512,Allocator.Temp); writer.WriteValueSafe(JsonUtility.ToJson(feedback));
            foreach(ulong id in manager.ConnectedClientsIds) if(id!=0)
                manager.CustomMessagingManager.SendNamedMessage(FeedbackMessage,id,writer,NetworkDelivery.ReliableSequenced);
        }
        private void ReadFeedback(ulong sender,FastBufferReader reader)
        {
            if(IsHost||sender!=NetworkManager.ServerClientId||reader.Length>512) return;
            try { reader.ReadValueSafe(out string json); var feedback=JsonUtility.FromJson<LanFeedback>(json); if(feedback!=null&&Finite(feedback.position)) PresentFeedback(feedback); }
            catch { RejectedActions++; }
        }
        private void PresentFeedback(LanFeedback feedback)
        {
            if(feedback.id<=lastFeedback) return;
            lastFeedback=feedback.id; FeedbackPresented++; feedbackCounts[feedback.kind]=FeedbackCount(feedback.kind)+1; g.PresentLanFeedback(feedback);
        }
        public void FixedTick()
        {
            if(!IsHost) return; double now=Time.realtimeSinceStartupAsDouble;
            AdvanceLifecycle(now);
            if(Lifecycle.Phase==RoomPhase.Draining&&!tableDrained)tableDrained=FinishTables(now);
            if(!Lifecycle.Accepting(now)) foreach(var c in controlled.Values) { c.move=Vector3.zero; c.run=false; c.chargeAt=-1; }
            while(pending.Count>0) { var command=pending.Dequeue(); ApplyIntent(command.sender,command.intent,now); }
            if(finalFrozen) {
                AdvanceLifecycle(now);
                if(now>=snapshotAt) { snapshotAt=now+.05; Publish(now); }
                return;
            }
            foreach(var c in controlled.Values) {
                c.actor.TickRecovery();
                if(c.actor.Fallen) { c.chargeAt=c.attackEnd=-1; if(c.held) { c.held.Release(Vector3.up); c.held=null; } }
                else {
                    if(!c.actor.Seated) { c.actor.Aim(Yaw(c.yaw)); c.actor.Move(now-c.inputAt>.3?Vector3.zero:c.move,c.run); }
                    if(c.chargeAt>=0) { c.actor.Aim(Yaw(c.yaw)); c.actor.PunchPose(Mathf.Clamp01((float)(now-c.chargeAt))); }
                }
            }
            g.Rival.TickRecovery(); if(!g.Rival.Fallen) g.Rival.Move(Vector3.zero,false);
            ResolveAttacks(now);
            ulong? kick=Roster.ResolveVote(now); if(kick.HasValue) { if(kick==0) { Fail("La votación cerró la sala del anfitrión."); return; } OnDisconnected(kick.Value); manager.DisconnectClient(kick.Value,"Expulsado por votación de la sala."); }
            g.Pool.FixedTick();
            foreach(var table in tables)table.Tick(now);
            if(Lifecycle.Phase==RoomPhase.Draining&&!tableDrained) tableDrained=FinishTables(now);
            AdvanceLifecycle(now);
            if(now>=snapshotAt) { snapshotAt=now+.05; Publish(now); }
        }
        private bool PhysicsPending()
        {
            if(g.Pool.Moving) return true;
            foreach(var c in controlled.Values) if(c.attackEnd>=0) return true;
            foreach(var bottle in g.Bottles) if(bottle.Flying) return true;
            return false;
        }
        private void AdvanceLifecycle(double now)
        {
            bool canPay=false; foreach(var a in Roster.Ledger.Accounts) if(a.Connected&&a.Balance>=minimumPurchase) { canPay=true; break; }
            var previous=Lifecycle.Phase;
            bool active=false;foreach(var table in tables)active|=table.Table.Phase==TablePhase.Playing;
            Lifecycle.Tick(now,active,Roster.Ledger.HasPending,PhysicsPending(),canPay);
            if(Lifecycle.Phase==RoomPhase.Ejecting||Lifecycle.Phase==RoomPhase.Results) FreezeFinalReport(now);
            if(previous!=Lifecycle.Phase) snapshotAt=0;
        }
        private void ApplyIntent(ulong sender,LanIntent intent,double now)
        {
            if(!Roster.Accept(sender,intent.sequence,now)||!controlled.TryGetValue(sender,out var c)) return;
            if(!Lifecycle.Accepting(now)&&intent.action!=LanAction.Stop&&intent.action!=LanAction.StatsRequest) { RejectedActions++; return; }
            if(!Finite(intent.move)||!float.IsFinite(intent.yaw)||Mathf.Abs(intent.yaw)>100000||!float.IsFinite(intent.pitch)||Mathf.Abs(intent.pitch)>85) { RejectedActions++; return; }
            if(intent.action==LanAction.Move) { c.move=Vector3.ClampMagnitude(new Vector3(intent.move.x,0,intent.move.z),1); c.run=intent.run; c.yaw=intent.yaw; c.pitch=intent.pitch; c.inputAt=now; return; }
            if(intent.action==LanAction.Stop) { c.move=Vector3.zero; c.chargeAt=-1;ArmTable?.Stop(sender,now); return; }
            if(intent.action==LanAction.ProposeKick) { Roster.BeginVote(sender,intent.target,now); return; }
            if(intent.action==LanAction.VoteYes) { Roster.Vote(sender); return; }
            if(intent.action==LanAction.StatsRequest) { PublishStats(sender,intent.page,now); return; }
            if(intent.action>=LanAction.PoolSit&&intent.action<=LanAction.PoolReady) {
                bool ok=intent.action==LanAction.PoolSit?PoolTable.Sit(sender,CanSit(PoolTable,sender,c),now):PoolTable.Handle(sender,intent,now);
                if(!ok) RejectedActions++; return;
            }
            if(intent.action>=LanAction.ArmSit&&intent.action<=LanAction.ArmClose) {
                bool ok=intent.action==LanAction.ArmSit?ArmTable.Sit(sender,CanSit(ArmTable,sender,c),now):ArmTable.Handle(sender,intent,now);
                if(!ok)RejectedActions++;return;
            }
            if(intent.action==LanAction.Reset) { ResetActor(c,sender,now); return; }
            if(c.actor.Fallen) { RejectedActions++; return; }
            if(c.actor.Seated) { if(intent.action==LanAction.Interact) {if(c.table=="arm-1")ArmTable.Leave(sender,now);else PoolTable.Leave(sender,now);} else RejectedActions++; return; }
            if(intent.hasLook) { c.yaw=intent.yaw; c.pitch=intent.pitch; }
            switch(intent.action) {
                case LanAction.Charge:
                    if(c.chargeAt<0&&c.attackEnd<0&&now>=c.cooldown) c.chargeAt=now; else RejectedActions++; break;
                case LanAction.Release:
                    if(c.chargeAt<0||now<c.cooldown) { RejectedActions++; return; }
                    c.power=Mathf.Clamp01((float)(now-c.chargeAt)); c.chargeAt=-1; c.cooldown=now+.6; c.actor.Aim(Yaw(c.yaw));
                    c.attackDirection=Quaternion.Euler(c.pitch,c.yaw,0)*Vector3.forward;
                    if(c.held) { c.held.Release(InteractionRules.LaunchVelocity(c.attackDirection,c.power)); c.held=null; EmitFeedback(LanFeedbackKind.Throw,c.actor.Focus); }
                    else { c.attackEnd=now+.25; c.actor.Swing(); } break;
                case LanAction.Drop: c.chargeAt=-1; if(c.held) { c.held.Release(Yaw(c.yaw)*1.5f); c.held=null; } break;
                case LanAction.Interact: Interact(c,sender,intent.sequence); break;
                default: RejectedActions++; break;
            }
        }
        private void Interact(Controlled c,ulong sender,ulong sequence)
        {
            if(ArmRoomController.Near(c.actor.transform.position)){ArmTable.Sit(sender,CanSit(ArmTable,sender,c),Time.realtimeSinceStartupAsDouble);return;}
            if(PoolRoomController.Near(c.actor.transform.position)){PoolTable.Sit(sender,CanSit(PoolTable,sender,c),Time.realtimeSinceStartupAsDouble);return;}
            if(!InteractionRules.CanInteract(c.actor,c.held,c.chargeAt>=0,c.attackEnd>=0)) return;
            if(Vector3.Distance(c.actor.transform.position,TavernWorld.BarPoint)<2.2f) {
                if(Roster.Spend(sender,drinkPrice,"drink/"+sender+"/"+sequence,"drink")) { c.actor.Drink(); EmitFeedback(LanFeedbackKind.Drink,c.actor.Focus); } else serverNotice="No tienes fichas suficientes para esa bebida."; return;
            }
            var best=InteractionRules.SelectBottle(c.actor,Quaternion.Euler(c.pitch,c.yaw,0)*Vector3.forward,g.Bottles);
            if(best&&best.Grab(c.actor)) { c.held=best; EmitFeedback(LanFeedbackKind.Grab,c.actor.Focus); }
        }
        public void ActorFell(TavernActor actor)
        {
            if(!IsHost) return;
            foreach(var pair in controlled) if(pair.Value.actor==actor) {
                var c=pair.Value; foreach(var table in tables)table.Leave(pair.Key,Time.realtimeSinceStartupAsDouble);
                Roster.Ledger.RecordFall("fall/"+(++fallSerial),pair.Key);
                c.chargeAt=c.attackEnd=-1;
                if(c.held) { c.held.Release(Yaw(c.yaw)*1.5f); c.held=null; }
                if(actor==g.Player) g.SetRoomHeld(null);
                break;
            }
        }
        private void ResetActor(Controlled c,ulong sender,double now)
        {
            // Reset is a personal respawn, never a room reset or a way to cancel a released attack.
            if(c.actor.Fallen||c.actor.Seated||c.attackEnd>=0||now<c.cooldown) { RejectedActions++; return; }
            if(!TavernActor.TryFindStandingPosition(SpawnPoint(Roster.Members[sender].Slot),c.actor,out var free)) { serverNotice="No hay un punto libre para recolocarte."; return; }
            if(c.held) { c.held.ResetToRack(); c.held=null; }
            c.move=Vector3.zero; c.run=false; c.yaw=c.pitch=0; c.chargeAt=-1; c.inputAt=now; c.resetRevision++;
            c.actor.ClearEffect(); c.actor.Teleport(free,Quaternion.identity);
            if(sender==LocalId) { g.SetRoomHeld(null); g.ResetLocalView(); }
        }
        public static Vector3 SpawnPoint(int slot)=>new((slot%3-1)*1.4f,.08f,-6.1f-(slot/3)*1.3f);
        private bool FinishTables(double now)
        {
            double cutoff=Lifecycle.Reason==RoomEndReason.Time?Math.Min(now,Lifecycle.Deadline):now;
            foreach(var table in tables)if(!drainedTables.Contains(table)&&table.FinishSession(cutoff))drainedTables.Add(table);
            return drainedTables.Count==tables.Count;
        }
        private bool CanSit(IRoomTable target,ulong id,Controlled c)
        {
            if(c.chargeAt>=0||c.attackEnd>=0||c.actor.Fallen||c.actor.Seated&&target.Table.SeatOf(id)<0)return false;
            foreach(var table in tables)if(table!=target&&table.Table.Phase==TablePhase.Playing&&table.Table.Participates(id))return false;
            return true;
        }
        private void ChangePoolSeat(ulong id,bool seated,int slot)=>ChangeSeat(id,seated,slot,"pool-1",PoolRoomController.SeatPosition(slot));
        private void ChangeArmSeat(ulong id,bool seated,int slot)=>ChangeSeat(id,seated,slot,"arm-1",ArmRoomController.SeatPosition(slot));
        private void ChangeSeat(ulong id,bool seated,int seat,string tableId,Vector3 seatPosition)
        {
            if(!controlled.TryGetValue(id,out var c)) return;
            c.move=Vector3.zero; c.run=false; c.chargeAt=-1;
            if(c.held) { c.held.Release(Vector3.down); c.held=null; }
            if(seated) {
                c.returnPosition=c.actor.transform.position; c.returnRotation=c.actor.transform.rotation;
                c.actor.Teleport(seatPosition,Quaternion.Euler(0,seat==0?90:270,0)); c.actor.SetSeated(true);c.table=tableId;
            } else {
                c.actor.SetSeated(false);c.table="";
                if(!c.actor.Fallen&&TavernActor.TryFindStandingPosition(c.returnPosition,c.actor,out var free)) c.actor.Teleport(free,c.returnRotation);
            }
            if(id==LocalId) { g.Pool.SetRoomView(seated&&tableId=="pool-1"); g.RefreshInputContext(); }
        }
        private void ResolveAttacks(double now)
        {
            var falls=new List<(TavernActor actor,Vector3 force)>();
            var feedback=new List<(LanFeedbackKind kind,Vector3 position)>();
            foreach(var c in controlled.Values) if(c.attackEnd>=0&&!c.actor.Fallen) {
                TavernActor target=null; float distance=InteractionRules.Reach+.001f; Vector3 direction=c.attackDirection;
                foreach(var candidate in CombatTargets()) if(candidate!=c.actor&&!candidate.Fallen) {
                    Vector3 d=candidate.transform.position-c.actor.transform.position; d.y=0;
                    if(d.magnitude<distance&&InteractionRules.CanStrike(c.actor,candidate,direction)) { target=candidate; distance=d.magnitude; }
                }
                if(target) { feedback.Add((LanFeedbackKind.PunchHit,target.Focus)); falls.Add((target,direction*Mathf.Lerp(3.4f,6,c.power))); c.attackEnd=-1; }
                else if(now>=c.attackEnd) { feedback.Add((LanFeedbackKind.Miss,c.actor.Focus)); falls.Add((c.actor,-direction*2.2f)); c.attackEnd=-1; }
            }
            foreach(var fall in falls) fall.actor.Fall(fall.force);
            foreach(var effect in feedback) EmitFeedback(effect.kind,effect.position);
        }
        public IEnumerable<TavernActor> CombatTargets()
        {
            yield return g.Rival; foreach(var c in controlled.Values) yield return c.actor;
        }
        private LanActorState State(ulong id,string name,TavernActor actor,long chips,int held=-1) => new() {
            id=id,name=name,position=actor.transform.position,focus=actor.Focus,eye=actor.EyePosition,yaw=actor.transform.eulerAngles.y,
            fallen=actor.Fallen,seated=actor.Seated,ebriety=actor.Ebriety,recovery=actor.Fallen?Mathf.Max(0,actor.RecoverAt-Time.time):0,effect=actor.EffectRemaining,chips=chips,held=held,body=actor.CaptureBodyPose(),effects=actor.CaptureEffects()
        };
        private void Publish(double now)
        {
            var actors=new List<LanActorState>();
            foreach(var pair in controlled) {
                var m=Roster.Members[pair.Key]; var c=pair.Value;
                var state=State(pair.Key,m.Name,c.actor,m.Chips,c.held?g.Bottles.IndexOf(c.held):-1);
                state.charge=c.chargeAt<0?-1:Mathf.Clamp01((float)(now-c.chargeAt)); actors.Add(state);
                state.resetRevision=c.resetRevision;state.table=c.table;
            }
            var bottles=new LanBottleState[g.Bottles.Count];
            for(int i=0;i<bottles.Length;i++) { var b=g.Bottles[i]; bottles[i]=new(){position=b.transform.position,rotation=b.transform.rotation,held=b.Held,flying=b.Flying}; }
            var snapshot=new LanSnapshot { revision=++nextRevision,serverTime=now,actors=actors.ToArray(),rival=State(ulong.MaxValue,"Bruno",g.Rival,0),bottles=bottles,notice=serverNotice,secondsRemaining=Mathf.Max(0,(float)(Lifecycle.Deadline-now)),voteActive=Roster.VoteTarget.HasValue,voteTarget=Roster.VoteTarget??0,votes=Roster.Votes,votesNeeded=Roster.VotesNeeded };
            snapshot.ending=new SessionEndingView{phase=Lifecycle.Phase,reason=Lifecycle.Reason,phaseStarted=Lifecycle.PhaseStarted,reportId=reportId,pages=finalPages.Count,participants=finalParticipants};
            Ending=snapshot.ending;
            snapshot.pool=PoolTable.Capture(now);snapshot.arm=ArmTable.Capture();
            snapshot.drinkPrice=drinkPrice; PresentReceipt(snapshot.pool.receipt);PresentReceipt(snapshot.arm?.receipt);
            View=snapshot; g.SetRoomHeld(controlled.TryGetValue(0,out var local)?local.held:null);
            g.RefreshInputContext();
            string json=JsonUtility.ToJson(snapshot); using var writer=new FastBufferWriter(PacketLimit,Allocator.Temp); writer.WriteValueSafe(json);
            foreach(ulong id in manager.ConnectedClientsIds) if(id!=0) manager.CustomMessagingManager.SendNamedMessage(SnapshotMessage,id,writer,NetworkDelivery.ReliableFragmentedSequenced);
        }
        private void ApplySnapshot(LanSnapshot snapshot)
        {
            Ending=snapshot.ending; serverTimeAtReceipt=snapshot.serverTime; endingReceivedAt=Time.realtimeSinceStartupAsDouble;
            var alive=new HashSet<ulong>();
            foreach(var state in snapshot.actors) {
                alive.Add(state.id);
                if(!replicas.TryGetValue(state.id,out var actor)) { actor=state.id==LocalId?g.Player:g.CreateRoomActor(state.name,state.position); actor.SetReplica(true); replicas.Add(state.id,actor); }
                bool reset=state.id==LocalId&&state.resetRevision!=localResetRevision;
                actor.ApplyReplica(state,reset);
                if(state.id==LocalId) {
                    g.SetRoomHeld(state.held>=0&&state.held<g.Bottles.Count?g.Bottles[state.held]:null);
                    if(reset) { localResetRevision=state.resetRevision; g.ResetLocalView(); }
                }
            }
            var removed=new List<ulong>(); foreach(var p in replicas) if(!alive.Contains(p.Key)) removed.Add(p.Key);
            foreach(ulong id in removed) { if(replicas[id]!=g.Player) Destroy(replicas[id].gameObject); replicas.Remove(id); }
            if(snapshot.rival!=null) g.Rival.ApplyReplica(snapshot.rival);
            if(snapshot.bottles!=null) for(int i=0;i<Math.Min(snapshot.bottles.Length,g.Bottles.Count);i++) g.Bottles[i].ApplyReplica(snapshot.bottles[i]);
            foreach(var bottle in g.Bottles) bottle.SetReplicaOwner(null);
            foreach(var state in snapshot.actors) if(state.held>=0&&state.held<g.Bottles.Count)
                g.Bottles[state.held].SetReplicaOwner(replicas[state.id]);
            if(snapshot.pool!=null) { g.Pool.ApplyRoomBalls(snapshot.pool); g.Pool.SetRoomView(snapshot.pool.IsSeated(LocalId)); g.RefreshInputContext(); PresentReceipt(snapshot.pool.receipt);PresentReceipt(snapshot.arm?.receipt); }
        }
        private void PresentReceipt(MatchReceipt receipt)
        {
            if(receipt==null||!seenReceipts.Add(receipt.id)) return;
            int side=Array.IndexOf(receipt.players,LocalId); string own=side>=0?" Recibes: "+receipt.payouts[side]+" fichas.":"";
            g.Notify((receipt.hasWinner?receipt.game+": gana "+receipt.winnerName+".":receipt.game+": ronda liquidada sin ganador.")+own); g.Audio.Play("ui",.6f);
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
