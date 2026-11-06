using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    // A shared timeline, rendered locally without colliders or an independent physical outcome.
    public sealed class SessionEndPresentation : MonoBehaviour
    {
        private DemoGame g;
        private GameObject stage;
        private readonly List<Transform> guests=new();
        private RoomPhase previous=RoomPhase.Open;
        private static readonly Vector3 Origin=new(0,0,-35);
        public float Progress { get; private set; }
        private void Awake() { g=GetComponent<DemoGame>(); }
        private void Update()
        {
            var ending=g.Lan?.Ending;
            bool show=ending!=null&&ending.reason==RoomEndReason.Insolvency&&(ending.phase==RoomPhase.Ejecting||ending.phase==RoomPhase.Results);
            if(!show) { if(stage) Destroy(stage); guests.Clear(); previous=RoomPhase.Open; Progress=0; return; }
            if(!stage) Build();
            Progress=ending.phase==RoomPhase.Results?1:Mathf.Clamp01((float)((g.Lan.EstimatedServerTime-ending.phaseStarted)/RoomLifecycle.EjectionSeconds));
            if(previous!=ending.phase) { previous=ending.phase; g.Audio.Play(ending.phase==RoomPhase.Ejecting?"miss":"ui",.6f); }
            for(int i=0;i<guests.Count;i++) {
                float t=Mathf.SmoothStep(0,1,Mathf.Clamp01((Progress-.12f-i*.04f)/.65f));
                float x=(i-(guests.Count-1)*.5f)*.85f;
                guests[i].localPosition=new Vector3(x,g.Rig.ReduceMotion?0:Mathf.Sin(t*Mathf.PI)*1.9f,Mathf.Lerp(.3f,-4,t));
                guests[i].localRotation=Quaternion.Euler(g.Rig.ReduceMotion?0:t*350,0,g.Rig.ReduceMotion?0:Mathf.Sin(t*Mathf.PI*2)*18);
            }
        }
        public bool TryCamera(out Vector3 position,out Vector3 target)
        {
            position=Origin+new Vector3(7,4,-9); target=Origin+new Vector3(0,1,-1.4f);
            return stage&&g.Lan.BlocksPlay&&g.Lan.Ending.reason==RoomEndReason.Insolvency;
        }
        private void Build()
        {
            stage=new GameObject("Session ejection | visual only"); stage.transform.position=Origin;
            var root=stage.transform;
            TavernWorld.Box("Outside landing",new(0,-.15f,-2),new(14,.2f,12),TavernWorld.DarkWood,root,false);
            TavernWorld.Box("Tavern frontage left",new(-3,2,1.2f),new(3.7f,4,.25f),TavernWorld.Brick,root,false);
            TavernWorld.Box("Tavern frontage right",new(3,2,1.2f),new(3.7f,4,.25f),TavernWorld.Brick,root,false);
            TavernWorld.Box("Door header",new(0,3.6f,1.2f),new(6,1,.3f),TavernWorld.Black,root,false);
            TavernWorld.Text("LA CASA INVITA... A SALIR",new(0,3.6f,.95f),.25f,new Color(.4f,1,.78f),root);
            Puppet("Door staff left",new(-1.7f,0,.2f),TavernWorld.Black,1.25f,root);
            Puppet("Door staff right",new(1.7f,0,.2f),TavernWorld.Black,1.25f,root);
            int count=Mathf.Clamp(g.Lan.Ending.participants,1,6);
            for(int i=0;i<count;i++) guests.Add(Puppet("Guest "+i,Vector3.zero,i%2==0?TavernWorld.Blue:TavernWorld.Red,.85f,root));
        }
        private static Transform Puppet(string name,Vector3 position,Material shirt,float size,Transform parent)
        {
            var root=new GameObject(name).transform; root.SetParent(parent,false); root.localPosition=position; root.localScale=Vector3.one*size;
            TavernWorld.Box("Jacket",new(0,1,0),new(.75f,.8f,.4f),shirt,root,false);
            TavernWorld.Shape("Head",PrimitiveType.Sphere,new(0,1.65f,0),Vector3.one*.48f,TavernWorld.Skin,root,false);
            TavernWorld.Box("Glasses",new(0,1.68f,-.22f),new(.42f,.12f,.07f),TavernWorld.Black,root,false);
            foreach(int side in new[]{-1,1}) {
                TavernWorld.Box("Leg",new(side*.2f,.35f,0),new(.24f,.65f,.3f),TavernWorld.Black,root,false);
                var arm=TavernWorld.Box("Arm",new(side*.55f,1.08f,0),new(.25f,.7f,.3f),TavernWorld.Skin,root,false);
                arm.transform.localRotation=Quaternion.Euler(0,0,side*45);
            }
            return root;
        }
        private void OnDestroy() { if(stage) Destroy(stage); }
    }
}
