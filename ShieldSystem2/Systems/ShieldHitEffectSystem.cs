using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
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
            // 원본 자원 경로 그대로 사용
            _hitTex = ModContent.Request<Texture2D>("ShieldSystem2/Assets/ShieldHit", AssetRequestMode.AsyncLoad);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Resource Bars"));
            if (idx < 0) idx = layers.Count - 1;

            layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                "ShieldSystem2: Shield Hit FX",
                () =>
                {
                    if (Main.gameMenu || _hitTex is null || !_hitTex.IsLoaded) return true;

                    Player p = Main.LocalPlayer;
                    if (p == null || !p.active) return true;

                    var sp = p.GetModPlayer<ShieldPlayer>();
                    if (sp.shieldHitTimer <= 0) return true;

                    // 잠깐만 보였다가 꺼짐: 고정값
                    const float alpha = 0.9f;
                    const float scale = 1.0f;

                    Texture2D tex = _hitTex.Value;
                    Vector2 pos = p.Center - Main.screenPosition;
                    Vector2 origin = new Vector2(tex.Width, tex.Height) * 0.5f; // ← 안전한 중앙 계산

                    Main.spriteBatch.Begin(
                        SpriteSortMode.Deferred,
                        BlendState.AlphaBlend,
                        SamplerState.PointClamp,
                        DepthStencilState.None,
                        RasterizerState.CullCounterClockwise,
                        null,
                        Main.UIScaleMatrix
                    );

                    Main.spriteBatch.Draw(tex, pos, null, Color.Cyan * alpha, 0f, origin, scale, SpriteEffects.None, 0f);

                    Main.spriteBatch.End();
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
