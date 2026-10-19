using System;
using System.Collections.Generic;
using System.Linq;

namespace MinecraftFoods
{
    internal sealed class LootChoice
    {
        internal string Id,Tier;
        internal double Weight;
        internal LootChoice(string id,string tier,double weight){Id=id;Tier=tier;Weight=weight;}
    }
    internal static class LootWeights
    {
        internal static Dictionary<string,double> Normalize(IEnumerable<LootChoice> choices,Dictionary<string,double> tiers)
        {
            var active=choices.Where(x=>x.Weight>0 && !double.IsInfinity(x.Weight) && !double.IsNaN(x.Weight) && tiers.TryGetValue(x.Tier,out var w) && w>0 && !double.IsNaN(w) && !double.IsInfinity(w)).ToArray();
            var sums=active.GroupBy(x=>x.Tier).ToDictionary(g=>g.Key,g=>g.Sum(x=>x.Weight));
            double total=sums.Keys.Sum(t=>tiers[t]);
            var result=new Dictionary<string,double>();
            if(total<=0)return result;
            foreach(var food in active)result[food.Id]=tiers[food.Tier]/total*food.Weight/sums[food.Tier];
            return result;
        }
    }
}
