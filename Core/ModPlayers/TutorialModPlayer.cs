using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace TutorialMod.Core.ModPlayers
{
    public class TutorialModPlayer : ModPlayer
    {
        public int shakeTimer;

        public override void ModifyScreenPosition()
        {
            if (shakeTimer > 0)
            {
                shakeTimer--;
                Vector2 shake = new Vector2(Main.rand.NextFloat(shakeTimer), Main.rand.NextFloat(shakeTimer));
                Main.screenPosition += shake;
            }
        }

        public void AddShake(int amount, bool clamped = true)
        {
            if (clamped)
            {
                if (shakeTimer < amount)
                    shakeTimer = amount;
            }
            else
                shakeTimer += amount;
        }
    }
}
