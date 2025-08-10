using Terraria;
using Terraria.ModLoader;
using ShieldSystem2.Config;
using ShieldSystem2.Globals;

namespace ShieldSystem2.Players
{
    public class ShieldPlayer : ModPlayer
    {
        public int Shield, MaxShield;
        public int shieldBreakCooldown, timeSinceLastHit, shieldHitTimer;
        private int regenTimer;

        const int BreakCD = 60;     // 1초
        const int RegenDelay = 120; // 2초
        const int RegenTick = 5;
        const int RegenPerTick = 1;

        public override void Initialize()
        {
            Shield = 0; MaxShield = 0;
            shieldBreakCooldown = 0; timeSinceLastHit = 9999;
            shieldHitTimer = 0; regenTimer = 0;
        }

        public override void ResetEffects()
        {
            if (Shield > MaxShield) Shield = MaxShield;
            if (Shield < 0) Shield = 0;
        }

        public override void PostUpdate()
        {
            RecalcMaxShield();
            if (shieldBreakCooldown > 0) shieldBreakCooldown--;
            if (shieldHitTimer > 0) shieldHitTimer--;
            timeSinceLastHit++;
            TryRegen();
        }

        public override void OnRespawn()
        {
            Shield = MaxShield / 2;
            timeSinceLastHit = 9999;
            shieldBreakCooldown = 0;
            regenTimer = 0;
        }

        public void OnShieldHit()
        {
            timeSinceLastHit = 0;
            shieldHitTimer = 15;
            regenTimer = 0;
            if (Shield <= 0) shieldBreakCooldown = BreakCD;
        }

        void TryRegen()
        {
            if (MaxShield <= 0 || Shield >= MaxShield) return;
            if (shieldBreakCooldown > 0) return;
            if (timeSinceLastHit < RegenDelay) return;

            if (++regenTimer >= RegenTick)
            {
                regenTimer = 0;
                Shield += RegenPerTick;
                if (Shield > MaxShield) Shield = MaxShield;
            }
        }

        void RecalcMaxShield()
        {
            var cfg = ModContent.GetInstance<ShieldSystemConfig>();
            int newMax = (int)(Player.statLifeMax2 * (cfg.ShieldMaxPercent / 100f));
            if (newMax < 0) newMax = 0;

            if (newMax != MaxShield)
            {
                float ratio = MaxShield > 0 ? (float)Shield / MaxShield : 1f;
                MaxShield = newMax;
                Shield = (int)(MaxShield * ratio);
                if (Shield < 0) Shield = 0;
                if (Shield > MaxShield) Shield = MaxShield;
            }
        }

        // ★ 모든 피해(몬스터/투사체/낙하/가시/용암) → Player 단계에서 한 번만 처리
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            ShieldAbsorb.Apply(Player, ref modifiers);
        }
    }
}
