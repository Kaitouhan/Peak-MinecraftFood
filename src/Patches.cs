using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using S=CharacterAfflictions.STATUSTYPE;

namespace MinecraftFoods
{
    [HarmonyPatch(typeof(Item),nameof(Item.GetName))]
    internal static class NamePatch
    {
        static bool Prefix(Item __instance,ref string __result)
        {
            var m=__instance.GetComponent<FoodMarker>();if(!m||m.Definition==null)return true;
            __result=m.Definition.NameAt(m.Stage);return false;
        }
    }
    [HarmonyPatch(typeof(Item),nameof(Item.GetItemName))]
    internal static class CookedNamePatch
    {
        static bool Prefix(Item __instance,ItemInstanceData data,ref string __result)
        {
            var m=__instance.GetComponent<FoodMarker>();if(!m||m.Definition==null)return true;
            __result=m.Definition.NameAt(data==null?m.Stage:FoodMarker.ReadStage(data));return false;
        }
    }
    [HarmonyPatch(typeof(InventoryItemUI),nameof(InventoryItemUI.SetItem))]
    internal static class SlotPatch
    {
        static void Prefix(InventoryItemUI __instance,ItemSlot slot)
        {
            if(slot?.prefab && slot.prefab.GetComponent<FoodMarker>())__instance._itemData=slot.data;
        }
        static void Postfix(InventoryItemUI __instance,ItemSlot slot)
        {
            if(slot==null || !slot.prefab)return;
            Apply(__instance,slot.prefab,slot.data);
        }
        internal static void Apply(InventoryItemUI ui,Item prefab,ItemInstanceData data)
        {
            var m=prefab?prefab.GetComponent<FoodMarker>():null;if(!m||m.Definition==null)return;
            int s=FoodMarker.ReadStage(data);ui.icon.texture=Visuals.IconFor(m.Definition.TextureAt(s));ui.icon.color=m.Definition.TintAt(s);
            if(ui.nameText)ui.nameText.text=m.Definition.NameAt(s);
        }
    }
    [HarmonyPatch(typeof(InventoryItemUI),"UpdateCookedAmount")]
    internal static class SlotCookingPatch
    {
        static void Postfix(InventoryItemUI __instance,Item ____itemPrefab)=>SlotPatch.Apply(__instance,____itemPrefab,__instance._itemData);
    }
    [HarmonyPatch(typeof(ItemCooking),nameof(ItemCooking.UpdateCookedBehavior))]
    internal static class CookingPatch
    {
        static bool Prefix(ItemCooking __instance)
        {
            var marker=__instance.GetComponent<FoodMarker>();if(!marker||marker.Definition==null)return true;
            __instance.GetComponent<Item>().GetData<IntItemData>(DataEntryKey.CookedAmount);
            marker.Refresh();
            if(marker.Stage>=marker.Definition.wreck && __instance.GetComponent<FoodEatAction>())__instance.Wreck();
            return false;
        }
    }
    [HarmonyPatch(typeof(CharacterAfflictions),nameof(CharacterAfflictions.AddStatus))]
    internal static class StatusPatch
    {
        static readonly System.Reflection.FieldInfo Invincible=AccessTools.Field(typeof(CharacterData),"isInvincible");
        static void Prefix(CharacterAfflictions __instance,S statusType,ref float amount,bool fromRPC,bool ignoreInvincibility)
        {
            var c=__instance.character;
            if(!c || !c.IsLocal || fromRPC || c.statusesLocked || c.data.dead || c.isZombie || c.isScoutmaster || c.data.isSkeleton || FoodPlayerState.InAirport(__instance))return;
            if((bool)Invincible.GetValue(c.data) && !ignoreInvincibility)return;
            if(statusType==S.Hunger && !__instance.canGetHungry)return;
            var state=c.GetComponent<FoodPlayerState>();if(state)state.FilterStatus(statusType,ref amount);
        }
    }
    [HarmonyPatch(typeof(CharacterAfflictions),nameof(CharacterAfflictions.SubtractStatus))]
    internal static class CurePatch
    {
        static void Postfix(CharacterAfflictions __instance,S statusType,float amount,bool fromRPC,bool decreasedNaturally)
        {
            // Real antidotes clear future food poison; passive poison decay must not.
            if(statusType!=S.Poison || amount<=0 || fromRPC || decreasedNaturally || !__instance.character || !__instance.character.IsLocal || __instance.character.statusesLocked)return;
            var state=__instance.character.GetComponent<FoodPlayerState>();if(state)state.RemoveFoodPoisonTimers();
        }
    }
    [HarmonyPatch(typeof(LootData),nameof(LootData.PopulateLootData))]
    internal static class LootPatch
    {
        static void Postfix()
        {
            if(Plugin.Registered.Count==0 || LootData.AllSpawnWeightData==null)return;
            var foods=Plugin.Registered.ToDictionary(x=>x.itemID,x=>x.GetComponent<FoodMarker>().Definition);
            foreach(var pool in LootData.AllSpawnWeightData)
            {
                if(!pool.Key.ToString().StartsWith("Luggage",StringComparison.Ordinal))continue;
                double share=pool.Key.ToString()=="LuggageCursed" ? Plugin.CursedLootShare.Value :
                    pool.Key.ToString()=="LuggageClown" ? Plugin.ClownLootShare.Value : Plugin.LootShare.Value;
                foreach(var id in foods.Keys)pool.Value.Remove(id);
                if(share<=0)continue;
                // A larger integer unit prevents epic items rounding up to common odds.
                foreach(var id in pool.Value.Keys.ToArray())pool.Value[id]*=128;
                double vanilla=pool.Value.Values.Sum(x=>(double)x);
                double budget=vanilla*share/(1-share);
                if(vanilla==0)budget=foods.Values.Sum(x=>(double)x.weight);
                var poolName=pool.Key.ToString();
                var tierShares=poolName=="LuggageCursed"
                    ? new System.Collections.Generic.Dictionary<string,double>{{"common",35},{"uncommon",25},{"rare",30},{"special",10}}
                    : poolName=="LuggageClown"
                        ? new System.Collections.Generic.Dictionary<string,double>{{"common",45},{"uncommon",30},{"rare",18},{"special",7}}
                        : new System.Collections.Generic.Dictionary<string,double>{{"common",60},{"uncommon",25},{"rare",12},{"special",3}};
                var tierTotals=foods.Values.GroupBy(f=>LootTier(f,poolName)).ToDictionary(g=>g.Key,g=>g.Sum(f=>(double)f.weight));
                foreach(var food in foods)
                {
                    string tier=LootTier(food.Value,poolName);
                    double tierTotal=tierTotals[tier];
                    double relative=tierShares[tier]/100.0 * food.Value.weight/tierTotal;
                    pool.Value[food.Key]=Math.Max(1,(int)Math.Round(budget*relative));
                }
            }
        }
        static string LootTier(FoodDefinition food,string poolName)
        {
            if(food.id=="golden_apple" || food.id=="enchanted_golden_apple" || food.id=="chorus_fruit")return "special";
            if(poolName=="LuggageCursed" && food.id=="rotten_flesh")return "uncommon";
            return string.IsNullOrEmpty(food.rarity) ? "common" : food.rarity.ToLowerInvariant();
        }
    }
}
