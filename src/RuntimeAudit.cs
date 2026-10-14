using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Zorro.Core;

namespace MinecraftFoods
{
    internal static class RuntimeAudit
    {
        internal static void Run()
        {
            var lines=new List<string>();int checks=0;
            void Check(bool condition,string label){checks++;if(!condition)throw new InvalidOperationException("AUDIT FAIL: "+label);}
            Check(Plugin.Registered.Count==34,"34 registered foods");
            foreach(var item in Plugin.Registered)
            {
                var m=item.GetComponent<FoodMarker>();var f=m.Definition;
                Check(item.GetComponent<FoodEatAction>()!=null,f.id+" consume action");
                Check(item.GetComponents<ItemActionBase>().All(x=>x is FoodEatAction || x is Action_ReduceUses),f.id+" no inherited effects");
                Check(item.GetComponentsInChildren<MeshRenderer>(true).Length==1,f.id+" only Minecraft mesh");
                Check(item.GetComponentsInChildren<Collider>(true).Length==1,f.id+" one matching collider");
                Check(Vector3.Angle(m.visual.transform.localRotation*Vector3.forward,Vector3.forward)<.01f,f.id+" front facing root");
                Check(Mathf.Abs(item.transform.Find("Hand_R").localPosition.x-item.transform.Find("Hand_L").localPosition.x)<.5f,f.id+" narrow grip");
                Check(NetworkPrefabExists(item),f.id+" network prefab");
                for(int stage=0;stage<=12;stage++)
                {
                    Check(!f.NameAt(stage).Contains("LOC:")&&!f.NameAt(stage).Contains("NAME_"),f.id+" localized name");
                    Check(f.HungerAt(stage)>=0&&f.HungerAt(stage)<=100&&f.BonusAt(stage)>=0&&f.BonusAt(stage)<=100,f.id+" valid nutrition");
                    Check(Visuals.MeshFor(f.TextureAt(stage)).vertexCount>0,f.id+" mesh");
                    Check(Visuals.TextureFor(f.TextureAt(stage)).filterMode==FilterMode.Point,f.id+" sharp texture");
                    Check(stage<f.wreck || f.HungerAt(stage)==0,f.id+" wreck nutrition");
                    if(f.HasCooked && stage==1)Check(f.NameAt(stage)==f.cookedName,f.id+" cooked name");
                }
                var clone=UnityEngine.Object.Instantiate(item.gameObject);
                clone.SetActive(false);
                var ci=clone.GetComponent<Item>();var cooking=clone.GetComponent<ItemCooking>();cooking.item=ci;
                for(int stage=0;stage<=12;stage++)
                {
                    ci.GetData<IntItemData>(DataEntryKey.CookedAmount).Value=stage;
                    cooking.UpdateCookedBehavior();
                    Check(ci.GetItemName()==f.NameAt(stage),f.id+" patched cooked name");
                    Check(stage<f.wreck || !ci.CanUsePrimary(),f.id+" native wreck disables use");
                }
                // Inactive copies never run Item.Awake or register into the live world.
                UnityEngine.Object.Destroy(clone);
                lines.Add("PASS "+f.id+" stages 0..12, hand anchors, mesh, texture, network registration, native burn-disable");
            }
            LootData.PopulateLootData();
            foreach(var pool in LootData.AllSpawnWeightData.Where(x=>x.Key.ToString().StartsWith("Luggage")))
            {
                double total=pool.Value.Values.Sum(x=>(double)x), mc=Plugin.Registered.Sum(x=>pool.Value.TryGetValue(x.itemID,out int w)?(double)w:0);
                Check(Math.Abs(mc/total-Plugin.LootShare.Value)<.002,pool.Key+" loot fraction");
                lines.Add($"PASS {pool.Key}: {mc/total:P4} Minecraft share");
            }
            lines.Add($"PASS {checks} runtime assertions. No player/world gameplay was exercised.");
            File.WriteAllLines(Path.Combine(BepInEx.Paths.BepInExRootPath,"MinecraftFoods-audit.txt"),lines);
            Plugin.Log.LogInfo(lines.Last());
        }
        static bool NetworkPrefabExists(Item item)=>PEAKLib.Core.NetworkPrefabManager.HasNetworkPrefab("0_Items/"+item.name);
    }
}
