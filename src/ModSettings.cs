using System;
using System.IO;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace MinecraftFoods
{
    internal static class ModSettings
    {
        internal static ConfigFile File;
        internal static ConfigEntry<string> Language;
        internal static readonly Dictionary<string,ConfigEntry<float>[]> Tiers=new Dictionary<string,ConfigEntry<float>[]>();
        internal static readonly Dictionary<string,ConfigEntry<float>> FoodWeights=new Dictionary<string,ConfigEntry<float>>();
        internal static readonly string[] Pools={"Ordinary","Cursed","Clown"};
        internal static readonly string[] TierNames={"common","uncommon","rare","special"};
        internal static void Open(string root,string legacy)
        {
            string path=Path.Combine(root,"settings.cfg");
            if(!System.IO.File.Exists(path) && System.IO.File.Exists(legacy))System.IO.File.Copy(legacy,path);
            File=new ConfigFile(path,true);File.SaveOnConfigSet=false;
            Language=Bind("UI","Language","Auto","语言：Auto 跟随游戏；zh-CN 中文、en 英语、ja 日语。其他游戏语言回退英语。","Language: Auto follows the game; zh-CN, en, ja override it. Other game languages fall back to English.","言語：Auto はゲームに従う。zh-CN/en/ja で固定。他言語は英語。",new AcceptableValueList<string>("Auto","zh-CN","en","ja"));
            float[][] defaults={new[]{60f,25,12,3},new[]{35f,25,30,10},new[]{45f,30,18,7}};
            for(int i=0;i<Pools.Length;i++)
            {
                string pool=Pools[i];var entries=new ConfigEntry<float>[4];
                for(int j=0;j<4;j++)entries[j]=Bind("Tiers."+pool,TierNames[j],defaults[i][j],
                    "该箱子内本模组食物的稀有度相对权重。0 禁用该档；其余非空档自动归一化。仅房主决定后续掉落。",
                    "Relative tier weight within mod loot in this chest. 0 disables the tier; nonempty tiers are normalized. Host controls future loot.",
                    "この箱のMOD食品内のレア度重み。0で無効。他の有効な区分で正規化。今後のドロップはホスト設定。",new AcceptableValueRange<float>(0,1000));
                Tiers[pool]=entries;
            }
        }
        internal static ConfigEntry<T> Bind<T>(string section,string key,T value,string zh,string en,string ja,AcceptableValueBase range=null)=>
            File.Bind(section,key,value,new ConfigDescription(zh+"\n"+en+"\n"+ja,range));
        internal static void BindFoods(IEnumerable<FoodDefinition> foods)
        {
            foreach(var pool in Pools)foreach(var food in foods)
                FoodWeights[pool+"/"+food.id]=Bind("FoodWeights."+pool,food.id,1f,
                    food.name+"：档内基础权重倍率，1 保持默认，0 禁用，2 表示基础权重加倍（不是最终概率加倍）。仅房主影响后续掉落。",
                    "Within-tier multiplier for "+food.id+". 1 = default, 0 = disabled, 2 = twice the base weight, not twice the final chance. Host affects future loot.",
                    food.id+"：区分内の重み倍率。1は既定、0は無効、2は基礎重みの2倍（最終確率の2倍ではない）。ホストが今後のドロップに適用。",new AcceptableValueRange<float>(0,100));
            File.Save();
        }
        internal static string Pool(string name)=>name=="LuggageCursed"?"Cursed":name=="LuggageClown"?"Clown":"Ordinary";
        internal static double Weight(string pool,string id)=>FoodWeights.TryGetValue(pool+"/"+id,out var value)?value.Value:1;
        internal static Dictionary<string,double> TierWeights(string pool)
        {
            var result=new Dictionary<string,double>();for(int i=0;i<4;i++)result[TierNames[i]]=Tiers[pool][i].Value;return result;
        }
        internal static string Help(ConfigEntryBase entry)
        {
            var lines=entry.Description.Description.Split('\n');int index=L10n.Language=="zh-CN"?0:L10n.Language=="ja"?2:1;
            return lines.Length>index?lines[index]:entry.Description.Description;
        }
    }
}
