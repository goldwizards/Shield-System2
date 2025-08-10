using System;
using System.Reflection;
using Microsoft.Xna.Framework; // Color
using Terraria;
using Terraria.ModLoader;
using Terraria.Audio;
using Terraria.ID;
using ShieldSystem2.Config;
using ShieldSystem2.Players;

namespace ShieldSystem2.Globals
{
    internal static class ShieldAbsorb
    {
        // ─────────────────────────────────────────────────────
        // Calamity 리플렉션(선택적): CalamityPlayer의 보호막 잔량 조회
        // ─────────────────────────────────────────────────────
        private static Type _calPlayerType;
        private static FieldInfo _fiRoverShield;
        private static FieldInfo _fiSpongeShield;
        private static MethodInfo _miGetModPlayerGeneric; // Player.GetModPlayer<T>() 제네릭 정의

        private static void EnsureCalamityReflection()
        {
            if (!ShieldSystem2.CalamityLoaded) return;
            if (_calPlayerType != null && _miGetModPlayerGeneric != null) return;

            var calamity = ModLoader.GetMod("CalamityMod");
            if (calamity == null) return;

            _calPlayerType = calamity.Code?.GetType("CalamityMod.CalamityPlayer");
            if (_calPlayerType == null) return;

            // 버전별 대소문/접근자 차이를 대비해 둘 다 시도
            _fiRoverShield = _calPlayerType.GetField("RoverDriveShieldDurability", BindingFlags.Public | BindingFlags.Instance)
                            ?? _calPlayerType.GetField("roverDriveShieldDurability", BindingFlags.NonPublic | BindingFlags.Instance);

            _fiSpongeShield = _calPlayerType.GetField("SpongeShieldDurability", BindingFlags.Public | BindingFlags.Instance)
                             ?? _calPlayerType.GetField("spongeShieldDurability", BindingFlags.NonPublic | BindingFlags.Instance);

            // Player.GetModPlayer<T>() 제네릭 정의 확보
            foreach (var m in typeof(Player).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (m.Name == "GetModPlayer" && m.IsGenericMethodDefinition && m.GetGenericArguments().Length == 1 && m.GetParameters().Length == 0)
                {
                    _miGetModPlayerGeneric = m;
                    break;
                }
            }
        }

        // 칼라미티 보호막 잔량만 조회(우리는 소모하지 않음)
        private static bool TryPeekCalamityShields(Player p, out int sponge, out int rover)
        {
            sponge = rover = 0;
            if (!ShieldSystem2.CalamityLoaded) return false;

            try
            {
                EnsureCalamityReflection();
                if (_calPlayerType == null || _miGetModPlayerGeneric == null) return false;

                var mi   = _miGetModPlayerGeneric.MakeGenericMethod(_calPlayerType);
                var calP = mi.Invoke(p, null);
                if (calP == null) return false;

                if (_fiSpongeShield != null)
                {
                    object v = _fiSpongeShield.GetValue(calP);
                    if (v is int vi) sponge = vi;
                }
                if (_fiRoverShield != null)
                {
                    object v = _fiRoverShield.GetValue(calP);
                    if (v is int vi) rover = vi;
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 모든 피해 소스에 대해 한 번만 호출: 반드시 HurtInfo에서 직접 수정
        /// </summary>
        public static void Apply(Player target, ref Player.HurtModifiers modifiers)
        {
            // 이 델리게이트 안에서 info.Damage/사운드/연출을 처리해야 실제 반영됩니다.
            modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
            {
                int incoming = info.Damage;
                if (incoming <= 0) return;

                var cfg = ModContent.GetInstance<ShieldSystemConfig>();
                var sp  = target.GetModPlayer<ShieldPlayer>();

                // ─────────────────────────────────────────────
                // 0) 칼라미티 보호막이 남아 있으면: 이번 히트는 '칼라미티만 흡수' (우리 보호막 스킵)
                //    → 동시 차감 방지
                //    → 사운드/이펙트는 대체로 재생
                // ─────────────────────────────────────────────
                if (TryPeekCalamityShields(target, out int sponge, out int rover) &&
                    (sponge > 0 || rover > 0))
                {
                    // 바닐라 피격음 끄고 커스텀 사운드 + 히트 이펙트 트리거
                    info.SoundDisabled = true;
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f }, target.Center);

                    sp.shieldHitTimer = Math.Max(sp.shieldHitTimer, 15); // 이펙트(ShieldHit.png) 0.25초

                    // 데미지/숫자 처리는 칼라미티 쪽 로직에 맡김
                    return;
                }

                // ─────────────────────────────────────────────
                // 1) 칼라미티 보호막이 없을 때만: 우리 보호막이 흡수
                // ─────────────────────────────────────────────
                int absorbedTotal = 0;

                if (incoming > 0 && sp.Shield > 0)
                {
                    int used = Math.Min(sp.Shield, incoming);
                    sp.Shield -= used;
                    incoming  -= used;
                    absorbedTotal += used;

                    // 재생 로직/쿨다운
                    sp.timeSinceLastHit = 0;
                    if (sp.Shield <= 0) sp.shieldBreakCooldown = 300;

                    // 바닐라 피격음 끄고 커스텀 사운드 + 히트 이펙트 트리거
                    info.SoundDisabled = true;
                    if (!Main.dedServ)
                        SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.6f }, target.Center);

                    sp.shieldHitTimer = Math.Max(sp.shieldHitTimer, 15);
                }

                // 2) 완전 흡수 → 빨간 숫자 숨김
                if (incoming <= 0)
                {
                    info.Damage = 0;

                    if (!Main.dedServ && absorbedTotal > 0 && cfg.ShowShieldText)
                        CombatText.NewText(target.Hitbox, Color.DodgerBlue, absorbedTotal); // 파란 숫자 1회
                    return;
                }

                // 3) 부분 흡수 → 남은 피해 반영 + 파란 숫자(선택)
                info.Damage = incoming;

                if (!Main.dedServ && absorbedTotal > 0 && cfg.ShowShieldText)
                    CombatText.NewText(target.Hitbox, Color.DodgerBlue, absorbedTotal);
            };
        }
    }
}
