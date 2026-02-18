using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria;
using Terraria.Audio;

namespace TutorialMod.Content.Items.Weapons.Melee
{
    public class ForestbornSword : ModItem
    {
        public override void SetDefaults()
        {
            Item.Size = new Vector2(32, 32);

            Item.DamageType = DamageClass.Melee;
            Item.damage = 10;
            Item.knockBack = 2.5f;
            Item.crit = 4;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;

            Item.useAnimation = 15;
            Item.useTime = 15;

            Item.scale = 1.25f;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.WoodenSword, 1)
                .AddIngredient(ItemID.Mushroom, 5)
                .AddIngredient(ItemID.Daybloom, 1)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (Main.rand.NextBool(3))
            {
                int d = Dust.NewDust(hitbox.TopLeft(), hitbox.Width, hitbox.Height, DustID.Grass);

                Dust dust = Main.dust[d];

                dust.noGravity = true;
            }          
        }

        public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (hit.Crit)
            {
                for (int i = 0; i < 5; i++)
                {
                    Dust.NewDustPerfect(target.Center, DustID.Grass, Main.rand.NextVector2Circular(5f, 5f));

                    Dust.NewDustPerfect(target.Center, DustID.Dirt, Main.rand.NextVector2Circular(5f, 5f)).noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.DD2_MonkStaffGroundImpact, target.Center);

                Item.NewItem(player.GetSource_OnHit(target), target.getRect(), ModContent.ItemType<HealingMushroom>());
            }
        }
    }

    public class HealingMushroom : ModItem
    {
        public override string Texture => $"Terraria/Images/Item_{ItemID.Mushroom}";

        public override void SetDefaults()
        {
            Item.Size = new Vector2(12, 12);

            Item.scale = 0.6f;
        }

        public override bool OnPickup(Player player)
        {
            player.Heal(5);

            return false;
        }
    }
}
