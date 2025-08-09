using Terraria;
using Terraria.ModLoader;
using ShieldSystem2.Config;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria.Audio;
using Terraria.ID;

namespace ShieldSystem2.Players
{
    public class ShieldPlayer : ModPlayer
    {
        // ── 실드 수치 ──
        public int Shield;
        public int MaxShield;

        // ── 재생 상태 ──
        public int shieldBreakCooldown;
        public int timeSinceLastHit;
        private int regenTimer;
        public float ShieldRegenBonus;

        // ── 중복 방지 ──
        public bool handledThisHit;

        // ✅ NEW: 피격 이펙트 타이머(0이면 표시 안 함)
        public int shieldHitTimer;

        public override void OnEnterWorld()
        {
            RecalcMaxShield();
            Shield = MaxShield;
            ResetRegenState();
        }

        public override void OnRespawn()
        {
            RecalcMaxShield();
            Shield = MaxShield;
            ResetRegenState();
        }

        public override void ResetEffects()
        {
            RecalcMaxShield();
            if (Shield > MaxShield) Shield = MaxShield;
        }

        public override void PostUpdate()
        {
            regenTimer++;
            timeSinceLastHit++;

            // ✅ NEW: 이펙트 타이머 감소
            if (shieldHitTimer > 0) shieldHitTimer--;

            if (shieldBreakCooldown > 0)
            {
                shieldBreakCooldown--;
                return;
            }

            if (Shield >= MaxShield)
            {
                Shield = MaxShield;
                return;
            }

            // 계단식 재생속도(이전과 동일)
            float regenPerSecond = 1f;
            if (timeSinceLastHit >= 300)  regenPerSecond = 2f;
            if (timeSinceLastHit >= 600)  regenPerSecond = 3f;
            if (timeSinceLastHit >= 900)  regenPerSecond = 5f;
            if (timeSinceLastHit >= 1200) regenPerSecond = 8f;
            if (timeSinceLastHit >= 1800) regenPerSecond = 12f;
            if (timeSinceLastHit >= 2400) regenPerSecond = 20f;

            regenPerSecond *= 1f + ShieldRegenBonus;

            if (Main.npc.Any(n => n.active && n.boss))
            {
                float bossLimit = 5f * (1f + ShieldRegenBonus);
                if (regenPerSecond > bossLimit) regenPerSecond = bossLimit;
            }

            if (regenPerSecond > 0f)
            {
                int interval = (int)(60f / regenPerSecond);
                if (interval < 1) interval = 1;

                if (regenTimer % interval == 0 && Shield < MaxShield)
                {
                    Shield++;
                    if (Shield > MaxShield) Shield = MaxShield;
                }
            }
        }

        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            if (handledThisHit) { handledThisHit = false; return; }

            int incoming = (int)modifiers.FinalDamage.Base;
            if (incoming <= 0) return;

            if (Shield > 0)
            {
                int absorbed  = System.Math.Min(Shield, incoming);
                int remaining = incoming - absorbed;

                Shield -= absorbed;
                OnShieldHit();
                if (Shield <= 0) OnShieldBroken(300);

                // 파란 숫자(설정)
                if (!Main.dedServ && absorbed > 0 &&
                    ModContent.GetInstance<ShieldSystemConfig>().ShowShieldText)
                {
                    CombatText.NewText(Player.Hitbox, Color.DodgerBlue, absorbed, false, false);
                }

                // 전용 사운드
                if (absorbed > 0 && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f }, Player.Center);
                }

                // ✅ NEW: 이펙트 0.25초 표시 트리거(60fps 기준 15프레임)
                if (absorbed > 0) shieldHitTimer = 15;

                if (remaining <= 0) modifiers.DisableSound();
                if (remaining < 0) remaining = 0;
                modifiers.FinalDamage.Base = remaining;

                handledThisHit = true;
            }
        }

        public void OnShieldHit() => timeSinceLastHit = 0;

        public void OnShieldBroken(int cooldownFrames = 300)
        {
            if (Shield <= 0) shieldBreakCooldown = cooldownFrames;
        }

        private void ResetRegenState()
        {
            shieldBreakCooldown = 0;
            timeSinceLastHit = 0;
            regenTimer = 0;
            ShieldRegenBonus = 0f;
            handledThisHit = false;
            shieldHitTimer = 0; // 🔸 초기 상태: 비표시
        }

        public void RecalcMaxShield()
        {
            int pct = ModContent.GetInstance<ShieldSystemConfig>().ShieldMaxPercent;
            MaxShield = (int)(Player.statLifeMax2 * (pct / 100f));
            if (MaxShield < 0) MaxShield = 0;
        }
    }
}