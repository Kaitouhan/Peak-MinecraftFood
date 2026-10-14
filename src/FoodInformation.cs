using System.Text;
using UnityEngine;

namespace MinecraftFoods
{
    internal static class FoodInformation
    {
        static string Good(string s)=>"<color=#9CDF62>"+s+"</color>";
        static string Bad(string s)=>"<color=#BF68E6>"+s+"</color>";
        static string Special(FoodDefinition f,int s)
        {
            if(s>=f.wreck)return "";
            switch(f.id)
            {
                case "pufferfish":return Bad($"获得 {30+2.5f*s:0.#} 点中毒；60 秒内再累积 {35+5*s} 点中毒\n15 秒内增加 15 点饥饿、20 点嗜睡（其中 10 点立即生效）");
                case "rotten_flesh":return Bad("80% 概率在 30 秒内增加 20 点饥饿"+(s>0?$"；增加 {Mathf.Min(50,5*s)} 点中毒":""));
                case "spider_eye":return Bad($"立即增加 {5+2.5f*s:0.#} 点中毒，4 秒内再增加 {5+2.5f*s:0.#} 点");
                case "poisonous_potato":return Bad($"60% 概率立即增加 {5+2.5f*s:0.#} 点中毒，5 秒内再增加 {5+2.5f*s:0.#} 点");
                case "chicken":return s==0?Bad("30% 概率增加 12 点中毒；烤熟消除生食风险"):"";
                case "beef":case "mutton":case "porkchop":case "rabbit":return s==0?Bad("15% 概率增加 8 点中毒；烤熟消除生食风险"):"";
                case "cod":case "salmon":return s==0?Bad("20% 概率增加 10 点中毒；烤熟消除生食风险"):"";
                case "tropical_fish":return s==0?Bad("20% 概率增加 10 点中毒；加热消除生食风险"):"";
            }
            if(s>=f.scorch)return f.id=="golden_apple" || f.id=="enchanted_golden_apple" || f.id=="milk_bucket" || f.id=="honey_bottle" || f.id=="chorus_fruit" || f.id=="suspicious_stew" ? Bad("烧焦：特殊效果已失效") : "";
            switch(f.id)
            {
                case "golden_apple":return Good("获得 5 秒无敌；5 秒内移除 20 点受伤\n获得 20 点受伤吸收盾，持续 60 秒")+(Plugin.AppleAftereffects.Value?"\n"+Bad("无敌结束后：彻底眩晕、倒地失控 3 秒"):"");
                case "enchanted_golden_apple":return Good("获得 12 秒无敌；20 秒内移除 80 点受伤\n获得 80 点受伤吸收盾，持续 60 秒\n60 秒内受伤减少 20%，免疫炎热增长")+(Plugin.AppleAftereffects.Value?"\n"+Bad("无敌结束后：彻底眩晕、倒地失控 6 秒"):"");
                case "honey_bottle":return Good("移除全部中毒及本模组持续中毒；同时清除孢子");
                case "milk_bucket":return Good("清除临时效果、全部中毒与嗜睡")+"\n"+Bad("同时移除无敌、吸收盾等正面效果");
                case "chorus_fruit":return Good("随机传送至水平 8 米内安全落脚点；无落脚点则留在原地");
                case "suspicious_stew":return "随机获得以下之一（每项 12.5%）：\n"+Good("8 秒恢复 15 点受伤／20 点额外体力／6 秒低重力／4 秒免疫炎热增长")+"\n"+Bad("8 秒失明／5 点中毒并在 12 秒内再增加 15 点／10 点嗜睡／8 秒内增加 15 点受伤");
                default:return "";
            }
        }
        internal static string Describe(FoodMarker m)
        {
            var f=m.Definition;if(f==null)return "";
            int s=m.Stage;var b=new StringBuilder();
            if(s>=f.wreck)return Bad("已烧毁，无法食用")+"\n"+f.description;
            b.AppendLine(Good($"移除 {f.HungerAt(s):0.#} 点饥饿值"));
            if(f.BonusAt(s)>0)b.AppendLine(Good($"获得 {f.BonusAt(s):0.#} 点额外体力"));
            string special=Special(f,s);if(special.Length>0)b.AppendLine(special);
            if(f.BurnPoisonAt(s)>0)b.AppendLine(Bad($"烧焦额外增加 {f.BurnPoisonAt(s)*100:0.#} 点中毒"));
            var item=m.GetComponent<Item>();
            b.AppendLine($"<color=#B87838>{item.CarryWeight*2.5f:0.#} 重量</color>");
            b.AppendLine($"已烹饪 {s} 次；{f.scorch} 次起烧焦，{f.wreck} 次烧毁");
            if(s+1>=f.wreck)b.AppendLine(Bad("下次烹饪：烧毁，无法食用"));
            else
            {
                b.AppendLine($"下次烹饪：{f.NameAt(s+1)}");
                b.AppendLine(Good($"· 移除 {f.HungerAt(s+1):0.#} 点饥饿；获得 {f.BonusAt(s+1):0.#} 点额外体力"));
                if(f.BurnPoisonAt(s+1)>0)b.AppendLine(Bad($"· 烧焦附加 {f.BurnPoisonAt(s+1)*100:0.#} 点中毒"));
                if(f.id=="pufferfish" || f.id=="rotten_flesh" || f.id=="spider_eye" || f.id=="poisonous_potato")b.AppendLine(Bad("· 有毒食物加热后毒性继续增加"));
            }
            if(f.uses>1)b.AppendLine($"可食用 {f.uses} 次，以上为每次效果");
            return b.ToString().TrimEnd();
        }
    }
}
