using System;
using System.Numerics;
using Dalamud.Game;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SimpleTweaksPlugin.Enums;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using SimpleTweaksPlugin.TweakSystem;
using SimpleTweaksPlugin.Utility;

namespace SimpleTweaksPlugin {
    public partial class UiAdjustmentsConfig {
        public bool ShouldSerializeShiftTargetCastBarText() => ShiftTargetCastBarText != null;
        public ShiftTargetCastBarText.Config ShiftTargetCastBarText = null;
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment {
    public unsafe class ShiftTargetCastBarText : UiAdjustments.SubTweak {

        public class Config : TweakConfig {
            public int Offset = 8;
            public Alignment NameAlignment = Alignment.BottomRight;
            public bool ShowCastTimeLeft;
            public Alignment CastTimeAlignment = Alignment.TopLeft;
            public int TimerOffset = 8;
        }
        
        public Config LoadedConfig { get; private set; }
        
        public override string Name => "调整目标咏唱栏文字位置";
        public override string Description => "调整目标咏唱栏文字位置以方便阅读";
        
        private readonly Vector2 buttonSize = new Vector2(26, 22);

        protected override DrawConfigDelegate DrawConfigTree => (ref bool changed) => {
            var bSize = buttonSize * ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextItemWidth(90 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputInt($"###{GetType().Name}_Offset", ref LoadedConfig.Offset)) {
                if (LoadedConfig.Offset > MaxOffset) LoadedConfig.Offset = MaxOffset;
                if (LoadedConfig.Offset < MinOffset) LoadedConfig.Offset = MinOffset;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2));
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)FontAwesomeIcon.ArrowUp}", bSize)) {
                LoadedConfig.Offset = 8;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("在进度条上方");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char) FontAwesomeIcon.CircleNotch}", bSize)) {
                LoadedConfig.Offset = 24;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("初始位置");

            
            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)FontAwesomeIcon.ArrowDown}", bSize)) {
                LoadedConfig.Offset = 32;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("在进度条下方");
            ImGui.PopStyleVar();
            ImGui.SameLine();
            ImGui.Text("技能名垂直偏移");

            changed |= ImGuiExt.HorizontalAlignmentSelector("技能名对齐方式", ref LoadedConfig.NameAlignment, VerticalAlignment.Bottom);

            ImGui.Checkbox("显示读条剩余时间", ref LoadedConfig.ShowCastTimeLeft);
            if (LoadedConfig.ShowCastTimeLeft)
            {
                
                changed |= ImGuiExt.HorizontalAlignmentSelector("读条剩余时间对齐方式", ref LoadedConfig.CastTimeAlignment, VerticalAlignment.Top);
                ImGui.SetNextItemWidth(90 * ImGui.GetIO().FontGlobalScale);
                if (ImGui.InputInt($"###Target Cast Time Left_Offset", ref LoadedConfig.TimerOffset)) {
                    if (LoadedConfig.TimerOffset > MaxOffset) LoadedConfig.TimerOffset = MaxOffset;
                    if (LoadedConfig.TimerOffset < MinOffset) LoadedConfig.TimerOffset = MinOffset;
                    changed = true;
                }
                ImGui.SameLine();
                ImGui.Text("读条剩余时间垂直偏移");
            }
        };

        public void OnFrameworkUpdate(Framework framework) {
            try {
                HandleBars();
            } catch (Exception ex) {
                Plugin.Error(this, ex);
            }
        }

        private void HandleBars(bool reset = false)
        {

            if (Service.ClientState.LocalPlayer == null) return;
            var focusTargetInfo = Common.GetUnitBase("_FocusTargetInfo");
            if (focusTargetInfo != null && focusTargetInfo->UldManager.NodeList != null && focusTargetInfo->UldManager.NodeListCount > 16 && (focusTargetInfo->IsVisible || reset)) {
                DoShift(focusTargetInfo->UldManager.NodeList[16]);
                if (LoadedConfig.ShowCastTimeLeft)
                {
                    AddCastTimeTextNode(focusTargetInfo, (AtkTextNode*)focusTargetInfo->UldManager.NodeList[16], focusTargetInfo->UldManager.NodeList[16]->IsVisible);
                }

            }


            var splitCastBar = Common.GetUnitBase("_TargetInfoCastBar");
            if (splitCastBar != null && splitCastBar->UldManager.NodeList != null && splitCastBar->UldManager.NodeListCount > 5 && (splitCastBar->IsVisible || reset)) {
                DoShift(splitCastBar->UldManager.NodeList[5]);
                if (LoadedConfig.ShowCastTimeLeft)
                {
                    AddCastTimeTextNode(splitCastBar,(AtkTextNode*)splitCastBar->UldManager.NodeList[5],splitCastBar->UldManager.NodeList[5]->IsVisible);
                }
                if (!reset) return;
            }

            var mainTargetInfo = Common.GetUnitBase("_TargetInfo");
            if (mainTargetInfo != null && mainTargetInfo->UldManager.NodeList != null && mainTargetInfo->UldManager.NodeListCount > 44 && (mainTargetInfo->IsVisible || reset)) {
                DoShift(mainTargetInfo->UldManager.NodeList[44]);
                if (LoadedConfig.ShowCastTimeLeft)
                {
                    AddCastTimeTextNode(mainTargetInfo,(AtkTextNode*)mainTargetInfo->UldManager.NodeList[44],mainTargetInfo->UldManager.NodeList[44]->IsVisible);
                }
            }
        }
        
        private const int MinOffset = 0;
        private const int MaxOffset = 48;

        private void DoShift(AtkResNode* node, bool reset = false) {
            if (node == null) return;
            var p = LoadedConfig.Offset;
            if (p < MinOffset) p = MinOffset;
            if (p > MaxOffset) p = MaxOffset;
            node->Height = reset ? (ushort) 24 : (ushort) p;
            var textNode = (AtkTextNode*) node;
            textNode->AlignmentFontType = reset ? (byte) AlignmentType.BottomRight : (byte) LoadedConfig.NameAlignment;
            if (reset) {
                UiHelper.SetPosition(node, 0, null);
                UiHelper.SetSize(node, 197, null);
            } else {
                UiHelper.SetPosition(node, 8, null);
                UiHelper.SetSize(node, 188, null);
            }
            
        }

        private const int TargetCastNodeId = 99990002;

        private void AddCastTimeTextNode(AtkUnitBase* unit, AtkTextNode* cloneTextNode, bool visible = false)
        {
            var textNode = (AtkTextNode*)GetNodeById(unit, TargetCastNodeId);
            
            if (textNode == null)
            {
                textNode = UiHelper.CloneNode(cloneTextNode);
                textNode->AtkResNode.NodeID = TargetCastNodeId;
                var newStrPtr = Common.Alloc(512);
                textNode->NodeText.StringPtr = (byte*) newStrPtr;
                textNode->NodeText.BufSize = 512;
                textNode->SetText("");
                UiHelper.ExpandNodeList(unit, 1);
                unit->UldManager.NodeList[unit->UldManager.NodeListCount++] = (AtkResNode*) textNode;

                var nextNode = (AtkResNode*)cloneTextNode;
                while (nextNode->PrevSiblingNode != null) nextNode = nextNode->PrevSiblingNode;

                textNode->AtkResNode.ParentNode = nextNode->ParentNode;
                textNode->AtkResNode.ChildNode = null;
                textNode->AtkResNode.NextSiblingNode = nextNode;
                textNode->AtkResNode.PrevSiblingNode = null;
                nextNode->PrevSiblingNode = (AtkResNode*) textNode;
                nextNode->ParentNode->ChildCount += 1;
            }

            if (!visible)
            {
                UiHelper.Hide(textNode);
            }
            else
            {
                textNode->AlignmentFontType = (byte)(0x26 + (byte)LoadedConfig.CastTimeAlignment);
                textNode->AtkResNode.Height = (ushort) LoadedConfig.TimerOffset;
                //UiHelper.SetPosition(textNode, PluginConfig.UiAdjustments.ShiftTargetCastBarText.CastTimeOffsetX,
                //    PluginConfig.UiAdjustments.ShiftTargetCastBarText.CastTimeOffsetY);
                //UiHelper.SetSize(textNode, cloneTextNode->AtkResNode.Width, cloneTextNode->AtkResNode.Height);
                textNode->FontSize = 15;//(byte) PluginConfig.UiAdjustments.ShiftTargetCastBarText.CastTimeFontSize;
                textNode->SetText(GetTargetCastTime().ToString("00.00"));
                UiHelper.Show(textNode);
            }

        }


        private float GetTargetCastTime()
        {
            if (Service.ClientState.LocalPlayer == null ||
                Service.Targets.Target == null)
                return 0;
            var target = Service.Targets.Target;
            if (target is BattleChara)
            {
                var castTime = ((BattleChara)target).CurrentCastTime;
                var totalCastTime =((BattleChara)target).TotalCastTime;
                return totalCastTime - castTime;
            }

            return 0;
        }

        
        private static AtkResNode* GetNodeById(AtkUnitBase* compBase, uint id)
        {
            if (compBase == null) return null;
            if ((compBase->UldManager.Flags1 & 1) == 0 || id <= 0) return null;
            var count = compBase->UldManager.NodeListCount;
            for (var i = 0; i < count; i++)
            {
                
                var node = compBase->UldManager.NodeList[i];
                //SimpleLog.Information(i+"@"+node->NodeID);
                if (node->NodeID == id) return node;
            }
            return null;
        }

        public override void Enable() {
            if (Enabled) return;
            LoadedConfig = LoadConfig<Config>() ?? PluginConfig.UiAdjustments.ShiftTargetCastBarText ?? new Config();
            Service.Framework.Update += OnFrameworkUpdate;
            Enabled = true;
        }

        public override void Disable() {
            if (!Enabled) return;
            SaveConfig(LoadedConfig);
            PluginConfig.UiAdjustments.ShiftTargetCastBarText = null;
            Service.Framework.Update -= OnFrameworkUpdate;
            SimpleLog.Debug($"[{GetType().Name}] Reset");
            HandleBars(true);
            Enabled = false;
        }

        public override void Dispose() {
            Service.Framework.Update -= OnFrameworkUpdate;
            Enabled = false;
            Ready = false;
        }
    }
}