using System;
using System.Collections.Generic;

namespace TavernChaos
{
    public enum EightBallPhase { Placement, Aiming, Rolling, BreakDecision, Result }
    public enum BallGroup { Open, Solids, Stripes }
    public sealed class EightBallEvents
    {
        public readonly HashSet<int> FirstContacts=new(), OffTable=new();
        public readonly Dictionary<int,int> Pockets=new();
        private readonly List<(int ball,long step)> rails=new();
        private long contactStep=long.MaxValue;
        public bool Timeout;
        public readonly HashSet<int> BehindHeadContacts=new();
        public void Contact(int ball,long step,bool behindLine=false)
        {
            if(ball<1||ball>15||step>contactStep) return;
            if(step<contactStep) { contactStep=step; FirstContacts.Clear(); BehindHeadContacts.Clear(); }
            FirstContacts.Add(ball); if(behindLine) BehindHeadContacts.Add(ball);
        }
        public void Rail(int ball,long step) { rails.Add((ball,step)); }
        public bool RailAfterContact { get { foreach(var r in rails) if(r.step>=contactStep) return true; return false; } }
        public int BreakRailCount { get { var unique=new HashSet<int>(); foreach(var r in rails) if(r.ball>0&&r.step>=contactStep) unique.Add(r.ball); return unique.Count; } }
    }

    // Project variant documented in Docs/REGLAS_BOLA8.md. No Unity or transport dependencies.
    public sealed class EightBallRules
    {
        public EightBallPhase Phase { get; private set; }=EightBallPhase.Placement;
        public int Turn { get; private set; }
        public int Winner { get; private set; }=-1;
        public bool Break { get; private set; }=true;
        public bool HeadOnly { get; private set; }=true;
        public bool Foul { get; private set; }
        public bool RerackRequired { get; private set; }
        public int CalledBall { get; private set; }
        public int CalledPocket { get; private set; }=-1;
        public string Notice { get; private set; }="Saque: coloca la blanca detrás de la línea.";
        public readonly BallGroup[] Groups={BallGroup.Open,BallGroup.Open};
        private readonly bool[] remaining=new bool[16];
        private int breaker;
        private bool decisionFoul;
        public EightBallRules(int first)
        {
            Turn=breaker=first==0?0:1; for(int i=1;i<16;i++) remaining[i]=true;
        }
        public bool OnTable(int ball) => ball>0&&ball<16&&remaining[ball];
        public static BallGroup GroupOf(int ball) => ball>=1&&ball<=7?BallGroup.Solids:ball>=9&&ball<=15?BallGroup.Stripes:BallGroup.Open;
        public int Left(int side)
        {
            if(side<0||side>1||Groups[side]==BallGroup.Open) return -1;
            int count=0; for(int i=1;i<16;i++) if(remaining[i]&&GroupOf(i)==Groups[side]) count++; return count;
        }
        public bool LegalTarget(int side,int ball)
        {
            if(side<0||side>1||!OnTable(ball)) return false;
            if(Groups[side]==BallGroup.Open) return ball!=8;
            return Left(side)==0?ball==8:GroupOf(ball)==Groups[side];
        }
        public bool CanCall(int side,int ball,int pocket) => side==Turn&&pocket>=0&&pocket<6&&LegalTarget(side,ball);
        public bool Place(int side)
        {
            if(side!=Turn||Phase!=EightBallPhase.Placement) return false;
            Phase=EightBallPhase.Aiming; Notice=Break?"Saque libre de anuncio. Emboca o lleva cuatro bolas a banda.":"Blanca colocada. Anuncia bola y tronera."; return true;
        }
        public bool Shoot(int side,int ball,int pocket)
        {
            if(side!=Turn||Phase!=EightBallPhase.Aiming||(!Break&&!CanCall(side,ball,pocket))) return false;
            CalledBall=Break?0:ball; CalledPocket=Break?-1:pocket; Foul=false; RerackRequired=false;
            Phase=EightBallPhase.Rolling; Notice="Tiro en curso. Se resuelve al detenerse toda la mesa."; return true;
        }
        public void Resolve(EightBallEvents shot)
        {
            if(Phase!=EightBallPhase.Rolling||shot==null) return;
            int shooter=Turn, opponent=1-Turn; bool cleared=Left(shooter)==0;
            bool legalFirst=false; foreach(int b in shot.FirstContacts) if(Break||LegalTarget(shooter,b)&&(!HeadOnly||!shot.BehindHeadContacts.Contains(b))) legalFirst=true;
            bool scratch=shot.Pockets.ContainsKey(0)||shot.OffTable.Contains(0);
            bool foul=scratch||shot.Timeout||!legalFirst||shot.OffTable.Count>0;
            if(!Break&&shot.Pockets.Count==0&&!shot.RailAfterContact) foul=true;
            foreach(var p in shot.Pockets) if(p.Key>0&&p.Key<16) remaining[p.Key]=false;
            Foul=foul;
            if(Break) {
                bool objectPot=false; foreach(int b in shot.Pockets.Keys) if(b>0) objectPot=true;
                remaining[8]=true; // 8 on a break is always re-spotted, never a win/loss in this variant.
                Break=false;
                if(RestoreOpenStalemate()) return;
                if(!objectPot&&shot.BreakRailCount<4) {
                    Turn=opponent; decisionFoul=foul; Phase=EightBallPhase.BreakDecision;
                    Notice="Saque insuficiente: el rival elige aceptar o repetir con el sacador original."; return;
                }
                if(foul) { Turn=opponent; HeadOnly=true; Phase=EightBallPhase.Placement; Notice="Falta de saque. Rival: blanca en mano detrás de la línea."; }
                else { Turn=objectPot?shooter:opponent; HeadOnly=false; Phase=EightBallPhase.Aiming; Notice="Mesa abierta. Anuncia bola y tronera; el primer emboque legal asigna grupos."; }
                return;
            }
            bool eightPot=shot.Pockets.TryGetValue(8,out int eightPocket);
            if(eightPot||shot.OffTable.Contains(8)) {
                Winner=eightPot&&!foul&&cleared&&CalledBall==8&&CalledPocket==eightPocket?shooter:opponent;
                Phase=EightBallPhase.Result; Notice=Winner==shooter?"¡Bola 8 legal! Partida ganada.":"Pérdida por bola 8: prematura, otra tronera, fuera o con falta."; return;
            }
            bool called=shot.Pockets.TryGetValue(CalledBall,out int actualPocket)&&actualPocket==CalledPocket;
            if(!foul&&called&&Groups[shooter]==BallGroup.Open) {
                Groups[shooter]=GroupOf(CalledBall); Groups[opponent]=Groups[shooter]==BallGroup.Solids?BallGroup.Stripes:BallGroup.Solids;
            }
            if(RestoreOpenStalemate()) return;
            HeadOnly=false;
            if(foul) { Turn=opponent; Phase=EightBallPhase.Placement; Notice="Falta: blanca en mano para el rival en cualquier punto libre."; }
            else { Turn=called?shooter:opponent; Phase=EightBallPhase.Aiming; Notice=called?"Emboque anunciado legal: conservas el turno.":"Sin emboque anunciado: cambia el turno."; }
        }
        public bool DecideBreak(int side,bool rerack)
        {
            if(Phase!=EightBallPhase.BreakDecision||side!=Turn) return false;
            if(rerack) {
                for(int i=1;i<16;i++) remaining[i]=true;
                Turn=breaker; Break=true; HeadOnly=true; Phase=EightBallPhase.Placement; Notice="Se repite el saque original. Coloca la blanca detrás de la línea.";
            } else {
                HeadOnly=decisionFoul; Phase=decisionFoul?EightBallPhase.Placement:EightBallPhase.Aiming;
                Notice=decisionFoul?"Mesa aceptada. Blanca en mano detrás de la línea.":"Mesa aceptada; comienza tu turno con mesa abierta.";
            }
            return true;
        }
        private bool RestoreOpenStalemate()
        {
            if(Groups[0]!=BallGroup.Open) return false;
            for(int i=1;i<16;i++) if(i!=8&&remaining[i]) return false;
            // Keep the approved open-table restriction without leaving a position with no legal call.
            for(int i=1;i<16;i++) remaining[i]=true;
            Turn=breaker; Break=HeadOnly=RerackRequired=true; Foul=false; Phase=EightBallPhase.Placement;
            Notice="Mesa bloqueada sin grupos asignados: se repite el saque original sin nuevo cobro."; return true;
        }
        public void Forfeit(int side,string reason)
        {
            if(Phase==EightBallPhase.Result||side<0||side>1) return;
            Winner=1-side; Phase=EightBallPhase.Result; Notice=reason;
        }
        public int[] Rack(Random random)
        {
            var pool=new List<int>(); for(int i=1;i<16;i++) if(i!=8) pool.Add(i);
            for(int i=pool.Count-1;i>0;i--) { int j=random.Next(i+1); (pool[i],pool[j])=(pool[j],pool[i]); }
            var rack=new int[15]; rack[4]=8;
            int solid=pool.FindIndex(n=>GroupOf(n)==BallGroup.Solids); rack[10]=pool[solid]; pool.RemoveAt(solid);
            int stripe=pool.FindIndex(n=>GroupOf(n)==BallGroup.Stripes); rack[14]=pool[stripe]; pool.RemoveAt(stripe);
            for(int i=0,j=0;i<15;i++) if(rack[i]==0) rack[i]=pool[j++];
            return rack;
        }
    }
}
