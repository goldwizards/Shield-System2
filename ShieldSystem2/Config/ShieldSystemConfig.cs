using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace ShieldSystem2.Config
{
    public class ShieldSystemConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        // Shield Settings
        [Header("Mods.ShieldSystem2.Config.ShieldSystemConfig.ShieldSettings")]
        [Range(25, 100)]
        [DefaultValue(100)]
        [Increment(5)]
        public int ShieldMaxPercent { get; set; } = 100;

        [DefaultValue(true)]
        public bool ShowShieldText { get; set; } = true;

        // Shield UI
        [Header("Mods.ShieldSystem2.Config.ShieldSystemConfig.ShieldUI")]
        [DefaultValue(ShieldUIDisplayStyle.Bar)]
        public ShieldUIDisplayStyle ShieldUIStyle { get; set; } = ShieldUIDisplayStyle.Bar;

        [DefaultValue(true)]
        public bool UseShieldPulseEffect { get; set; } = true;
    }

    public enum ShieldUIDisplayStyle
    {
        Bar,
        Icon
    }
}