using UnityEngine;

namespace MinecraftFoods
{
    // A dedicated trigger lets SoundAPI replace only our foods, during use.
    public sealed class FoodAudio : MonoBehaviour
    {
        private Item item;
        private AudioSource source;
        private static AudioClip trigger;
        private bool playing;
        void Awake() { item=GetComponent<Item>(); }
        void Update()
        {
            var holder=item?item.trueHolderCharacter:null;
            var marker=item?item.GetComponent<FoodMarker>():null;
            bool eating=holder && holder.IsLocal && !holder.data.dead && marker && marker.Definition!=null && marker.Stage<marker.Definition.wreck && !item.finishedCast && !item.consuming
                && (item.isUsingPrimary || item.isUsingSecondary);
            if(eating && !playing)
            {
                if(!source)
                {
                    var audioObject=new GameObject("MCFoods_EatingSource");
                    audioObject.transform.SetParent(transform,false);
                    source=audioObject.AddComponent<AudioSource>();
                    source.playOnAwake=false; source.loop=true; source.spatialBlend=0;
                    source.volume=.7f; source.pitch=1;
                    if(SFX_Player.instance)source.outputAudioMixerGroup=SFX_Player.instance.defaultMixerGroup;
                    if(!trigger)trigger=AudioClip.Create("MCFoods_Eating",70560,1,44100,false);
                }
                source.clip=trigger;
                source.Play(); // SoundAPI substitutes the packaged, unmodified MCEat.mp3.
                playing=true;
            }
            if(!eating)Stop();
        }
        private void Stop(){if(source && playing)source.Stop();playing=false;}
        void OnDisable(){Stop();}
    }
}
