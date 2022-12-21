using System;
using Dalamud.Game;
using ImGuiNET;
using SimpleTweaksPlugin.Tweaks.UiAdjustment;
using SimpleTweaksPlugin.TweakSystem;
using SimpleTweaksPlugin.Utility;

namespace SimpleTweaksPlugin {
    public partial class UiAdjustmentsConfig {
        public bool ShouldSerializeHideAchievementsNotifications() => HideAchievementsNotifications != null;
        public HideAchievementsNotifications.Configs HideAchievementsNotifications = null;
    }
}

namespace SimpleTweaksPlugin.Tweaks.UiAdjustment {
    public class HideAchievementsNotifications : UiAdjustments.SubTweak {
        public class Configs : TweakConfig {
            public bool HideLogIn = true;
            public bool HideZoneIn = true;
        }

        public Configs Config { get; private set; }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) => {
            hasChanged |= ImGui.Checkbox(LocString("HideLogIn", "Hide the login notification."), ref this.Config.HideLogIn);
            hasChanged |= ImGui.Checkbox(LocString("HideZoneIn", "Hide the zone-in notification."), ref this.Config.HideZoneIn);
        };

        public override string Name => "隐藏接近达成成就提示";
        public override string Description => "完全隐藏登录或切换区域时绿色带倒计时的接近达成成就的通知";
        protected override string Author => "Anna";

        public override void Enable() {
            Config = LoadConfig<Configs>() ?? PluginConfig.UiAdjustments.HideAchievementsNotifications ?? new Configs();
            Service.Framework.Update += this.HideNotifications;
            base.Enable();
        }

        public override void Disable() {
            SaveConfig(Config);
            PluginConfig.UiAdjustments.HideAchievementsNotifications = null;
            Service.Framework.Update -= this.HideNotifications;
            base.Disable();
        }

        private const int VisibilityFlag = 1 << 5;

        private void HideNotifications(Framework framework) {
            if (this.Config.HideLogIn) {
                this.HideNotification("_NotificationAchieveLogIn");
            }

            if (this.Config.HideZoneIn) {
                this.HideNotification("_NotificationAchieveZoneIn");
            }
        }

        private unsafe void HideNotification(string name) {
            try {
                var atkUnitBase = Common.GetUnitBase(name);
                if (atkUnitBase == null) return;
                atkUnitBase->Flags = (byte) (atkUnitBase->Flags & ~VisibilityFlag);
            } catch (Exception) {
                // ignore
            }
        }
    }
}
