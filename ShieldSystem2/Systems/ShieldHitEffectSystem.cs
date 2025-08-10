using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using ShieldSystem2.Players;

namespace ShieldSystem2.Systems
{
    public class ShieldHitEffectSystem : ModSystem
    {
        private static Asset<Texture2D> _hitTex;

        public override void Load()
        {
            if (Main.dedServ) return;
            _hitTex = ModContent.Request<Texture2D>("ShieldSystem2/Assets/ShieldHit"); // ShieldHit.png
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Main.dedServ) return;

            // 바닐라 자원 바 다음에 그리기
            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Resource Bars"));
            if (idx < 0) idx = layers.Count - 1;

            layers.Insert(idx + 2, new LegacyGameInterfaceLayer(
                "ShieldSystem2: Shield Hit FX (Follow Player)",
                () =>
                {
                    var player = Main.LocalPlayer;
                    if (player == null || !player.active) return true;

                    var sp = player.GetModPlayer<ShieldPlayer>();
                    if (sp.shieldHitTimer <= 0) return true;

                    // 0~1로 줄어드는 타이머 기반 알파/스케일
                    float t = sp.shieldHitTimer / 15f;
                    float alpha = MathHelper.Clamp(t, 0f, 1f);
                    float scale = 1.0f + 0.15f * (1f - t);

                    // 플레이어 월드→스크린 좌표 (Game 스케일 레이어에서 자연스럽게 보정됨)
                    Vector2 worldPos = player.Center + new Vector2(0f, -16f); // 머리 위 살짝
                    Vector2 screenPos = worldPos - Main.screenPosition;

                    if (_hitTex != null && _hitTex.IsLoaded)
                    {
                        Texture2D tex = _hitTex.Value;
                        Vector2 origin = new Vector2(tex.Bounds.Size().X, tex.Bounds.Size().Y) * 0.5f;

                        // 색/회전은 취향에 맞게 조절 가능
                        Main.spriteBatch.Draw(
                            tex,
                            screenPos,
                            null,
                            Color.Cyan * alpha,
                            0f,
                            origin,
                            scale,
                            SpriteEffects.None,
                            0f
                        );
                    }

                    return true; // ★ 깜빡임 방지
                },
                InterfaceScaleType.Game // ★ 월드 기준으로 그리기
            ));
        }
    }
}

