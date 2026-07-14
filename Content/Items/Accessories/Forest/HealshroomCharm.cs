using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.WorldBuilding;
using TutorialMod.Content.Items.Weapons.Magic;
using TutorialMod.Core.ModPlayers;

namespace TutorialMod.Content.Items.Accessories.Forest
{
    public class HealshroomCharm : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToAccessory();

            Item.rare = ItemRarityID.Blue;
            Item.defense = 2;
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<HealshroomPlayer>().equipped = true;
        }
    }

    class HealshroomPlayer : ModPlayer
    {
        // 3 seconds
        public const int MAX_HEAL_TIMER = 180;

        public bool equipped;
        public int healTimer;

        public override void ResetEffects()
        {
            if (healTimer > 0)
            {
                float progress = 1f - healTimer / (float)MAX_HEAL_TIMER;

                if (Main.rand.NextBool((int)MathHelper.Lerp(9, 2, progress)))
                {
                    Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(Player.width, Player.height), ModContent.DustType<SporebloomSparkle>(),
                        -Vector2.UnitY, 0, Color.Lerp(new Color(13, 76, 20), new Color(51, 135, 36), Main.rand.NextFloat()) with { A = 150 }, Main.rand.NextFloat(0.2f, 0.4f));
                }

                if (progress > 0.75f)
                {
                    float interpolant = 1f - (progress - 0.75f) / 0.25f;

                    Vector2 position = Player.Center + Main.rand.NextVector2CircularEdge(150 * interpolant, 150 * interpolant);

                    Dust.NewDustPerfect(position, ModContent.DustType<SporebloomSparkle>(),
                        position.DirectionTo(Player.Center) * 0.5f, 0, Color.Lerp(new Color(13, 76, 20), new Color(51, 135, 36), Main.rand.NextFloat()) with { A = 0 }, Main.rand.NextFloat(0.2f, 0.4f)).customData = true;
                }

                healTimer--;
                if (healTimer == 0)
                    DoHealSpore();         
            }
        }

        internal void DoHealSpore()
        {
            SoundEngine.PlaySound(SoundID.DD2_DarkMageHealImpact, Player.Center);
            SoundEngine.PlaySound(SoundID.DoubleJump with { Pitch = -0.3f }, Player.Center);

            Player.GetModPlayer<TutorialModPlayer>().AddShake(5);
            Player.Heal(15);

            for (int i = 0; i < 30; i++)
            {
                Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(Player.width, Player.height), ModContent.DustType<SporebloomSmoke>(),
                    Main.rand.NextVector2Circular(6f, 6f), 0, Color.Lerp(new Color(13, 76, 20), new Color(51, 135, 36), Main.rand.NextFloat()) * Main.rand.NextFloat(0.05f, 0.3f), Main.rand.NextFloat(0.05f));
            }

            for (int i = 0; i < 20; i++)
            {
                Projectile.NewProjectile(Player.GetSource_Misc("TutorialMod: HealshroomCharm spore spawn"), Player.Center + Main.rand.NextVector2Circular(Player.width, Player.height), Main.rand.NextVector2Circular(6f, 6f), ModContent.ProjectileType<HealshroomCharmSpore>(), 0, 0, Player.whoAmI);
            }
        }

        class HealshroomCharmSpore : ModProjectile
        {
            internal Color drawColor;
            public override void SetDefaults()
            {
                Projectile.width = 35;
                Projectile.height = 35;
                Projectile.friendly = false;

                Projectile.timeLeft = 600;
                Projectile.tileCollide = false;

                Projectile.scale *= Main.rand.NextFloat(1.25f, 2.5f);
                Projectile.rotation = Main.rand.NextFloat(6.28f);

                Projectile.frame = Main.rand.Next(3);

                drawColor = Color.Lerp(new Color(51, 135, 36), new Color(161, 220, 96), Main.rand.NextFloat());
            }

            public override void AI()
            {
                Projectile.velocity *= 0.96f;
                Projectile.rotation += Projectile.velocity.Length() * 0.02f;

                if (Main.rand.NextBool(60))
                {
                    Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), ModContent.DustType<SporebloomSmoke>(),
                        Main.rand.NextVector2Circular(5f, 5f), 0, Color.Lerp(new Color(51, 135, 36), new Color(161, 220, 96), Main.rand.NextFloat()) * Main.rand.NextFloat(0.2f, 0.5f), Main.rand.NextFloat(0.5f, 1.2f));
                }

                if (Main.rand.NextBool(90))
                {
                    Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(25f, 25f), ModContent.DustType<SporebloomSparkle>(),
                        Main.rand.NextVector2Circular(2.5f, 2.5f), 0, Color.Lerp(new Color(13, 76, 20), new Color(51, 135, 36), Main.rand.NextFloat()) with { A = 0 }, Main.rand.NextFloat(0.2f, 0.4f)).customData = true;
                }

                Player player = Main.player[Projectile.owner];

                if (Projectile.Hitbox.Intersects(player.Hitbox))
                    player.AddBuff(BuffID.Regeneration, 120);
            }

            public override bool PreDraw(ref Color lightColor)
            {
                Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;

                Rectangle frame = tex.Frame(1, 3, 0, Projectile.frame);

                float progress = 1f - Projectile.timeLeft / 600f;

                float fadeIn;
                if (progress < 0.25f)
                    fadeIn = progress / 0.25f;
                else
                    fadeIn = 1f - (progress - 0.25f) / 0.75f;

                Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, drawColor * 0.5f * fadeIn, Projectile.rotation, frame.Size() / 2f, Projectile.scale * MathHelper.Lerp(1f, 1.5f, progress), SpriteEffects.None, 0f);

                return false;
            }
        }
    }
    
    class HealshroomGlobalItem : GlobalItem
    {
        public override void OnConsumeItem(Item item, Player player)
        {
            if (item.healLife >= 15 && player.TryGetModPlayer<HealshroomPlayer>(out var gp) && gp.equipped && gp.healTimer <= 0)
                gp.healTimer = HealshroomPlayer.MAX_HEAL_TIMER;
        }
    }

    class GenerateHealshroomCharm : ModSystem
    {
        public override void PostWorldGen()
        {
            for (int chestIndex = 0; chestIndex < Main.maxChests; chestIndex++)
            {
                Chest chest = Main.chest[chestIndex];
                // Generate in wooden chests
                if (chest != null && Main.tile[chest.x, chest.y].TileType == TileID.Containers && Main.tile[chest.x, chest.y].TileFrameX == 0 * 36)
                {
                    for (int inventoryIndex = 0; inventoryIndex < 40; inventoryIndex++)
                    {
                        if (chest.item[inventoryIndex].type == ItemID.None)
                        {
                            if (WorldGen.genRand.NextFloat() < 0.5f)
                            {
                                chest.item[inventoryIndex].SetDefaults(ModContent.ItemType<HealshroomCharm>());
                                chest.item[inventoryIndex].Prefix(-1);
                            }

                            break;
                        }
                    }
                }
            }
        }
    }
}
