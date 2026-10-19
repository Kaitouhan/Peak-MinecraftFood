using System;
using System.Collections.Generic;
using System.Linq;

namespace MinecraftFoods
{
    internal enum FoodEffectKind { Poison, Hunger, Drowsy, Injury, Heal, Stamina, Invincible, Shield, Resist, Fireproof, AppleAftereffect, CurePoison, Milk, Teleport, LowGravity, Blind }

    // Amounts are PEAK points (0..100), seconds are real seconds. Both gameplay and UI
    // consume these definitions; probabilities apply to the entire group, not each step.
    internal sealed class FoodEffect
    {
        internal FoodEffectKind Kind;
        internal float Amount, Seconds;
        internal string Key;
        internal FoodEffect(FoodEffectKind kind, float amount=0, float seconds=0, string key=null)
        { Kind=kind; Amount=amount; Seconds=seconds; Key=key; }
        internal bool Harmful => Kind==FoodEffectKind.Poison || Kind==FoodEffectKind.Hunger || Kind==FoodEffectKind.Drowsy || Kind==FoodEffectKind.Injury || Kind==FoodEffectKind.Blind || Kind==FoodEffectKind.AppleAftereffect;
        internal string Describe()
        {
            string n=L10n.N(Amount), t=L10n.N(Seconds);
            string positive(string text)=>L10n.Color(text,"BCEB00");
            switch(Kind)
            {
                case FoodEffectKind.Poison:case FoodEffectKind.Hunger:case FoodEffectKind.Drowsy:case FoodEffectKind.Injury:
                    return (Key=="burn"?L10n.T("烧焦附加：","Burn penalty: ","焦げの追加効果："):"")+L10n.Change(Kind,Amount,false,Seconds);
                case FoodEffectKind.Heal:return L10n.Change(FoodEffectKind.Injury,Amount,true,Seconds);
                case FoodEffectKind.Stamina:return L10n.Change(Kind,Amount,false);
                case FoodEffectKind.Invincible:return positive(L10n.T($"无敌 持续 {t} 秒",$"Invincibility for {t}s",$"無敵 {t} 秒"));
                case FoodEffectKind.Shield:return positive(L10n.T($"获得 {n} 点吸收盾 持续 {t} 秒",$"Gain {n} injury shield for {t}s",$"吸収シールド {n} ポイント、{t} 秒"));
                case FoodEffectKind.Resist:return positive(L10n.T($"受伤减少 {n}% 持续 {t} 秒",$"{n}% less injury for {t}s",$"負傷を {n}% 軽減、{t} 秒"));
                case FoodEffectKind.Fireproof:return positive(L10n.T($"免疫炎热增长 持续 {t} 秒",$"Block heat gain for {t}s",$"暑さの増加を無効化、{t} 秒"));
                case FoodEffectKind.AppleAftereffect:return L10n.Color(L10n.T($"无敌结束后：睡意在 {t} 秒内逐渐升满；实际入睡后维持 {t} 秒",$"After invincibility: drowsiness rises over {t}s, then sleep lasts {t}s from actual unconsciousness",$"無敵終了後：{t} 秒で眠気が最大になり、入眠してから {t} 秒維持"),"CB65BC");
                case FoodEffectKind.CurePoison:return L10n.T("清除全部中毒、孢子及持续中毒","Clear poison, spores and ongoing poison","毒・胞子・継続毒を解除");
                case FoodEffectKind.Milk:return L10n.T("清除临时状态、中毒、孢子与睡意；同时移除正面效果、金苹果后遗症和紫颂果感染","Clear temporary effects, poison, spores and drowsiness; also remove buffs, apple aftereffects and chorus exposure","一時効果・毒・胞子・眠気を解除。有利な効果・金のリンゴの反動・コーラス感染も解除");
                case FoodEffectKind.Teleport:return L10n.T($"在水平 {n} 米内寻找传送落点；成功后随机增加 {FoodEffectCatalog.ChorusHunger} 点饥饿或 {FoodEffectCatalog.ChorusSpores} 点孢子（各50%）；失败无额外代价；成功传送间隔不超过120秒时累积，第4次在5秒后死亡并生成敌对僵尸；提前喝牛奶解除",$"Find a landing within {n}m horizontally; on success gain {FoodEffectCatalog.ChorusHunger} hunger or {FoodEffectCatalog.ChorusSpores} spores (50% each); no extra cost if no landing is found. Successful warps at most 120s apart accumulate: the fourth causes death and a hostile zombie after 5s; drink milk before then to cancel",$"水平 {n}m 以内にテレポート。成功時は空腹 {FoodEffectCatalog.ChorusHunger} または胞子 {FoodEffectCatalog.ChorusSpores} 増加（各50%）。失敗時は追加負担なし。成功間隔120秒以内で累積、4回目の5秒後に死亡し敵ゾンビが出現。事前にミルクで解除");
                case FoodEffectKind.LowGravity:return positive(L10n.T($"低重力 持续 {t} 秒",$"Low gravity for {t}s",$"低重力 {t} 秒"));
                case FoodEffectKind.Blind:return L10n.Color(L10n.T($"失明 持续 {t} 秒",$"Blindness for {t}s",$"盲目 {t} 秒"),"CB65BC");
                default:throw new ArgumentOutOfRangeException();
            }
        }
    }

    internal sealed class FoodEffectGroup
    {
        internal float Chance;
        internal FoodEffect[] Effects;
        internal FoodEffectGroup(float chance,params FoodEffect[] effects){Chance=chance;Effects=effects;}
    }

    internal sealed class FoodEffectPlan
    {
        internal readonly List<FoodEffectGroup> Groups=new List<FoodEffectGroup>();
        internal readonly List<FoodEffectGroup> Choices=new List<FoodEffectGroup>();
        internal bool SpecialLost;
        internal void Add(params FoodEffect[] effects)=>Groups.Add(new FoodEffectGroup(1,effects));
        internal void Chance(float chance,params FoodEffect[] effects)=>Groups.Add(new FoodEffectGroup(chance,effects));
        internal IEnumerable<FoodEffect> AllEffects=>Groups.Concat(Choices).SelectMany(g=>g.Effects);
    }

    internal static class FoodEffectCatalog
    {
        internal const float TeleportRadius=30;
        internal const float ChorusHunger=25, ChorusSpores=15;
        static FoodEffect E(FoodEffectKind k,float amount=0,float seconds=0,string key=null)=>new FoodEffect(k,amount,seconds,key);
        internal static FoodEffectPlan Create(string id,int stage,int scorch,int wreck,float burnPoison,bool appleAftereffects)
        {
            var p=new FoodEffectPlan();
            if(stage>=wreck)return p;
            switch(id)
            {
                case "pufferfish":
                    p.Add(E(FoodEffectKind.Poison,30+2.5f*stage),E(FoodEffectKind.Poison,35+5*stage,60,"puffer-poison"),E(FoodEffectKind.Hunger,15,15,"puffer-hunger"),E(FoodEffectKind.Drowsy,10),E(FoodEffectKind.Drowsy,10,15,"puffer-nausea"));break;
                case "rotten_flesh":
                    p.Chance(.8f,E(FoodEffectKind.Hunger,20,30,"rotten-hunger"));
                    if(stage>0)p.Add(E(FoodEffectKind.Poison,Math.Min(50,5*stage)));break;
                case "spider_eye":p.Add(E(FoodEffectKind.Poison,5+2.5f*stage),E(FoodEffectKind.Poison,5+2.5f*stage,4,"spider-poison"));break;
                case "poisonous_potato":p.Chance(.6f,E(FoodEffectKind.Poison,5+2.5f*stage),E(FoodEffectKind.Poison,5+2.5f*stage,5,"potato-poison"));break;
                case "chicken":if(stage==0)p.Chance(.3f,E(FoodEffectKind.Poison,12));break;
                case "beef":case "porkchop":case "mutton":case "rabbit":if(stage==0)p.Chance(.15f,E(FoodEffectKind.Poison,8));break;
                case "cod":case "salmon":case "tropical_fish":if(stage==0)p.Chance(.2f,E(FoodEffectKind.Poison,10));break;
                case "honey_bottle":case "milk_bucket":case "golden_apple":case "enchanted_golden_apple":case "chorus_fruit":case "suspicious_stew":
                    if(stage>=scorch){p.SpecialLost=true;break;}
                    if(id=="honey_bottle")p.Add(E(FoodEffectKind.CurePoison));
                    else if(id=="milk_bucket")p.Add(E(FoodEffectKind.Milk));
                    else if(id=="chorus_fruit")p.Add(E(FoodEffectKind.Teleport,TeleportRadius));
                    else if(id=="suspicious_stew")
                    {
                        p.Choices.Add(new FoodEffectGroup(1,E(FoodEffectKind.Heal,15,8,"regeneration")));
                        p.Choices.Add(new FoodEffectGroup(1,E(FoodEffectKind.Stamina,20)));
                        p.Choices.Add(new FoodEffectGroup(1,E(FoodEffectKind.LowGravity,1,6)));
                        p.Choices.Add(new FoodEffectGroup(1,E(FoodEffectKind.Fireproof,0,4)));
                        p.Choices.Add(new FoodEffectGroup(1,E(FoodEffectKind.Blind,0,8)));
                        p.Choices.Add(new FoodEffectGroup(1,E(FoodEffectKind.Poison,5),E(FoodEffectKind.Poison,15,12,"stew-poison")));
                        p.Choices.Add(new FoodEffectGroup(1,E(FoodEffectKind.Drowsy,10)));
                        p.Choices.Add(new FoodEffectGroup(1,E(FoodEffectKind.Injury,15,8,"stew-wither")));
                    }
                    else
                    {
                        bool enchanted=id=="enchanted_golden_apple";
                        p.Add(E(FoodEffectKind.Invincible,0,enchanted?12:5),E(FoodEffectKind.Shield,enchanted?80:20,60),E(FoodEffectKind.Heal,enchanted?80:20,enchanted?20:5,"regeneration"));
                        if(enchanted)p.Add(E(FoodEffectKind.Resist,20,60),E(FoodEffectKind.Fireproof,0,60));
                        if(appleAftereffects)p.Add(E(FoodEffectKind.AppleAftereffect,0,enchanted?12:6));
                    }
                    break;
            }
            if(burnPoison>0)p.Add(E(FoodEffectKind.Poison,burnPoison*100,0,"burn"));
            return p;
        }
    }
}
