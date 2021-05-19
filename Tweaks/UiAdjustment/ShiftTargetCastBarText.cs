﻿using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;
using Dalamud.Game.Internal;
using Dalamud.Interface;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SimpleTweaksPlugin.Helper;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using static SimpleTweaksPlugin.Tweaks.UiAdjustments.Step;
using Addon = Dalamud.Game.Internal.Gui.Addon.Addon;

namespace SimpleTweaksPlugin
{
    public partial class UiAdjustmentsConfig
    {
        public ShiftTargetCastBarText.Config ShiftTargetCastBarText = new ShiftTargetCastBarText.Config();
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment
{
    public class ShiftTargetCastBarText : UiAdjustments.SubTweak
    {

        public class Config
        {
            public int Offset = 8;
            public bool EnableCastTime = false;
        }

        public override string Name => "调整目标咏唱栏文字位置";
        public override string Description => "调整目标咏唱栏文字位置以方便阅读";

        private readonly Vector2 buttonSize = new Vector2(26, 22);

        protected override DrawConfigDelegate DrawConfigTree => (ref bool changed) => {
            var bSize = buttonSize * ImGui.GetIO().FontGlobalScale;
            ImGui.SetNextItemWidth(90 * ImGui.GetIO().FontGlobalScale);
            if (ImGui.InputInt($"###{GetType().Name}_Offset", ref PluginConfig.UiAdjustments.ShiftTargetCastBarText.Offset)) {
                if (PluginConfig.UiAdjustments.ShiftTargetCastBarText.Offset > MaxOffset) PluginConfig.UiAdjustments.ShiftTargetCastBarText.Offset = MaxOffset;
                if (PluginConfig.UiAdjustments.ShiftTargetCastBarText.Offset < MinOffset) PluginConfig.UiAdjustments.ShiftTargetCastBarText.Offset = MinOffset;
                changed = true;
            }
            ImGui.SameLine();
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(2));
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)FontAwesomeIcon.ArrowUp}", bSize)) {
                PluginConfig.UiAdjustments.ShiftTargetCastBarText.Offset = 8;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("显示在进度条上方");

            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)FontAwesomeIcon.CircleNotch}", bSize)) {
                PluginConfig.UiAdjustments.ShiftTargetCastBarText.Offset = 24;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("初始位置");


            ImGui.SameLine();
            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button($"{(char)FontAwesomeIcon.ArrowDown}", bSize)) {
                PluginConfig.UiAdjustments.ShiftTargetCastBarText.Offset = 32;
                changed = true;
            }
            ImGui.PopFont();
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("显示在进度条下方");
            ImGui.PopStyleVar();
            ImGui.SameLine();
            ImGui.Text("垂直偏移量");

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Checkbox($"###{GetType().Name}_EnableCastTime", ref PluginConfig.UiAdjustments.ShiftTargetCastBarText.EnableCastTime)) {
                changed = true;
            }
            ImGui.PopFont();
            ImGui.SameLine();
            ImGui.Text("显示咏唱时间");
        };

        public void OnFrameworkUpdate(Framework framework) {
            try {
                HandleBars(framework);
            }
            catch (Exception ex) {
                Plugin.Error(this, ex);
            }
        }

        private void HandleBars(Framework framework, bool reset = false) {

            var focusTargetInfo = framework.Gui.GetAddonByName("_FocusTargetInfo", 1);
            if (focusTargetInfo != null && (focusTargetInfo.Visible || reset)) {
                HandleFocusTargetInfo(focusTargetInfo, reset);
            }


            var seperatedCastBar = framework.Gui.GetAddonByName("_TargetInfoCastBar", 1);
            if (seperatedCastBar != null && (seperatedCastBar.Visible || reset)) {
                HandleSeperatedCastBar(seperatedCastBar, reset);
                if (!reset) return;
            }

            var mainTargetInfo = framework.Gui.GetAddonByName("_TargetInfo", 1);
            if (mainTargetInfo != null && (mainTargetInfo.Visible || reset)) {
                HandleMainTargetInfo(mainTargetInfo, reset);
            }
        }

        private unsafe void HandleSeperatedCastBar(Addon addon, bool reset = false) {
            var addonStruct = (AtkUnitBase*)(addon.Address);
            if (addonStruct->RootNode == null) return;
            var rootNode = addonStruct->RootNode;
            if (rootNode->ChildNode == null) return;
            var child = rootNode->ChildNode;
            DoShift(child, reset);
            if (!PluginConfig.UiAdjustments.ShiftTargetCastBarText.EnableCastTime)
                return;
            var textNode = (AtkTextNode*)addonStruct->UldManager.NodeList[5];
            AddCastTimeTextNode(addonStruct, textNode, !textNode->AtkResNode.IsVisible);
        }

        private unsafe void HandleMainTargetInfo(Addon addon, bool reset = false) {
            var addonStruct = (AtkUnitBase*)(addon.Address);
            if (addonStruct->RootNode == null) return;


            var rootNode = addonStruct->RootNode;
            if (rootNode->ChildNode == null) return;
            var child = rootNode->ChildNode;
            for (var i = 0; i < 8; i++) {
                if (child->PrevSiblingNode == null) return;
                child = child->PrevSiblingNode;
            }

            DoShift(child, reset);
        }

        private unsafe void HandleFocusTargetInfo(Addon addon, bool reset = false) {
            var addonStruct = (AtkUnitBase*)(addon.Address);
            if (addonStruct->RootNode == null) return;

            var rootNode = addonStruct->RootNode;
            if (rootNode->ChildNode == null) return;
            var child = rootNode->ChildNode;
            for (var i = 0; i < 6; i++) {
                if (child->PrevSiblingNode == null) return;
                child = child->PrevSiblingNode;
            }

            DoShift(child, reset);

        }

        private const int MinOffset = 0;
        private const int MaxOffset = 48;

        private unsafe void DoShift(AtkResNode* node, bool reset = false) {
            if (node == null) return;
            if (node->ChildCount != 5) return; // Should have 5 children
            var skillTextNode = UiAdjustments.GetResNodeByPath(node, Child, Previous, Previous, Previous);
            if (skillTextNode == null) return;
            var p = PluginConfig.UiAdjustments.ShiftTargetCastBarText.Offset;
            if (p < MinOffset) p = MinOffset;
            if (p > MaxOffset) p = MaxOffset;
            Marshal.WriteInt16(new IntPtr(skillTextNode), 0x92, reset ? (short)24 : (short)p);
        }

        private const int TargetCastNodeID = 99990002;

        private unsafe void AddCastTimeTextNode(AtkUnitBase* Unit, AtkTextNode* cloneTextNode, bool reset = false) {
            AtkTextNode* textNode = null;
            for (var i = 7; i < Unit->UldManager.NodeListCount; i++) {
                var node = Unit->UldManager.NodeList[i];
                if (node->Type == NodeType.Text && node->NodeID == TargetCastNodeID) {
                    textNode = (AtkTextNode*)node;
                    break;
                }
            }

            if (textNode == null) {
                textNode = UiHelper.CloneNode(cloneTextNode);
                textNode->AtkResNode.NodeID = TargetCastNodeID;
                var newStrPtr = Common.Alloc(512);
                textNode->NodeText.StringPtr = (byte*)newStrPtr;
                textNode->NodeText.BufSize = 512;
                UiHelper.SetText(textNode, "");
                UiHelper.ExpandNodeList(Unit, 1);
                Unit->UldManager.NodeList[Unit->UldManager.NodeListCount++] = (AtkResNode*)textNode;

                var nextNode = Unit->UldManager.RootNode;

                textNode->AtkResNode.ParentNode = (AtkResNode*)Unit;
                textNode->AtkResNode.ChildNode = null;
                textNode->AtkResNode.PrevSiblingNode = null;
                textNode->AtkResNode.NextSiblingNode = nextNode;
            }

            if (reset) {
                UiHelper.Hide(textNode);
                return;
            }
            textNode->AlignmentFontType = (byte)39;
            UiHelper.SetPosition(textNode, 0, 0);
            UiHelper.SetSize(textNode, cloneTextNode->AtkResNode.Width, cloneTextNode->AtkResNode.Height);
            textNode->EdgeColor = cloneTextNode->EdgeColor;
            textNode->FontSize = cloneTextNode->FontSize;
            UiHelper.SetText(textNode, GetTargetCastTime().ToString("00.00"));
            UiHelper.Show(textNode);

        }


        private unsafe float GetTargetCastTime() {
            if (PluginInterface.ClientState.LocalPlayer == null || PluginInterface.ClientState.Targets.CurrentTarget == null)
                return 0;
            var target = PluginInterface.ClientState.Targets.CurrentTarget;
            if (target is Chara chara) {
                var CastTime = Marshal.PtrToStructure<float>((target.Address + Dalamud.Game.ClientState.Structs.ActorOffsets.CurrentCastTime));
                var TotalCastTime = Marshal.PtrToStructure<float>((target.Address + Dalamud.Game.ClientState.Structs.ActorOffsets.TotalCastTime));
                return TotalCastTime - CastTime;
            }
            return 0;
        }

        public override void Enable() {
            if (Enabled) return;
            PluginInterface.Framework.OnUpdateEvent += OnFrameworkUpdate;
            Enabled = true;
        }

        public override void Disable() {
            if (!Enabled) return;
            PluginInterface.Framework.OnUpdateEvent -= OnFrameworkUpdate;
            SimpleLog.Debug($"[{GetType().Name}] Reset");
            HandleBars(PluginInterface.Framework, true);
            Enabled = false;
        }

        public override void Dispose() {
            PluginInterface.Framework.OnUpdateEvent -= OnFrameworkUpdate;
            Enabled = false;
            Ready = false;
        }
    }
}
