using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HarmonyLib;
using Peak.Afflictions;
using Photon.Pun;
using UnityEngine;
using S = CharacterAfflictions.STATUSTYPE;

namespace MinecraftFoods
{
    public sealed class FoodEatAction : ItemAction
    {
        private float lastUse = -100;
        public override void RunAction()
        {
            var c=character;
            var m=item.GetComponent<FoodMarker>();
            if(!c || !c.IsLocal || c.data.dead || !m || m.Definition==null || item.consuming || Time.time-lastUse<.3f || m.Stage>=m.Definition.wreck)return;
            if(item.totalUses>0){var uses=item.GetData<OptionableIntItemData>(DataEntryKey.ItemUses);if(uses.HasData && uses.Value<=0)return;}
            lastUse=Time.time;
            var state=c.GetComponent<FoodPlayerState>()??c.gameObject.AddComponent<FoodPlayerState>();
            state.Eat(m.Definition,m.Stage);
            if(item.totalUses>1) item.GetComponent<Action_ReduceUses>().RunAction();
            else item.StartCoroutine(item.ConsumeDelayed(true));
        }
    }

    public sealed class FoodPlayerState : MonoBehaviour
    {
        internal float Shield;
        private float shieldUntil, resistUntil, resistFraction, fireUntil, nextTeleport, appleInvincibleUntil;
        private readonly AppleAftereffectClock aftereffect=new AppleAftereffectClock();
        private readonly AppleSleepClock appleSleep=new AppleSleepClock();
        private readonly ChorusExposure chorus=new ChorusExposure();
        private Character character;
        private readonly Dictionary<string,TimedStatus> timers=new Dictionary<string,TimedStatus>();
        private float tick;
        private float appleRampStart=-1, appleRampBaseline;
        private sealed class TimedStatus { internal S status; internal readonly FoodEffectClock clock=new FoodEffectClock(); internal bool healing; }
        private static readonly System.Reflection.FieldInfo Airport=AccessTools.Field(typeof(CharacterAfflictions),"m_inAirport");
        internal static bool InAirport(CharacterAfflictions a) => Airport!=null && (bool)Airport.GetValue(a);
        void Awake(){character=GetComponent<Character>();}
        internal void Eat(FoodDefinition f,int stage)
        {
            var a=character.refs.afflictions;
            a.SubtractStatus(S.Hunger,f.HungerAt(stage)/100f);
            character.AddExtraStamina(f.BonusAt(stage)/100f);
            var plan=f.EffectsAt(stage);
            foreach(var group in plan.Groups)
                if(group.Chance>=1 || UnityEngine.Random.value<group.Chance)
                    foreach(var effect in group.Effects)ApplyEffect(effect);
            if(plan.Choices.Count>0)
                foreach(var effect in plan.Choices[UnityEngine.Random.Range(0,plan.Choices.Count)].Effects)ApplyEffect(effect);
            Plugin.Log.LogInfo($"EAT {f.id} stage={stage} nutrition={f.HungerAt(stage):F2}/{f.BonusAt(stage):F2} receiver={character.photonView.ViewID} airport={InAirport(a)}");
        }
        private void Timed(string key,S status,float seconds,float total,bool heal=false)
        {
            if(!timers.TryGetValue(key,out var timer))timers[key]=timer=new TimedStatus{status=status,healing=heal};
            timer.clock.Refresh(total,seconds);
        }
        internal void Apply(S status,float amount)
        {
            var a=character.refs.afflictions;
            bool before=InAirport(a);
            try
            {
                if(before && Plugin.AirportEffects.Value)Airport.SetValue(a,false);
                a.AddStatus(status,Mathf.Max(0,amount));
            }
            finally { if(before)Airport.SetValue(a,true); }
        }
        private void ApplyEffect(FoodEffect e)
        {
            var a=character.refs.afflictions;
            switch(e.Kind)
            {
                case FoodEffectKind.Poison:StatusEffect(e,S.Poison);break;
                case FoodEffectKind.Hunger:StatusEffect(e,S.Hunger);break;
                case FoodEffectKind.Drowsy:StatusEffect(e,S.Drowsy);break;
                case FoodEffectKind.Injury:StatusEffect(e,S.Injury);break;
                case FoodEffectKind.Heal:StatusEffect(e,S.Injury,true);break;
                case FoodEffectKind.Stamina:character.AddExtraStamina(e.Amount/100);break;
                case FoodEffectKind.Invincible:
                    if(!a.HasAfflictionType(Affliction.AfflictionType.Invincibility,out var previous) || previous.totalTime-previous.timeElapsed<e.Seconds)
                        a.AddAffliction(new Affliction_Invincibility{totalTime=e.Seconds});
                    // Read back native stacking instead of assuming an expiry time.
                    if(a.HasAfflictionType(Affliction.AfflictionType.Invincibility,out var active))
                        appleInvincibleUntil=Time.time+Mathf.Max(0,active.totalTime-active.timeElapsed);
                    break;
                case FoodEffectKind.Shield:
                    Shield=Mathf.Max(Time.time<shieldUntil?Shield:0,e.Amount/100);shieldUntil=Mathf.Max(shieldUntil,Time.time+e.Seconds);break;
                case FoodEffectKind.Resist:
                    resistFraction=Mathf.Max(Time.time<resistUntil?resistFraction:0,e.Amount/100);resistUntil=Mathf.Max(resistUntil,Time.time+e.Seconds);break;
                case FoodEffectKind.Fireproof:fireUntil=Mathf.Max(fireUntil,Time.time+e.Seconds);break;
                case FoodEffectKind.AppleAftereffect:
                    appleSleep.Clear();
                    aftereffect.Queue(Time.time,e.Seconds);break;
                case FoodEffectKind.CurePoison:ClearPoison();break;
                case FoodEffectKind.Milk:Milk();break;
                case FoodEffectKind.Teleport:Teleport(e.Amount);break;
                case FoodEffectKind.LowGravity:a.AddAffliction(new Affliction_LowGravity((int)e.Amount,e.Seconds));break;
                case FoodEffectKind.Blind:a.AddAffliction(new Affliction_Blind{totalTime=e.Seconds});break;
            }
        }
        private void StatusEffect(FoodEffect e,S status,bool healing=false)
        {
            if(e.Seconds>0)Timed(e.Key,status,e.Seconds,e.Amount/100,healing);
            else if(healing)character.refs.afflictions.SubtractStatus(status,e.Amount/100);
            else Apply(status,e.Amount/100);
        }
        internal void RemoveFoodPoisonTimers()
        {
            foreach(var key in timers.Where(x=>x.Value.status==S.Poison).Select(x=>x.Key).ToArray())timers.Remove(key);
        }
        private void ClearPoison()
        {
            RemoveFoodPoisonTimers();
            var a=character.refs.afflictions;
            a.RemoveAffliction(Affliction.AfflictionType.PoisonOverTime);
            // Native SetStatus/SubtractStatus also clears Spores when healing poison.
            // Use the native operation; this is documented as a PEAK-specific extension.
            a.SubtractStatus(S.Poison,1f);
        }
        private void Milk()
        {
            ResetEffects();
            var a=character.refs.afflictions;
            foreach(var aff in a.afflictionList.ToArray())a.RemoveAffliction(aff);
            a.SubtractStatus(S.Poison,1);a.SubtractStatus(S.Drowsy,1);
        }
        private void ResetEffects()
        {
            timers.Clear();Shield=0;
            aftereffect.Clear();
            appleSleep.Clear();
            chorus.Clear();
            appleRampStart=-1;
            shieldUntil=resistUntil=resistFraction=fireUntil=appleInvincibleUntil=nextTeleport=tick=0;
        }
        void OnDisable(){ResetEffects();}
        private void Teleport(float radius)
        {
            if(Time.time<nextTeleport || character.data.dead || !character.data.fullyConscious)return;
            nextTeleport=Time.time+1;
            Vector3 origin=character.Center;
            for(int i=0;i<16;i++)
            {
                var circle=UnityEngine.Random.insideUnitCircle*radius;
                var start=origin+new Vector3(circle.x,4,circle.y);
                if(!Physics.Raycast(start,Vector3.down,out var hit,12,~0,QueryTriggerInteraction.Ignore) || hit.normal.y<.65f || hit.rigidbody)continue;
                Vector3 feet=hit.point+Vector3.up*.1f, center=feet+Vector3.up*.9f;
                if(Vector3.Distance(center,origin)<1 || Mathf.Abs(center.y-origin.y)>8)continue;
                bool blocked=false;
                foreach(var col in Physics.OverlapCapsule(feet+Vector3.up*.36f,feet+Vector3.up*1.45f,.35f,~0,QueryTriggerInteraction.Collide))
                {
                    if(col.GetComponentInParent<Character>()==character || col.GetComponentInParent<Item>())continue;
                    string n=col.name.ToLowerInvariant();
                    bool hazard=n.Contains("lava")||n.Contains("water")||n.Contains("kill")||n.Contains("death")||n.Contains("fire");
                    if(!col.isTrigger || hazard){blocked=true;break;}
                }
                if(blocked)continue;
                character.photonView.RPC("WarpPlayerRPC",RpcTarget.All,center,true);
                // Charge only a successful warp, never a failed landing search.
                if(UnityEngine.Random.value<.5f)Apply(S.Hunger,FoodEffectCatalog.ChorusHunger/100f);
                else Apply(S.Spores,FoodEffectCatalog.ChorusSpores/100f);
                if(!InAirport(character.refs.afflictions) || Plugin.AirportEffects.Value)chorus.Record(Time.time);
                Plugin.Log.LogInfo("Chorus fruit teleport: " + center);return;
            }
            Plugin.Log.LogInfo("Chorus fruit: no clear landing found in 16 attempts; stayed in place.");
        }
        internal void FilterStatus(S status,ref float amount)
        {
            if(amount<=0)return;
            if(status==S.Hot && Time.time<fireUntil) amount=0;
            else if(status==S.Injury)
            {
                if(Time.time<resistUntil)amount*=1-resistFraction;
                if(Time.time<shieldUntil){float used=Mathf.Min(Shield,amount);Shield-=used;amount-=used;}
            }
        }
        void Update()
        {
            if(!character || !character.IsLocal)return;
            if(character.data.dead){ResetEffects();return;}
            chorus.Expire(Time.time);
            if(!character.data.zombified && !character.isZombie && chorus.TakeTransformation(Time.time))
            {
                // The native RPC delegates enemy creation to the host and synchronizes zombification.
                character.photonView.RPC("RPCA_Zombify",RpcTarget.All);
                return;
            }
            if(Time.time>=shieldUntil)Shield=0;
            tick+=Time.deltaTime;if(tick<.25f)return;
            float dt=tick;tick=0;
            if(!Plugin.AppleAftereffects.Value){aftereffect.Clear();appleSleep.Clear();}
            float progress=aftereffect.Progress(Time.time,character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.Invincibility,out var invincibility));
            if(progress>=0)
            {
                var a=character.refs.afflictions;float cap=Mathf.Min(1f,a.GetStatusCap(S.Drowsy));
                if(appleRampStart!=aftereffect.StartedAt)
                {appleRampStart=aftereffect.StartedAt;appleRampBaseline=a.GetCurrentStatus(S.Drowsy);}
                float target=Mathf.Lerp(appleRampBaseline,cap,progress);
                Apply(S.Drowsy,Mathf.Max(0,target-a.GetCurrentStatus(S.Drowsy)));
                if(progress>=1)appleSleep.Begin(aftereffect.Duration);
            }
            if(appleSleep.Active)
            {
                var a=character.refs.afflictions;
                if(appleSleep.Observe(Time.time,character.data.fullyPassedOut))
                {
                    // Restore only the drowsiness above the pre-apple baseline.
                    // Other status types and the native wake-up transition are untouched.
                    a.SubtractStatus(S.Drowsy,Mathf.Max(0,a.GetCurrentStatus(S.Drowsy)-appleRampBaseline));
                }
                else Apply(S.Drowsy,Mathf.Max(0,Mathf.Min(1f,a.GetStatusCap(S.Drowsy))-a.GetCurrentStatus(S.Drowsy)));
            }
            foreach(var key in timers.Keys.ToArray())
            {
                var effect=timers[key];float amount=effect.clock.Take(dt);
                if(effect.healing)character.refs.afflictions.SubtractStatus(effect.status,amount);
                else Apply(effect.status,amount);
                if(effect.clock.Remaining<=0)timers.Remove(key);
            }
        }
        internal string CountdownText()
        {
            if(!character || !character.IsLocal || character.data.dead)return "";
            var b=new StringBuilder();
            void Line(string label,float until){if(until>Time.time)b.AppendLine(label+L10n.T($"：{Mathf.CeilToInt(until-Time.time)} 秒",$": {Mathf.CeilToInt(until-Time.time)}s",$"：{Mathf.CeilToInt(until-Time.time)} 秒"));}
            if(appleInvincibleUntil>Time.time && character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.Invincibility,out var inv))
                Line(L10n.T("金苹果无敌","Apple invincibility","金のリンゴの無敵"),Time.time+Mathf.Max(0,inv.totalTime-inv.timeElapsed));
            if(Shield>0)Line(L10n.T($"吸收盾 {Shield*100:0.#} 点",$"Shield {Shield*100:0.#} points",$"シールド {Shield*100:0.#} ポイント"),shieldUntil);
            Line(L10n.T($"受伤减少 {resistFraction*100:0.#}%",$"Injury reduction {resistFraction*100:0.#}%",$"負傷軽減 {resistFraction*100:0.#}%"),resistUntil);
            Line(L10n.T("炎热防护","Heat protection","暑さ防御"),fireUntil);
            if(aftereffect.Pending)b.AppendLine(L10n.T($"待触发：睡意在 {aftereffect.PendingSeconds:0.#} 秒内升满",$"Pending: drowsiness ramp over {aftereffect.PendingSeconds:0.#}s",$"発動待ち：{aftereffect.PendingSeconds:0.#} 秒で眠気最大"));
            Line(L10n.T("睡意逐渐增加","Drowsiness rising","眠気上昇中"),aftereffect.HoldUntil);
            if(appleSleep.Active)
            {
                if(appleSleep.Until>0)Line(L10n.T("金苹果睡眠","Apple sleep","金のリンゴの睡眠"),appleSleep.Until);
                else b.AppendLine(L10n.T("金苹果：等待进入睡眠后计时","Apple: sleep timer starts once unconscious","金のリンゴ：眠ってから計測開始"));
            }
            if(chorus.TransformAt>Time.time)
                Line(L10n.T("僵尸化倒计时！喝牛奶解除","Zombification! Drink milk to cancel","ゾンビ化！ミルクで解除"),chorus.TransformAt);
            else if(chorus.Count>0 && !chorus.Triggered)
                Line(L10n.T($"紫颂果感染 {chorus.Count}/{ChorusExposure.Limit}",$"Chorus exposure {chorus.Count}/{ChorusExposure.Limit}",$"コーラス感染 {chorus.Count}/{ChorusExposure.Limit}"),chorus.ExpiresAt);
            foreach(var effect in timers.Values)
            {
                string name=effect.healing?L10n.T("恢复受伤","Injury recovery","負傷回復"):effect.status==S.Poison?L10n.T("持续中毒","Ongoing poison","継続毒"):effect.status==S.Hunger?L10n.T("持续饥饿","Ongoing hunger","継続空腹"):effect.status==S.Drowsy?L10n.T("持续睡意","Ongoing drowsiness","継続眠気"):L10n.T("持续受伤","Ongoing injury","継続負傷");
                int seconds=Mathf.CeilToInt(effect.clock.Remaining);string amount=L10n.N(effect.clock.Remaining*effect.clock.Rate*100);
                b.AppendLine(L10n.T($"{name}：{seconds} 秒，剩余 {amount} 点",$"{name}: {seconds}s, {amount} points remaining",$"{name}：{seconds} 秒、残り {amount} ポイント"));
            }
            return b.ToString().TrimEnd();
        }
    }
}
