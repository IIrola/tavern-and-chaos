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
            Instance=this; SmokeMode=System.Array.Exists(System.Environment.GetCommandLineArgs(),x=>x=="-demo-smoke-tests"); Application.runInBackground=SmokeMode; Application.targetFrameRate=60; QualitySettings.vSyncCount=1; Time.fixedDeltaTime=.01f; Time.maximumDeltaTime=.1f;
            Physics.defaultSolverIterations=10; Physics.defaultSolverVelocityIterations=4;
            TavernWorld.Build();
            Player=CreateActor("You | Mica",true,new(0,.08f,-4)); Rival=CreateActor("Rival | Bruno",false,new(0,.08f,-.1f));
            Rival.transform.rotation=Quaternion.Euler(0,180,0);
            Pool=new PoolPractice();
            for(int i=0;i<5;i++) Bottles.Add(ThrowableBottle.Create(new(-2.8f+i*.3f,1.18f,-2.3f)));
            TavernWorld.Box("Bottle stand",new(-2.2f,.48f,-2.3f),new(1.8f,.96f,.8f),TavernWorld.DarkWood);
            TavernWorld.Text("BOTELLAS",new(-2.2f,1.8f,-2.3f),.13f,new Color(.5f,.95f,.83f));
            var cam=new GameObject("Tavern camera"); cam.tag="MainCamera"; var c=cam.AddComponent<Camera>(); c.fieldOfView=60; c.nearClipPlane=.08f; c.farClipPlane=70; c.backgroundColor=new(.06f,.065f,.09f); cam.AddComponent<AudioListener>(); Rig=cam.AddComponent<TavernCamera>();
            Audio=gameObject.AddComponent<DemoAudio>(); Input=new DemoInput(); hud=gameObject.AddComponent<DemoHUD>();
            Pool.Changed+=OnPoolChanged;
            Rig.ReduceMotion=PlayerPrefs.GetInt("reduceMotion",0)==1; Rig.Sensitivity=PlayerPrefs.GetFloat("sensitivity",.13f); Audio.Volume=PlayerPrefs.GetFloat("volume",.6f);
            SetCursor(false);
            if(SmokeMode) gameObject.AddComponent<DemoSmokeTests>();
        }
        private TavernActor CreateActor(string name,bool player,Vector3 home)
        {
            var go=new GameObject(name); go.layer=8; var actor=go.AddComponent<TavernActor>(); actor.Initialize(player,home); foreach(Transform t in go.GetComponentsInChildren<Transform>()) t.gameObject.layer=8; return actor;
        }
        public void Begin()
        {
            Started=true; Settings=false; Paused=false; Time.timeScale=1; ResetSession(); SetCursor(true); Rig.Snap(); RefreshInputContext(); Audio.Play("ui"); Notify("Acércate a Bruno. Mantén el golpe y suelta cerca de él.");
        }
        public void BeginPoolPractice()
        {
            Begin(); Player.Teleport(new Vector3(3.7f,.08f,.4f),Quaternion.Euler(0,90,0)); Pool.Enter(); Rig.Snap();
        }
        public void SetPause(bool value)
        {
            if(!Started) return; Paused=value; Charging=false; Charge=0; pendingPower=-1; attackUntil=-1; move=Vector2.zero; run=false; interactRequest=dropRequest=false;
            Time.timeScale=value?0:1; RefreshInputContext(); Audio.Play("ui");
        }
        public void Menu()
        {
            Time.timeScale=1; Started=false; Paused=false; Settings=false; Pool.Exit(); Charging=false; Charge=0; pendingPower=-1; attackUntil=-1; move=Vector2.zero;
            if(Held) { Held.ResetToRack(); Held=null; } Player.ClearEffect(); SetCursor(false); Rig.Snap(); RefreshInputContext();
        }
        public void SaveSettings()
        {
            PlayerPrefs.SetInt("reduceMotion",Rig.ReduceMotion?1:0); PlayerPrefs.SetFloat("sensitivity",Rig.Sensitivity); PlayerPrefs.SetFloat("volume",Audio.Volume); PlayerPrefs.Save();
        }
        private static void SetCursor(bool capture) { Cursor.lockState=capture?CursorLockMode.Locked:CursorLockMode.None; Cursor.visible=!capture; }
        private void OnPoolChanged()
        {
            if(!Pool.Active) { pendingPower=-1; Charging=false; Charge=0; }
            RefreshInputContext();
        }
        private bool lastPlaying, lastBilliards;
        public void RefreshInputContext()
        {
            bool playing=Started&&!Paused, billiards=playing&&Pool.Active;
            if(playing!=lastPlaying || billiards!=lastBilliards) { Input.SetContext(playing,billiards); lastPlaying=playing; lastBilliards=billiards; poolPointerSeen=false; }
            SetCursor(playing&&!billiards);
        }
        private void Update()
        {
            for(int i=sparks.Count-1;i>=0;i--) if(Time.time>sparks[i].end) { Destroy(sparks[i].obj); sparks.RemoveAt(i); }
            if(SmokeMode&&!TestDriveInput) { Pool.UpdateGuide(); return; }
            if(!Started || Paused) { hud.HandleMenuInput(); return; }
            if(Pool.Active) { UpdatePoolInput(); Pool.UpdateGuide(); return; }
            if(Input.Pause.WasPressedThisFrame()) { Settings=false; SetPause(true); return; }
            Rig.Look(Input.Look.ReadValue<Vector2>());
            move=Input.Move.ReadValue<Vector2>(); run=Input.Run.IsPressed();
            if(Input.Interact.WasPressedThisFrame()) interactRequest=true;
            if(Input.Drop.WasPressedThisFrame()) dropRequest=true;
            if(Input.Reset.WasPressedThisFrame()) ResetSession();
            if(!Player.Fallen && Input.Punch.WasPressedThisFrame() && Time.time>=cooldown) { Charging=true; Charge=0; }
            if(Charging) { Charge=Mathf.Min(1,Charge+Time.deltaTime); Player.PunchPose(Charge); if(Input.Punch.WasReleasedThisFrame()) { pendingPower=Charge; Charging=false; Charge=0; } }
            if(Player.Fallen) { Charging=false; Charge=0; }
        }
        private void UpdatePoolInput()
        {
            if(Input.PoolPause.WasPressedThisFrame()) {
                if(Pool.ResetPending) Pool.CancelReset(); else SetPause(true); return;
            }
            if(Input.PoolExit.WasPressedThisFrame()) { Pool.Exit(); return; }
            if(Input.PoolReset.WasPressedThisFrame()) Pool.RequestReset();
            if(Input.PoolNext.WasPressedThisFrame()&&!Pool.Moving) Pool.SelectDrill((PoolDrill)(((int)Pool.Drill+1)%4));
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
                if(SmokeMode) Debug.Log($"POOL_INPUT click: phase={Pool.Phase} canShoot={Pool.CanShoot} pointer={pointer} ui={DemoHUD.PoolUIContains(pointer)} shots={Pool.Shots}");
                // UI clicks are consumed by widgets, never also by the simulation.
                bool mouse=Input.PoolShoot.activeControl?.device is UnityEngine.InputSystem.Mouse;
                if(!mouse || !DemoHUD.PoolUIContains(pointer)) {
                    if(Pool.Phase==PoolPhase.BallInHand) Pool.ConfirmPlacement(); else QueuePoolShot();
                }
            }
        }
        public void QueuePoolShot()
        {
            if(!Started||Paused||!Pool.CanShoot) return;
            pendingPower=Pool.Power; pendingPoolDirection=Pool.Direction;
            if(SmokeMode) Debug.Log($"POOL_INPUT queued: {pendingPower}, {pendingPoolDirection}");
        }
        private void FixedUpdate()
        {
            if(!Started || Paused) return;
            Player.TickRecovery(); Rival.TickRecovery();
            if(!Pool.Active) { Player.Move(Rig.Right*move.x+Rig.Forward*move.y,run); if(Charging) Player.Aim(Rig.Forward); }
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
                var bottle=Held; Held=null; bottle.Release((Player.transform.forward+Vector3.up*.1f).normalized*Mathf.Lerp(7,15,power)); Throws++; Audio.Play("miss",.4f); Notify("¡Botella al aire!"); return;
            }
            hitPower=power; attackDirection=Rig.Forward; attackUntil=Time.time+.25f; Player.Swing();
        }
        private bool TryHit()
        {
            if(Rival.Fallen) return false;
            Vector3 delta=Rival.transform.position-Player.transform.position; delta.y=0;
            if(delta.magnitude>2.05f || Vector3.Dot(attackDirection,delta.normalized)<.45f) return false;
            if(Physics.Linecast(Player.Focus,Rival.Focus,1<<0,QueryTriggerInteraction.Ignore)) return false;
            Rival.Fall(attackDirection*Mathf.Lerp(3.4f,6,hitPower)); Hits++; Goals[0]=true; Impact(Rival.Focus); Audio.Play("hit"); Notify("¡CONECTADO! Un golpe. Una caída. Ningún rencor."); return true;
        }
        public void Interact()
        {
            if(Player.Fallen) return;
            if(Pool.Active) { Pool.Exit(); Charging=false; move=Vector2.zero; return; }
            if(Held) { Notify("Tienes una botella. Lánzala con el golpe o suéltala con Q."); return; }
            if(Vector3.Distance(Player.transform.position,TavernWorld.BarPoint)<2.2f) { Drink(); return; }
            if(Vector3.Distance(Player.transform.position,TavernWorld.PoolPoint)<2.5f) { Pool.Enter(); Charging=false; move=Vector2.zero; pendingPower=-1; attackUntil=-1; Notify("Apunta con el cursor. Rueda para potencia, clic para tirar."); return; }
            ThrowableBottle nearest=null; float dist=2.05f;
            foreach(var b in Bottles) if(!b.Held&&!b.Flying) { float d=Vector3.Distance(Player.Focus,b.transform.position); if(d<dist) { dist=d; nearest=b; } }
            if(nearest) { Held=nearest; Held.Grab(Player); Audio.Play("clink",.4f); Notify("Botella equipada. Apunta hacia Bruno y carga el lanzamiento."); }
            else Notify("Busca las botellas, la barra o el billar. Acércate y pulsa E.");
        }
        public void Drink() { if(Player.Fallen||Held) return; Player.Drink(); Goals[2]=true; Audio.Play("drink"); Impact(Player.Focus,true); Notify("LAGUNA AZUL · 8 s de pies deslizantes. Repetir renueva el efecto."); }
        public void Drop() { if(!Held) return; var b=Held; Held=null; b.Release(Player.transform.forward*1.5f); }
        public void ActorFell(TavernActor actor) { if(actor==Player) { Drop(); Pool.Exit(); Charging=false; pendingPower=-1; } }
        public void BottleHit(Vector3 pos) { Goals[3]=true; Impact(pos); Audio.Play("hit"); Notify("¡BOTELLAZO! Las botellas vuelven al expositor tras unos segundos."); }
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
            Charging=false; Charge=0; pendingPower=-1; attackUntil=-1; cooldown=0; move=Vector2.zero; interactRequest=dropRequest=false;
            if(Held) { Held.ResetToRack(); Held=null; } foreach(var b in Bottles) b.ResetToRack();
            Pool.Exit(); Player.ClearEffect(); Player.Teleport(Player.Home,Quaternion.identity); Rival.Teleport(Rival.Home,Quaternion.Euler(0,180,0)); Pool.Reset();
            Hits=Misses=Throws=0; Goals=new bool[4]; Rig.Yaw=0; Rig.Pitch=23; Rig.Snap(); RefreshInputContext();
        }
        private void OnApplicationFocus(bool focus) { if(!focus&&Started&&!Paused&&!SmokeMode) SetPause(true); }
        private void OnDestroy() { if(Pool!=null) Pool.Changed-=OnPoolChanged; Input?.Dispose(); Time.timeScale=1; Cursor.lockState=CursorLockMode.None; Cursor.visible=true; if(Instance==this) Instance=null; }
    }
}
