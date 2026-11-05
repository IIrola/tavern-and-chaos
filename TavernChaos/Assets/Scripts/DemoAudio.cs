using System.Collections.Generic;
using UnityEngine;

namespace TavernChaos
{
    public sealed class DemoAudio : MonoBehaviour
    {
        public float Volume=.6f;
        private AudioSource source;
        private readonly Dictionary<string,AudioClip> clips=new();
        private void Awake()
        {
            source=gameObject.AddComponent<AudioSource>();
            Create("hit",.17f,100,.65f); Create("clink",.11f,1100,.14f); Create("drink",.3f,430,.09f); Create("miss",.23f,150,.15f); Create("ui",.09f,620,.04f);
        }
        private void Create(string name,float length,float frequency,float noise)
        {
            const int rate=22050; var data=new float[(int)(rate*length)]; var random=new System.Random(13);
            for(int i=0;i<data.Length;i++) { float t=(float)i/rate; float env=Mathf.Exp(-t/length*7); data[i]=(Mathf.Sin(t*frequency*(1-t*.7f)*Mathf.PI*2)*.4f+((float)random.NextDouble()*2-1)*noise)*env; }
            var clip=AudioClip.Create(name,data.Length,1,rate,false); clip.SetData(data,0); clips[name]=clip;
        }
        public void Play(string name,float gain=1) { if(source && clips.TryGetValue(name,out var c)) { source.pitch=Random.Range(.94f,1.06f); source.PlayOneShot(c,Volume*gain); } }
    }
}
