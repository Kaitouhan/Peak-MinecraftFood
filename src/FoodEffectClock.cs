using System;

namespace MinecraftFoods
{
    internal sealed class AppleSleepClock
    {
        internal bool Active { get; private set; }
        internal float Until { get; private set; }
        private float duration;
        internal void Begin(float seconds){Active=true;duration=seconds;Until=0;}
        internal bool Observe(float now,bool fullyAsleep)
        {
            if(!Active)return false;
            if(!fullyAsleep){Until=0;return false;}
            if(Until==0)Until=now+duration;
            if(now<Until)return false;
            Active=false;return true;
        }
        internal void Clear(){Active=false;Until=duration=0;}
    }
    internal sealed class ChorusExposure
    {
        internal const int Limit=4;
        internal const float Window=120, Warning=5;
        internal int Count { get; private set; }
        internal float ExpiresAt { get; private set; }
        internal float TransformAt { get; private set; }
        internal bool Triggered { get; private set; }
        internal void Record(float now)
        {
            Expire(now);
            if(TransformAt>0 || Triggered)return;
            Count++;ExpiresAt=now+Window;
            if(Count>=Limit)TransformAt=now+Warning;
        }
        internal void Expire(float now){if(TransformAt<=0 && now>=ExpiresAt)Count=0;}
        internal bool TakeTransformation(float now)
        {
            if(Triggered || TransformAt<=0 || now<TransformAt)return false;
            Triggered=true;return true;
        }
        internal void Clear(){Count=0;ExpiresAt=TransformAt=0;Triggered=false;}
    }
    internal sealed class FoodEffectClock
    {
        internal float Remaining { get; private set; }
        internal float Rate { get; private set; }
        internal void Refresh(float total,float seconds)
        {
            if(seconds<=0 || total<0)throw new ArgumentOutOfRangeException();
            // Keep the greater remaining budget and stronger rate. Multiplying the
            // longest duration by the strongest rate would invent extra healing.
            float budget=Math.Max(total,Remaining*Rate);
            Rate=Math.Max(Rate,total/seconds);
            Remaining=Rate>0?budget/Rate:0;
        }
        internal float Take(float seconds)
        {
            float dt=Math.Min(Math.Max(0,seconds),Remaining);
            Remaining=Math.Max(0,Remaining-dt);
            return dt*Rate;
        }
    }

    internal sealed class AppleAftereffectClock
    {
        internal bool Pending { get; private set; }
        internal float PendingSeconds { get; private set; }
        internal float HoldUntil { get; private set; }
        internal float StartedAt { get; private set; }
        private float rampSeconds;
        internal float Duration => rampSeconds;
        private bool completionDelivered;
        internal void Queue(float now,float seconds)
        {
            PendingSeconds=Math.Max(Math.Max(PendingSeconds,HoldUntil-now),seconds);
            Pending=true;HoldUntil=0;completionDelivered=false;
        }
        internal bool Tick(float now,bool invincible)
        {
            if(Pending && !invincible)
            {Pending=false;StartedAt=now;rampSeconds=PendingSeconds;HoldUntil=now+PendingSeconds;PendingSeconds=0;}
            return now<HoldUntil;
        }
        // Return the final 100% sample once even when a frame crosses the deadline.
        internal float Progress(float now,bool invincible)
        {
            Tick(now,invincible);
            if(Pending || HoldUntil<=0 || completionDelivered)return -1;
            float value=Math.Min(1,Math.Max(0,(now-StartedAt)/rampSeconds));
            if(value>=1)completionDelivered=true;
            return value;
        }
        internal void Clear(){Pending=false;PendingSeconds=HoldUntil=StartedAt=rampSeconds=0;completionDelivered=false;}
    }
}
