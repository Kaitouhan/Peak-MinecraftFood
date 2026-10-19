using System.Text.Json;
using MinecraftFoods;

int checks=0;
void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
void Near(float actual,float expected,string message)=>Check(Math.Abs(actual-expected)<.0001f,message+": "+actual);
var root=args.Length>0?args[0]:".";
using var json=JsonDocument.Parse(File.ReadAllText(Path.Combine(root,"assets","catalogue.json")));
var foods=json.RootElement.GetProperty("foods").EnumerateArray().ToArray();
Check(foods.Length==34,"catalogue count");
var rows=new List<string>{"# 食物特殊效果（由当前代码生成）","","基础回复取 assets/catalogue.json；以下为未烧焦的特殊效果。实际状态仍受 PEAK 上限、免疫和难度机制影响。",""};
foreach(var f in foods)
{
    string id=f.GetProperty("id").GetString()!;
    int scorch=f.GetProperty("scorch").GetInt32(),wreck=f.GetProperty("wreck").GetInt32();
    Check(scorch==2,id+" second cook must char");
    Check(wreck>scorch && wreck<=12,id+" valid destruction threshold");
    Check(f.GetProperty("cookedHunger").GetSingle()>=f.GetProperty("hunger").GetSingle(),id+" first cook improves hunger");
    for(int stage=0;stage<=12;stage++)
    {
        float burn=stage<scorch || stage>=wreck?0:Math.Min(.6f,f.GetProperty("burnPoison").GetSingle()*(stage-scorch+1));
        var p=FoodEffectCatalog.Create(id,stage,scorch,wreck,burn,true);
        Check(stage<wreck || !p.AllEffects.Any(),id+" wreck must be inert");
        foreach(var g in p.Groups.Concat(p.Choices))
        {
            Check(g.Chance>0 && g.Chance<=1,id+" probability");
            foreach(var e in g.Effects)
            {
                Check(float.IsFinite(e.Amount)&&e.Amount>=0&&float.IsFinite(e.Seconds)&&e.Seconds>=0,id+" finite nonnegative effect");
                Check(!string.IsNullOrWhiteSpace(e.Describe()),id+" description");
                if(e.Seconds>0 && e.Kind<=FoodEffectKind.Heal)Check(!string.IsNullOrEmpty(e.Key),id+" timer identity");
            }
        }
        if(id=="golden_apple" || id=="enchanted_golden_apple")
        {
            Check(stage<scorch || !p.AllEffects.Any(e=>e.Kind==FoodEffectKind.Invincible),id+" burned special disabled");
            if(stage<scorch)Near(p.AllEffects.Single(e=>e.Kind==FoodEffectKind.AppleAftereffect).Seconds,id=="golden_apple"?6:12,"apple aftereffect duration");
            Check(!FoodEffectCatalog.Create(id,stage,scorch,wreck,burn,false).AllEffects.Any(e=>e.Kind==FoodEffectKind.AppleAftereffect),"config disables aftereffect");
        }
        if(stage==0)
        {
            rows.Add("## "+f.GetProperty("name").GetString());rows.Add("");
            rows.Add($"饥饿回复 {f.GetProperty("hunger")}，额外体力 {f.GetProperty("bonusStamina")}；首次加热 {f.GetProperty("cookedHunger")} / {f.GetProperty("cookedBonusStamina")}；{scorch} 次烧焦，{wreck} 次烧毁。");rows.Add("");
            foreach(var g in p.Groups)rows.Add($"- 概率 {g.Chance*100:0.#}%："+string.Join("；",g.Effects.Select(e=>System.Text.RegularExpressions.Regex.Replace(e.Describe(),"<[^>]+>",""))));
            foreach(var g in p.Choices)rows.Add($"- 随机分支 {100f/p.Choices.Count:0.#}%："+string.Join("；",g.Effects.Select(e=>System.Text.RegularExpressions.Regex.Replace(e.Describe(),"<[^>]+>",""))));
            rows.Add("");
        }
    }
}
var puffer=FoodEffectCatalog.Create("pufferfish",0,5,10,0,true);
Near(puffer.AllEffects.Where(e=>e.Kind==FoodEffectKind.Poison).Sum(e=>e.Amount),65,"raw puffer poison total");
Near(puffer.AllEffects.Where(e=>e.Kind==FoodEffectKind.Drowsy).Sum(e=>e.Amount),20,"puffer drowsy total");
var potato=FoodEffectCatalog.Create("poisonous_potato",0,5,10,0,true);
Check(potato.Groups.Count==1 && potato.Groups[0].Effects.Length==2,"poisonous potato uses one shared random roll");
Near(potato.Groups[0].Chance,.6f,"potato chance");
for(int stage=0;stage<4;stage++)
{
    float Poison(int s)=>FoodEffectCatalog.Create("pufferfish",s,5,10,0,true).AllEffects.Where(e=>e.Kind==FoodEffectKind.Poison).Sum(e=>e.Amount);
    Check(Poison(stage+1)>Poison(stage),"puffer poison rises with cooking");
}
Check(FoodEffectCatalog.Create("suspicious_stew",0,5,10,0,true).Choices.Count==8,"stew eight exclusive choices");
Near(FoodEffectCatalog.Create("chorus_fruit",0,5,10,0,true).AllEffects.Single().Amount,30,"chorus radius");

// Test the production clocks, including the mixed-duration regeneration regression.
var clock=new FoodEffectClock();clock.Refresh(15,8);clock.Refresh(20,5);
Near(clock.Remaining*clock.Rate,20,"refresh must not invent 32 healing points");
Near(clock.Take(2),8,"partial healing");clock.Refresh(15,8);
Near(clock.Remaining*clock.Rate,15,"weaker refresh preserves strongest rate without adding budgets");
Near(clock.Take(100),15,"long frame cannot overshoot");Near(clock.Take(1),0,"expired timer inert");
var apple=new AppleAftereffectClock();apple.Queue(0,6);
Check(!apple.Tick(4,true),"wait for invincibility");Check(apple.Tick(5,false),"start drowsy after invincibility");
Near(apple.HoldUntil,11,"normal apple six seconds");Check(!apple.Tick(11,false),"normal apple expires");
apple.Clear();apple.Queue(0,12);apple.Queue(1,6);
Check(!apple.Tick(9,true),"external invincibility defers cost");Check(apple.Tick(12,false),"deferred cost starts");Near(apple.HoldUntil,24,"weaker apple cannot shorten pending enchanted cost");
apple.Queue(14,6);Check(!apple.Tick(15,true),"re-eating pauses hold until new invincibility ends");
apple.Tick(20,false);Near(apple.HoldUntil,30,"remaining cost preserved when re-eating");
apple.Clear();Check(!apple.Pending && !apple.Tick(40,false),"milk/death clear pending and active aftereffects");
File.WriteAllLines(Path.Combine(root,"docs","FOOD-EFFECTS.generated.zh-CN.md"),rows);
Check(L10n.Resolve("Auto","Japanese")=="ja","Japanese client selection");
Check(L10n.Resolve("Auto","SimplifiedChinese")=="zh-CN","Chinese client selection");
Check(L10n.Resolve("Auto","German")=="en","unsupported language fallback");
Check(L10n.Resolve("ja","English")=="ja","explicit language override");
var previews=new List<string>();
foreach(string language in new[]{"zh-CN","en","ja"})
{
    L10n.Language=language;
    foreach(var f in foods)
    {
        string id=f.GetProperty("id").GetString()!,name=f.GetProperty("name").GetString()!;
        string translated=L10n.FoodName(id,name);
        Check(!string.IsNullOrEmpty(translated) && translated!=id,"localized food name "+id);
        if(language!="zh-CN" && f.TryGetProperty("cookedTexture",out var cooked))Check(L10n.FoodName(cooked.GetString()!,"cooked")!="cooked","cooked translation");
        int scorch=f.GetProperty("scorch").GetInt32(),wreck=f.GetProperty("wreck").GetInt32();
        for(int stage=0;stage<=12;stage++)
        {
            FoodEffectPlan Plan(int n)=>FoodEffectCatalog.Create(id,n,scorch,wreck,n<scorch||n>=wreck?0:Math.Min(.6f,f.GetProperty("burnPoison").GetSingle()*(n-scorch+1)),true);
            float Factor(int n)=>n>=wreck?0:n<scorch?1:Math.Max(.1f,1-.5f*(n-scorch+1));
            float Nutrition(string key,int n)=>f.GetProperty(n==0?key:key=="hunger"?"cookedHunger":"cookedBonusStamina").GetSingle()*Factor(n);
            string text=FoodTooltip.Render(Nutrition("hunger",stage),Nutrition("bonusStamina",stage),Nutrition("hunger",stage+1),Nutrition("bonusStamina",stage+1),2.5f,stage,wreck,1,1,Plan(stage),Plan(stage+1));
            Check(!string.IsNullOrWhiteSpace(text),"tooltip not empty");
            Check(System.Text.RegularExpressions.Regex.Matches(text,"<color=").Count==System.Text.RegularExpressions.Regex.Matches(text,"</color>").Count,"balanced color tags");
            if(language=="en")Check(!System.Text.RegularExpressions.Regex.IsMatch(text+translated,"[\\u4e00-\\u9fff]"),"no Chinese text in English tooltips: "+id);
            if((id=="apple" && stage<4) || (id=="suspicious_stew" && stage==0))previews.Add($"<section><h2>{language} · {System.Net.WebUtility.HtmlEncode(translated)} · {stage}</h2><pre>"+System.Text.RegularExpressions.Regex.Replace(text,"<color=#([A-Fa-f0-9]+)>","<span style='color:#$1'>").Replace("</color>","</span>")+"</pre></section>");
        }
    }
}
L10n.Language="en";
var empty=new FoodEffectPlan();
Check(FoodTooltip.Render(20,10,20,10,5,1,8,1,1,empty,empty).Contains("Next cook: no effect change"),"unchanged next cook");
var deltas=FoodTooltip.Delta(20,15,10,0,empty,empty);
Check(deltas.Count==2 && deltas[0].Contains("Gain") && deltas[0].Contains("5") && deltas[1].Contains("Remove") && deltas[1].Contains("10"),"nutrition loss shown as signed delta");
var tiers=new Dictionary<string,double>{{"common",60},{"rare",40}};
var choices=new[]{new LootChoice("a","common",1),new LootChoice("b","common",3),new LootChoice("c","rare",1)};
var loot=LootWeights.Normalize(choices,tiers);
Near((float)loot["a"],.15f,"tier and food weight combined");Near((float)loot["b"],.45f,"per-food multiplier");Near((float)loot["c"],.4f,"rare tier weight");
choices[2].Weight=0;loot=LootWeights.Normalize(choices,tiers);
Check(!loot.ContainsKey("c"),"disabled item never selected");Near((float)loot.Values.Sum(),1,"empty tier redistributed");
tiers["common"]=0;Check(LootWeights.Normalize(choices,tiers).Count==0,"all disabled produces empty mod selection for vanilla fallback");
choices[2].Weight=double.NaN;Check(LootWeights.Normalize(choices,tiers).Count==0,"nonfinite weights rejected");
File.WriteAllText(Path.Combine(root,"docs","tooltip-preview.html"),"<!doctype html><meta charset='utf-8'><title>Minecraft Foods tooltips</title><style>body{background:#292c26;color:#eee;font:18px Arial,'Yu Gothic UI','Microsoft YaHei',sans-serif;padding:30px}section{display:inline-block;vertical-align:top;width:430px;padding:20px;margin:10px;background:#34382f;border-radius:10px}pre{white-space:pre-wrap;font:inherit;line-height:1.5}h2{font-size:16px;color:#bbb}</style><h1>Tooltip layout preview / 物品说明预览 / 説明プレビュー</h1><p>Generated from production tooltip code. This is not an in-game screenshot.</p>"+string.Join("",previews));
L10n.Language="zh-CN";
Console.WriteLine($"PASS {checks} assertions: 34 foods x 13 stages x 3 languages, tooltip deltas, loot normalization, effect definitions and clocks. Unity gameplay/UI/input/network/audio not exercised.");

var ramp=new AppleAftereffectClock();ramp.Queue(0,6);
Near(ramp.Progress(4,true),-1,"invincibility postpones ramp");
Near(ramp.Progress(5,false),0,"ramp starts without a jump");
Near(ramp.Progress(8,false),.5f,"normal apple halfway after three seconds");
Near(ramp.Progress(11.2f,false),1,"crossing deadline delivers full ramp");
Near(ramp.Progress(12,false),-1,"completed ramp does not hold maximum");
ramp.Clear();ramp.Queue(0,12);ramp.Progress(0,false);
Near(ramp.Progress(6,false),.5f,"enchanted apple halfway after six seconds");
ramp.Clear();Near(ramp.Progress(7,false),-1,"milk clears ramp");
Console.WriteLine($"Including cooking and ramp checks: {checks} checks passed.");
var exposure=new ChorusExposure();
exposure.Record(0);exposure.Record(10);exposure.Record(20);
Check(exposure.Count==3 && !exposure.TakeTransformation(24),"three teleports do not zombify");
exposure.Record(30);Near(exposure.TransformAt,35,"fourth warp gives five-second warning");
Check(!exposure.TakeTransformation(34.9f),"warning is not lethal early");
Check(exposure.TakeTransformation(35),"native transformation due");
Check(!exposure.TakeTransformation(36),"transformation requested once");
exposure.Clear();exposure.Record(0);exposure.Record(121);
Check(exposure.Count==1,"separated uses reset exposure");
exposure.Record(122);exposure.Record(123);exposure.Record(124);exposure.Clear();
Check(!exposure.TakeTransformation(130) && exposure.Count==0,"milk prevents pending zombification");
Console.WriteLine($"Final balance checks: {checks} assertions passed.");

var sleep=new AppleSleepClock();sleep.Begin(6);
Check(!sleep.Observe(10,false) && sleep.Until==0,"ramp/transition do not count as sleep");
Check(!sleep.Observe(15,true),"actual sleep starts timer");Near(sleep.Until,21,"six full seconds asleep");
Check(!sleep.Observe(20.9f,true),"sleep does not end early");
Check(sleep.Observe(21,true) && !sleep.Active,"release after six seconds");
sleep.Begin(12);sleep.Observe(30,true);Near(sleep.Until,42,"enchanted twelve seconds asleep");
Check(!sleep.Observe(41.9f,true) && sleep.Observe(42,true),"enchanted duration");
sleep.Begin(6);sleep.Observe(50,true);sleep.Observe(52,false);sleep.Observe(53,true);
Near(sleep.Until,59,"wake interruption restarts continuous sleep duration");
sleep.Clear();Check(!sleep.Active && !sleep.Observe(60,true),"milk clears sleep hold");
Console.WriteLine($"Sleep-inclusive total: {checks} assertions passed.");
