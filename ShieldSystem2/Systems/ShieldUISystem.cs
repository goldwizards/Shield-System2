using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using ShieldSystem2.UI;

namespace ShieldSystem2.Systems
{
    public class ShieldUISystem : ModSystem
    {
        private UserInterface _ui;
        private ShieldUI _state;

        public override void Load()
        {
            if (Main.dedServ) return;
            _state = new ShieldUI();
            _ui = new UserInterface();
            _ui.SetState(_state);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            if (Main.dedServ) return;
            _ui?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (Main.dedServ) return;

            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Resource Bars"));
            if (idx < 0) idx = layers.Count - 1;

            layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                "ShieldSystem2: Shield UI",
                () =>
                {
                    if (!Main.gameMenu)
                        _ui.Draw(Main.spriteBatch, new GameTime());
                    return true; // ★ 반드시 true
                },
                InterfaceScaleType.UI));
        }
    }
}

