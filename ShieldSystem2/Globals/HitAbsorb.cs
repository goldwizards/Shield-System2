using Terraria;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;   // Color
using Terraria.Audio;            // SoundEngine
using Terraria.ID;               // SoundID
using ShieldSystem2.Players;
using ShieldSystem2.Config;

namespace ShieldSystem2.Globals
{
    internal static class ShieldAbsorb
    {
        public static void Apply(Player target, ref Player.HurtModifiers modifiers)
        {
            var sp = target.GetModPlayer<ShieldPlayer>();

            // ModPlayer.ModifyHurt에서 이미 처리됐다면 스킵(중복 방지)
            if (sp.handledThisHit) return;

            int incoming = (int)modifiers.FinalDamage.Base;
            if (incoming <= 0) return;

            if (sp.Shield > 0)
            {
                int absorbed  = System.Math.Min(sp.Shield, incoming);
                int remaining = incoming - absorbed;

                sp.Shield -= absorbed;
                sp.OnShieldHit();
                if (sp.Shield <= 0) sp.OnShieldBroken(300);

                var cfg = ModContent.GetInstance<ShieldSystemConfig>();

                // 파란 숫자 (옵션)
                if (!Main.dedServ && absorbed > 0 && cfg.ShowShieldText)
                    CombatText.NewText(target.Hitbox, Color.DodgerBlue, absorbed, false, false);

                // 실드 전용 사운드
                if (absorbed > 0 && !Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f }, target.Center);

                // 완전 흡수면 기본 피격 사운드 억제
                if (remaining <= 0)
                    modifiers.DisableSound();

                modifiers.FinalDamage.Base = remaining;

                sp.handledThisHit = true; // 이번 타격은 여기서 처리 완료
            }
            else
            {
                sp.OnShieldHit(); // 실드가 없어도 피격 타이머는 리셋(선택)
            }
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