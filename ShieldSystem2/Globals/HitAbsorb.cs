using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ID;
using ShieldSystem2.Players;
using ShieldSystem2.Config;

namespace ShieldSystem2.Globals
{
    internal static class ShieldAbsorb
    {
        public static void Apply(Player target, ref Player.HurtModifiers modifiers)
        {
            var sp = target.GetModPlayer<ShieldPlayer>();
            int incoming = (int)modifiers.FinalDamage.Base;
            if (incoming <= 0 || sp.Shield <= 0) return;

            int absorbed  = System.Math.Min(sp.Shield, incoming);
            int remaining = incoming - absorbed;

            sp.Shield -= absorbed;
            sp.timeSinceLastHit = 0;
            if (sp.Shield <= 0) sp.shieldBreakCooldown = 300;

            var cfg = ModContent.GetInstance<ShieldSystemConfig>();
            if (!Main.dedServ && absorbed > 0 && cfg.ShowShieldText)
                CombatText.NewText(target.Hitbox, Color.DodgerBlue, absorbed);

            if (!Main.dedServ && absorbed > 0)
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f }, target.Center);

            if (absorbed > 0) sp.shieldHitTimer = 15;
            if (remaining <= 0) modifiers.DisableSound();

            modifiers.FinalDamage.Base = remaining < 0 ? 0 : remaining;
        }
    }

    public class HitAbsorbGlobalNPC : GlobalNPC
    {
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
            => ShieldAbsorb.Apply(target, ref modifiers);
    }

    public class HitAbsorbGlobalProjectile : GlobalProjectile
    {
        public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
            => ShieldAbsorb.Apply(target, ref modifiers);
    }
}
