using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace MinecraftFoods
{
    internal static class Visuals
    {
        static readonly Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        static readonly Dictionary<string,Mesh> meshes=new Dictionary<string,Mesh>();
        static readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
        internal static Texture2D TextureFor(string id)
        {
            if(textures.TryGetValue(id,out var found))return found;
            var t=new Texture2D(2,2,TextureFormat.RGBA32,false,false){name="MC_"+id};
            if(!ImageConversion.LoadImage(t,File.ReadAllBytes(Path.Combine(Plugin.Root,"textures",id+".png")),false))throw new InvalidDataException(id);
            t.filterMode=FilterMode.Point;t.wrapMode=TextureWrapMode.Clamp;t.anisoLevel=0;
            textures[id]=t;return t;
        }
        internal static Texture2D IconFor(string id)=>TextureFor(id);
        internal static Material MaterialFor(string id,Color tint)
        {
            string key=id+ColorUtility.ToHtmlStringRGBA(tint);
            if(materials.TryGetValue(key,out var material))return material;
            var shader=Shader.Find("Universal Render Pipeline/Unlit")??Shader.Find("Unlit/Texture");
            if(!shader)throw new InvalidOperationException("No unlit item shader available.");
            material=new Material(shader){name="MC_"+key,mainTexture=TextureFor(id)};
            if(material.HasProperty("_BaseMap"))material.SetTexture("_BaseMap",TextureFor(id));
            if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",tint);
            if(material.HasProperty("_Color"))material.SetColor("_Color",tint);
            if(material.HasProperty("_Surface"))material.SetFloat("_Surface",0);
            if(material.HasProperty("_Cull"))material.SetFloat("_Cull",(int)CullMode.Off);
            material.renderQueue=(int)RenderQueue.Geometry;
            materials[key]=material;return material;
        }
        internal static void Build(Item item,FoodMarker marker)
        {
            var root=item.transform;
            var left=root.Find("Hand_L");var right=root.Find("Hand_R");
            if(!left || !right)throw new InvalidOperationException("Native hand anchors missing: "+item.name);
            // Keep every hand transform and rotation intact. Do not inherit the -90 degree
            // rotation of the source mesh (the cause of the previous edge-on sprite).
            Vector3 midpoint=(left.localPosition+right.localPosition)*.5f;
            float span=Mathf.Abs(right.localPosition.x-left.localPosition.x);
            float width=Mathf.Clamp(span+.065f,.18f,.52f);
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))
                if(r is MeshRenderer || r is SkinnedMeshRenderer)Object.DestroyImmediate(r);
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))Object.DestroyImmediate(filter);
            foreach(var col in root.GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(col);
            var visual=new GameObject("MinecraftFoodVisual");
            visual.layer=item.gameObject.layer;
            visual.transform.SetParent(root,false);
            visual.transform.localPosition=midpoint+new Vector3(0,.015f,.035f);
            visual.transform.localRotation=Quaternion.identity;
            visual.transform.localScale=Vector3.one*width;
            marker.mesh=visual.AddComponent<MeshFilter>();marker.visual=visual.AddComponent<MeshRenderer>();marker.meshSize=width;
            marker.visual.shadowCastingMode=ShadowCastingMode.Off;marker.visual.receiveShadows=false;
            marker.Refresh();
            // Collider follows the visible volume instead of a giant invisible egg.
            var box=visual.AddComponent<BoxCollider>();
            box.center=marker.mesh.sharedMesh.bounds.center;
            box.size=new Vector3(marker.mesh.sharedMesh.bounds.size.x,marker.mesh.sharedMesh.bounds.size.y,.09f);
            item.mainRenderer=marker.visual;item.addtlRenderers=new Renderer[0];
            item.offsetLuggageSpawn=false;
            item.forceScale=true;
        }
        internal static Mesh MeshFor(string id)
        {
            if(meshes.TryGetValue(id,out var mesh))return mesh;
            var t=TextureFor(id);var pixels=t.GetPixels32();
            int minX=t.width,maxX=-1,minY=t.height,maxY=-1;
            for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(pixels[y*t.width+x].a>127){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
            if(maxX<minX)throw new InvalidDataException("Empty texture: "+id);
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
            float unit=1f/(maxX-minX+1), cx=(minX+maxX+1)*.5f,cy=(minY+maxY+1)*.5f,z=unit*.5f;
            bool Solid(int x,int y)=>x>=0&&y>=0&&x<t.width&&y<t.height&&pixels[y*t.width+x].a>127;
            void Face(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector2 p)
            {
                int n=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);vertices.Add(d);
                uv.Add(p);uv.Add(p);uv.Add(p);uv.Add(p);
                indices.Add(n);indices.Add(n+1);indices.Add(n+2);indices.Add(n);indices.Add(n+2);indices.Add(n+3);
            }
            for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
            {
                if(!Solid(x,y))continue;
                float l=(x-cx)*unit,r=l+unit,b=(y-cy)*unit,a=b+unit;
                Vector2 p=new Vector2((x+.5f)/t.width,(y+.5f)/t.height);
                Face(new Vector3(l,b,-z),new Vector3(l,a,-z),new Vector3(r,a,-z),new Vector3(r,b,-z),p);
                Face(new Vector3(r,b,z),new Vector3(r,a,z),new Vector3(l,a,z),new Vector3(l,b,z),p);
                if(!Solid(x-1,y))Face(new Vector3(l,b,z),new Vector3(l,a,z),new Vector3(l,a,-z),new Vector3(l,b,-z),p);
                if(!Solid(x+1,y))Face(new Vector3(r,b,-z),new Vector3(r,a,-z),new Vector3(r,a,z),new Vector3(r,b,z),p);
                if(!Solid(x,y-1))Face(new Vector3(l,b,z),new Vector3(l,b,-z),new Vector3(r,b,-z),new Vector3(r,b,z),p);
                if(!Solid(x,y+1))Face(new Vector3(l,a,-z),new Vector3(l,a,z),new Vector3(r,a,z),new Vector3(r,a,-z),p);
            }
            mesh=new Mesh{name="MC_generated_"+id};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
            meshes[id]=mesh;return mesh;
        }
    }
}
