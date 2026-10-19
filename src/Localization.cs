using System;
using System.Collections.Generic;
using System.Globalization;

namespace MinecraftFoods
{
    internal static class L10n
    {
        internal static string Language="zh-CN";
        internal static string Resolve(string preference,string client)
        {
            if(preference!="Auto")return preference;
            if(client=="Japanese")return "ja";
            if(client=="SimplifiedChinese" || client=="TraditionalChinese")return "zh-CN";
            return "en";
        }
        internal static string T(string zh,string en,string ja)=>Language=="ja"?ja:Language=="zh-CN"?zh:en;
        internal static string N(float n)=>n.ToString("0.#",CultureInfo.InvariantCulture);
        internal static string Color(string text,string hex)=>"<color=#"+hex+">"+text+"</color>";
        internal static string Status(FoodEffectKind kind)=>kind==FoodEffectKind.Poison?T("中毒","poison","毒"):
            kind==FoodEffectKind.Hunger?T("饥饿值","hunger","空腹度"):
            kind==FoodEffectKind.Drowsy?T("睡意","drowsiness","眠気"):
            kind==FoodEffectKind.Stamina?T("额外体力","extra stamina","追加スタミナ"):T("受伤","injury","負傷");
        internal static string StatusColor(FoodEffectKind kind)=>kind==FoodEffectKind.Hunger?"FFB600":kind==FoodEffectKind.Stamina?"BCEB00":kind==FoodEffectKind.Poison?"A040E8":kind==FoodEffectKind.Drowsy?"CB65BC":"F07831";
        internal static string Change(FoodEffectKind kind,float amount,bool remove,float seconds=0)
        {
            string value=Color(T(N(amount)+" 点 "+Status(kind),N(amount)+" "+Status(kind),Status(kind)+" "+N(amount)+" ポイント"),StatusColor(kind));
            string verb=remove?T("移除 ","Remove ",""):T("获得 ","Gain ","");
            string line=T(verb+value,verb+value,value+(remove?"を減少":"を獲得"));
            return seconds<=0?line:T(N(seconds)+" 秒内："+line,"Over "+N(seconds)+"s: "+line,N(seconds)+" 秒間で："+line);
        }
        static readonly Dictionary<string,string[]> Names=new Dictionary<string,string[]>
        {
            {"apple",new[]{"Apple","リンゴ"}}, {"baked_potato",new[]{"Baked Potato","ベイクドポテト"}},
            {"beef",new[]{"Raw Beef","生の牛肉"}}, {"cooked_beef",new[]{"Steak","ステーキ"}},
            {"beetroot",new[]{"Beetroot","ビートルート"}}, {"beetroot_soup",new[]{"Beetroot Soup","ビートルートスープ"}},
            {"bread",new[]{"Bread","パン"}}, {"cake",new[]{"Cake","ケーキ"}}, {"carrot",new[]{"Carrot","ニンジン"}},
            {"chicken",new[]{"Raw Chicken","生の鶏肉"}}, {"cooked_chicken",new[]{"Cooked Chicken","焼き鳥"}},
            {"chorus_fruit",new[]{"Chorus Fruit","コーラスフルーツ"}}, {"cod",new[]{"Raw Cod","生鱈"}}, {"cooked_cod",new[]{"Cooked Cod","焼き鱈"}},
            {"cookie",new[]{"Cookie","クッキー"}}, {"enchanted_golden_apple",new[]{"Enchanted Golden Apple","エンチャントされた金のリンゴ"}},
            {"glow_berries",new[]{"Glow Berries","グロウベリー"}}, {"golden_apple",new[]{"Golden Apple","金のリンゴ"}},
            {"golden_carrot",new[]{"Golden Carrot","金のニンジン"}}, {"honey_bottle",new[]{"Honey Bottle","ハチミツ入りの瓶"}},
            {"melon_slice",new[]{"Melon Slice","スイカの薄切り"}}, {"milk_bucket",new[]{"Milk Bucket","ミルク入りバケツ"}},
            {"mushroom_stew",new[]{"Mushroom Stew","キノコシチュー"}}, {"mutton",new[]{"Raw Mutton","生の羊肉"}}, {"cooked_mutton",new[]{"Cooked Mutton","焼き羊肉"}},
            {"poisonous_potato",new[]{"Poisonous Potato","青くなったジャガイモ"}}, {"porkchop",new[]{"Raw Porkchop","生の豚肉"}}, {"cooked_porkchop",new[]{"Cooked Porkchop","焼き豚"}},
            {"potato",new[]{"Potato","ジャガイモ"}}, {"pufferfish",new[]{"Pufferfish","フグ"}}, {"pumpkin_pie",new[]{"Pumpkin Pie","パンプキンパイ"}},
            {"rabbit",new[]{"Raw Rabbit","生の兎肉"}}, {"cooked_rabbit",new[]{"Cooked Rabbit","焼き兎肉"}}, {"rabbit_stew",new[]{"Rabbit Stew","ウサギシチュー"}},
            {"rotten_flesh",new[]{"Rotten Flesh","腐った肉"}}, {"salmon",new[]{"Raw Salmon","生鮭"}}, {"cooked_salmon",new[]{"Cooked Salmon","焼き鮭"}},
            {"spider_eye",new[]{"Spider Eye","クモの目"}}, {"suspicious_stew",new[]{"Suspicious Stew","怪しげなシチュー"}},
            {"sweet_berries",new[]{"Sweet Berries","スイートベリー"}}, {"tropical_fish",new[]{"Tropical Fish","熱帯魚"}}
        };
        internal static string FoodName(string id,string chinese)=>Language=="zh-CN"?chinese:Names.TryGetValue(id,out var n)?n[Language=="ja"?1:0]:id;
    }
}
