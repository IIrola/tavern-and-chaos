using UnityEngine;

namespace TavernChaos
{
    public sealed class DemoHUD : MonoBehaviour
    {
        private readonly Color ivory=new(.96f,.91f,.81f), muted=new(.67f,.71f,.72f), mint=new(.37f,.89f,.74f), orange=new(1,.43f,.24f), ink=new(.035f,.046f,.06f,1);
        private GUIStyle text, button;
        private DemoGame g;
        private float scale, ox, oy;
        private const float W=1440,H=900;
        private int focus, controls, lastControls, accept=-1;
        private float nextNavigate;
        private string menuScreen;
        private bool lanLobby,roomRoster,editingLanField,tableRules,roomStats;
        private float nextStatsPage;
        private GUIStyle lanField;
        private string lanName="Invitado", lanAddress="127.0.0.1", lanPort="7777";
        private int lanMinutes=30;
        private string poolStatus, poolResult, drillName;
        private int poolShots, poolPotted, poolTotal;
        private PoolPhase poolPhase;
        private void Start() { g=DemoGame.Instance; g.Pool.Changed+=PoolChanged; PoolChanged(); }
        private void OnDestroy() { if(g&&g.Pool!=null) g.Pool.Changed-=PoolChanged; }
        private void PoolChanged() { poolStatus=g.Pool.Status; poolResult=g.Pool.Result; drillName=g.Pool.DrillName; poolShots=g.Pool.Shots; poolPotted=g.Pool.Potted; poolTotal=g.Pool.TotalTargets; poolPhase=g.Pool.Phase; }
        public void HandleMenuInput()
        {
            if(!g) g=DemoGame.Instance;
            string screen=(g.Started?"pause":"menu")+(g.Settings?"settings":lanLobby?"lan":roomRoster?"roster":roomStats?"stats":tableRules?"rules":"");
            if(g.Lan&&g.Lan.BlocksPlay) screen="session-"+g.Lan.Ending.phase;
            if(menuScreen!=screen) { menuScreen=screen; focus=0; accept=-1; nextNavigate=0; }
            if(g.Lan&&g.Lan.BlocksPlay&&g.Input.Back.WasPressedThisFrame()) { if(g.Lan.Ending.phase==RoomPhase.Results) g.Menu(); return; }
            if(g.Input.Back.WasPressedThisFrame()) { if(roomStats) roomStats=false; else if(tableRules) { tableRules=false; g.SetPause(false); } else if(roomRoster) roomRoster=false; else if(lanLobby) lanLobby=false; else if(g.Settings) { g.SaveSettings(); g.Settings=false; } else if(g.Paused) g.SetPause(false); return; }
            if(lanLobby&&editingLanField) return;
            Vector2 nav=g.Input.Navigate.ReadValue<Vector2>();
            if((roomStats||g.Lan&&g.Lan.BlocksPlay)&&Mathf.Abs(nav.x)>.5f&&Time.unscaledTime>=nextStatsPage&&g.Lan.StatsPage!=null) { g.Lan.RequestStats(Mathf.Clamp(g.Lan.StatsPage.page+(nav.x>0?1:-1),0,g.Lan.StatsPage.pages-1)); nextStatsPage=Time.unscaledTime+.3f; }
            if(Mathf.Abs(nav.y)>.5f && Time.unscaledTime>=nextNavigate && lastControls>0) { focus=(focus+(nav.y>0?-1:1)+lastControls)%lastControls; nextNavigate=Time.unscaledTime+.2f; }
            if(Mathf.Abs(nav.y)<.2f) nextNavigate=0;
            if(g.Settings&&(!g.Lan||!g.Lan.BlocksPlay)&&Mathf.Abs(nav.x)>.2f) { if(focus==0) g.Audio.Volume=Mathf.Clamp01(g.Audio.Volume+nav.x*Time.unscaledDeltaTime*.5f); if(focus==1) g.Rig.Sensitivity=Mathf.Clamp(g.Rig.Sensitivity+nav.x*Time.unscaledDeltaTime*.15f,.04f,.3f); }
            if(g.Input.Accept.WasPressedThisFrame()) accept=focus;
        }
        public static bool PoolUIContains(Vector2 screenPoint)
        {
            float s=Mathf.Min(Screen.width/W,Screen.height/H);
            Vector2 p=new((screenPoint.x-(Screen.width-W*s)*.5f)/s,(Screen.height-screenPoint.y-(Screen.height-H*s)*.5f)/s);
            return p.x<390 || p.x>1000 || p.y<106 || p.y>754;
        }
        private void Init()
        {
            g=DemoGame.Instance;
            var font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text=new GUIStyle { font=font,richText=true,wordWrap=true,normal={textColor=ivory} };
            button=new GUIStyle(text) { alignment=TextAnchor.MiddleCenter,fontSize=18,fontStyle=FontStyle.Bold };
        }
        private void OnGUI()
        {
            if(text==null) Init();
            // Fit a logical safe area, including 16:10 and ultrawide, without stretching the type.
            scale=Mathf.Min(Screen.width/W,Screen.height/H); ox=(Screen.width-W*scale)*.5f; oy=(Screen.height-H*scale)*.5f;
            GUI.matrix=Matrix4x4.TRS(new Vector3(ox,oy,0),Quaternion.identity,Vector3.one*scale);
            controls=0;
            if(g.Lan&&g.Lan.BlocksPlay) SessionEndScreen();
            else if(!g.Started) Menu(); else { GUI.enabled=!g.Paused; Game(); GUI.enabled=true; if(g.Paused) Pause(); }
            lastControls=controls;
            editingLanField=lanLobby&&GUI.GetNameOfFocusedControl().StartsWith("lan-");
            GUI.matrix=Matrix4x4.identity;
        }
        private void Box(Rect r,Color c) { GUI.color=QualitySettings.activeColorSpace==ColorSpace.Linear?c.linear:c; GUI.DrawTexture(r,Texture2D.whiteTexture); GUI.color=Color.white; }
        private void Label(Rect r,string s,int size,Color? color=null,FontStyle style=FontStyle.Normal,TextAnchor align=TextAnchor.UpperLeft)
        {
            text.fontSize=size; text.fontStyle=style; text.normal.textColor=color??ivory; text.alignment=align; GUI.Label(r,s,text);
        }
        private int FocusControl(Rect r)
        {
            if(!GUI.enabled || (g.Started&&!g.Paused&&(!g.Lan||!g.Lan.BlocksPlay))) return -1;
            int index=controls++;
            if(index==focus) { Box(new(r.x-4,r.y-4,r.width+8,3),mint); Box(new(r.x-4,r.y+r.height+1,r.width+8,3),mint); }
            return index;
        }
        private bool Button(Rect r,string s,bool primary=false)
        {
            int index=FocusControl(r); Vector2 p=Event.current.mousePosition; bool hover=r.Contains(p)&&GUI.enabled;
            Box(r,!GUI.enabled?new Color(.07f,.09f,.1f):primary?(hover?new Color(1,.57f,.34f):orange):(hover?new Color(.18f,.23f,.24f):new Color(.11f,.15f,.17f)));
            button.normal.textColor=!GUI.enabled?muted:primary?ink:ivory;
            bool keyboard=index>=0&&accept==index&&Event.current.type==EventType.Repaint;
            if(keyboard) accept=-1;
            return GUI.Button(r,s,button)||keyboard;
        }
        private void Tag(float x,float y,string s,Color c) { Box(new(x,y,8,8),c); Label(new(x+18,y-4,360,25),s,13,c,FontStyle.Bold); }
        private void Menu()
        {
            roomStats=roomRoster=tableRules=false;
            GUI.enabled=!g.Settings&&!lanLobby;
            Box(new(0,0,660,H),new Color(.025f,.035f,.048f,.94f)); Box(new(0,0,8,H),orange);
            Tag(64,57,"PRIMERA RONDA / LOCAL + LAN "+Application.version,mint);
            Label(new(60,130,580,180),"TAVERN\n<color=#FF7947>& CHAOS</color>",81,ivory,FontStyle.Bold);
            Label(new(64,338,480,45),"Una taberna. Un golpe. Mucho caos.",25,ivory,FontStyle.Bold);
            Label(new(64,397,475,84),"Entra, prueba tu puntería y deja que la física\nhaga el resto. Bruno ya te está esperando.",20,muted);
            if(Button(new(64,521,440,66),"ABRIR LA TABERNA    →",true)) g.Begin();
            if(Button(new(64,603,440,49),"PRACTICAR BILLAR")) g.BeginPoolPractice();
            if(Button(new(64,668,212,49),"AJUSTES")) g.Settings=!g.Settings;
            if(Button(new(292,668,212,49),"SALIR")) Application.Quit();
            Label(new(64,739,500,32),"PRÁCTICA LOCAL O SALA LAN · HASTA 6 PERSONAS",13,mint,FontStyle.Bold);
            Label(new(64,778,505,52),"WASD  moverse     RATÓN  mirar\nE  interactuar     MANTENER CLIC  cargar golpe",16,muted);
            Label(new(64,838,520,30),"↑ ↓  ELEGIR    ENTER  ACEPTAR    ESC  VOLVER",12,muted);
            Box(new(1010,57,366,44),ink); Label(new(1024,67,338,26),"ROCK, FÍSICA Y MALAS DECISIONES",13,ivory,FontStyle.Bold);
            Card(702,650,"01","CONECTA O CAE","Aciertas: cae Bruno.\nFallas: caes tú.");
            Card(936,650,"02","SIRVE EL CAOS","Pies de hielo y\nbotellas voladoras.");
            Card(1170,650,"03","BAJA EL RITMO","Billar libre entre\ngolpe y golpe.");
            if(Button(new(1010,115,366,48),"CREAR / UNIRSE A SALA LAN")) lanLobby=true;
            if(g.Lan&&!string.IsNullOrEmpty(g.Lan.Status)) Label(new(704,180,672,68),g.Lan.Status,17,muted);
            GUI.enabled=true;
            if(lanLobby) Lobby();
            if(g.Settings) Settings(new(710,130,640,475));
        }
        private void Card(float x,float y,string num,string title,string desc)
        {
            Box(new(x,y,212,183),ink); Box(new(x,y,212,3),mint); Label(new(x+18,y+20,160,30),num,24,orange,FontStyle.Bold); Label(new(x+18,y+64,180,28),title,14,ivory,FontStyle.Bold); Label(new(x+18,y+106,177,65),desc,16,muted);
        }
        private void Game()
        {
            Box(new(32,28,1376,62),ink); Label(new(52,43,350,35),"TAVERN <color=#FF7947>& CHAOS</color>",22,ivory,FontStyle.Bold);
            Label(new(480,49,520,30),g.Lan&&g.Lan.Running?(g.Pool.Active?"BOLA 8 EN SALA":"SALA LAN · PRIMERA PERSONA"):g.Pool.Active?"02  /  BILLAR LIBRE":"01  /  LA PRIMERA RONDA",15,mint,FontStyle.Bold,TextAnchor.UpperCenter);
            Label(new(1110,49,270,30),g.Pool.Active?"ESC  Pausa     E  Volver":"ESC  Pausa y ajustes",15,muted,FontStyle.Normal,TextAnchor.UpperRight);
            if(g.Lan&&g.Lan.Running) { if(g.Pool.Active) PoolRoomHUD(); else RoomHUD(); }
            else if(!g.Pool.Active) {
                Box(new(32,114,312,258),ink); Tag(52,137,g.Complete?"RONDA COMPLETADA":"TU PRIMERA RONDA",g.Complete?orange:mint);
                string[] goals={"Conecta un golpe a Bruno","Falla un golpe y vuelve a pie","Prueba Laguna Azul en la barra","Derriba a Bruno con una botella"};
                for(int i=0;i<4;i++) { Label(new(52,181+i*43,25,28),g.Goals[i]?"✓":"○",20,g.Goals[i]?mint:muted); Label(new(87,183+i*43,238,36),goals[i],16,g.Goals[i]?ivory:muted); }
                if(g.Complete) { Box(new(32,388,312,100),ink); Label(new(52,406,276,68),"¡Ya eres de la casa!\nSigue jugando o prueba el billar.",18,ivory,FontStyle.Bold); }
                // A target label is shown only close to Bruno and in front of the camera.
                Vector3 sp=Camera.main.WorldToScreenPoint(g.Rival.Focus+Vector3.up*1.1f);
                if(sp.z>0&&Vector3.Distance(g.Player.transform.position,g.Rival.transform.position)<6) {
                    float sx=(sp.x-ox)/scale, sy=(Screen.height-sp.y-oy)/scale;
                    if(sx>360&&sx<1300&&sy>100&&sy<670) { Box(new(sx-85,sy-10,170,40),ink); Label(new(sx-80,sy-2,160,30),g.Rival.Fallen?"BRUNO · AL SUELO":"BRUNO · RIVAL",13,g.Rival.Fallen?orange:mint,FontStyle.Bold,TextAnchor.UpperCenter); }
                }
                Box(new(32,690,230,65),ink); Label(new(50,704,195,42),"ACIERTO  "+g.Hits+"     FALLO  "+g.Misses,15,ivory,FontStyle.Bold);
                if(g.Player.EffectRemaining>0) { Box(new(1050,114,358,78),ink); Label(new(1070,132,320,52),"LAGUNA AZUL  /  "+g.Player.EffectRemaining.ToString("0.0")+" s\n¡Chocar fuerte te derriba!",16,mint,FontStyle.Bold); }
                // Small reticle; targeting is based on actor orientation and reach, not color alone.
                Box(new(W/2-3,H/2-3,6,6),new Color(1,.94f,.8f,.8f));
            } else { PoolHUD(); }
            if(!g.Pool.Active&&Time.unscaledTime<g.ToastUntil) { Box(new(385,666,670,76),ink); Label(new(407,684,626,50),g.Toast,18,ivory,FontStyle.Bold,TextAnchor.UpperCenter); }
            if(g.Charging) {
                Box(new(510,590,420,56),ink); Label(new(528,600,380,24),(g.Held?"LANZAMIENTO":g.Pool.Active?"POTENCIA DEL TIRO":"CARGANDO GOLPE")+"  "+Mathf.RoundToInt(g.Charge*100)+"%",14,ivory,FontStyle.Bold);
                Box(new(528,629,384,5),new Color(.18f,.23f,.25f)); Box(new(528,629,384*g.Charge,5),orange);
            }
            Box(new(284,763,872,54),ink); Label(new(302,780,836,32),g.Prompt(),16,ivory,FontStyle.Bold,TextAnchor.UpperCenter);
            string controls=g.Pool.Active?(g.Lan&&g.Lan.Running?"RATÓN / STICK apuntar · ↑ ↓ potencia · TAB / RB bola · C / LB tronera · CLIC / RT tirar · ENTER / A confirmar · E / B salir":"RATÓN / STICK apuntar · ↑ ↓ potencia · CLIC / RT tirar · TAB / RB ejercicio · E / B salir"):"WASD / STICK moverse · RATÓN / STICK mirar · CLIC / RT golpe · E / A interactuar · Q / B soltar · ESC menú";
            if(g.Lan&&g.Lan.Running&&g.Pool.Active&&g.Lan.View?.pool?.phase==TablePhase.Preparation) controls="TAB / RB siguiente apuesta · C / LB anterior · ENTER / A confirmar o iniciar · H / X reglas · E / B levantarte";
            Box(new(32,838,1376,40),ink); Label(new(52,850,1336,28),controls,14,muted,FontStyle.Normal,TextAnchor.UpperCenter);
        }
        internal void ShowLanPanelForCheck(bool show) { lanLobby=show; }
        internal void ShowRosterForCheck() { roomRoster=true; }
        public void ShowStats() { roomStats=true; g.SetPause(true); g.Lan.RequestStats(); }
        public void CloseStats() { roomStats=false; g.SetPause(false); }
        public void ShowTableRules() { tableRules=true; g.SetPause(true); }
        public void CloseTableRules() { tableRules=false; g.SetPause(false); }
        private void Lobby()
        {
            Box(new(700,255,676,375),ink); Label(new(726,277,620,38),"UNA MESA PARA TU GRUPO",27,ivory,FontStyle.Bold);
            if(lanField==null) lanField=new GUIStyle(GUI.skin.textField){fontSize=19,padding=new RectOffset(10,10,4,4)};
            Label(new(726,330,150,28),"Tu nombre",18,muted);
            GUI.SetNextControlName("lan-name"); lanName=GUI.TextField(new(880,326,462,32),lanName,24,lanField);
            Label(new(726,375,150,28),"IP o código",18,muted);
            GUI.SetNextControlName("lan-address"); lanAddress=GUI.TextField(new(880,371,312,32),lanAddress,32,lanField);
            GUI.SetNextControlName("lan-port"); lanPort=GUI.TextField(new(1207,371,135,32),lanPort,5,lanField);
            if(Button(new(726,420,616,40),"DURACIÓN DE SALA: "+lanMinutes+" MIN")) lanMinutes=lanMinutes==20?30:lanMinutes==30?45:lanMinutes==45?60:20;
            bool valid=ushort.TryParse(lanPort,out ushort port)&&port>0;
            GUI.enabled=valid;
            if(Button(new(726,479,295,51),"CREAR SALA",true)) { if(g.Lan.Host(lanName,port,lanMinutes)) lanLobby=false; }
            if(Button(new(1042,479,300,51),"ENTRAR EN SALA")) { if(g.Lan.Join(lanName,lanAddress,port)) lanLobby=false; }
            GUI.enabled=true;
            Label(new(726,547,616,60),"Mismo equipo: IP 127.0.0.1 y el mismo puerto.\nOtra máquina en tu red: usa el código del anfitrión.",16,muted);
            if(Button(new(726,601,616,36),"VOLVER")) lanLobby=false;
        }
        private void RoomHUD()
        {
            var room=g.Lan; var view=room.View;
            Box(new(W/2-3,H/2-3,6,6),new Color(1,.94f,.8f,.8f));
            float effectY=view?.voteActive==true?285:114;
            Box(new(1032,effectY,376,110),ink);
            Label(new(1052,effectY+18,332,80),g.Player.EffectRemaining>0?"LAGUNA AZUL · "+g.Player.EffectRemaining.ToString("0.0")+" s\n¡Chocar fuerte te derriba!":"BOLA 8 · E junto a un asiento\nDos personas · apertura "+(view?.pool?.openingFee??10)+"\nBARRA · Laguna Azul "+(view?.drinkPrice??20)+" fichas",16,g.Player.EffectRemaining>0?mint:muted,FontStyle.Bold);
            Box(new(32,114,330,390),ink); Tag(52,138,room.IsHost?"SALA LAN · ANFITRIÓN":room.Connected?"SALA LAN · CONECTADO":"SALA LAN · CONECTANDO",mint);
            Label(new(52,175,290,32),"CÓDIGO  "+room.RoomCode,19,ivory,FontStyle.Bold);
            Label(new(52,216,290,30),(view?.actors?.Length??0)+" / 6 personas",18,mint,FontStyle.Bold);
            if(view?.actors!=null) for(int i=0;i<view.actors.Length;i++) {
                var actor=view.actors[i]; float y=260+i*32;
                Label(new(52,y,290,28),(actor.name.Length>18?actor.name.Substring(0,17)+"…":actor.name)+"  ·  "+actor.chips,16,ivory);
                if(room.ActorFor(actor.id) is TavernActor visual&&actor.id!=room.LocalId) {
                    Vector3 p=Camera.main.WorldToScreenPoint(visual.Focus+Vector3.up*.6f);
                    if(p.z>0) Label(new((p.x-ox)/scale-70,(Screen.height-p.y-oy)/scale-18,140,28),actor.name,15,mint,FontStyle.Bold,TextAnchor.UpperCenter);
                }
            }
            if(view!=null) {
                Label(new(52,461,290,30),"QUEDAN  "+Mathf.CeilToInt(view.secondsRemaining/60)+" MIN",15,muted);
                Box(new(32,522,330,110),ink); Label(new(52,541,290,78),view.notice,17,ivory);
                if(view.voteActive) {
                    Box(new(1032,114,376,150),ink); Label(new(1052,134,330,55),"VOTACIÓN DE EXPULSIÓN\n"+view.votes+" / "+view.votesNeeded+" votos necesarios",17,ivory,FontStyle.Bold);
                    Label(new(1052,206,332,40),"ESC → Jugadores para votar",16,mint,FontStyle.Bold);
                }
                if(view.pool?.phase==TablePhase.Playing&&view.pool.IsParticipant(room.LocalId)&&!view.pool.IsSeated(room.LocalId)) {
                    Box(new(1032,445,376,145),ink);
                    Label(new(1052,463,332,112),view.pool.turn==room.LocalId?"SIGUES EN LA PARTIDA\nEs tu turno: vuelve al asiento en "+Mathf.CeilToInt(view.pool.absenceRemaining)+" s.\nE junto al billar para sentarte.":"SIGUES EN LA PARTIDA\nEl minuto solo contará al llegar tu turno. Puedes volver al asiento.",18,orange,FontStyle.Bold);
                }
            }
        }
        private string RoomName(ulong id)
        {
            if(g.Lan.View?.actors!=null) foreach(var a in g.Lan.View.actors) if(a.id==id) return a.name;
            return "Persona desconectada";
        }
        private void PoolRoomHUD()
        {
            var room=g.Lan; var table=room.View?.pool; if(table==null) return;
            bool enabled=GUI.enabled,mine=room.CanLocalPoolAct;
            Box(new(32,114,330,610),ink); Tag(52,138,"BOLA 8 · DOS LADOS",mint);
            Label(new(52,177,290,34),table.phase==TablePhase.Preparation?"PREPARAR PARTIDA":table.phase==TablePhase.Result?"RESULTADO":table.hasTurn?"TURNO: "+RoomName(table.turn):"MESA DISPONIBLE",20,orange,FontStyle.Bold);
            for(int i=0;i<table.seated.Length;i++) Label(new(52,225+i*35,290,34),(table.seated[i]==room.LocalId?"TÚ · ":"")+RoomName(table.seated[i])+(table.hasOrganizer&&table.organizer==table.seated[i]?" ★":""),17,ivory,FontStyle.Bold);
            for(int i=0;i<table.participants.Length;i++) {
                int group=table.groups.Length>i?table.groups[i]:0;
                Label(new(52,315+i*51,290,50),(table.participantNames.Length>i?table.participantNames[i]:RoomName(table.participants[i]))+"\n"+(group==1?"LISAS 1–7":group==2?"RAYADAS 9–15":"MESA ABIERTA"),16,mint);
            }
            Label(new(52,440,290,120),table.notice,18,ivory);
            if(table.phase==TablePhase.Result&&table.winner>=0&&table.winner<table.participants.Length) Label(new(52,573,290,56),"GANA "+table.participantNames[table.winner],21,mint,FontStyle.Bold);
            Label(new(52,646,290,55),"En espera: "+table.waiting.Length+" · tiros: "+table.shots,16,muted);
            if(Button(new(52,691,290,40),"REGLAS · H / X")) ShowTableRules();
            Box(new(1032,114,376,610),ink);
            if(table.phase==TablePhase.Preparation||table.phase==TablePhase.Available) {
                bool organizer=table.hasOrganizer&&table.organizer==room.LocalId;
                long fee=organizer?table.openingFee:0;
                Label(new(1052,137,332,112),"TU DESGLOSE\nApertura "+fee+" + apuesta "+table.stake+"\nTOTAL AL INICIAR: "+(fee+table.stake),20,ivory,FontStyle.Bold);
                Label(new(1052,254,332,56),"Apertura para el bar. Apuestas al bote.\nCero permite jugar sin apostar.",17,muted);
                GUI.enabled=enabled&&organizer;
                if(Button(new(1052,327,332,48),"APUESTA "+table.stake+" · TAB / RB")) room.CyclePoolWager();
                if(Button(new(1052,386,332,40),"APUESTA ANTERIOR · C / LB")) room.CyclePoolWager(true);
                bool localReady=table.ready!=null&&System.Array.IndexOf(table.ready,room.LocalId)>=0;
                bool allReady=table.seated.Length==2&&table.ready?.Length==2;
                GUI.enabled=enabled&&table.IsSeated(room.LocalId);
                if(Button(new(1052,458,332,58),allReady&&organizer?"INICIAR · ENTER / A":localReady?"CANCELAR LISTO · ENTER / A":"CONFIRMAR TOTAL · ENTER / A",true)) room.ConfirmPoolSetup();
                GUI.enabled=enabled;
                Label(new(1052,534,332,58),(table.ready?.Length??0)+" / 2 confirmaciones\nCambiar apuesta/asientos cancela el listo.",17,muted);
                if(room.View.actors!=null) foreach(var a in room.View.actors) if(a.id==room.LocalId) Label(new(1052,596,332,40),"TU SALDO: "+a.chips+" FICHAS",18,mint,FontStyle.Bold);
            } else if(table.phase==TablePhase.Result) {
                int local=System.Array.IndexOf(table.participants,room.LocalId); long paid=local>=0&&table.payouts.Length>local?table.payouts[local]:0;
                Label(new(1052,137,332,128),"BOTE "+table.pot+" · "+(table.settled?"LIQUIDADO":"PENDIENTE")+"\nTu premio: "+paid+"\nApuesta propia: "+table.stake+"\nBeneficio de apuesta: "+(paid-table.stake),19,ivory);
                GUI.enabled=enabled&&table.settled&&table.hasOrganizer&&table.organizer==room.LocalId;
                if(Button(new(1052,305,332,56),"PREPARAR REVANCHA · A",true)) room.Send(LanAction.PoolClose);
                GUI.enabled=enabled; Label(new(1052,392,332,75),"La apertura permanece en el bar.\nLa revancha requiere confirmar de nuevo.",18,muted);
            } else if(table.playPhase==EightBallPhase.BreakDecision) {
                Label(new(1052,137,332,105),"Saque insuficiente.\nElige qué hacer con la mesa.",20,ivory);
                GUI.enabled=enabled&&mine;
                if(Button(new(1052,265,332,56),"ACEPTAR · ENTER / A",true)) room.Send(LanAction.PoolAccept);
                if(Button(new(1052,339,332,56),"REPETIR · RETROCESO / Y")) room.Send(LanAction.PoolRerack);
            } else {
                bool placing=table.playPhase==EightBallPhase.Placement;
                Label(new(1052,137,332,56),placing?(table.headOnly?"BLANCA TRAS LA LÍNEA":"BLANCA EN MANO"):table.breaking?"SAQUE · SIN ANUNCIO":"ANUNCIA BOLA Y TRONERA",18,mint,FontStyle.Bold);
                GUI.enabled=enabled&&mine&&table.playPhase==EightBallPhase.Aiming&&!table.breaking;
                if(Button(new(1052,212,332,45),"BOLA "+(table.callBall>0?table.callBall.ToString():"—")+" · TAB / RB")) room.CyclePoolCall(false);
                if(Button(new(1052,269,332,45),"TRONERA "+(table.callPocket>=0?(table.callPocket+1).ToString():"—")+" · C / LB")) room.CyclePoolCall(true);
                GUI.enabled=enabled&&mine;
                Label(new(1052,339,332,28),"POTENCIA · "+Mathf.RoundToInt(g.Pool.Power*100)+" %",18,ivory,FontStyle.Bold);
                g.Pool.SetPower(GUI.HorizontalSlider(new(1052,387,332,24),g.Pool.Power,.05f,1));
                bool called=table.breaking||table.callBall>0&&table.callPocket>=0;
                GUI.enabled=enabled&&mine&&(placing&&g.Pool.CanPlace||g.Pool.CanShoot&&called);
                if(Button(new(1052,440,332,56),placing?"COLOCAR · CLIC / RT":"TIRAR · CLIC / RT",true)) { if(placing) g.ConfirmPoolPlacement(); else g.QueuePoolShot(); }
                GUI.enabled=enabled;
                Label(new(1052,519,332,70),mine?placing?"Busca un hueco libre en el paño.":called?"La guía muestra el primer contacto.":"Selecciona bola y tronera antes de tirar.":"Observa el tiro de tu rival.\nPuedes levantarte y volver.",17,muted);
            }
            GUI.enabled=enabled;
            if(Button(new(1052,652,332,48),"LEVANTARTE · E / B")) room.Send(LanAction.PoolLeave);
            if(table.phase!=TablePhase.Preparation&&table.phase!=TablePhase.Available) Label(new(1052,597,332,46),"8 en saque: se repone.\nFuera en tu turno: máximo 60 s.",15,muted);
            for(int i=0;i<g.Pool.Balls.Count;i++) {
                var b=g.Pool.Balls[i]; if(!b.gameObject.activeSelf) continue;
                Vector3 p=Camera.main.WorldToScreenPoint(b.position); float x=(p.x-ox)/scale,y=(Screen.height-p.y-oy)/scale;
                if(p.z>0&&x>390&&x<1000&&y>106&&y<754) Label(new(x-12,y-11,24,24),i==0?"•":i.ToString(),16,i==0?ink:ivory,FontStyle.Bold,TextAnchor.MiddleCenter);
            }
            for(int i=0;i<g.Pool.Pockets.Count;i++) {
                Vector3 p=Camera.main.WorldToScreenPoint(PoolPractice.Center+g.Pool.Pockets[i]); float x=(p.x-ox)/scale,y=(Screen.height-p.y-oy)/scale;
                Label(new(x-16,y-14,32,30),(i+1).ToString(),20,table.callPocket==i?orange:mint,FontStyle.Bold,TextAnchor.MiddleCenter);
            }
        }
        private void PoolHUD()
        {
            var pool=g.Pool;
            Box(new(32,114,330,212),ink); Tag(52,138,"BILLAR · PRÁCTICA",mint);
            Label(new(52,173,290,35),drillName??pool.DrillName,21,ivory,FontStyle.Bold);
            Label(new(52,221,290,44),poolPotted+" / "+poolTotal+" embocadas",27,mint,FontStyle.Bold);
            Label(new(52,280,290,30),"TIROS  "+poolShots,17,muted,FontStyle.Bold);
            Box(new(32,344,330,199),ink); Label(new(52,365,290,54),poolStatus??pool.Status,18,poolPhase==PoolPhase.Complete?mint:orange,FontStyle.Bold);
            Label(new(52,425,290,100),poolResult??pool.Result,18,ivory);
            Box(new(1032,114,376,307),ink);
            bool placing=pool.Phase==PoolPhase.BallInHand, complete=pool.Phase==PoolPhase.Complete;
            Label(new(1052,137,330,30),placing?"BLANCA EN MANO":"POTENCIA",16,mint,FontStyle.Bold);
            Label(new(1052,173,330,48),placing?"Busca un hueco":Mathf.RoundToInt(pool.Power*100)+" %",placing?25:34,ivory,FontStyle.Bold);
            bool enabled=GUI.enabled; GUI.enabled=enabled&&!g.Paused;
            if(!placing) {
                float power=GUI.HorizontalSlider(new(1052,236,332,25),pool.Power,.05f,1);
                if(!Mathf.Approximately(power,pool.Power)) pool.SetPower(power);
            }
            Label(new(1052,275,330,48),placing?"Cursor sobre el paño y clic.\nEl contorno verde indica sitio libre.":"Rueda o ↑ ↓ para ajustar.\n← → para afinar la dirección.",16,muted);
            GUI.enabled=enabled&&!g.Paused&&(pool.CanShoot||(placing&&pool.CanPlace)||complete);
            string action=complete?"REPETIR EJERCICIO":placing?"COLOCAR BLANCA":pool.Moving?"BOLAS EN MOVIMIENTO":"TIRAR    CLIC / RT";
            if(Button(new(1052,340,332,55),action,true)) { if(complete) pool.Reset(); else if(placing) pool.ConfirmPlacement(); else g.QueuePoolShot(); }
            GUI.enabled=enabled&&!g.Paused&&!pool.Moving;
            Box(new(1032,443,376,298),ink); Label(new(1052,464,330,28),"ELIGE UN EJERCICIO",16,mint,FontStyle.Bold);
            string[] names={"Emboque recto","Tiro de corte","Bandas","Triángulo de seis"};
            for(int i=0;i<4;i++) if(Button(new(1052,508+i*49,332,40),(pool.Drill==(PoolDrill)i?"●  ":"")+(i+1)+"  "+names[i])) pool.SelectDrill((PoolDrill)i);
            GUI.enabled=enabled&&!g.Paused;
            if(Button(new(32,565,330,48),pool.ResetPending?"CONFIRMAR REINICIO · R":"REINICIAR EJERCICIO · R")) pool.RequestReset();
            if(Button(new(32,630,330,48),"VOLVER A LA TABERNA · E")) pool.Exit();
            GUI.enabled=enabled;
            Label(new(52,706,290,43),"Sin apuestas ni reglas de bola 8.\nObjetivo: embocar en pocos tiros.",14,muted);
            for(int i=0;i<pool.Balls.Count;i++) {
                var ball=pool.Balls[i]; if(!ball.gameObject.activeSelf||ball.isKinematic) continue;
                Vector3 p=Camera.main.WorldToScreenPoint(ball.position); float x=(p.x-ox)/scale,y=(Screen.height-p.y-oy)/scale;
                if(p.z>0&&x>390&&x<1000&&y>106&&y<754) Label(new(x-12,y-11,24,24),i==0?"•":i.ToString(),16,i==0?ink:ivory,FontStyle.Bold,TextAnchor.MiddleCenter);
            }
        }
        private void Pause()
        {
            Box(new(-ox/scale,-oy/scale,Screen.width/scale,Screen.height/scale),new Color(0,0,0,.65f));
            if(roomStats&&g.Lan&&g.Lan.Running) { StatsMenu(); return; }
            if(tableRules) { TableRulesMenu(); return; }
            if(roomRoster&&g.Lan&&g.Lan.Running) { RosterMenu(); return; }
            if(g.Settings) { Settings(new(400,185,640,475)); return; }
            Box(new(480,160,480,610),ink); Tag(524,197,"LA TABERNA PUEDE ESPERAR",mint); Label(new(524,245,380,65),"UN RESPIRO",42,ivory,FontStyle.Bold);
            if(Button(new(524,390,392,58),"VOLVER A LA RONDA",true)) g.SetPause(false);
            if(Button(new(524,466,392,50),"AJUSTES")) g.Settings=true;
            if(g.Lan&&g.Lan.Running) { if(Button(new(524,534,392,50),"JUGADORES Y VOTACIÓN")) roomRoster=true; }
            else if(Button(new(524,534,392,50),"REINICIAR DEMO")) { g.ResetSession(); g.SetPause(false); }
            if(g.Lan&&g.Lan.Running) { if(Button(new(524,602,392,50),"ESTADÍSTICAS")) ShowStats(); }
            if(Button(new(524,670,392,50),"VOLVER AL INICIO")) g.Menu();
        }
        private void SessionEndScreen()
        {
            var ending=g.Lan.Ending;
            if(ending.phase==RoomPhase.Results) { StatsMenu(true); return; }
            if(ending.phase==RoomPhase.Ejecting) {
                Box(new(W*.12f,H*.06f,W*.76f,110),ink);
                Label(new(W*.15f,H*.075f,W*.7f,42),"SE ACABÓ EL CRÉDITO",32,mint,FontStyle.Bold,TextAnchor.UpperCenter);
                Label(new(W*.15f,H*.13f,W*.7f,38),"La casa invita… a salir. Los resultados vienen enseguida.",20,ivory,FontStyle.Normal,TextAnchor.UpperCenter);
                return;
            }
            Box(new(W*.15f,H*.3f,W*.7f,H*.35f),ink);
            Label(new(W*.18f,H*.34f,W*.64f,55),"ÚLTIMO TIRO. CERRAMOS LA TABERNA.",30,mint,FontStyle.Bold,TextAnchor.UpperCenter);
            Label(new(W*.22f,H*.43f,W*.56f,110),"Se termina lo que ya estaba en movimiento.\nSe pagan los resultados alcanzados y se devuelven las apuestas de partidas inconclusas.",21,ivory,FontStyle.Normal,TextAnchor.UpperCenter);
        }
        private void StatsMenu(bool final=false)
        {
            var page=g.Lan.StatsPage; Box(new(130,130,1180,650),ink); Label(new(165,164,1110,42),final?"LA ÚLTIMA RONDA · RESULTADOS":"ESTADÍSTICAS DE ESTA SALA",30,mint,FontStyle.Bold);
            Label(new(165,221,1110,42),final?(g.Lan.ResultsDisconnected?g.Lan.Status:g.Lan.Ending.reason==RoomEndReason.Time?"Tiempo agotado. Partidas terminadas pagadas; apuestas inconclusas devueltas. Las fichas eran de esta sala.":"Nadie podía pagar otra apertura o bebida. Las fichas pertenecían solo a esta sala."):"Mesa / empates / apuestas ganadas · beneficio/pérdida neta · gastos de servicios. Incluye a quienes salieron.",17,muted);
            Label(new(165,278,300,30),"PERSONA",17,mint,FontStyle.Bold);
            Label(new(465,278,170,40),"GANADAS / EMPATES\nAPUESTAS GANADAS",14,mint,FontStyle.Bold);
            Label(new(642,278,210,30),"+ / − FICHAS",17,mint,FontStyle.Bold);
            Label(new(890,278,130,30),"GASTOS",17,mint,FontStyle.Bold);
            Label(new(1080,278,190,30),"BEBIDAS / CAÍDAS",16,mint,FontStyle.Bold);
            if(page?.players!=null) for(int i=0;i<page.players.Length;i++) {
                var a=page.players[i]; float y=326+i*49;
                Label(new(165,y,300,44),a.name+(a.connected?"":" · salió")+(final?"\nSaldo al cerrar: "+a.balance:""),final?16:17,a.connected?ivory:muted);
                Label(new(482,y,150,40),a.wins+" / "+a.draws+" / "+a.wagersWon,18,ivory);
                Label(new(642,y,210,40),"+"+a.won+" / −"+a.lost,18,ivory);
                Label(new(890,y,130,40),a.spent,18,ivory);
                Label(new(1080,y,180,40),a.drinks+" / "+a.falls,18,ivory);
            }
            else Label(new(165,345,1110,50),"Recibiendo las estadísticas del anfitrión…",21,ivory);
            if(page!=null) {
                Label(new(585,645,280,32),"PÁGINA "+(page.page+1)+" / "+page.pages,18,muted,FontStyle.Bold,TextAnchor.UpperCenter);
                GUI.enabled=page.page>0&&g.Lan.CanReadStatsPage(page.page-1); if(Button(new(165,640,240,42),"ANTERIOR · ←")) g.Lan.RequestStats(page.page-1);
                GUI.enabled=page.page+1<page.pages&&g.Lan.CanReadStatsPage(page.page+1); if(Button(new(1025,640,240,42),"SIGUIENTE · →")) g.Lan.RequestStats(page.page+1); GUI.enabled=true;
            }
            if(final) {
                if(Button(new(165,708,530,48),"CREAR SALA · "+g.Lan.Minutes+" MIN",true)) StartCoroutine(g.CreateRoomAfterEnding());
                if(Button(new(735,708,530,48),"VOLVER AL INICIO · ESC / B")) g.Menu();
            } else if(Button(new(165,708,1100,48),"VOLVER · ESC / B",true)) roomStats=false;
        }
        private void TableRulesMenu()
        {
            Box(new(175,90,1090,720),ink); Label(new(210,117,1015,45),"BOLA 8 · REGLAS DE ESTA TABERNA",28,mint,FontStyle.Bold);
            string rules="Dos lados. Anuncia bola y tronera; primer emboque legal asigna lisas o rayadas. Primero toca tu grupo, después debe haber emboque o banda. Limpia tu grupo antes de tirar a la 8.\n\nSaque: embocar o llevar cuatro bolas distintas a banda. Si es insuficiente, el rival acepta o pide repetir al sacador original. Primer saque aleatorio. La 8 en saque se repone siempre.\n\nFalta: blanca en mano; tras falta de saque, detrás de la línea. Una bola de color saltada se repone. La 8 prematura, en otra tronera, con falta o fuera pierde. Mesa abierta: nunca contactar primero la 8.\n\nPuedes levantarte o caer sin abandonar la partida. Solo cuenta ausencia durante tu turno: más de 60 s pierde. Mientras estés sentado no hay reloj. Un tiro ya iniciado se resuelve antes de una desconexión.\n\nReferencia: WPA, Rules of Play 15-09-2025. El azar inicial, las opciones de saque, la reposición de colores y la ausencia son variantes del proyecto. El organizador paga apertura. Ambos confirman apuesta, incluido cero. El ganador recibe el bote; salir destruye el saldo disponible y la apuesta sigue en el bote.";
            Label(new(210,185,1015,520),rules,18,ivory);
            if(Button(new(210,735,1015,48),"VOLVER A LA MESA · ESC / B",true)) CloseTableRules();
        }
        private void RosterMenu()
        {
            var room=g.Lan; var view=room.View;
            Box(new(420,160,600,590),ink); Label(new(455,189,530,42),"JUGADORES DE LA SALA",29,ivory,FontStyle.Bold);
            Label(new(455,239,530,58),"Proponer una expulsión abre una votación de 15 s.\nHace falta más de la mitad de la sala.",17,muted);
            if(view?.actors!=null) for(int i=0;i<view.actors.Length;i++) {
                var member=view.actors[i]; float y=320+i*43;
                Label(new(455,y+3,318,34),member.name+(member.id==room.LocalId?" (tú)":""),17,ivory);
                GUI.enabled=member.id!=room.LocalId&&!view.voteActive;
                if(Button(new(793,y,192,34),"PROPONER")) room.Send(LanAction.ProposeKick,target:member.id);
                GUI.enabled=true;
            }
            if(view?.voteActive==true) {
                Label(new(455,590,310,40),view.votes+" / "+view.votesNeeded+" votos necesarios",18,mint,FontStyle.Bold);
                GUI.enabled=view.voteTarget!=room.LocalId;
                if(Button(new(793,584,192,39),"A FAVOR",true)) room.Send(LanAction.VoteYes);
                GUI.enabled=true;
            }
            if(Button(new(455,665,530,48),"VOLVER")) roomRoster=false;
        }
        private void Settings(Rect panel)
        {
            Box(panel,ink); float x=panel.x+36,y=panel.y+30;
            Label(new(x,y,560,55),"A TU MANERA",32,ivory,FontStyle.Bold);
            Label(new(x,y+79,560,28),"Volumen de efectos",18,ivory); FocusControl(new(x,y+115,550,28)); g.Audio.Volume=GUI.HorizontalSlider(new(x,y+118,550,22),g.Audio.Volume,0,1);
            Label(new(x,y+155,560,28),"Sensibilidad de cámara",18,ivory); FocusControl(new(x,y+191,550,28)); g.Rig.Sensitivity=GUI.HorizontalSlider(new(x,y+194,550,22),g.Rig.Sensitivity,.04f,.3f);
            if(Button(new(x,y+239,550,48),g.Rig.ReduceMotion?"MOVIMIENTO REDUCIDO: ACTIVADO":"MOVIMIENTO REDUCIDO: DESACTIVADO")) g.Rig.ReduceMotion=!g.Rig.ReduceMotion;
            Label(new(x,y+305,550,44),"Desactiva las sacudidas de impacto.\n↑ ↓ elegir · ← → ajustar · Enter aceptar",15,muted);
            if(Button(new(x,y+369,550,46),"GUARDAR Y VOLVER",true)) { g.SaveSettings(); g.Settings=false; }
        }
    }
}
