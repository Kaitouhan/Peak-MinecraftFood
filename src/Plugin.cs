using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PEAKLib.Core;
using PEAKLib.Items;
using UnityEngine;
using UnityEngine.Networking;
using Zorro.Core;
using Object = UnityEngine.Object;

namespace MinecraftFoods
{
    [BepInPlugin(Id, "Minecraft Foods", "1.0.0")]
    [BepInDependency("com.github.PEAKModding.PEAKLib.Items", "1.6.2")]
    [BepInDependency("com.github.PEAKModding.PEAKLib.Core")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "codex.peak.minecraftfoods";
        internal static Plugin Instance;
        internal static ManualLogSource Log;
        internal static string Root;
        internal static readonly Dictionary<string, FoodDefinition> Foods = new Dictionary<string, FoodDefinition>(StringComparer.Ordinal);
        internal static readonly List<Item> Registered = new List<Item>();
        internal static ConfigEntry<float> LootShare;
        internal static ConfigEntry<bool> AirportEffects, AppleAftereffects;
        internal static ConfigEntry<string> GripTemplate;
        internal static ConfigEntry<bool> ShowInfo, EatAudio;
        internal static readonly List<AudioClip> Sounds = new List<AudioClip>();
        private Harmony harmony;
        private bool ready, failed, externalInfo;
        private GameObject templates;
        private GUIStyle infoStyle;
        void Awake()
        {
            Instance = this; Log = Logger;
            Root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            LootShare = Config.Bind("Loot", "MinecraftShare", .25f, new ConfigDescription("Minecraft food share per ordinary luggage slot. Host setting; restart for changes.", new AcceptableValueRange<float>(0f,.8f)));
            AirportEffects = Config.Bind("Effects", "AllowFoodEffectsInAirport", true, "Allow only this mod's food effects in the airport, for testing. Does not unlock environmental damage.");
            AppleAftereffects = Config.Bind("Effects", "GoldenAppleAftereffects", true, "PEAK adaptation: 2.5/5 drowsiness after golden/enchanted apple invulnerability ends.");
            GripTemplate = Config.Bind("Visuals", "GripTemplate", "TrailMix", "TrailMix (recommended) or SmallEgg. Restart after changing.");
            ShowInfo = Config.Bind("UI", "ShowFoodInformation", true, "Show names, nutrition, effects and cooking thresholds; independent of other personal mods.");
            EatAudio = Config.Bind("Audio", "MinecraftEatingSounds", true, "Play the bundled Minecraft_Eating MCEat sound locally on successful consumption.");
            try
            {
                var catalogue = Newtonsoft.Json.JsonConvert.DeserializeObject<Catalogue>(File.ReadAllText(Path.Combine(Root,"catalogue.json")));
                if(catalogue?.foods==null || catalogue.foods.Length==0)throw new InvalidDataException("catalogue.json contains no foods");
                foreach (var f in catalogue.foods.OrderBy(x => x.id, StringComparer.Ordinal))
                {
                    if (string.IsNullOrEmpty(f.id) || Foods.ContainsKey(f.id) || f.scorch < 1 || f.wreck <= f.scorch || f.wreck > 12 || f.weight <= 0 || f.uses < 1)
                        throw new InvalidDataException("Invalid food definition: " + f.id);
                    Foods.Add(f.id, f);
                    Visuals.TextureFor(f.texture);
                    if (f.HasCooked) Visuals.TextureFor(f.cookedTexture);
                }
                harmony = new Harmony(Id); harmony.PatchAll();
                StartCoroutine(LoadAudio());
            }
            catch (Exception e) { failed = true; Log.LogError("Minecraft Foods initialization failed: " + e); if(Environment.GetCommandLineArgs().Contains("--mcfoods-audit"))Application.Quit(2); }
        }
        void Update()
        {
            if (ready || failed) return;
            var db = SingletonAsset<ItemDatabase>.Instance;
            if (db == null || db.itemLookup.Count < 50 || LocalizedText.mainTable == null) return;
            bool audit=Environment.GetCommandLineArgs().Contains("--mcfoods-audit");
            try { Register(db); ready = true; if(audit)RuntimeAudit.Run(); }
            catch (Exception e) { failed = true; Log.LogError("Food registration failed; fix before entering a multiplayer game: " + e); }
            finally { if(audit)Application.Quit(failed?2:0); }
        }
        private void Register(ItemDatabase db)
        {
            string wanted = GripTemplate.Value == "SmallEgg" ? "NestEgg_Raven" : "TrailMix";
            Item template = db.itemLookup.Values.FirstOrDefault(x => x.name == wanted)
                ?? db.itemLookup.Values.FirstOrDefault(x => x.name == "NestEgg_Raven");
            if (!template) throw new InvalidOperationException("Neither TrailMix nor Small Egg template was found.");
            templates = new GameObject("MinecraftFoods_PrefabStorage");
            templates.SetActive(false); DontDestroyOnLoad(templates);
            var mod = ModDefinition.GetOrCreate(Info);
            var allLuggage = (SpawnPool)0;
            foreach (SpawnPool pool in Enum.GetValues(typeof(SpawnPool)))
                if (pool.ToString().StartsWith("Luggage", StringComparison.Ordinal)) allLuggage |= pool;
            foreach (var f in Foods.Values)
            {
                var go = Instantiate(template.gameObject, templates.transform, false);
                go.SetActive(false); go.name = "MCFood_" + f.id;
                // Only the native grip, physics and networking infrastructure is retained.
                foreach (var action in go.GetComponentsInChildren<ItemActionBase>(true)) DestroyImmediate(action);
                foreach (var component in go.GetComponents<ItemComponent>())
                    if (!(component is ItemCooking)) DestroyImmediate(component);
                foreach (var animator in go.GetComponentsInChildren<Animator>(true)) DestroyImmediate(animator);
                var item = go.GetComponent<Item>();
                item.totalUses = f.uses > 1 ? f.uses : -1;
                item.data = null; item.isSecretlyOtherItemPrefab = null;
                item.canUseOnFriend = true; item.mustUseOnFriend = true;
                item.rightHandOnly = template.rightHandOnly; item.gliderHold = false;
                item.usingTimePrimary = f.id == "honey_bottle" ? 2f : 1.6f;
                var feedback=go.GetComponent<ItemUseFeedback>();
                if(feedback)feedback.sfxUsed=null;
                item.itemTags = Item.ItemTags.PackagedFood;
                item.UIData = new Item.ItemUIData { itemName=f.name, icon=Visuals.IconFor(f.texture), mainInteractPrompt=f.id == "milk_bucket" || f.id == "honey_bottle" ? "饮用" : "吃", secondaryInteractPrompt="喂食", hasSecondInteract=true, hideSecondInteract=false, hideFuel=f.uses==1 };
                // Unity serializes the marker and native hand transforms into every instance.
                var marker = go.AddComponent<FoodMarker>(); marker.foodId = f.id;
                Visuals.Build(item, marker);
                var cooking = go.GetComponent<ItemCooking>() ?? go.AddComponent<ItemCooking>();
                cooking.preCooked = 0; cooking.wreckWhenCooked=false;
                cooking.ignoreDefaultCookBehavior=true; cooking.ignoreDefaultPoisonBehavior=true;
                cooking.additionalCookingBehaviors = new AdditionalCookingBehavior[0];
                AccessTools.Field(typeof(ItemCooking),"disableCooking").SetValue(cooking,false);
                go.AddComponent<FoodEatAction>().OnCastFinished = true;
                if (f.uses > 1) go.AddComponent<Action_ReduceUses>().consumeOnFullyUsed = true;
                var loot = go.GetComponent<LootData>() ?? go.AddComponent<LootData>();
                loot.spawnLocations = allLuggage; loot.Rarity = RarityFor(f.rarity);
                loot.banInSolo = false; loot.excludeBasedOnCustomRunSetting=false;
                loot.excludeFromCustomRunSelection=false; loot.useOtherItemForSpawningValidity=null;
                loot.rarityOverrides = new List<ItemRarityOverride>();
                new ItemContent(item).Register(mod);
                if (!db.itemLookup.TryGetValue(item.itemID, out var registered) || registered != item)
                    throw new InvalidOperationException("PEAKLib did not register " + f.id);
                Registered.Add(item);
            }
            LootData.PopulateLootData();
            InstallInfoIntegration();
            Log.LogInfo($"REGISTERED {Registered.Count} independent Minecraft foods; template={template.name}; no PeakCustomItemMod dependency.");
            var diagnostic = new List<string> { "Minecraft Foods 2.0.0", "Grip template: " + template.name, "Config: " + Config.ConfigFilePath };
            foreach (var item in Registered)
            {
                var m=item.GetComponent<FoodMarker>();
                diagnostic.Add($"{m.foodId} ID={item.itemID} L={item.transform.Find("Hand_L")?.localPosition} R={item.transform.Find("Hand_R")?.localPosition} model={m.visual.transform.localPosition} size={m.meshSize:F3}");
            }
            foreach (var pool in LootData.AllSpawnWeightData.Where(x=>x.Key.ToString().StartsWith("Luggage")))
            {
                double total=pool.Value.Values.Sum(x=>(double)x), mc=Registered.Sum(i=>pool.Value.TryGetValue(i.itemID,out int w)?(double)w:0);
                diagnostic.Add($"{pool.Key}: Minecraft {mc}/{total} ({(total>0?mc/total:0):P2})");
            }
            File.WriteAllLines(Path.Combine(Paths.BepInExRootPath,"MinecraftFoods-diagnostics.txt"),diagnostic);
        }
        static Rarity RarityFor(string rarity) => rarity == "epic" ? Rarity.Mythic : rarity == "rare" ? Rarity.Rare : rarity == "uncommon" ? Rarity.Uncommon : Rarity.Common;
        IEnumerator LoadAudio()
        {
            string directory=Path.Combine(Root,"sounds");
            if(!Directory.Exists(directory)) yield break;
            foreach (var path in Directory.GetFiles(directory).Where(p =>
                string.Equals(Path.GetExtension(p), ".mp3", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(Path.GetExtension(p), ".ogg", StringComparison.OrdinalIgnoreCase)))
            {
                AudioType type=string.Equals(Path.GetExtension(path), ".mp3", StringComparison.OrdinalIgnoreCase) ? AudioType.MPEG : AudioType.OGGVORBIS;
                using (var req = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,type))
                {
                    yield return req.SendWebRequest();
                    if (req.result == UnityWebRequest.Result.Success) Sounds.Add(DownloadHandlerAudioClip.GetContent(req));
                    else Log.LogWarning("Eating audio unavailable: " + req.error);
                }
            }
        }
        internal static void PlayEat(Character c)
        {
            if (!EatAudio.Value || Sounds.Count == 0 || !c.IsLocal) return;
            var obj = new GameObject("MinecraftEatAudio");
            var audio=obj.AddComponent<AudioSource>(); audio.spatialBlend=0; audio.volume=.65f;
            audio.clip=Sounds[UnityEngine.Random.Range(0,Sounds.Count)]; audio.Play(); Destroy(obj,2);
        }
        void InstallInfoIntegration()
        {
            var type=AccessTools.TypeByName("ItemInfoDisplay.Plugin");
            var method=type==null?null:AccessTools.Method(type,"ProcessItemGameObject",new[]{typeof(Item)});
            if(method==null) return;
            harmony.Patch(method,postfix:new HarmonyMethod(typeof(Plugin),nameof(AfterExternalInfo)));
            externalInfo=true;
        }
        static void AfterExternalInfo(Item __0)
        {
            var marker=__0?__0.GetComponent<FoodMarker>():null;
            if(!marker || !ShowInfo.Value) return;
            var field=AccessTools.Field(AccessTools.TypeByName("ItemInfoDisplay.Plugin"),"itemInfoDisplayTextMesh");
            var text=field?.GetValue(null); if(text==null)return;
            AccessTools.Property(text.GetType(),"text")?.SetValue(text,Describe(marker));
        }
        internal static string Describe(FoodMarker m)
        {
            var f=m.Definition; int s=m.Stage;
            string text=$"<b>{f.NameAt(s)}</b>\n{f.description}\n饱食 {f.HungerAt(s):0.#}/20（移除 {f.HungerAt(s)*5:0.#} 点饥饿）｜饱和 {f.SaturationAt(s):0.#}/20\n{f.effectDescription}\n烹饪 {s}/12；{f.scorch} 次起烧焦；{f.wreck} 次烧毁";
            if(f.BurnPoisonAt(s)>0)text+=$"\n烧焦附加中毒 {f.BurnPoisonAt(s)*100:0.#} 点";
            if(s>=f.wreck) text+="\n已烧毁，无法食用或获得效果。";
            var c=Character.localCharacter; var state=c?c.GetComponent<FoodPlayerState>():null;
            if(state)text+=$"\n当前饱和储备 {state.Saturation*20:0.#}/20；吸收盾 {state.Shield*100:0.#}";
            return text;
        }
        void OnGUI()
        {
            if(!ready || !ShowInfo.Value || externalInfo)return;
            var c=Character.localCharacter; var m=c && c.data.currentItem ? c.data.currentItem.GetComponent<FoodMarker>():null;
            if(!m)return;
            if(infoStyle==null) infoStyle=new GUIStyle(GUI.skin.label) { richText=true, wordWrap=true, font=Font.CreateDynamicFontFromOSFont("Microsoft YaHei",18), normal={textColor=new Color(1,.96f,.8f)} };
            infoStyle.fontSize=Mathf.Clamp(Mathf.RoundToInt(Screen.height/65f),12,25);
            GUI.Label(new Rect(Screen.width*.69f,Screen.height*.57f,Screen.width*.29f,Screen.height*.29f),Describe(m),infoStyle);
        }
    }
}


