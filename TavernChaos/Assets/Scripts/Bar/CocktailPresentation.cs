using UnityEngine;

namespace TavernChaos
{
    // Local presentation follows authoritative effect snapshots; it never changes movement or clocks.
    [DefaultExecutionOrder(75)]
    public sealed class CocktailPresentation : MonoBehaviour
    {
        private TavernActor actor;
        private Light blue;
        private Transform fire,smoke;
        private readonly Transform[] puffs=new Transform[3];
        private Material smokeMaterial;
        public bool BlueVisible=>blue&&blue.enabled;
        public bool FireVisible=>fire&&fire.gameObject.activeSelf;
        public bool SmokeVisible=>smoke&&smoke.gameObject.activeSelf;
        private void Awake()
        {
            actor=GetComponent<TavernActor>();
            blue=new GameObject("Cocktail blue light").AddComponent<Light>();blue.transform.SetParent(transform,false);
            blue.type=LightType.Point;blue.color=new Color(.12f,.45f,1);blue.range=2.2f;blue.enabled=false;blue.shadows=LightShadows.None;
            fire=new GameObject("Cocktail mouth fire").transform;fire.SetParent(transform,false);
            TavernWorld.Shape("Flame",PrimitiveType.Sphere,Vector3.zero,new(.13f,.18f,.48f),TavernWorld.Red,fire,false);
            TavernWorld.Shape("Flame core",PrimitiveType.Sphere,new(0,0,-.08f),new(.07f,.1f,.3f),TavernWorld.Cream,fire,false);
            smoke=new GameObject("Cocktail KO smoke").transform;smoke.SetParent(transform,false);
            smokeMaterial=TavernWorld.Mat("Cocktail smoke",new Color(.38f,.4f,.43f));
            for(int i=0;i<puffs.Length;i++)puffs[i]=TavernWorld.Shape("Smoke puff",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.2f,smokeMaterial,smoke,false).transform;
            fire.gameObject.SetActive(false);smoke.gameObject.SetActive(false);
        }
        private void LateUpdate()
        {
            var game=DemoGame.Instance;if(!actor||!game||!game.Rig)return;
            float intensity=Mathf.Clamp01(game.Rig.EffectIntensity);
            blue.enabled=intensity>0&&actor.HasCocktail(CocktailKind.Blue);blue.intensity=2*intensity;blue.transform.position=actor.Focus;
            bool flame=intensity>0&&actor.HasCocktail(CocktailKind.Volcano),cloud=intensity>0&&actor.HasCocktail(CocktailKind.Poison);
            fire.gameObject.SetActive(flame);smoke.gameObject.SetActive(cloud);
            Vector3 mouth=actor.EyePosition+transform.forward*.42f-Vector3.up*.12f;
            fire.SetPositionAndRotation(mouth,transform.rotation);
            float flutter=game.Rig.ReduceMotion?1:1+Mathf.Sin(Time.unscaledTime*10)*.08f;
            fire.localScale=Vector3.one*(intensity*flutter);
            smoke.position=actor.EyePosition;
            for(int i=0;i<puffs.Length;i++) {
                float age=game.Rig.ReduceMotion?(i+.5f)/3:Mathf.Repeat(Time.unscaledTime*.45f+i/3f,1);
                puffs[i].localPosition=new Vector3((i-1)*.13f,age*.75f,.06f);puffs[i].localScale=Vector3.one*(.12f+age*.22f)*intensity;
            }
        }
        private void OnDestroy(){if(smokeMaterial)Destroy(smokeMaterial);}
    }
}
