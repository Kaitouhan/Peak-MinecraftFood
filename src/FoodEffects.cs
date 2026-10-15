using System;
using System.Collections.Generic;
using System.Linq;
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
            if(!c || !c.IsLocal || !m || item.consuming || Time.time-lastUse<.3f || m.Stage>=m.Definition.wreck)return;
            if(item.totalUses>0 && item.GetData<OptionableIntItemData>(DataEntryKey.ItemUses).Value<=0)return;
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
        private float shieldUntil, resistUntil, fireUntil, afterAt, afterDrowsyDuration, drowsyHoldUntil, nextTeleport;
        private Character character;
        private readonly Dictionary<string,TimedStatus> timers=new Dictionary<string,TimedStatus>();
        private float tick;
        private sealed class TimedStatus { internal S status; internal float remaining,rate; internal bool healing; }
        private static readonly System.Reflection.FieldInfo Airport=AccessTools.Field(typeof(CharacterAfflictions),"m_inAirport");
        internal static bool InAirport(CharacterAfflictions a) => (bool)Airport.GetValue(a);
        void Awake(){character=GetComponent<Character>();}
        internal void Eat(FoodDefinition f,int stage)
        {
            var a=character.refs.afflictions;
            a.SubtractStatus(S.Hunger,f.HungerAt(stage)/100f);
            character.AddExtraStamina(f.BonusAt(stage)/100f);
            bool wholesome=stage<f.scorch;
            switch(f.id)
            {
                case "pufferfish":
                    Apply(S.Poison,.30f+.025f*stage);
                    Timed("puffer-poison",S.Poison,60,.35f+.05f*stage);
                    Timed("puffer-hunger",S.Hunger,15,.15f);
                    Apply(S.Drowsy,.10f); Timed("puffer-nausea",S.Drowsy,15,.10f);
                    break;
                case "rotten_flesh":
                    if(UnityEngine.Random.value<.8f)Timed("rotten-hunger",S.Hunger,30,.20f);
                    if(stage>0)Apply(S.Poison,Mathf.Min(.5f,.05f*stage));
                    break;
                case "spider_eye":
                    Apply(S.Poison,.05f+.025f*stage); Timed("spider-poison",S.Poison,4,.05f+.025f*stage); break;
                case "poisonous_potato":
                    if(UnityEngine.Random.value<.6f){Apply(S.Poison,.05f+.025f*stage);Timed("potato-poison",S.Poison,5,.05f+.025f*stage);}break;
                case "chicken":
                    if(stage==0 && UnityEngine.Random.value<.3f)Apply(S.Poison,.12f);break;
                case "beef": case "porkchop": case "mutton": case "rabbit":
                    if(stage==0 && UnityEngine.Random.value<.15f)Apply(S.Poison,.08f);break;
                case "cod": case "salmon": case "tropical_fish":
                    if(stage==0 && UnityEngine.Random.value<.2f)Apply(S.Poison,.10f);break;
                case "honey_bottle": if(wholesome)ClearPoison();break;
                case "milk_bucket": if(wholesome)Milk();break;
                case "golden_apple": if(wholesome)Golden(false);break;
                case "enchanted_golden_apple": if(wholesome)Golden(true);break;
                case "chorus_fruit": if(wholesome)Teleport();break;
                case "suspicious_stew": if(wholesome)Stew();break;
            }
            if(f.BurnPoisonAt(stage)>0)Apply(S.Poison,f.BurnPoisonAt(stage));
            Plugin.Log.LogInfo($"EAT {f.id} stage={stage} nutrition={f.HungerAt(stage):F2}/{f.BonusAt(stage):F2} receiver={character.photonView.ViewID} airport={InAirport(a)}");
        }
        private void Timed(string key,S status,float seconds,float total,bool heal=false)
        {
            // Refresh equivalent effects rather than multiplying them indefinitely.
            float rate=total/seconds;
            if(timers.TryGetValue(key,out var previous)){seconds=Mathf.Max(seconds,previous.remaining);rate=Mathf.Max(rate,previous.rate);}
            timers[key]=new TimedStatus{status=status,remaining=seconds,rate=rate,healing=heal};
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
        private void Golden(bool enchanted)
        {
            float invincible=enchanted?12:5;
            var a=character.refs.afflictions;
            if(!a.HasAfflictionType(Affliction.AfflictionType.Invincibility,out var previous) || previous.totalTime-previous.timeElapsed<invincible)
                a.AddAffliction(new Affliction_Invincibility{totalTime=invincible});
            else invincible=previous.totalTime-previous.timeElapsed;
            Shield=Mathf.Max(Shield,enchanted?.8f:.2f); shieldUntil=Time.time+60;
            Timed("regeneration",S.Injury,enchanted?20:5,enchanted?.8f:.2f,true);
            if(enchanted){resistUntil=Time.time+60;fireUntil=Time.time+60;}
            // Replace the old 3/6-second knockdown with PEAK's native Drowsy meter,
            // held at its cap for twice as long: 6/12 seconds after invincibility ends.
            if(Plugin.AppleAftereffects.Value){afterDrowsyDuration=Mathf.Max(afterAt>Time.time?afterDrowsyDuration:0,enchanted?12f:6f);afterAt=Mathf.Max(afterAt,Time.time+invincible);}
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
            timers.Clear(); Shield=0; shieldUntil=resistUntil=fireUntil=afterAt=afterDrowsyDuration=drowsyHoldUntil=0;
            var a=character.refs.afflictions;
            foreach(var aff in a.afflictionList.ToArray())a.RemoveAffliction(aff);
            a.SubtractStatus(S.Poison,1);a.SubtractStatus(S.Drowsy,1);
        }
        private void Stew()
        {
            switch(UnityEngine.Random.Range(0,8))
            {
                case 0:Timed("regeneration",S.Injury,8,.15f,true);break;
                case 1:character.AddExtraStamina(.20f);break;
                case 2:character.refs.afflictions.AddAffliction(new Affliction_LowGravity(1,6));break;
                case 3:fireUntil=Mathf.Max(fireUntil,Time.time+4);break;
                case 4:character.refs.afflictions.AddAffliction(new Affliction_Blind{totalTime=8});break;
                case 5:Apply(S.Poison,.05f);Timed("stew-poison",S.Poison,12,.15f);break;
                case 6:Apply(S.Drowsy,.10f);break;
                case 7:Timed("stew-wither",S.Injury,8,.15f);break;
            }
        }
        private void Teleport()
        {
            if(Time.time<nextTeleport || character.data.dead || !character.data.fullyConscious)return;
            nextTeleport=Time.time+1;
            Vector3 origin=character.Center;
            for(int i=0;i<16;i++)
            {
                var circle=UnityEngine.Random.insideUnitCircle*30;
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
                if(Time.time<resistUntil)amount*=.8f;
                if(Time.time<shieldUntil){float used=Mathf.Min(Shield,amount);Shield-=used;amount-=used;}
            }
        }
        void Update()
        {
            if(!character || !character.IsLocal)return;
            if(character.data.dead){timers.Clear();Shield=0;afterAt=resistUntil=fireUntil=shieldUntil=drowsyHoldUntil=0;return;}
            if(Time.time>=shieldUntil)Shield=0;
            if(afterAt>0 && Time.time>=afterAt){if(!character.refs.afflictions.HasAfflictionType(Affliction.AfflictionType.Invincibility,out var invincibility)){afterAt=0;drowsyHoldUntil=Time.time+afterDrowsyDuration;character.refs.afflictions.SetStatus(S.Drowsy,character.refs.afflictions.GetStatusCap(S.Drowsy));afterDrowsyDuration=0;}}
            if(drowsyHoldUntil>Time.time)character.refs.afflictions.SetStatus(S.Drowsy,character.refs.afflictions.GetStatusCap(S.Drowsy));
            tick+=Time.deltaTime;if(tick<.25f)return;
            float dt=tick;tick=0;
            foreach(var key in timers.Keys.ToArray())
            {
                var effect=timers[key];float step=Mathf.Min(dt,effect.remaining);
                if(effect.healing)character.refs.afflictions.SubtractStatus(effect.status,effect.rate*step);
                else Apply(effect.status,effect.rate*step);
                effect.remaining-=step;if(effect.remaining<=0)timers.Remove(key);
            }
        }
    }
}
