using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    public sealed class DemoGame : MonoBehaviour
    {
        public static DemoGame Instance { get; private set; }
        public TavernActor Player { get; private set; }
        public TavernActor Rival { get; private set; }
        public TavernCamera Rig { get; private set; }
        public PoolPractice Pool { get; private set; }
        public DemoAudio Audio { get; private set; }
        public DemoInput Input { get; private set; }
        public LanSession Lan { get; private set; }
        public EconomySettings Economy { get; private set; }
        private bool ownsEconomySettings;
        public bool Started { get; private set; }
        public bool SmokeMode { get; private set; }
        internal bool TestDriveInput;
        public bool Paused { get; private set; }
        public bool Settings;
        private DemoHUD hud;
        private Vector2 lastPoolPointer;
        private bool poolPointerSeen;
        private Vector3 pendingPoolDirection;
        public bool[] Goals = new bool[4];
        public int Hits, Misses, Throws;
        public ThrowableBottle Held { get; private set; }
        public readonly List<ThrowableBottle> Bottles=new();
        public float Charge { get; private set; }
        public string Toast { get; private set; }
        public float ToastUntil { get; private set; }
        public bool Charging { get; private set; }
        public bool Complete => Goals[0]&&Goals[1]&&Goals[2]&&Goals[3];
        private Vector2 move;
        private bool run, interactRequest, dropRequest;
        private float pendingPower=-1, attackUntil=-1, hitPower, cooldown;
        private Vector3 attackDirection;
        private readonly List<(GameObject obj,float end)> sparks=new();
        private void Awake()
        {
            Instance=this; SmokeMode=System.Array.Exists(System.Environment.GetCommandLineArgs(),x=>x=="-demo-smoke-tests"||x=="-lan-check"||x=="-pool-room-check"||x=="-session-end-check"||x=="-arm-room-check"); Application.runInBackground=SmokeMode; Application.targetFrameRate=60; QualitySettings.vSyncCount=1; Time.fixedDeltaTime=.01f; Time.maximumDeltaTime=.1f;
            Physics.defaultSolverIterations=10; Physics.defaultSolverVelocityIterations=4;
            TavernWorld.Build();
            Player=CreateActor("You | Mica",true,new(0,.08f,-4)); Rival=CreateActor("Rival | Bruno",false,new(0,.08f,-.1f));
            Rival.transform.rotation=Quaternion.Euler(0,180,0);
            Pool=new PoolPractice();
            for(int i=0;i<5;i++) Bottles.Add(ThrowableBottle.Create(new(-2.8f+i*.3f,1.18f,-2.3f)));
            TavernWorld.Box("Bottle stand",new(-2.2f,.48f,-2.3f),new(1.8f,.96f,.8f),TavernWorld.DarkWood);
            TavernWorld.Text("BOTELLAS",new(-2.2f,1.8f,-2.3f),.13f,new Color(.5f,.95f,.83f));
            var cam=new GameObject("Tavern camera"); cam.tag="MainCamera"; var c=cam.AddComponent<Camera>(); c.fieldOfView=60; c.nearClipPlane=.03f; c.farClipPlane=70; c.backgroundColor=new(.06f,.065f,.09f); cam.AddComponent<AudioListener>(); Rig=cam.AddComponent<TavernCamera>();
            Audio=gameObject.AddComponent<DemoAudio>(); Input=new DemoInput(); hud=gameObject.AddComponent<DemoHUD>();
            Pool.Changed+=OnPoolChanged;
            Rig.ReduceMotion=PlayerPrefs.GetInt("reduceMotion",0)==1; Rig.Sensitivity=PlayerPrefs.GetFloat("sensitivity",.13f); Audio.Volume=PlayerPrefs.GetFloat("volume",.6f);
            SetCursor(false);
            Economy=Resources.Load<EconomySettings>("EconomySettings");
            if(!Economy||!Economy.Valid) { Economy=ScriptableObject.CreateInstance<EconomySettings>(); ownsEconomySettings=true; }
            Lan=gameObject.AddComponent<LanSession>();
            gameObject.AddComponent<SessionEndPresentation>();
            if(System.Array.Exists(System.Environment.GetCommandLineArgs(),x=>x=="-demo-smoke-tests")) gameObject.AddComponent<DemoSmokeTests>();
            if(System.Array.Exists(System.Environment.GetCommandLineArgs(),x=>x=="-lan-check")) gameObject.AddComponent<LanProcessChecks>();
            if(System.Array.Exists(System.Environment.GetCommandLineArgs(),x=>x=="-pool-room-check")) gameObject.AddComponent<PoolRoomChecks>();
            if(System.Array.Exists(System.Environment.GetCommandLineArgs(),x=>x=="-session-end-check")) gameObject.AddComponent<SessionEndChecks>();
            if(System.Array.Exists(System.Environment.GetCommandLineArgs(),x=>x=="-arm-room-check")) gameObject.AddComponent<ArmRoomChecks>();
        }
        internal TavernActor CreateRoomActor(string name,Vector3 home)=>CreateActor(name,true,home);
        internal void SetRoomHeld(ThrowableBottle bottle) { Held=bottle; }
        public IEnumerable<TavernActor> CombatTargets()
        {
            if(Lan&&Lan.IsHost) { foreach(var actor in Lan.CombatTargets()) yield return actor; }
            else yield return Rival;
        }
        public void ResetLocalView() { Rig.Yaw=Rig.Pitch=0; Charging=false; Charge=0; move=Vector2.zero; run=false; Rig.Snap(); }
        private TavernActor CreateActor(string name,bool player,Vector3 home)
        {
            var go=new GameObject(name); go.layer=8; var actor=go.AddComponent<TavernActor>(); actor.Initialize(player,home); foreach(Transform t in go.GetComponentsInChildren<Transform>()) t.gameObject.layer=8; return actor;
        }
#if UNITY_EDITOR
        public void RunFirstPersonChecks()
        {
            SmokeMode=true;
            var checks=gameObject.AddComponent<DemoSmokeTests>(); checks.FirstPersonOnly=true;
        }
#endif
        public void Begin()
        {
            Started=true; Settings=false; Paused=false; Time.timeScale=1; ResetSession(); SetCursor(true); Rig.Snap(); RefreshInputContext(); Audio.Play("ui"); Notify("Acércate a Bruno. Mantén el golpe y suelta cerca de él.");
        }
        public void BeginPoolPractice()
        {
            Begin(); Player.Teleport(new Vector3(3.7f,.08f,.4f),Quaternion.Euler(0,90,0)); Pool.Enter(); Rig.Snap();
        }
        public IEnumerator CreateRoomAfterEnding(ushort? newPort=null)
        {
            string name=Lan.LocalName; ushort port=newPort??Lan.Port; int minutes=Lan.Minutes;
            Menu(); yield return null; yield return null;
            Lan.Host(name,port,minutes);
        }
        public void SetPause(bool value)
        {
            if(!Started) return; Paused=value; Charging=false; Charge=0; pendingPower=-1; attackUntil=-1; move=Vector2.zero; run=false; interactRequest=dropRequest=false;
            Time.timeScale=Lan&&Lan.Running?1:value?0:1;
            if(value&&Lan&&Lan.Connected) Lan.Send(LanAction.Stop);
            RefreshInputContext(); Audio.Play("ui");
        }
        public void Menu()
        {
            if(Lan) Lan.Leave();
            if(Lan) Lan.ClearEnding();
            Time.timeScale=1; Started=false; Paused=false; Settings=false; Pool.Exit(); Charging=false; Charge=0; pendingPower=-1; attackUntil=-1; move=Vector2.zero;
            if(Held) { Held.ResetToRack(); Held=null; } Player.ClearEffect(); SetCursor(false); Rig.Snap(); RefreshInputContext();
        }
        public void SaveSettings()
        {
            PlayerPrefs.SetInt("reduceMotion",Rig.ReduceMotion?1:0); PlayerPrefs.SetFloat("sensitivity",Rig.Sensitivity); PlayerPrefs.SetFloat("volume",Audio.Volume); PlayerPrefs.Save();
        }
        private static void SetCursor(bool capture)
        {
            // Headless test peers must never request an OS cursor lock or interfere with a visible player.
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null) return;
            var state=capture?CursorLockMode.Locked:CursorLockMode.None;
            if(Cursor.lockState!=state) Cursor.lockState=state;
            if(Cursor.visible==capture) Cursor.visible=!capture;
        }
        private PoolPhase lastPoolPhase;
        private void OnPoolChanged()
        {
            if(Pool.Phase!=lastPoolPhase) { lastPoolPhase=Pool.Phase; poolPointerSeen=false; }
            if(!Pool.Active) { pendingPower=-1; Charging=false; Charge=0; }
            RefreshInputContext();
        }
        private bool lastPlaying, lastBilliards,lastArm;
        public void RefreshInputContext()
        {
            bool playing=Started&&!Paused&&(!Lan||!Lan.BlocksPlay), billiards=playing&&Pool.Active;
            bool arm=playing&&Lan&&Lan.LocalArmSeated;
            if(playing!=lastPlaying || billiards!=lastBilliards||arm!=lastArm) { Input.SetContext(playing,billiards,arm); lastPlaying=playing; lastBilliards=billiards;lastArm=arm; poolPointerSeen=false; }
            SetCursor(playing&&!billiards&&!arm);
        }
        private void Update()
        {
            for(int i=sparks.Count-1;i>=0;i--) if(Time.time>sparks[i].end) { Destroy(sparks[i].obj); sparks.RemoveAt(i); }
            if(SmokeMode&&!TestDriveInput) { Pool.UpdateGuide(); return; }
            if(Lan&&Lan.BlocksPlay) { hud.HandleMenuInput(); return; }
            if(!Started || Paused) { hud.HandleMenuInput(); return; }
            if(Lan&&Lan.Running) { if(Lan.LocalArmSeated)UpdateArmInput();else if(Pool.Active) { UpdatePoolInput(); Pool.UpdateGuide(); } else { UpdateRoomInput(); Pool.UpdateGuide(); } return; }
            if(Pool.Active) { UpdatePoolInput(); Pool.UpdateGuide(); return; }
            if(Input.Pause.WasPressedThisFrame()) { Settings=false; SetPause(true); return; }
            if(Input.Reset.WasPressedThisFrame()) { ResetSession(); return; }
            Vector2 look=Input.Look.ReadValue<Vector2>();
            // Mouse delta is already per-frame; a stick is an angular rate and needs delta time.
            if(Input.Look.activeControl?.device is UnityEngine.InputSystem.Gamepad) look*=Time.unscaledDeltaTime*60;
            Rig.Look(look);
            move=Input.Move.ReadValue<Vector2>(); run=Input.Run.IsPressed();
            if(Input.Interact.WasPressedThisFrame()) interactRequest=true;
            if(Input.Drop.WasPressedThisFrame()) dropRequest=true;
            if(!Player.Fallen && Input.Punch.WasPressedThisFrame() && Time.time>=cooldown) { Charging=true; Charge=0; }
            if(Charging) { Charge=Mathf.Min(1,Charge+Time.deltaTime); Player.PunchPose(Charge); if(Input.Punch.WasReleasedThisFrame()) { pendingPower=Charge; Charging=false; Charge=0; } }
            if(Player.Fallen) { Charging=false; Charge=0; }
        }
        private void UpdateRoomInput()
        {
            if(Input.Pause.WasPressedThisFrame()) { Settings=false; SetPause(true); return; }
            if(Input.Reset.WasPressedThisFrame()) { Charging=false; Charge=0; Lan.Send(LanAction.Reset); return; }
            Vector2 look=Input.Look.ReadValue<Vector2>();
            if(Input.Look.activeControl?.device is UnityEngine.InputSystem.Gamepad) look*=Time.unscaledDeltaTime*60;
            Rig.Look(look); Vector2 axis=Input.Move.ReadValue<Vector2>();
            Lan.LocalMove(Rig.Right*axis.x+Rig.Forward*axis.y,Input.Run.IsPressed(),Rig.Yaw,Rig.Pitch);
            if(Input.Interact.WasPressedThisFrame()) Lan.Send(LanAction.Interact,yaw:Rig.Yaw,pitch:Rig.Pitch);
            if(Input.Drop.WasPressedThisFrame()) { Charging=false; Charge=0; Lan.Send(LanAction.Drop,yaw:Rig.Yaw,pitch:Rig.Pitch); }
            if(Input.Punch.WasPressedThisFrame()&&!Player.Fallen) { Charging=true; Charge=0; Lan.Send(LanAction.Charge,yaw:Rig.Yaw,pitch:Rig.Pitch); }
            if(Charging) { Charge=Mathf.Min(1,Charge+Time.deltaTime); if(Input.Punch.WasReleasedThisFrame()) { Lan.Send(LanAction.Release,yaw:Rig.Yaw,pitch:Rig.Pitch); Charging=false; Charge=0; } }
            if(Player.Fallen) { Charging=false; Charge=0; }
        }
        private double armSendAt,armEffortAt;
        private void UpdateArmInput()
        {
            var table=Lan.View?.arm;if(table==null)return;
            if(Input.ArmPause.WasPressedThisFrame()){SetPause(true);return;}
            if(Input.ArmExit.WasPressedThisFrame()){Lan.Send(LanAction.ArmLeave);return;}
            if(Input.ArmRules.WasPressedThisFrame()){hud.ShowArmRules();return;}
            int side=table.phase==TablePhase.Preparation?table.SeatOf(Lan.LocalId):System.Array.IndexOf(table.participants,Lan.LocalId);
            if(Input.ArmConfirm.WasPressedThisFrame())Lan.ConfirmArmSetup();
            if(Input.ArmWager.WasPressedThisFrame())Lan.CycleArmWager();
            if(side<0||side>1)return;
            if(Input.ArmMode.WasPressedThisFrame())Lan.Send(LanAction.ArmMode,choice:1-table.modes[side]);
            float effort=Input.ArmEffort.ReadValue<float>();double now=Time.realtimeSinceStartupAsDouble;
            if(Mathf.Abs(effort)>.5f&&now>=armEffortAt){armEffortAt=now+.25;Lan.Send(LanAction.ArmEffort,choice:Mathf.Clamp(table.efforts[side]+(effort>0?1:-1),0,2));}
            if(table.phase!=TablePhase.Playing)return;
            if(table.modes[side]==(int)ArmMode.Hold) {
                if(now>=armSendAt){armSendAt=now+.08;Lan.Send(LanAction.ArmHold,run:Input.ArmPush.IsPressed()||hud.ArmPointerHeld);}
            } else if(Input.ArmPush.WasPressedThisFrame())Lan.Send(LanAction.ArmTap);
        }
        private void UpdatePoolInput()
        {
            if(Input.PoolPause.WasPressedThisFrame()) {
                if(Pool.ResetPending) Pool.CancelReset(); else SetPause(true); return;
            }
            bool room=Lan&&Lan.Running;
            if(Input.PoolExit.WasPressedThisFrame()) { if(room) Lan.Send(LanAction.PoolLeave); else Pool.Exit(); return; }
            if(room) {
                if(Input.PoolRules.WasPressedThisFrame()) { hud.ShowTableRules(); return; }
                var table=Lan.View?.pool;
                if(Input.PoolConfirm.WasPressedThisFrame()&&table!=null) { if(table.phase==TablePhase.Preparation) Lan.ConfirmPoolSetup(); else Lan.Send(table.phase==TablePhase.Result?LanAction.PoolClose:LanAction.PoolAccept); }
                if(Input.PoolAlternative.WasPressedThisFrame()) Lan.Send(LanAction.PoolRerack);
                if(Input.PoolNext.WasPressedThisFrame()) { if(table?.phase==TablePhase.Preparation) Lan.CyclePoolWager(); else Lan.CyclePoolCall(false); }
                if(Input.PoolPocket.WasPressedThisFrame()) { if(table?.phase==TablePhase.Preparation) Lan.CyclePoolWager(true); else Lan.CyclePoolCall(true); }
            }
            if(!room&&Input.PoolReset.WasPressedThisFrame()) Pool.RequestReset();
            if(!room&&Input.PoolNext.WasPressedThisFrame()&&!Pool.Moving) Pool.SelectDrill((PoolDrill)(((int)Pool.Drill+1)%4));
            if(Pool.Transitioning) { poolPointerSeen=false; return; }
            Vector2 pointer=Input.Point.ReadValue<Vector2>();
            bool pointerMoved=!poolPointerSeen||(pointer-lastPoolPointer).sqrMagnitude>.2f;
            if(pointerMoved && !DemoHUD.PoolUIContains(pointer)) {
                var plane=new Plane(Vector3.up,PoolPractice.Center); var ray=Camera.main.ScreenPointToRay(pointer);
                if(plane.Raycast(ray,out float distance)) Pool.PointAt(ray.GetPoint(distance));
            }
            lastPoolPointer=pointer; poolPointerSeen=true;
            Vector2 stick=Input.PoolAim.ReadValue<Vector2>();
            if(stick.sqrMagnitude>.12f) {
                if(Pool.Phase==PoolPhase.BallInHand) Pool.UpdatePlacement(Pool.Placement+new Vector3(stick.x,0,stick.y)*Time.deltaTime*1.4f);
                else Pool.Angle=Mathf.Atan2(stick.x,stick.y)*Mathf.Rad2Deg;
            }
            Pool.Angle+=Input.FineAim.ReadValue<float>()*Time.deltaTime*14;
            float wheel=Input.Scroll.ReadValue<float>();
            if(Mathf.Abs(wheel)>.01f) Pool.SetPower(Pool.Power+Mathf.Sign(wheel)*.05f);
            float power=Input.PoolPower.ReadValue<float>();
            if(Mathf.Abs(power)>.1f) Pool.SetPower(Pool.Power+power*Time.deltaTime*.45f);
            if(Input.PoolShoot.WasPressedThisFrame()) {
                if(SmokeMode) Debug.Log($"POOL_INPUT click: phase={Pool.Phase} canShoot={Pool.CanShoot} pointer={pointer} source={Input.Point.activeControl?.path} ui={DemoHUD.PoolUIContains(pointer)} shots={Pool.Shots}");
                // UI clicks are consumed by widgets, never also by the simulation.
                bool mouse=Input.PoolShoot.activeControl?.device is UnityEngine.InputSystem.Mouse;
                if(!mouse || !DemoHUD.PoolUIContains(pointer)) {
                    if(Pool.Phase==PoolPhase.BallInHand) ConfirmPoolPlacement(); else QueuePoolShot();
                }
            }
        }
        public void QueuePoolShot()
        {
            if(!Started||Paused||!Pool.CanShoot) return;
            if(Lan&&Lan.Running) { var table=Lan.View?.pool; if(table!=null) Lan.Send(LanAction.PoolShoot,Pool.Direction,ball:table.callBall,pocket:table.callPocket,power:Pool.Power); return; }
            pendingPower=Pool.Power; pendingPoolDirection=Pool.Direction;
            if(SmokeMode) Debug.Log($"POOL_INPUT queued: {pendingPower}, {pendingPoolDirection}");
        }
        public void ConfirmPoolPlacement()
        {
            if(Lan&&Lan.Running) { if(Lan.CanLocalPoolAct&&Pool.CanPlace) Lan.Send(LanAction.PoolPlace,Pool.Placement); }
            else Pool.ConfirmPlacement();
        }
        private void FixedUpdate()
        {
            if(Lan&&Lan.Running) { Lan.FixedTick(); return; }
            if(Lan&&Lan.BlocksPlay) return;
            if(!Started || Paused) return;
            Player.TickRecovery(); Rival.TickRecovery();
            if(!Pool.Active) { Player.Aim(Rig.Forward); Player.Move(Rig.Right*move.x+Rig.Forward*move.y,run); }
            if(!Rival.Fallen) { Rival.Move(Vector3.zero,false); Rival.Face(Player.transform.position); }
            if(interactRequest) { interactRequest=false; Interact(); }
            if(dropRequest) { dropRequest=false; Drop(); }
            if(pendingPower>=0) { float power=pendingPower; pendingPower=-1; if(Pool.Active) Pool.Shoot(power,pendingPoolDirection); else ResolveAction(power); }
            if(attackUntil>=0) {
                if(TryHit()) attackUntil=-1;
                else if(Time.time>=attackUntil) { attackUntil=-1; Misses++; Goals[1]=true; Player.Fall(-attackDirection*2.2f); Audio.Play("miss"); Notify("¡Al suelo! Si fallas el golpe, caes tú. Te levantas en 3 s."); }
            }
            Pool.FixedTick();
        }
        // Also used by the packaged-player regression runner: intent is resolved in FixedUpdate.
        public void QueueAction(float power) { if(Started&&!Paused&&!Player.Fallen) { pendingPower=Mathf.Clamp01(power); pendingPoolDirection=Pool.Direction; } }
        private void ResolveAction(float power)
        {
            if(Player.Fallen || Time.time<cooldown) return;
            cooldown=Time.time+.6f; Player.Aim(Rig.Forward);
            if(Held) {
                var bottle=Held; Held=null; bottle.Release(InteractionRules.LaunchVelocity(Rig.ViewForward,power)); Throws++; Audio.Play("miss",.4f); Notify("¡Botella al aire!"); return;
            }
            hitPower=power; attackDirection=Rig.ViewForward; attackUntil=Time.time+.25f; Player.Swing();
        }
        private bool TryHit()
        {
            if(!InteractionRules.CanStrike(Player,Rival,attackDirection)) return false;
            Rival.Fall(attackDirection*Mathf.Lerp(3.4f,6,hitPower)); Hits++; Goals[0]=true; Impact(Rival.Focus); Audio.Play("hit"); Notify("¡CONECTADO! Un golpe. Una caída. Ningún rencor."); return true;
        }
        public void Interact()
        {
            if(Player.Fallen) return;
            if(Pool.Active) { Pool.Exit(); Charging=false; move=Vector2.zero; return; }
            if(Held) { Notify("Tienes una botella. Lánzala con el golpe o suéltala con Q."); return; }
            if(!InteractionRules.CanInteract(Player,false,Charging,attackUntil>=0||pendingPower>=0)) return;
            if(Vector3.Distance(Player.transform.position,TavernWorld.BarPoint)<2.2f) { Drink(); return; }
            if(Vector3.Distance(Player.transform.position,TavernWorld.PoolPoint)<2.5f) { Pool.Enter(); Charging=false; move=Vector2.zero; pendingPower=-1; attackUntil=-1; Notify("Apunta con el cursor. Rueda para potencia, clic para tirar."); return; }
            ThrowableBottle nearest=InteractionRules.SelectBottle(Player,Rig.ViewForward,Bottles);
            if(nearest&&nearest.Grab(Player)) { Held=nearest; Audio.Play("clink",.4f); Notify("Botella equipada. Apunta hacia Bruno y carga el lanzamiento."); }
            else Notify("Busca las botellas, la barra o el billar. Acércate y pulsa E.");
        }
        public void Drink() { if(Player.Fallen||Held) return; Player.Drink(); Goals[2]=true; Audio.Play("drink"); Impact(Player.Focus,true); Notify("LAGUNA AZUL · 8 s deslizándote. ¡Chocar fuerte te derriba!"); }
        public void Drop()
        {
            Charging=false; Charge=0;
            if(attackUntil<0) pendingPower=-1;
            if(!Held) return; var b=Held; Held=null; b.Release(Player.transform.forward*1.5f);
        }
        public void ActorFell(TavernActor actor)
        {
            if(Lan&&Lan.Running) Lan.ActorFell(actor);
            if(actor==Player) { if(!Lan||!Lan.Running) Drop(); Pool.Exit(); Charging=false; Charge=0; pendingPower=attackUntil=-1; }
        }
        public void BottleHit(Vector3 pos)
        {
            if(Lan&&Lan.IsHost) { Lan.EmitFeedback(LanFeedbackKind.BottleHit,pos); return; }
            Goals[3]=true; Impact(pos); Audio.Play("hit"); Notify("¡BOTELLAZO! Las botellas vuelven al expositor tras unos segundos.");
        }
        public void SlipFeedback(Vector3 position)
        {
            if(Lan&&Lan.IsHost) { Lan.EmitFeedback(LanFeedbackKind.Slip,position); return; }
            Audio.Play("hit",.5f); Rig.AddImpact(.35f); Notify("¡Resbalón! Con Laguna Azul, chocar a gran velocidad te derriba.");
        }
        public int LanHitEffects { get; private set; }
        public void PresentLanFeedback(LanFeedback feedback)
        {
            float gain=Mathf.Clamp01(1-Vector3.Distance(Rig.transform.position,feedback.position)/25);
            switch(feedback.kind) {
                case LanFeedbackKind.PunchHit: case LanFeedbackKind.BottleHit:
                    LanHitEffects++; Impact(feedback.position); Audio.Play("hit",gain); break;
                case LanFeedbackKind.Miss: Audio.Play("miss",gain); break;
                case LanFeedbackKind.Throw: Audio.Play("miss",gain*.4f); break;
                case LanFeedbackKind.Grab: Audio.Play("clink",gain*.4f); break;
                case LanFeedbackKind.Drink: Impact(feedback.position,true); Audio.Play("drink",gain); break;
                case LanFeedbackKind.Slip: Impact(feedback.position); Audio.Play("hit",gain*.5f); break;
                case LanFeedbackKind.PoolContact: Audio.Play("clink",gain*.35f); break;
                case LanFeedbackKind.PoolPot: Audio.Play("clink",gain*.65f); break;
                case LanFeedbackKind.PoolShot: Audio.Play("clink",gain*.5f); break;
            }
        }
        private void Impact(Vector3 pos,bool blue=false)
        {
            Rig.AddImpact(blue?.2f:.55f);
            for(int i=0;i<10;i++) {
                var g=TavernWorld.Shape("Impact spark",PrimitiveType.Cube,pos+Random.insideUnitSphere*.5f,Vector3.one*Random.Range(.06f,.13f),blue?TavernWorld.Blue:TavernWorld.Cream,null,false); sparks.Add((g,Time.time+.24f));
            }
        }
        public void Notify(string text) { Toast=text; ToastUntil=Time.unscaledTime+4.5f; }
        public string Prompt()
        {
            if(Lan&&Lan.Running) {
                if(Player.Fallen) return "RECUPERÁNDOTE · "+Mathf.CeilToInt(Player.RecoverAt-Time.time)+" s";
                if(Lan.LocalArmSeated)return "PULSEADAS · E / B levantarte · el reloj continúa · ESC pausa local";
                if(Pool.Active) return "BOLA 8 · E / B levantarte · la partida continúa · ESC pausa local";
                if(ArmRoomController.Near(Player.transform.position))return "E / A · Sentarte a pulseadas o esperar siguiente duelo";
                if(PoolRoomController.Near(Player.transform.position)) return "E / A · Sentarte a bola 8 o apuntarte a la espera";
                return "SALA LAN · E interactuar   CLIC cargar   Q soltar   R recolocarte   ESC menú";
            }
            if(Player.Fallen) return "RECUPERÁNDOTE · "+Mathf.CeilToInt(Player.RecoverAt-Time.time)+" s";
            if(Pool.Active) return Pool.Phase switch {
                PoolPhase.Rolling=>"Bolas en movimiento · prepara tu siguiente línea",
                PoolPhase.BallInHand=>Pool.CanPlace?"CLIC / RT  Colocar blanca":"Busca un hueco libre dentro del paño",
                PoolPhase.Complete=>"¡Ejercicio completado! Elige otro reto o vuelve a la taberna.",
                _=>"RATÓN  Apuntar    RUEDA / ↑ ↓  Potencia    CLIC / RT  Tirar"
            };
            if(Held) return "MANTENER GOLPE  Lanzar botella     Q  Soltar";
            if(Vector3.Distance(Player.transform.position,TavernWorld.BarPoint)<2.2f) return "E  Tomar Laguna Azul · fricción reducida durante 8 s";
            if(Vector3.Distance(Player.transform.position,TavernWorld.PoolPoint)<2.5f) return "E  Jugar billar libre";
            foreach(var b in Bottles) if(!b.Flying&&!b.Held&&Vector3.Distance(Player.Focus,b.transform.position)<2.05f) return "E  Agarrar botella";
            if(Vector3.Distance(Player.transform.position,Rival.transform.position)<2.05f&&!Rival.Fallen) return "BRUNO ESTÁ A TIRO · Mantén el golpe y suelta para derribarlo";
            return "Explora la taberna · Acércate a Bruno, las botellas, la barra o el billar";
        }
        public void ResetSession()
        {
            foreach(var spark in sparks) Destroy(spark.obj); sparks.Clear();
            Charging=false; Charge=0; pendingPower=-1; attackUntil=-1; cooldown=0; move=Vector2.zero; run=false; interactRequest=dropRequest=false;
            if(Held) { Held.ResetToRack(); Held=null; } foreach(var b in Bottles) b.ResetToRack();
            Pool.Exit(); Player.ClearEffect(); Player.ClearIntoxication();Rival.ClearIntoxication(); Player.Teleport(Player.Home,Quaternion.identity); Rival.Teleport(Rival.Home,Quaternion.Euler(0,180,0)); Pool.Reset();
            Hits=Misses=Throws=0; Goals=new bool[4]; Rig.Yaw=0; Rig.Pitch=0; Rig.Snap(); RefreshInputContext();
        }
        private void OnApplicationFocus(bool focus) { if(!focus&&Started&&!Paused&&!SmokeMode) SetPause(true); }
        private void OnDestroy() { if(Lan) Lan.Leave(); if(Pool!=null) Pool.Changed-=OnPoolChanged; Input?.Dispose(); if(ownsEconomySettings&&Economy) Destroy(Economy); Time.timeScale=1; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; if(Instance==this) Instance=null; }
    }
}
