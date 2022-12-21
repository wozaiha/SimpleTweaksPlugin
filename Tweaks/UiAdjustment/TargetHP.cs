using System;
using System.ComponentModel;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using SimpleTweaksPlugin.TweakSystem;
using SimpleTweaksPlugin.Utility;
using AlignmentType = FFXIVClientStructs.FFXIV.Component.GUI.AlignmentType;
using GameObject = Dalamud.Game.ClientState.Objects.Types.GameObject;

namespace SimpleTweaksPlugin {
    public partial class UiAdjustmentsConfig {
        public bool ShouldSerializeTargetHP() => TargetHP != null;
        public TargetHP.Configs TargetHP = null;
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment {
    public unsafe class TargetHP : UiAdjustments.SubTweak {
        public class Configs : TweakConfig {
            public DisplayFormat DisplayFormat = DisplayFormat.OneDecimalPrecision;
            public Vector2 Position = new Vector2(0);
            public bool UseCustomColor = false;
            public Vector4 CustomColor = new Vector4(1);
            public byte FontSize = 14;
            public bool HideAutoAttack = false;
            
            public bool NoFocus;
            public Vector2 FocusPosition = new Vector2(0);
            public bool EnableDistance = false;
            public bool EnableEffectiveDistance = false;
            public bool FocusUseCustomColor = false;
            public Vector4 FocusCustomColor = new Vector4(1);
            public byte FocusFontSize = 14;
        }
        
        public enum DisplayFormat {
            [Description("完整数值")] 
            FullNumber,
            [Description("带分隔符的完整值 (5,555,555)")]
            FullNumberSeparators,
            [Description("简写 (5K, 5M)")]
            ZeroDecimalPrecision,
            [Description("一位小数 (5.5K, 5.5M)")]
            OneDecimalPrecision,
            [Description("两位小数 (5.55K, 5.55M)")]
            TwoDecimalPrecision,
        }
        
        public Configs Config { get; private set; }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) => {
            if (ImGui.BeginCombo("显示格式###targetHpFormat", Config.DisplayFormat.GetDescription())) {
                foreach (var v in (DisplayFormat[])Enum.GetValues(typeof(DisplayFormat))) {
                    if (!ImGui.Selectable($"{v.GetDescription()}##targetHpFormatSelect", Config.DisplayFormat == v)) continue;
                    Config.DisplayFormat = v;
                    hasChanged = true;
                }
                ImGui.EndCombo();
            }

            ImGui.SetNextItemWidth(150);
            hasChanged |= ImGui.InputFloat("水平偏移##AdjustTargetHPPositionX", ref Config.Position.X, 1, 5, "%.0f");
            ImGui.SetNextItemWidth(150);
            hasChanged |= ImGui.InputFloat("垂直偏移##AdjustTargetHPPositionY", ref Config.Position.Y, 1, 5, "%0.f");
            hasChanged |= ImGuiExt.InputByte("字体大小##TargetHPFontSize", ref Config.FontSize);
            hasChanged |= ImGui.Checkbox("自定义颜色##TargetHPUseCustomColor", ref Config.UseCustomColor);
            if (Config.UseCustomColor) {
                ImGui.SameLine();
                hasChanged |= ImGui.ColorEdit4("##TargetHPCustomColor", ref Config.CustomColor);
            }
            hasChanged |= ImGui.Checkbox("Hide Auto Attack Icon", ref Config.HideAutoAttack);
            
            ImGui.Dummy(new Vector2(5) * ImGui.GetIO().FontGlobalScale);
            hasChanged |= ImGui.Checkbox("不显示焦点目标HP", ref Config.NoFocus);

            if (!Config.NoFocus) {
                ImGui.SetNextItemWidth(150);
                hasChanged |= ImGui.InputFloat("焦点目标水平偏移##AdjustTargetHPFocusPositionX", ref Config.FocusPosition.X, 1, 5, "%.0f");
                ImGui.SetNextItemWidth(150);
                hasChanged |= ImGui.InputFloat("焦点目标垂直偏移##AdjustTargetHPFocusPositionY", ref Config.FocusPosition.Y, 1, 5, "%0.f");
                hasChanged |= ImGuiExt.InputByte("字体大小##TargetHPFocusFontSize", ref Config.FocusFontSize);
                hasChanged |= ImGui.Checkbox("自定义颜色##TargetHPFocusUseCustomColor", ref Config.FocusUseCustomColor);
                if (Config.FocusUseCustomColor) {
                    ImGui.SameLine();
                    hasChanged |= ImGui.ColorEdit4("##TargetHPFocusCustomColor", ref Config.FocusCustomColor);
                }
            }
            hasChanged |= ImGui.Checkbox("显示与目标和的距离", ref Config.EnableDistance);
            hasChanged |= ImGui.Checkbox("显示与目标的有效距离", ref Config.EnableEffectiveDistance);

        };
        
        public override string Name => "目标HP";
        public override string Description => "显示目标的精确HP(或简化后的数值)";

        public override void Enable() {
            Config = LoadConfig<Configs>() ?? PluginConfig.UiAdjustments.TargetHP ?? new Configs();
            Service.Framework.Update += FrameworkUpdate;
            base.Enable();
        }

        public override void Disable() {
            SaveConfig(Config);
            PluginConfig.UiAdjustments.TargetHP = null;
            Service.Framework.Update -= FrameworkUpdate;
            Update(true);
            base.Disable();
        }

        private void FrameworkUpdate(Framework framework) {
            try {
                Update();
            } catch(Exception ex) {
                SimpleLog.Error(ex);
            }
        }

        private void Update(bool reset = false) {
            var target = Service.Targets.SoftTarget ?? Service.Targets.Target;
            if (target != null || reset) {
                var ui = Common.GetUnitBase("_TargetInfo", 1);
                if (ui != null && (ui->IsVisible || reset)) {
                    UpdateMainTarget(ui, target, reset);
                }
                
                var splitUi = Common.GetUnitBase("_TargetInfoMainTarget", 1);
                if (splitUi != null && (splitUi->IsVisible || reset)) {
                    UpdateMainTargetSplit(splitUi, target, reset);
                }
            }
            
            if (Service.Targets.FocusTarget != null || reset) {
                var ui = Common.GetUnitBase("_FocusTargetInfo", 1);
                if (ui != null && (ui->IsVisible || reset)) {
                    UpdateFocusTarget(ui, Service.Targets.FocusTarget, reset);

                }
            }
        }
        
        private void UpdateMainTarget(AtkUnitBase* unitBase, GameObject target, bool reset = false) {
            if (unitBase == null || unitBase->UldManager.NodeList == null || unitBase->UldManager.NodeListCount < 40) return;
            var gauge = (AtkComponentNode*) unitBase->UldManager.NodeList[36];
            var textNode = (AtkTextNode*) unitBase->UldManager.NodeList[39];
            UiHelper.SetSize(unitBase->UldManager.NodeList[37], reset || !Config.HideAutoAttack ? 44 : 0, reset || !Config.HideAutoAttack ? 20 : 0);
            UpdateGaugeBar(gauge, textNode, target, Config.Position, Config.UseCustomColor ? Config.CustomColor : null, Config.FontSize, reset);
        }
        private void UpdateFocusTarget(AtkUnitBase* unitBase, GameObject target, bool reset = false) {
            if (Config.NoFocus) reset = true;
            if (unitBase == null || unitBase->UldManager.NodeList == null || unitBase->UldManager.NodeListCount < 11) return;
            var gauge = (AtkComponentNode*) unitBase->UldManager.NodeList[2];
            var textNode = (AtkTextNode*) unitBase->UldManager.NodeList[10];
            UpdateGaugeBar(gauge, textNode, target, Config.FocusPosition, Config.FocusUseCustomColor ? Config.FocusCustomColor : null, Config.FocusFontSize, reset);
        }
        private void UpdateMainTargetSplit(AtkUnitBase* unitBase, GameObject target, bool reset = false) {
            if (unitBase == null || unitBase->UldManager.NodeList == null || unitBase->UldManager.NodeListCount < 9) return;
            var gauge = (AtkComponentNode*) unitBase->UldManager.NodeList[5];
            var textNode = (AtkTextNode*) unitBase->UldManager.NodeList[8];
            UiHelper.SetSize(unitBase->UldManager.NodeList[6], reset || !Config.HideAutoAttack ? 44 : 0, reset || !Config.HideAutoAttack ? 20 : 0);
            UpdateGaugeBar(gauge, textNode, target, Config.Position, Config.UseCustomColor ? Config.CustomColor : null, Config.FontSize, reset);
        }
        
        private void UpdateGaugeBar(AtkComponentNode* gauge, AtkTextNode* cloneTextNode, GameObject target, Vector2 positionOffset, Vector4? customColor, byte fontSize, bool reset = false) {
            if (gauge == null || (ushort) gauge->AtkResNode.Type < 1000 || Service.ClientState.LocalPlayer == null) return;
            
            AtkTextNode* textNode = null;

            for (var i = 5; i < gauge->Component->UldManager.NodeListCount; i++) {
                var node = gauge->Component->UldManager.NodeList[i];
                if (node->Type == NodeType.Text && node->NodeID == CustomNodes.TargetHP) {
                    textNode = (AtkTextNode*) node;
                    break;
                }
            }

            if (textNode == null && reset) return; // Nothing to clean
            
            if (textNode == null) {
                textNode = UiHelper.CloneNode(cloneTextNode);
                textNode->AtkResNode.NodeID = CustomNodes.TargetHP;
                var newStrPtr = UiHelper.Alloc(512);
                textNode->NodeText.StringPtr = (byte*) newStrPtr;
                textNode->NodeText.BufSize = 512;
                textNode->SetText("");
                UiHelper.ExpandNodeList(gauge, 1);
                gauge->Component->UldManager.NodeList[gauge->Component->UldManager.NodeListCount++] = (AtkResNode*) textNode;

                var nextNode = gauge->Component->UldManager.RootNode;
                while (nextNode->PrevSiblingNode != null) {
                    nextNode = nextNode->PrevSiblingNode;
                }
                
                textNode->AtkResNode.ParentNode = (AtkResNode*) gauge;
                textNode->AtkResNode.ChildNode = null;
                textNode->AtkResNode.PrevSiblingNode = null;
                textNode->AtkResNode.NextSiblingNode = nextNode;
                nextNode->PrevSiblingNode = (AtkResNode*) textNode;
            }

            if (reset) {
                textNode->AtkResNode.ToggleVisibility(false);
                return;
            }

            textNode->AlignmentFontType = (byte)AlignmentType.BottomRight;
            
            UiHelper.SetPosition(textNode, positionOffset.X, positionOffset.Y);
            UiHelper.SetSize(textNode, gauge->AtkResNode.Width - 5, gauge->AtkResNode.Height);
            textNode->AtkResNode.ToggleVisibility(true);
            if (!customColor.HasValue) {
                textNode->TextColor = cloneTextNode->TextColor;
            } else {
                textNode->TextColor.A = (byte) (customColor.Value.W * 255);
                textNode->TextColor.R = (byte) (customColor.Value.X * 255);
                textNode->TextColor.G = (byte) (customColor.Value.Y * 255);
                textNode->TextColor.B = (byte) (customColor.Value.Z * 255);
            }
            textNode->EdgeColor = cloneTextNode->EdgeColor;
            textNode->FontSize = fontSize;
            
            
            if (target is Character chara)
            {
                var y = "";
                if (Config.EnableDistance){
                    Vector3 me = Service.ClientState.LocalPlayer.Position;
                    Vector3 tar = chara.Position;
                    y += "  " + Vector3.Distance(me, tar).ToString("00.0");
                }
                if (Config.EnableEffectiveDistance){
                    y += "  " + target.YalmDistanceX.ToString();
                }
                textNode->SetText($"{FormatNumber(chara.CurrentHp)}/{FormatNumber(chara.MaxHp)}"+y);
            } else {
                textNode->SetText("");
            }
        }

        private string FormatNumber(uint num, DisplayFormat? displayFormat = null) {
            displayFormat ??= Config.DisplayFormat;
            if (displayFormat == DisplayFormat.FullNumber) return num.ToString(Culture);
            if (displayFormat == DisplayFormat.FullNumberSeparators) return num.ToString("N0", Culture);
            var fStr = displayFormat switch {
                DisplayFormat.OneDecimalPrecision => "F1",
                DisplayFormat.TwoDecimalPrecision => "F2",
                _ => "F0"
            };
            return num switch {
                >= 1000000 => $"{(num / 1000000f).ToString(fStr, Culture)}M",
                >= 1000 => $"{(num / 1000f).ToString(fStr, Culture)}K",
                _ => $"{num}"
            };
        }
    }
}
