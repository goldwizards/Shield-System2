using Terraria.ModLoader;

namespace ShieldSystem2
{
    public class ShieldSystem2 : Mod
    {
        public static bool CalamityLoaded { get; private set; }

        public override void Load()
        {
            CalamityLoaded = ModLoader.TryGetMod("CalamityMod", out _);
        }

        public override void Unload()
        {
            CalamityLoaded = false;
        }
    }
}
