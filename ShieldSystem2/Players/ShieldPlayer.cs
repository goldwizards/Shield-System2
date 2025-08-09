using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ShieldSystem2.Config;

namespace ShieldSystem2.Players
{
    public class ShieldPlayer : ModPlayer
    {
        // ───────── 상태 값 ─────────
        public int Shield;                  // 현재 보호막
        public int MaxShield;               // 최대 보호막 (최대 생명력 비례)
        public int shieldBreakCooldown;     // 보호막 파괴 후 재생 금지 프레임
        public int timeSinceLastHit;        // 마지막 피격 후 경과 프레임
        private int regenTimer;             // 재생 틱 타이머
        public float ShieldRegenBonus;      // (옵션) 재생 보너스

        // 중복 처리 방지 플래그
        public bool handledThisHit;

        // 피격 순간 표시되는 보호막 이펙트(0이면 표시 안 함)
        public int shieldHitTimer;

        // ───────── 생애 주기 ─────────
        public override void OnEnterWorld()
        {
            // 첫 진입 시: 최종 체력 기준으로 계산되도록 PostUpdate에서 계산됨
            Shield = MaxShield = 0;
            ResetRegenState();
        }

        public override void OnRespawn()
        {
            // 리스폰 후에도 PostUpdate에서 재계산됨
            Shield = MaxShield = 0;
            ResetRegenState();
        }

        public override void ResetEffects()
        {
            // ⚠️ 여기서는 최대 보호막 재계산하지 않음.
            // (ResetEffects는 버프/장비 보정보다 먼저 호출될 수 있음)
            if (Shield > MaxShield) Shield = MaxShield;
        }

        // ───────── 틱 업데이트 ─────────
        public override void PostUpdate()
        {
            // 1) 최종 보정이 끝난 statLifeMax2 기준으로 최대 보호막 재계산 + 비율 유지
            RecalcMaxShieldAfterAll();

            // 2) 피격 이펙트 타이머 감소
            if (shieldHitTimer > 0) shieldHitTimer--;

            // 3) 보호막 재생 로직
            regenTimer++;
            timeSinceLastHit++;

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

            // 계단식(시간 경과에 따라 빨라짐) — 이전 동작과 동일
            float regenPerSecond = 1f;
            if (timeSinceLastHit >= 300)  regenPerSecond = 2f;
            if (timeSinceLastHit >= 600)  regenPerSecond = 3f;
            if (timeSinceLastHit >= 900)  regenPerSecond = 5f;
            if (timeSinceLastHit >= 1200) regenPerSecond = 8f;
            if (timeSinceLastHit >= 1800) regenPerSecond = 12f;
            if (timeSinceLastHit >= 2400) regenPerSecond = 20f;

            regenPerSecond *= 1f + ShieldRegenBonus;

            // 보스전 제한
            if (Main.npc.Any(n => n.active && n.boss))
            {
                float bossLimit = 5f * (1f + ShieldRegenBonus);
                if (regenPerSecond > bossLimit)
                    regenPerSecond = bossLimit;
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

        // ───────── 피해 흡수 ─────────
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
                timeSinceLastHit = 0;

                if (Shield <= 0)
                    shieldBreakCooldown = 300; // 5초 금지 (60fps 기준)

                // 파란 숫자 (설정 가능)
                var cfg = ModContent.GetInstance<ShieldSystemConfig>();
                if (!Main.dedServ && absorbed > 0 && cfg.ShowShieldText)
                    CombatText.NewText(Player.Hitbox, Color.DodgerBlue, absorbed);

                // 전용 사운드
                if (!Main.dedServ && absorbed > 0)
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f }, Player.Center);

                // 맞는 순간 0.25초 이펙트 표시
                if (absorbed > 0) shieldHitTimer = 15;

                // 완전 흡수 시 기본 피격 사운드 억제
                if (remaining <= 0) modifiers.DisableSound();

                // 최종 데미지 축소 적용
                if (remaining < 0) remaining = 0;
                modifiers.FinalDamage.Base = remaining;

                handledThisHit = true;
            }
        }

        // ───────── 유틸 ─────────
        private void ResetRegenState()
        {
            shieldBreakCooldown = 0;
            timeSinceLastHit = 0;
            regenTimer = 0;
            ShieldRegenBonus = 0f;
            handledThisHit = false;
            shieldHitTimer = 0;
        }

        /// <summary>
        /// 버프/장비/포션이 모두 반영된 statLifeMax2 이후에 실행.
        /// 최대 보호막을 재계산하고, 기존 Shield 비율을 최대한 유지.
        /// </summary>
        private void RecalcMaxShieldAfterAll()
        {
            int pct = ModContent.GetInstance<ShieldSystemConfig>().ShieldMaxPercent; // 25~100
            int newMax = (int)(Player.statLifeMax2 * (pct / 100f));
            if (newMax < 0) newMax = 0;

            if (newMax != MaxShield)
            {
                float ratio = MaxShield > 0 ? (float)Shield / MaxShield : 1f;
                MaxShield = newMax;
                Shield = (int)(MaxShield * ratio);

                if (Shield > MaxShield) Shield = MaxShield;
                if (Shield < 0) Shield = 0;
            }
        }
    }
}
