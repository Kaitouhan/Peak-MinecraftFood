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
using Zorro.Core;
using Object = UnityEngine.Object;

namespace MinecraftFoods
{
    [BepInPlugin(Id, "Minecraft Foods", ModVersion.Value)]
    [BepInDependency("com.github.PEAKModding.PEAKLib.Items", "1.6.2")]
    [BepInDependency("com.github.PEAKModding.PEAKLib.Core")]
    [BepInDependency("me.loaforc.soundapi", "2.0.13")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "codex.peak.minecraftfoods";
        internal static Plugin Instance;
        internal static ManualLogSource Log;
        internal static string Root;
        internal static readonly Dictionary<string, FoodDefinition> Foods = new Dictionary<string, FoodDefinition>(StringComparer.Ordinal);
        internal static readonly List<Item> Registered = new List<Item>();
        internal static ConfigEntry<float> LootShare;
        internal static ConfigEntry<float> CursedLootShare, ClownLootShare;
        internal static ConfigEntry<bool> AirportEffects, AppleAftereffects;
        internal static ConfigEntry<string> GripTemplate;
        internal static ConfigEntry<bool> ShowInfo, DetailedInfo, ShowEffectTimers;
        private Harmony harmony;
        private bool ready, failed, externalInfo;
        private GameObject templates;
        private GUIStyle infoStyle;
        private Vector2 infoScroll;
        private SettingsWindow settingsWindow;
        void Awake()
        {
            Instance = this; Log = Logger;
            Root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            ModSettings.Open(Root,Config.ConfigFilePath);
            var shareRange=new AcceptableValueRange<float>(0f,1f);
            LootShare = ModSettings.Bind("Loot","MinecraftShare",.25f,"普通箱本模组食物占比：0 禁用，0.25=25%，1=仅本模组食品。不是每箱必出概率；仅房主影响后续生成。","Ordinary chest mod-food share: 0=off, 0.25=25%, 1=mod foods only. Not a per-chest guarantee. Host controls future generation.","通常の箱の食品比率：0=無効、0.25=25%、1=MOD食品のみ。箱ごとの保証ではなく、ホストが今後の生成に適用。",shareRange);
            CursedLootShare = ModSettings.Bind("Loot","CursedLuggageMinecraftShare",.35f,"诅咒箱占比，0~1；0.35=35%。仅房主影响后续生成。","Cursed chest share, 0..1; 0.35=35%. Host affects future generation.","呪いの箱の比率、0～1。0.35=35%。ホストが今後の生成に適用。",shareRange);
            ClownLootShare = ModSettings.Bind("Loot","ClownLuggageMinecraftShare",.30f,"小丑箱占比，0~1；0.30=30%。仅房主影响后续生成。","Clown chest share, 0..1; 0.30=30%. Host affects future generation.","ピエロの箱の比率、0～1。0.30=30%。ホストが今後の生成に適用。",shareRange);
            AirportEffects = ModSettings.Bind("Effects","AllowFoodEffectsInAirport",true,"在机场启用本模组食品状态效果，方便测试。","Allow this mod's food status effects in the airport for testing.","テスト用に空港内でも本MODの食品効果を有効化。");
            AppleAftereffects = ModSettings.Bind("Effects","GoldenAppleAftereffects",true,"金苹果无敌后睡意在6秒内逐渐升满，附魔为12秒；实际入睡后再维持6／12秒。关闭取消待触发或进行中的后遗症。","After invincibility, drowsiness rises to maximum over 6s / 12s, then sleep lasts 6s / 12s from actual unconsciousness. Disabling cancels pending/active ramps.","無敵終了後、6秒／12秒かけて眠気が最大まで上昇し、入眠後6秒／12秒維持。無効化で保留中・進行中の反動を中止。");
            GripTemplate = ModSettings.Bind("Visuals","GripTemplate","TrailMix","握持模板：TrailMix 混合坚果，SmallEgg 小煎蛋；修改后需重启游戏。","Grip: TrailMix or SmallEgg. Restart the game after changing.","持ち方：TrailMix または SmallEgg。変更後は再起動。",new AcceptableValueList<string>("TrailMix","SmallEgg"));
            ShowInfo = ModSettings.Bind("UI","ShowFoodInformation",true,"显示手持食品效果、重量和烹饪增量。","Show held-food effects, weight and next-cook deltas.","手持ち食品の効果・重量・次回調理の変化を表示。");
            DetailedInfo = ModSettings.Bind("UI","DetailedFoodInformation",false,"额外显示烧焦和烧毁次数。随机效果列表始终显示。","Also show charring/destruction thresholds. Random effect lists are always shown.","焦げ・燃え尽きの回数も表示。ランダム効果の一覧は常に表示。");
            ShowEffectTimers = ModSettings.Bind("UI","ShowEffectTimers",true,"显示本模组临时效果倒计时及剩余护盾。","Show mod effect timers and remaining shield points.","MOD効果の残り時間と残りシールドを表示。");
            L10n.Language=L10n.Resolve(ModSettings.Language.Value,LocalizedText.CURRENT_LANGUAGE.ToString());
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
                ModSettings.BindFoods(Foods.Values);
                ValidateSettings();
                settingsWindow=gameObject.AddComponent<SettingsWindow>();
                harmony = new Harmony(Id); harmony.PatchAll();
            }
            catch (Exception e) { failed = true; Log.LogError("Minecraft Foods initialization failed: " + e); if(Environment.GetCommandLineArgs().Contains("--mcfoods-audit"))Application.Quit(2); }
        }
        void Update()
        {
            RefreshLanguage();
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
                go.AddComponent<FoodAudio>();
                item.itemTags = Item.ItemTags.PackagedFood;
                item.UIData = new Item.ItemUIData { itemName=f.name, icon=Visuals.IconFor(f.texture), mainInteractPrompt=f.id == "milk_bucket" || f.id == "honey_bottle" ? "饮用" : "吃", secondaryInteractPrompt="喂食", hasSecondInteract=true, hideSecondInteract=false, hideFuel=f.uses==1 };
                LocalizeItem(item,f);
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
            var diagnostic = new List<string> { "Minecraft Foods " + ModVersion.Value, "Grip template: " + template.name, "Config: " + ModSettings.File.ConfigFilePath };
            foreach (var item in Registered)
            {
                var m=item.GetComponent<FoodMarker>();
                diagnostic.Add($"{m.foodId} ID={item.itemID} L={item.transform.Find("Hand_L")?.localPosition} R={item.transform.Find("Hand_R")?.localPosition} model={m.visual.transform.localPosition} size={m.meshSize:F3}");
            }
            foreach (var pool in LootData.AllSpawnWeightData.Where(x=>x.Key.ToString().StartsWith("Luggage")))
            {
                double total=pool.Value.Values.Sum(x=>(double)x), mc=Registered.Sum(i=>pool.Value.TryGetValue(i.itemID,out int w)?(double)w:0);
                diagnostic.Add($"{pool.Key}: Minecraft {mc}/{total} ({(total>0?mc/total:0):P2})");
                foreach(var tier in Registered.GroupBy(i=>LootPatch.LootTier(i.GetComponent<FoodMarker>().Definition,pool.Key.ToString())))
                {
                    double weight=tier.Sum(i=>pool.Value.TryGetValue(i.itemID,out int w)?(double)w:0);
                    diagnostic.Add($"  {tier.Key}: {weight} ({(mc>0?weight/mc:0):P2} within Minecraft foods)");
                }
            }
            File.WriteAllLines(Path.Combine(Paths.BepInExRootPath,"MinecraftFoods-diagnostics.txt"),diagnostic);
        }
        private void ValidateSettings()
        {
            foreach(var pair in ModSettings.File)
                if(pair.Value.BoxedValue is float value && (float.IsNaN(value) || float.IsInfinity(value)))
                {pair.Value.BoxedValue=pair.Value.DefaultValue;Log.LogWarning("Reset non-finite setting: "+pair.Key);}
        }
        internal void ApplySettings()
        {
            ValidateSettings();RefreshLanguage();
            if(ready && SettingsWindow.Host)
            {
                LootData.PopulateLootData();
                Log.LogInfo("Loot settings rebuilt for future generation. Existing contents are unchanged.");
            }
        }
        internal static void LocalizeItem(Item item,FoodDefinition f)
        {
            item.UIData.itemName=L10n.FoodName(f.id,f.name);
            item.UIData.mainInteractPrompt=f.id=="milk_bucket" || f.id=="honey_bottle"?L10n.T("饮用","Drink","飲む"):L10n.T("吃","Eat","食べる");
            item.UIData.secondaryInteractPrompt=L10n.T("喂食","Feed","食べさせる");
        }
        private void RefreshLanguage()
        {
            if(ModSettings.Language==null)return;
            string language=L10n.Resolve(ModSettings.Language.Value,LocalizedText.CURRENT_LANGUAGE.ToString());
            if(language==L10n.Language)return;
            L10n.Language=language;
            foreach(var item in Registered)LocalizeItem(item,item.GetComponent<FoodMarker>().Definition);
            foreach(var marker in Object.FindObjectsByType<FoodMarker>(FindObjectsSortMode.None))if(marker.Definition!=null)LocalizeItem(marker.GetComponent<Item>(),marker.Definition);
            foreach(var ui in Object.FindObjectsByType<InventoryItemUI>(FindObjectsSortMode.None))ui.UpdateNameText();
        }
        static Rarity RarityFor(string rarity) => rarity == "epic" ? Rarity.Mythic : rarity == "rare" ? Rarity.Rare : rarity == "uncommon" ? Rarity.Uncommon : Rarity.Common;
        void InstallInfoIntegration()
        {
            var type=AccessTools.TypeByName("ItemInfoDisplay.Plugin");
            var method=type==null?null:AccessTools.Method(type,"ProcessItemGameObject",new[]{typeof(Item)});
            var field=type==null?null:AccessTools.Field(type,"itemInfoDisplayTextMesh");
            if(method==null || field==null || !field.IsStatic || AccessTools.Property(field.FieldType,"text")?.CanWrite!=true) return;
            harmony.Patch(method,prefix:new HarmonyMethod(typeof(Plugin),nameof(AfterExternalInfo)));
            externalInfo=true;
        }
        static bool AfterExternalInfo(Item __0)
        {
            var marker=__0?__0.GetComponent<FoodMarker>():null;
            if(!marker || !ShowInfo.Value) return true;
            var field=AccessTools.Field(AccessTools.TypeByName("ItemInfoDisplay.Plugin"),"itemInfoDisplayTextMesh");
            var text=field?.GetValue(null); if(text==null){Instance.externalInfo=false;return true;}
            Instance.externalInfo=true;
            AccessTools.Property(text.GetType(),"text")?.SetValue(text,Describe(marker));
            return false;
        }
        internal static string Describe(FoodMarker m)
        {
            return FoodInformation.Describe(m);
        }

        void OnGUI()
        {
            if(!ready)return;
            if(settingsWindow && settingsWindow.isOpen)return;
            var c=Character.localCharacter; var m=c && c.data.currentItem ? c.data.currentItem.GetComponent<FoodMarker>():null;
            if(!c || c.data.dead)return;
            if(infoStyle==null) infoStyle=new GUIStyle(GUI.skin.label) { richText=true, wordWrap=true, font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Yu Gothic UI","Arial"},18), normal={textColor=new Color(1,.96f,.8f)} };
            infoStyle.fontSize=Mathf.Clamp(Mathf.RoundToInt(Screen.height/65f),12,25);
            if(ShowEffectTimers.Value)
            {
                var state=c.GetComponent<FoodPlayerState>();string timers=state?state.CountdownText():"";
                if(timers.Length>0)
                {
                    float w=Screen.width*.29f,h=infoStyle.CalcHeight(new GUIContent(timers),w);
                    GUI.Label(new Rect(Screen.width*.025f,Screen.height*.16f,w,h),timers,infoStyle);
                }
            }
            if(!m || !ShowInfo.Value || externalInfo)return;
            string description=Describe(m);
            float width=Screen.width*.25f;
            float height=infoStyle.CalcHeight(new GUIContent(description),width-24);
            float visible=Mathf.Min(height,Screen.height*.52f);
            var rect=new Rect(Screen.width*.64f,Mathf.Max(20,Screen.height*.78f-visible),width,visible);
            if(height>visible)
            {
                infoScroll=GUI.BeginScrollView(rect,infoScroll,new Rect(0,0,width-24,height));
                GUI.Label(new Rect(0,0,width-24,height),description,infoStyle);GUI.EndScrollView();
            }
            else{infoScroll=Vector2.zero;GUI.Label(rect,description,infoStyle);}
        }
    }
}
