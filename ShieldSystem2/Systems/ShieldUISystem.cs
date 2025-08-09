using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;
using ShieldSystem2.UI;
using Microsoft.Xna.Framework;

namespace ShieldSystem2.Systems
{
    public class ShieldUISystem : ModSystem
    {
        private UserInterface _ui;
        private ShieldUI _state;

        public override void Load()
        {
            if (Main.dedServ) return;
            _ui = new UserInterface();
            _state = new ShieldUI();
            _state.Activate();
            _ui.SetState(_state);
        }

        public override void UpdateUI(GameTime gameTime)
        {
            _ui?.Update(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            if (_ui is null) return;

            int idx = layers.FindIndex(l => l.Name.Equals("Vanilla: Resource Bars"));
            if (idx < 0) idx = layers.Count - 1;

            layers.Insert(idx + 1, new LegacyGameInterfaceLayer(
                "ShieldSystem2: Shield UI",
                () =>
                {
                    if (!Main.gameMenu)
                    {
                        _ui.Draw(Main.spriteBatch, new GameTime());
                    }
                    return true;
                },
                InterfaceScaleType.UI));
        }
    }
}
