using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;         // TextureAssets.MagicPixel
using Terraria.ModLoader;
using Terraria.UI;
using ShieldSystem2.Config;
using ShieldSystem2.Players;

namespace ShieldSystem2.UI
{
    public class ShieldUI : UIState
    {
        public override void Draw(SpriteBatch spriteBatch)
        {
            var sp = Main.LocalPlayer.GetModPlayer<ShieldPlayer>();
            var cfg = ModContent.GetInstance<ShieldSystemConfig>();
            if (sp.MaxShield <= 0) return;

            if (cfg.ShieldUIStyle == ShieldUIDisplayStyle.Icon)
                DrawShieldIcons(spriteBatch, sp, cfg);
            else
                DrawShieldBar(spriteBatch, sp);
        }

        private void DrawShieldBar(SpriteBatch sb, ShieldPlayer sp)
        {
            int shield = sp.Shield;
            int maxShield = sp.MaxShield;
            float percent = MathHelper.Clamp(maxShield > 0 ? (float)shield / maxShield : 0f, 0f, 1f);

            Vector2 position = new Vector2(Main.screenWidth - 350, 120);
            int barWidth = 26;
            int barHeight = 200;
            int fillHeight = (int)(barHeight * percent);

            // 배경
            sb.Draw(TextureAssets.MagicPixel.Value, new Rectangle((int)position.X, (int)position.Y, barWidth, barHeight), Color.Black * 0.5f);

            // 채움(아래→위)
            if (fillHeight > 0)
            {
                sb.Draw(TextureAssets.MagicPixel.Value,
                    new Rectangle((int)position.X, (int)(position.Y + (barHeight - fillHeight)), barWidth, fillHeight),
                    Color.DodgerBlue);
            }

            // 프레임 (이전 경로 그대로)
            Texture2D frameTex = ModContent.Request<Texture2D>("ShieldMod/Assets/ShieldFrame").Value;
            float frameScale = 1f;
            Vector2 frameOffset = new Vector2((barWidth - frameTex.Width * frameScale) / 2f, -40f);
            sb.Draw(frameTex, position + frameOffset, null, Color.White, 0f, Vector2.Zero, frameScale, SpriteEffects.None, 0f);

            // 텍스트
            string text = $"{shield} / {maxShield}";
            Vector2 textPos = new Vector2(position.X + (barWidth / 2f), position.Y + barHeight + 18f);
            Utils.DrawBorderString(sb, text, textPos, Color.White, 1f, 0.5f, 0.5f);
        }

        private void DrawShieldIcons(SpriteBatch sb, ShieldPlayer sp, ShieldSystemConfig cfg)
        {
            Texture2D icon = ModContent.Request<Texture2D>("ShieldMod/Assets/ShieldIcon").Value;

            int iconCount = 5;
            float shieldPerIcon = Math.Max(1f, sp.MaxShield / (float)iconCount);
            float value = sp.Shield;

            Vector2 startPos = new Vector2(Main.screenWidth - 350, 120);
            int spacing = 36;

            int activeIndex = Math.Clamp((int)(value / shieldPerIcon), 0, iconCount - 1);

            for (int i = 0; i < iconCount; i++)
            {
                float min = i * shieldPerIcon;
                float max = (i + 1) * shieldPerIcon;

                float alpha;
                if (value >= max) alpha = 1f;
                else if (value > min) alpha = (value - min) / shieldPerIcon;
                else alpha = 0f;

                alpha = MathHelper.Clamp(alpha, 0.1f, 1f);

                float scale = 1f;
                if (cfg.UseShieldPulseEffect && i == activeIndex)
                {
                    // 살짝 펄스
                    scale = 1f + 0.13f * (1f + (float)Math.Sin(Main.GameUpdateCount / 10f));
                }

                Vector2 origin = new Vector2(icon.Width, icon.Height) * 0.5f; // 중앙 기준
                Vector2 pos = new Vector2(startPos.X, startPos.Y + i * spacing) + origin;
                sb.Draw(icon, pos, null, Color.White * alpha, 0f, origin, scale, SpriteEffects.None, 0f);
            }

            string text = $"{sp.Shield} / {sp.MaxShield}";
            Vector2 textPos = new Vector2(startPos.X + 16, startPos.Y + iconCount * spacing + 8);
            Utils.DrawBorderString(sb, text, textPos, Color.White, 1f, 0.5f, 0.5f);
        }
    }
}