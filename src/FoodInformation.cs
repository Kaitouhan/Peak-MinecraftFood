using UnityEngine;
namespace MinecraftFoods
{
    internal static class FoodInformation
    {
        internal static int RemainingUses(Item item,FoodDefinition f)
        {
            if(item.data!=null && item.data.TryGetDataEntry<OptionableIntItemData>(DataEntryKey.ItemUses,out var data) && data.HasData && data.Value>=0)
                return Mathf.Clamp(data.Value,0,f.uses);
            return f.uses;
        }
        internal static string Describe(FoodMarker marker)
        {
            var f=marker.Definition;if(f==null)return "";
            int stage=marker.Stage;var item=marker.GetComponent<Item>();
            string text=FoodTooltip.Render(f.HungerAt(stage),f.BonusAt(stage),f.HungerAt(stage+1),f.BonusAt(stage+1),item.CarryWeight*2.5f,stage,f.wreck,f.uses,RemainingUses(item,f),f.EffectsAt(stage),f.EffectsAt(stage+1));
            if(Plugin.DetailedInfo.Value && stage<f.wreck)
                text+="\n"+L10n.T($"{f.scorch} 次起烧焦，{f.wreck} 次烧毁",$"Charring starts at {f.scorch} cooks; destroyed at {f.wreck}.",$"調理 {f.scorch} 回で焦げ、{f.wreck} 回で燃え尽き。");
            return text;
        }
    }
}
