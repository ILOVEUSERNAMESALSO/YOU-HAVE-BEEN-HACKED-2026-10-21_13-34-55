using UnityEngine;
using UnityEngine.UIElements;

namespace Checkpoint
{
    // Original procedural illustration. No downloaded art or paid assets.
    public sealed class CheckpointIllustration : VisualElement
    {
        public string Person { get; set; }
        public bool Warm { get; set; }
        public bool Wide { get; set; }
        public CheckpointIllustration(string person, bool warm = false, bool wide = false)
        {
            Person = person; Warm = warm; Wide = wide;
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }
        static Color C(string value) { ColorUtility.TryParseHtmlString(value, out var c); return c; }
        void Draw(MeshGenerationContext ctx)
        {
            float w = contentRect.width, h = contentRect.height;
            if (w < 1 || h < 1) return;
            var p = ctx.painter2D;
            Rect(p, 0, 0, w, h, Warm ? "#4b4035" : "#223c40");
            // Architectural depth, overhead lights, gate mullions.
            for (int i = 0; i < 6; i++)
            {
                float x = w * i / 5;
                Line(p, new Vector2(x, 0), new Vector2(w * .5f + (x - w * .5f) * .4f, h * .65f), "#40585a", 1);
            }
            Rect(p, w * .07f, h * .1f, w * .86f, h * .025f, "#9fae9d");
            Rect(p, w * .11f, h * .24f, w * .78f, h * .32f, Warm ? "#716552" : "#567070");
            for (int i = 1; i < 5; i++) Rect(p, w * i / 5, h * .24f, 3, h * .32f, "#253b3f");
            Rect(p, 0, h * .65f, w, h * .35f, "#172b30");
            for (int i = 0; i < 5; i++) Line(p, new Vector2(0, h * (.7f + i * .06f)), new Vector2(w, h * (.7f + i * .06f)), "#2b4245", 1);
            if (!string.IsNullOrEmpty(Person))
            {
                float size = Mathf.Min(w * .78f, h * 1.12f);
                DrawPerson(p, w * (Wide ? .66f : .5f), h * .57f, size, Person);
            }
            Rect(p, 0, h * .92f, w, h * .08f, "#11252a");
            Rect(p, w * .04f, h * .04f, 5, 5, Warm ? "#e6b675" : "#9bd4be");
            // Subtle glass lines, not flashing/glitching.
            Line(p, new Vector2(w * .12f, h * .15f), new Vector2(w * .06f, h * .6f), "#729084", 1);
        }
        static void DrawPerson(Painter2D p, float x, float y, float size, string person)
        {
            bool granny = person == "grandmother", tomas = person == "tomas", sena = person == "sena", leila = person == "leila", mira = person == "mira";
            string skin = granny ? "#c8ac91" : tomas ? "#b68c70" : leila ? "#a3765b" : sena ? "#ba8b70" : mira ? "#c59779" : "#b8896d";
            string coat = granny ? "#a28b71" : tomas ? "#787775" : leila ? "#a46a40" : sena ? "#6b7278" : mira ? "#658778" : "#5f7564";
            string hair = granny ? "#d5d2c2" : tomas ? "#5b514c" : "#303133";
            float u = size;
            Poly(p, coat, new Vector2(x-u*.4f,y+u*.5f),new Vector2(x-u*.32f,y+u*.15f),new Vector2(x-u*.13f,y+u*.04f),new Vector2(x+u*.13f,y+u*.04f),new Vector2(x+u*.32f,y+u*.15f),new Vector2(x+u*.4f,y+u*.5f));
            Rect(p,x-u*.065f,y-u*.05f,u*.13f,u*.17f,skin);
            if (leila || mira || granny) Ellipse(p,x,y-u*.19f,u*.205f,u*.26f,hair);
            Ellipse(p,x,y-u*.19f,u*.156f,u*.215f,skin);
            Poly(p,hair,new Vector2(x-u*.16f,y-u*.22f),new Vector2(x-u*.16f,y-u*.35f),new Vector2(x-u*.08f,y-u*.43f),new Vector2(x+u*.1f,y-u*.4f),new Vector2(x+u*.16f,y-u*.3f),new Vector2(x+u*.12f,y-u*.24f),new Vector2(x+u*.07f,y-u*.33f),new Vector2(x-u*.1f,y-u*.3f));
            // A faceted shadow gives the face some volume.
            Poly(p,"#9b745e",new Vector2(x+u*.03f,y-u*.08f),new Vector2(x+u*.14f,y-u*.2f),new Vector2(x+u*.1f,y-u*.01f),new Vector2(x,y+u*.015f));
            Line(p,new Vector2(x-u*.1f,y-u*.22f),new Vector2(x-u*.04f,y-u*.225f),"#393b38",2);
            Line(p,new Vector2(x+u*.04f,y-u*.225f),new Vector2(x+u*.1f,y-u*.22f),"#393b38",2);
            Line(p,new Vector2(x,y-u*.21f),new Vector2(x-u*.014f,y-u*.12f),"#805d4c",2);
            Line(p,new Vector2(x-u*.04f,y-u*.065f),new Vector2(x+u*.045f,y-u*.06f),"#674d44",2);
            if (granny || tomas)
            {
                Line(p,new Vector2(x-u*.12f,y-u*.24f),new Vector2(x+u*.12f,y-u*.24f),"#d6cdb8",2);
                Line(p,new Vector2(x-u*.115f,y-u*.24f),new Vector2(x-u*.1f,y-u*.18f),"#d6cdb8",2);
                Line(p,new Vector2(x+u*.115f,y-u*.24f),new Vector2(x+u*.1f,y-u*.18f),"#d6cdb8",2);
            }
            Poly(p,"#c7c2a9",new Vector2(x-u*.12f,y+u*.06f),new Vector2(x,y+u*.17f),new Vector2(x-u*.05f,y+u*.27f),new Vector2(x-u*.17f,y+u*.1f));
            Poly(p,"#aaa991",new Vector2(x+u*.12f,y+u*.06f),new Vector2(x,y+u*.17f),new Vector2(x+u*.05f,y+u*.27f),new Vector2(x+u*.17f,y+u*.1f));
            Line(p,new Vector2(x,y+u*.18f),new Vector2(x,y+u*.48f),"#394c49",2);
        }
        static void Rect(Painter2D p,float x,float y,float w,float h,string color)
            => Poly(p,color,new Vector2(x,y),new Vector2(x+w,y),new Vector2(x+w,y+h),new Vector2(x,y+h));
        static void Poly(Painter2D p,string color,params Vector2[] points)
        {
            p.fillColor=C(color); p.BeginPath(); p.MoveTo(points[0]);
            for(int i=1;i<points.Length;i++) p.LineTo(points[i]);
            p.ClosePath(); p.Fill();
        }
        static void Line(Painter2D p,Vector2 a,Vector2 b,string color,float width)
        { p.strokeColor=C(color);p.lineWidth=width;p.BeginPath();p.MoveTo(a);p.LineTo(b);p.Stroke(); }
        static void Ellipse(Painter2D p,float x,float y,float rx,float ry,string color)
        {
            p.fillColor=C(color);p.BeginPath();p.MoveTo(new Vector2(x+rx,y));
            const float k=.5522848f;
            p.BezierCurveTo(new Vector2(x+rx,y+ry*k),new Vector2(x+rx*k,y+ry),new Vector2(x,y+ry));
            p.BezierCurveTo(new Vector2(x-rx*k,y+ry),new Vector2(x-rx,y+ry*k),new Vector2(x-rx,y));
            p.BezierCurveTo(new Vector2(x-rx,y-ry*k),new Vector2(x-rx*k,y-ry),new Vector2(x,y-ry));
            p.BezierCurveTo(new Vector2(x+rx*k,y-ry),new Vector2(x+rx,y-ry*k),new Vector2(x+rx,y));
            p.ClosePath();p.Fill();
        }
    }
}
