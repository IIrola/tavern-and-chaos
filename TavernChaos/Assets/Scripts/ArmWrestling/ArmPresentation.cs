using UnityEngine;
namespace TavernChaos
{
    // Visual response only: these arms have no colliders and never decide force or victory.
    public sealed class ArmPresentation : MonoBehaviour
    {
        private Transform left,right,grip;
        private float shown,flashUntil;
        private readonly int[] last={0,0};
        public static void BuildTable()
        {
            var center=ArmRoomController.Center;
            var root=new GameObject("Arm wrestling | provisional table").transform;root.position=center;
            TavernWorld.Box("Table top",new(0,1.0f,0),new(1.8f,.18f,1.0f),TavernWorld.Wood,root);
            TavernWorld.Box("Table base",new(0,.48f,0),new(.65f,.9f,.65f),TavernWorld.DarkWood,root);
            TavernWorld.Box("Left pad",new(-.6f,1.12f,0),new(.4f,.06f,.5f),TavernWorld.Blue,root,false);
            TavernWorld.Box("Right pad",new(.6f,1.12f,0),new(.4f,.06f,.5f),TavernWorld.Red,root,false);
            foreach(int side in new[]{-1,1}) {
                TavernWorld.Box("Seat visual",new(side*1.35f,.75f,0),new(.6f,.12f,.6f),TavernWorld.DarkWood,root,false);
                TavernWorld.Box("Seat stem",new(side*1.35f,.35f,0),new(.1f,.7f,.1f),TavernWorld.Metal,root,false);
            }
            TavernWorld.Text("03 / PULSEADAS",new(0,.08f,-1.0f),.18f,new Color(.4f,1,.8f),root);
            root.GetChild(root.childCount-1).localRotation=Quaternion.Euler(90,0,0);
            var presentation=root.gameObject.AddComponent<ArmPresentation>();
            presentation.left=TavernWorld.Cylinder("Blue forearm",Vector3.zero,Vector3.one,TavernWorld.Blue,root,false).transform;
            presentation.right=TavernWorld.Cylinder("Red forearm",Vector3.zero,Vector3.one,TavernWorld.Red,root,false).transform;
            presentation.grip=TavernWorld.Shape("Joined hands",PrimitiveType.Sphere,Vector3.zero,Vector3.one*.25f,TavernWorld.Skin,root,false).transform;
        }
        private void Update()
        {
            var g=DemoGame.Instance;var state=g?.Lan?.View?.arm;
            float target=state?.position??0;shown=Mathf.Lerp(shown,target,1-Mathf.Exp(-16*Time.unscaledDeltaTime));
            float x=shown/100*.6f;var point=new Vector3(x,Mathf.Lerp(1.65f,1.12f,Mathf.Abs(shown)/100),0);
            Segment(left,new Vector3(-.6f,1.12f,0),point);Segment(right,new Vector3(.6f,1.12f,0),point);grip.localPosition=point;
            if(state!=null)for(int i=0;i<2;i++) {
                if(state.pulses[i]>last[i]&&state.feedback[i]==(int)ArmFeedback.Hit) {
                    flashUntil=Time.unscaledTime+.18f;
                    if(g.Lan.LocalArmSeated){g.Audio.Play("clink",.4f);if(!g.Rig.ReduceMotion)g.Rig.AddImpact(.12f);}
                }
                last[i]=state.pulses[i];
            }
            float pop=g&&g.Rig.ReduceMotion?0:Mathf.Clamp01((flashUntil-Time.unscaledTime)/.18f);
            grip.localScale=Vector3.one*(.25f+pop*.035f);
        }
        private static void Segment(Transform part,Vector3 from,Vector3 to)
        {
            part.localPosition=(from+to)*.5f;part.localRotation=Quaternion.FromToRotation(Vector3.up,to-from);part.localScale=new Vector3(.18f,Vector3.Distance(from,to)*.5f,.18f);
        }
    }
}
