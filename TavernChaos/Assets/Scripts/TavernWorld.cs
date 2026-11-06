using UnityEngine;
using UnityEngine.Rendering;

namespace TavernChaos
{
    // Original modular blockout. No downloaded art or external content.
    public static class TavernWorld
    {
        public static Material Wood, DarkWood, Metal, Brick, Cream, Teal, Red, Blue, Skin, Black;
        public static Transform Root;
        public static readonly Vector3 BarPoint = new(-7.0f, 0, 1.1f);
        public static readonly Vector3 PoolPoint = new(5.8f, 0, .4f);
        public static Material Mat(string name, Color color, float metallic = 0, bool glow = false)
        {
            var m = new Material(Shader.Find("Standard")) { name = name, color = color };
            m.SetFloat("_Metallic", metallic); m.SetFloat("_Glossiness", metallic > 0 ? .42f : .15f);
            if (glow) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * 2.3f); }
            return m;
        }
        public static GameObject Shape(string name, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Transform parent = null, bool solid = true)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name;
            g.transform.SetParent(parent ? parent : Root, false); g.transform.localPosition = pos; g.transform.localScale = scale;
            g.GetComponent<Renderer>().sharedMaterial = mat;
            if (!solid) Object.Destroy(g.GetComponent<Collider>());
            return g;
        }
        public static GameObject Box(string n, Vector3 p, Vector3 s, Material m, Transform parent = null, bool solid = true) => Shape(n, PrimitiveType.Cube, p, s, m, parent, solid);
        public static GameObject Cylinder(string n, Vector3 p, Vector3 s, Material m, Transform parent = null, bool solid = true) => Shape(n, PrimitiveType.Cylinder, p, s, m, parent, solid);
        public static void Text(string value, Vector3 pos, float size, Color color, Transform parent = null)
        {
            var go = new GameObject(value); go.transform.SetParent(parent ? parent : Root, false); go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.identity;
            var t = go.AddComponent<TextMesh>(); t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); go.GetComponent<Renderer>().sharedMaterial=t.font.material; t.text = value; t.fontSize = 80; t.characterSize = size/8; t.anchor = TextAnchor.MiddleCenter; t.alignment = TextAlignment.Center; t.color = color;
        }
        public static void LightAt(Vector3 pos, Color color, float intensity, float range)
        {
            var g = new GameObject("Practical light"); g.transform.parent = Root; g.transform.position = pos;
            var l = g.AddComponent<Light>(); l.type = LightType.Point; l.color = color; l.intensity = intensity; l.range = range;
        }
        public static void Build()
        {
            Root = new GameObject("TAVERN | modular environment").transform;
            Wood = Mat("Warm oak", new Color(.32f,.15f,.075f)); DarkWood = Mat("Smoked walnut", new Color(.105f,.055f,.045f));
            Metal = Mat("Blackened steel", new Color(.07f,.085f,.10f), .55f); Brick = Mat("Terracotta", new Color(.25f,.13f,.12f));
            Cream = Mat("Ivory",new Color(.93f,.81f,.59f)); Teal = Mat("Neon mint",new Color(.12f,.8f,.64f),0,true);
            Red = Mat("Neon coral",new Color(1,.18f,.12f),0,true); Blue = Mat("Blue glass",new Color(.07f,.36f,.9f),.4f,true);
            Skin = Mat("Skin",new Color(.78f,.48f,.29f)); Black = Mat("Rubber",new Color(.025f,.03f,.045f));
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.34f,.37f,.44f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.075f,.075f,.105f); RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 15; RenderSettings.fogEndDistance = 40;
            var sun = new GameObject("Soft overhead").AddComponent<Light>(); sun.transform.rotation=Quaternion.Euler(58,-25,0); sun.type=LightType.Directional; sun.color=new Color(1,.78f,.59f); sun.intensity=1.15f; sun.shadows=LightShadows.Soft;
            Box("Foundation", new(0,-.22f,0), new(24,.4f,18),DarkWood);
            for(int x=0;x<24;x++) for(int z=0;z<6;z++)
                Box("Oak floorboard",new(x-11.5f,-.025f,z*3-7.5f),new(.97f,.08f,2.97f), (x+z)%4==0 ? DarkWood : Wood);
            Box("Dark ceiling",new(0,6,0),new(24,.2f,18),Black);
            Box("Back wall",new(0,3,9),new(24,6,.3f),Brick); Box("Left wall",new(-12,3,0),new(.3f,6,18),Brick); Box("Right wall",new(12,3,0),new(.3f,6,18),Brick);
            Box("Front wall",new(0,3,-9),new(24,6,.3f),DarkWood);
            for(int x=-10;x<=10;x+=4) { Box("Pillar",new(x,2.9f,8.7f),new(.22f,5.8f,.3f),Metal); Box("Ceiling beam",new(x,5.6f,0),new(.22f,.32f,18),Metal); }
            for(int z=-8;z<9;z+=2) { Box("Wainscot",new(-11.8f,.9f,z),new(.15f,1.8f,1.95f),DarkWood); Box("Wainscot",new(11.8f,.9f,z),new(.15f,1.8f,1.95f),DarkWood); }
            Box("Stage",new(0,.27f,6.8f),new(10,.54f,3.8f),DarkWood);
            for(int i=-5;i<6;i++) Box("Curtain fold",new(i*.76f,3.3f,8.65f),new(.55f,4.3f,.18f),i%2==0?Black:DarkWood,null,false);
            Box("Stage neon",new(0,.57f,4.87f),new(10,.05f,.055f),Red,null,false);
            Box("Sign backing",new(0,4.15f,8.35f),new(6,1.4f,.15f),Black);
            Text("TAVERN & CHAOS",new(0,4.2f,8.22f),.43f,new Color(1,.54f,.28f));
            Text("LIVE LOUD. FALL HARD.",new(0,3.65f,8.18f),.16f,new Color(.55f,.9f,.8f));
            DrumKit();
            foreach(int side in new[]{-1,1}) {
                Box("Amp",new(side*3.7f,1.2f,7.6f),new(1.2f,1.3f,.65f),Metal);
                for(int j=0;j<2;j++) { var speaker=Cylinder("Speaker",new(side*3.7f,.95f+j*.55f,7.23f),new(.44f,.06f,.44f),Black,null,false); speaker.transform.rotation=Quaternion.Euler(90,0,0); }
                LightAt(new(side*3,3.8f,6.3f),side<0?new Color(1,.12f,.08f):new Color(.12f,.26f,1),4,9);
            }
            // Bar is reachable from the central aisle, with an open end for the bartender.
            Box("Bar cabinet",new(-9, .65f,2.6f),new(2,1.3f,8),DarkWood); Box("Bar counter",new(-8.85f,1.4f,2.6f),new(2.5f,.22f,8.3f),Wood);
            Box("Counter light",new(-7.57f,1.2f,2.6f),new(.035f,.05f,8),Teal,null,false);
            for(int z=-1;z<7;z+=2) { Stool(new(-6.7f,0,z)); }
            for(int y=2;y<=4;y++) { Box("Bottle shelf",new(-11.5f,y,3),new(.7f,.12f,7),Wood); for(int z=0;z<7;z++) BottleVisual(new(-11.4f,y+.3f,z),null,z%2==0?Blue:Red); }
            Text("THE ALCHEMY BAR",new(-8.7f,4.7f,7.9f),.24f,new Color(.37f,1,.79f));
            // Cafe furniture, leaving the middle clear for the duel.
            Table(new(-5,0,-4.8f)); Table(new(5.6f,0,-5.4f)); Table(new(8.9f,0,5.8f));
            for(int i=0;i<3;i++) { var lampPos=new Vector3(-5+i*5,4.8f,-3); Cylinder("Pendant",lampPos,new(.65f,.10f,.65f),Metal,null,false); Cylinder("Lamp glow",lampPos+Vector3.down*.12f,new(.5f,.03f,.5f),Cream,null,false); LightAt(lampPos+Vector3.down*.3f,new Color(1,.62f,.3f),2.1f,8); }
            // A small rug makes the punch space readable.
            Box("Duel rug",new(0,.03f,-.8f),new(4.8f,.025f,5.4f),Mat("Rug burgundy",new Color(.28f,.055f,.065f)),null,false);
            for(int s=-1;s<=1;s+=2) Box("Rug edging",new(s*2.2f,.05f,-.8f),new(.055f,.01f,5.1f),Cream,null,false);
            Text("ONE HIT CLUB",new(0,2.9f,8.15f),.2f,new Color(.9f,.8f,.6f));
            Text("01  /  EL RING",new(0,.065f,-.8f),.2f,Color.white); var floorText=Root.GetChild(Root.childCount-1); floorText.localRotation=Quaternion.Euler(90,0,0);
            LightAt(new(0,4,-.8f),new Color(1,.62f,.36f),2,9); LightAt(new(6,3,1),new Color(.13f,.8f,.66f),2.5f,8);
            PoolPractice.BuildTable();
            ArmPresentation.BuildTable();
        }
        public static void BottleVisual(Vector3 pos, Transform parent, Material mat)
        {
            Cylinder("Bottle body",pos,new(.16f,.2f,.16f),mat,parent,false);
            Cylinder("Bottle neck",pos+Vector3.up*.27f,new(.07f,.09f,.07f),mat,parent,false);
            Cylinder("Bottle label",pos+Vector3.down*.02f,new(.164f,.08f,.164f),Cream,parent,false);
        }
        private static void Stool(Vector3 p)
        {
            Cylinder("Stool seat",p+Vector3.up*.9f,new(.64f,.07f,.64f),DarkWood);
            Cylinder("Stool stem",p+Vector3.up*.45f,new(.08f,.45f,.08f),Metal);
            Cylinder("Stool foot",p+Vector3.up*.06f,new(.5f,.06f,.5f),Metal);
        }
        private static void Table(Vector3 p)
        {
            Cylinder("Cafe table",p+Vector3.up*1.05f,new(1.9f,.1f,1.9f),Wood); Cylinder("Table stem",p+Vector3.up*.5f,new(.12f,.5f,.12f),Metal);
            for(int i=0;i<3;i++) { float a=i*Mathf.PI*2/3; Vector3 q=p+new Vector3(Mathf.Sin(a)*1.5f,0,Mathf.Cos(a)*1.5f); Stool(q); }
            BottleVisual(p+new Vector3(.25f,1.4f,.1f),null,Blue);
        }
        private static void DrumKit()
        {
            var d=Cylinder("Kick drum",new(0,1.12f,7.3f),new(1.2f,.4f,1.2f),Red); d.transform.rotation=Quaternion.Euler(90,0,0);
            var front=Cylinder("Kick skin",new(0,1.12f,6.87f),new(1.07f,.015f,1.07f),Cream,null,false);front.transform.rotation=Quaternion.Euler(90,0,0);
            Text("T&C",new(0,1.12f,6.83f),.18f,new Color(.15f,.1f,.1f));
            foreach(float x in new[]{-1.3f,1.3f}) { Cylinder("Cymbal stand",new(x,1.3f,7.2f),new(.035f,.75f,.035f),Metal); Cylinder("Cymbal",new(x,2.07f,7.2f),new(.92f,.025f,.92f),Cream); }
            Cylinder("Tom",new(-.55f,1.65f,7.4f),new(.58f,.22f,.58f),Red); Cylinder("Tom",new(.55f,1.65f,7.4f),new(.58f,.22f,.58f),Red);
        }
    }
}
