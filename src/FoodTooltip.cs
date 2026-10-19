using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace MinecraftFoods
{
    internal static class FoodTooltip
    {
        internal static string Group(FoodEffectGroup group)=>
            (group.Chance<1?L10n.T($"{L10n.N(group.Chance*100)}% 概率：",$"{L10n.N(group.Chance*100)}% chance: ",$"{L10n.N(group.Chance*100)}% の確率："):"")+
            string.Join(L10n.T("；","; ","、"),group.Effects.Select(e=>e.Describe()));
        internal static string Special(FoodEffectPlan p)
        {
            var lines=new List<string>();
            foreach(var group in p.Groups)
            {
                if(group.Chance<1)lines.Add(Group(group));
                else lines.AddRange(group.Effects.Select(e=>e.Describe()));
            }
            if(p.SpecialLost)lines.Add(L10n.Color(L10n.T("烧焦：特殊效果已失效","Charred: special effects disabled","焦げ：特殊効果は無効"),"A040E8"));
            if(p.Choices.Count>0)
            {
                lines.Add(L10n.T("随机效果（其一）：","Random effect (one):","ランダム効果（いずれか）："));
                foreach(var g in p.Choices)lines.Add("· "+Group(g)+$" ({L10n.N(100f/p.Choices.Count)}%)");
            }
            return string.Join("\n",lines);
        }
        internal static List<string> Delta(float hunger,float nextHunger,float stamina,float nextStamina,FoodEffectPlan current,FoodEffectPlan next)
        {
            var result=new List<string>();
            float dh=nextHunger-hunger,ds=nextStamina-stamina;
            if(Math.Abs(dh)>.001f)result.Add(L10n.Change(FoodEffectKind.Hunger,Math.Abs(dh),dh>0));
            if(Math.Abs(ds)>.001f)result.Add(L10n.Change(FoodEffectKind.Stamina,Math.Abs(ds),ds<0));
            foreach(var group in next.Groups)
            foreach(var effect in group.Effects)
            {
                var old=current.Groups.Where(g=>g.Chance==group.Chance).SelectMany(g=>g.Effects).FirstOrDefault(e=>e.Kind==effect.Kind && e.Key==effect.Key);
                if(old!=null && old.Amount==effect.Amount && old.Seconds==effect.Seconds)continue;
                if(old!=null && old.Seconds==effect.Seconds && effect.Kind<=FoodEffectKind.Stamina)
                {
                    float d=effect.Amount-old.Amount;
                    // This is a change in the food's effect, not an immediate cure on the player.
                    string chance=group.Chance<1?L10n.N(group.Chance*100)+"% ":"";
                    string stat=L10n.Status(effect.Kind==FoodEffectKind.Heal?FoodEffectKind.Injury:effect.Kind);
                    string change=(d>0?"+":"−")+L10n.N(Math.Abs(d));
                    result.Add(chance+L10n.T("食用效果变化：","Change on consumption: ","食べた時の変化：")+
                        L10n.Color(stat+" "+change,L10n.StatusColor(effect.Kind))+
                        (effect.Seconds>0?L10n.T($"（{L10n.N(effect.Seconds)} 秒累计）",$" (over {L10n.N(effect.Seconds)}s)",$"（{L10n.N(effect.Seconds)} 秒間の合計）"):""));
                }
                else result.Add(L10n.T("食用时：","On consumption: ","食べた時：")+Group(new FoodEffectGroup(group.Chance,effect)));
            }
            foreach(var group in current.Groups)
            foreach(var effect in group.Effects)
                if(!next.Groups.Any(g=>g.Chance==group.Chance && g.Effects.Any(e=>e.Kind==effect.Kind && e.Key==effect.Key)))
                    result.Add(L10n.T("不再提供：","No longer applies: ","適用されなくなる効果：")+Group(new FoodEffectGroup(group.Chance,effect)));
            if(current.Choices.Count>0 && next.Choices.Count==0)result.Add(L10n.T("随机特殊效果失效","Random special effect disabled","ランダム特殊効果が無効"));
            return result;
        }
        internal static string Render(float hunger,float bonus,float nextHunger,float nextBonus,float weight,int stage,int wreck,int uses,int remaining,FoodEffectPlan current,FoodEffectPlan next)
        {
            if(stage>=wreck)return L10n.Color(L10n.T("已烧毁，无法食用","Burnt: cannot be consumed","燃え尽き：食べられない"),"A040E8");
            var b=new StringBuilder();
            if(hunger>0)b.AppendLine(L10n.Change(FoodEffectKind.Hunger,hunger,true));
            if(bonus>0)b.AppendLine(L10n.Change(FoodEffectKind.Stamina,bonus,false));
            var effects=Special(current);if(effects.Length>0)b.AppendLine(effects);
            if(uses>1)b.AppendLine(L10n.T($"剩余 {remaining}/{uses} 次（每次效果）",$"{remaining}/{uses} uses left (effects per use)",$"残り {remaining}/{uses} 回（1回あたりの効果）"));
            b.AppendLine();b.AppendLine(L10n.Color(L10n.T($"{L10n.N(weight)} 重量",$"{L10n.N(weight)} weight",$"重量 {L10n.N(weight)}"),"B87838"));b.AppendLine();
            string cooked=stage==0?L10n.T("可以烹饪","Cookable","調理可能"):L10n.T($"已烹饪 {stage} 次",$"Cooked {stage} time(s)",$"調理済み {stage} 回");
            if(stage+1>=wreck)b.Append(cooked+"\n"+L10n.Color(L10n.T("下次烹饪：烧毁，无法食用","Next cook: burnt and inedible","次の調理：燃え尽きて食べられない"),"A040E8"));
            else
            {
                var delta=Delta(hunger,nextHunger,bonus,nextBonus,current,next);
                if(delta.Count==0)b.Append(cooked+"\n"+L10n.Color(L10n.T("下次烹饪：没有效果","Next cook: no effect change","次の調理：効果の変化なし"),"6550B8"));
                else{b.AppendLine(cooked+" "+L10n.T("下次烹饪增量：","Next cook changes:","次の調理による変化："));foreach(var line in delta)b.AppendLine("· "+line);}
            }
            return b.ToString().TrimEnd();
        }
    }
}
