using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BepInEx.Configuration;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MinecraftFoods
{
    public sealed class SettingsWindow : MenuWindow
    {
        public override bool openOnStart=>false;
        public override bool autoHideOnClose=>false;
        public override bool selectOnOpen=>false;
        public override bool closeOnPause=>true;
        private readonly Dictionary<ConfigEntryBase,string> draft=new Dictionary<ConfigEntryBase,string>();
        private Vector2 scroll;
        private int tab;
        private string message="";
        private GUISkin skin;
        private Rect window;
        protected override void Start(){} // StartClosed would deactivate the plugin itself.
        protected override void Update()
        {
            base.Update();
            var keyboard=Keyboard.current;
            if(keyboard==null)return;
            if(keyboard.f10Key.wasPressedThisFrame)Toggle();
            else if(isOpen && keyboard.escapeKey.wasPressedThisFrame)ClosePanel();
        }
        internal static bool Host=>!PhotonNetwork.InRoom || PhotonNetwork.IsMasterClient;
        void Toggle(){if(isOpen)ClosePanel();else AccessTools.Method(typeof(MenuWindow),"Open").Invoke(this,null);}
        void ClosePanel()=>AccessTools.Method(typeof(MenuWindow),"Close").Invoke(this,null);
        protected override void OnOpen()
        {
            Read();message="";scroll=Vector2.zero;
            var c=Character.localCharacter;var item=c?c.data.currentItem:null;
            if(item){if(item.isUsingPrimary)item.CancelUsePrimary();if(item.isUsingSecondary)item.CancelUseSecondary();}
        }
        void OnDisable(){if(isOpen)ClosePanel();}
        protected override void OnClose(){if(Character.localCharacter)Character.localCharacter.input.pauseWasPressed=false;}
        void Read(){draft.Clear();foreach(var pair in ModSettings.File)draft[pair.Value]=Convert.ToString(pair.Value.BoxedValue,CultureInfo.InvariantCulture);}
        string Label(ConfigEntryBase e)
        {
            if(e.Definition.Section.StartsWith("FoodWeights.") && Plugin.Foods.TryGetValue(e.Definition.Key,out var f))return L10n.FoodName(f.id,f.name)+" ("+f.id+")";
            switch(e.Definition.Key)
            {
                case "Language":return L10n.T("显示语言","Language","表示言語");
                case "MinecraftShare":return L10n.T("普通箱食物占比","Ordinary chest share","通常の箱の比率");
                case "CursedLuggageMinecraftShare":return L10n.T("诅咒箱食物占比","Cursed chest share","呪いの箱の比率");
                case "ClownLuggageMinecraftShare":return L10n.T("小丑箱食物占比","Clown chest share","ピエロの箱の比率");
                case "common":return L10n.T("常见权重","Common weight","コモンの重み");
                case "uncommon":return L10n.T("少见权重","Uncommon weight","アンコモンの重み");
                case "rare":return L10n.T("稀有权重","Rare weight","レアの重み");
                case "special":return L10n.T("特殊权重","Special weight","特殊の重み");
                case "ShowFoodInformation":return L10n.T("显示食物说明","Show food information","食品説明を表示");
                case "DetailedFoodInformation":return L10n.T("显示烹饪阈值详情","Show cooking thresholds","調理回数の詳細");
                case "ShowEffectTimers":return L10n.T("显示效果倒计时","Show effect timers","効果タイマーを表示");
                case "GripTemplate":return L10n.T("握持模板（需重启）","Grip template (restart)","持ち方（再起動が必要）");
                case "GoldenAppleAftereffects":return L10n.T("启用金苹果睡意后遗症","Golden apple drowsiness cost","金のリンゴの眠気反動");
                case "AllowFoodEffectsInAirport":return L10n.T("机场内启用食物效果","Food effects in airport","空港内の食品効果");
                default:return e.Definition.Key;
            }
        }
        string SectionTitle(string section)
        {
            if(section=="UI")return L10n.T("显示与语言","Display & language","表示と言語");
            if(section=="Effects")return L10n.T("食物效果","Food effects","食品効果");
            if(section=="Visuals")return L10n.T("握持外观","Grip appearance","持ち方");
            if(section=="Loot")return L10n.T("箱子食物占比","Chest food shares","箱の食品比率");
            string pool=section.EndsWith("Cursed")?L10n.T("诅咒箱","Cursed luggage","呪いの箱"):section.EndsWith("Clown")?L10n.T("小丑箱","Clown luggage","ピエロの箱"):L10n.T("普通箱","Ordinary luggage","通常の箱");
            return pool+" — "+(section.StartsWith("Tiers.")?L10n.T("稀有度权重","Tier weights","レア度の重み"):L10n.T("食品权重倍率","Food multipliers","食品の重み倍率"));
        }
        void OnGUI()
        {
            if(!isOpen)return;
            if(!skin)
            {
                skin=Instantiate(GUI.skin);skin.font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Yu Gothic UI","Arial"},16);
                skin.label.wordWrap=true;skin.label.richText=true;
                skin.label.fontSize=skin.button.fontSize=skin.toggle.fontSize=skin.textField.fontSize=16;
            }
            var previous=GUI.skin;GUI.skin=skin;
            try
            {
                float width=Mathf.Min(900,Screen.width-40),height=Mathf.Min(750,Screen.height-40);
                window=new Rect((Screen.width-width)/2,(Screen.height-height)/2,width,height);
                GUI.ModalWindow(926126,window,Draw,"Minecraft Foods — "+L10n.T("设置 [F10]","Settings [F10]","設定 [F10]"));
            }
            finally{GUI.skin=previous;}
        }
        void Draw(int id)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(L10n.T("设置保存在插件目录 settings.cfg。保存后影响后续生成；已生成箱子不会重抽。",
                "Saved to settings.cfg beside the plugin. Loot changes affect future generation, not existing contents.","プラグイン横の settings.cfg に保存。生成済みの中身は変更しません。"));
            if(!Host)GUILayout.Label(L10n.T("当前为客机：箱子掉落由房主设置。","Client: chest loot is controlled by the host.","クライアント：箱の設定はホストが管理します。"));
            int next=GUILayout.Toolbar(tab,new[]{L10n.T("常规","General","一般"),L10n.T("箱子与稀有度","Chests & tiers","箱とレア度"),L10n.T("逐物品权重","Per-food weights","食品別の重み")});
            if(next!=tab){tab=next;scroll=Vector2.zero;}
            scroll=GUILayout.BeginScrollView(scroll,GUILayout.Height(window.height-220));
            string section="";
            foreach(var pair in ModSettings.File.OrderBy(x=>x.Key.Section).ThenBy(x=>x.Key.Key))
            {
                var e=pair.Value;bool food=e.Definition.Section.StartsWith("FoodWeights."),loot=food || e.Definition.Section=="Loot" || e.Definition.Section.StartsWith("Tiers.");
                if(tab==0 && loot || tab==1 && (!loot || food) || tab==2 && !food)continue;
                if(section!=e.Definition.Section){section=e.Definition.Section;GUILayout.Space(12);GUILayout.Label("<b>"+SectionTitle(section)+"</b>");}
                bool enabled=GUI.enabled;GUI.enabled=!loot || Host;
                GUILayout.BeginHorizontal();GUILayout.Label(Label(e),GUILayout.Width(window.width-260));
                if(e.SettingType==typeof(bool))draft[e]=GUILayout.Toggle(draft[e]=="True"," ").ToString();
                else if(e.SettingType==typeof(string))
                {
                    string[] values=e.Definition.Key=="Language"?new[]{"Auto","zh-CN","en","ja"}:new[]{"TrailMix","SmallEgg"};
                    if(GUILayout.Button(draft[e],GUILayout.Width(160)))draft[e]=values[(Array.IndexOf(values,draft[e])+1)%values.Length];
                }
                else draft[e]=GUILayout.TextField(draft[e],32,GUILayout.Width(160));
                GUILayout.EndHorizontal();GUILayout.Label(ModSettings.Help(e));GUI.enabled=enabled;GUILayout.Space(7);
            }
            GUILayout.EndScrollView();GUILayout.Label(message);
            GUILayout.BeginHorizontal();
            if(GUILayout.Button(L10n.T("应用并保存","Apply & save","適用して保存")))Apply();
            if(GUILayout.Button(L10n.T("重新载入文件","Reload file","ファイルを再読込")))
            {
                try{ModSettings.File.Reload();Plugin.Instance.ApplySettings();Read();message=L10n.T("已载入文件。","File reloaded.","再読込しました。");}
                catch(Exception ex){message=ex.Message;Plugin.Log.LogError(ex);}
            }
            if(GUILayout.Button(L10n.T("恢复默认（待保存）","Defaults (not saved)","既定値（未保存）")))
                foreach(var e in draft.Keys.ToArray())draft[e]=Convert.ToString(e.DefaultValue,CultureInfo.InvariantCulture);
            if(GUILayout.Button(L10n.T("关闭","Close","閉じる")))ClosePanel();
            GUILayout.EndHorizontal();GUILayout.EndVertical();
        }
        void Apply()
        {
            try
            {
                var parsed=new Dictionary<ConfigEntryBase,object>();
                foreach(var pair in draft)
                {
                    var e=pair.Key;
                    if(!Host && (e.Definition.Section=="Loot" || e.Definition.Section.StartsWith("Tiers.") || e.Definition.Section.StartsWith("FoodWeights.")))continue;
                    object value=pair.Value;
                    if(e.SettingType==typeof(float))
                    {
                        if(!float.TryParse(pair.Value,NumberStyles.Float,CultureInfo.InvariantCulture,out float v) || float.IsNaN(v) || float.IsInfinity(v))throw new FormatException(Label(e)+": "+L10n.T("请输入有效数字（小数点用 .）","Enter a finite number (use . for decimals)","有効な数値を入力（小数点は .）"));
                        value=v;
                    }
                    else if(e.SettingType==typeof(bool))value=bool.Parse(pair.Value);
                    if(e.Description.AcceptableValues!=null && !e.Description.AcceptableValues.IsValid(value))throw new FormatException(Label(e)+": "+e.Description.AcceptableValues.ToDescriptionString());
                    parsed[e]=value;
                }
                foreach(var pair in parsed)pair.Key.BoxedValue=pair.Value;
                Plugin.Instance.ApplySettings();ModSettings.File.Save();Read();
                message=L10n.T("已保存。握持模板需重启；其余设置已应用。","Saved. Grip changes require restart; other settings applied.","保存しました。持ち方は再起動後、他の設定は適用済みです。");
            }
            catch(Exception ex){message=ex.Message;Plugin.Log.LogWarning(ex.Message);}
        }
    }
}
