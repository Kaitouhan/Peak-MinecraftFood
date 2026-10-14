using System;
using UnityEngine;

namespace MinecraftFoods
{
    [Serializable] public sealed class Catalogue { public FoodDefinition[] foods; }
    [Serializable] public sealed class FoodDefinition
    {
        public string id, name, texture, cookedName, cookedTexture, rarity, description, effectDescription;
        public float hunger, saturation, cookedHunger, cookedSaturation;
        public int scorch, wreck, weight, uses = 1;
        public float burnPoison;
        public bool HasCooked => !string.IsNullOrEmpty(cookedTexture);
        public bool Cooked(int stage) => HasCooked && stage >= 1;
        public string NameAt(int stage)
        {
            string n = Cooked(stage) ? cookedName : name;
            if (stage >= wreck) return n + "（烧毁，不能食用）";
            if (stage >= scorch) return n + "（烧焦）";
            return stage > 0 && !HasCooked ? n + "（加热" + stage + "次）" : n;
        }
        public string TextureAt(int stage) => Cooked(stage) ? cookedTexture : texture;
        public float NutritionFactor(int stage) => stage >= wreck ? 0 : stage < scorch ? 1 : Mathf.Max(.1f, 1f - .3f * (stage - scorch + 1));
        public float HungerAt(int stage) => (Cooked(stage) ? cookedHunger : hunger) * NutritionFactor(stage);
        public float SaturationAt(int stage) => (Cooked(stage) ? cookedSaturation : saturation) * NutritionFactor(stage);
        public float BurnPoisonAt(int stage) => stage < scorch || stage >= wreck ? 0 : Mathf.Min(.6f, burnPoison * (stage - scorch + 1));
        public Color TintAt(int stage)
        {
            if (stage >= wreck) return new Color(.08f, .065f, .055f, 1);
            if (stage >= scorch) return Color.Lerp(new Color(.48f,.30f,.20f), new Color(.12f,.09f,.07f), (stage-scorch)/(float)Math.Max(1,wreck-scorch));
            return stage == 0 ? Color.white : Color.Lerp(Color.white,new Color(.77f,.64f,.47f), stage/(float)scorch);
        }
    }
    // A marker owned by this plugin is the only criterion for applying patches.
    // In particular, descriptions and loose item-name substring matching are not used.
    public sealed class FoodMarker : MonoBehaviour
    {
        public string foodId;
        public MeshFilter mesh;
        public MeshRenderer visual;
        public float meshSize;
        private Item item;
        private int lastStage = -1;
        public FoodDefinition Definition => Plugin.Foods.TryGetValue(foodId ?? "", out var f) ? f : null;
        public int Stage => ReadStage(item ? item.data : GetComponent<Item>()?.data);
        public static int ReadStage(ItemInstanceData data) => data != null && data.TryGetDataEntry<IntItemData>(DataEntryKey.CookedAmount, out var value) ? Mathf.Clamp(value.Value, 0, 12) : 0;
        void Awake() { item = GetComponent<Item>(); }
        void LateUpdate() { if (Stage != lastStage) Refresh(); }
        public void Refresh()
        {
            var f = Definition; if (f == null || !visual || !mesh) return;
            lastStage = Stage;
            mesh.sharedMesh = Visuals.MeshFor(f.TextureAt(lastStage));
            visual.sharedMaterial = Visuals.MaterialFor(f.TextureAt(lastStage), f.TintAt(lastStage));
            var box=visual.GetComponent<BoxCollider>();
            if(box){box.center=mesh.sharedMesh.bounds.center;box.size=new Vector3(mesh.sharedMesh.bounds.size.x,mesh.sharedMesh.bounds.size.y,.09f);}
        }
    }
}
