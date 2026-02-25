using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria.GameContent;
using Terraria.Audio;
using System.Collections.Generic;
using TutorialMod.Core.ModPlayers;

namespace TutorialMod.Content.Items.Weapons.Ranged
{
    public class GildedGrenade : ModItem
    {
        public override void SetDefaults()
        {
            Item.Size = new Vector2(16, 16);

            Item.DamageType = DamageClass.Ranged;
            Item.damage = 20;
            Item.knockBack = 5f;

            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;

            Item.useAnimation = 35;
            Item.useTime = 35;

            Item.shoot = ModContent.ProjectileType<GildedGrenadeProjectile>();
            Item.shootSpeed = 12f;

            Item.rare = ItemRarityID.Green;

            Item.noMelee = true;
            Item.noUseGraphic = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.Grenade, 1)
                .AddIngredient(ItemID.PlatinumBar, 12)
                .AddTile(TileID.Anvils)
                .Register();

            CreateRecipe()
               .AddIngredient(ItemID.Grenade, 1)
               .AddIngredient(ItemID.GoldBar, 12)
               .AddTile(TileID.Anvils)
               .Register();
        }
    }

    public class GildedGrenadeProjectile : ModProjectile
    {
        int deathTimer;
        public override string Texture => "TutorialMod/Content/Items/Weapons/Ranged/GildedGrenade";

        public override void SetDefaults()
        {
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.Size = new Vector2(12);

            Projectile.friendly = true;
            Projectile.hostile = false;

            Projectile.penetrate = -1;

            Projectile.timeLeft = 1200;
        }

        public override void AI()
        {
            if (deathTimer > 0)
            {
                deathTimer--;
                if (deathTimer == 1)
                {
                    Explode();
                    Projectile.Kill();
                }

                Projectile.velocity.Y += 0.25f;
                Projectile.velocity *= 0.8f;
                Projectile.rotation += Projectile.velocity.Length() * 0.1f;

                Dust.NewDustPerfect(Projectile.Center + new Vector2(-5, -5).RotatedBy(Projectile.rotation), DustID.Torch, Vector2.Zero, 0, default, 2f).noGravity = true;

                return;
            }

            if (Projectile.timeLeft < 1180)
                Projectile.velocity.Y += 0.35f;

            if (Projectile.velocity.Y > 16f)
                Projectile.velocity.Y = 16f;

            Projectile.rotation += Projectile.velocity.Length() * 0.05f;
        }

        public override bool? CanDamage()
        {
            return deathTimer <= 0;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Unlock with { Volume = 0.4f }, Projectile.Center);

            deathTimer = 25;
            Projectile.velocity = -oldVelocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.7f, 1.5f);
            Projectile.velocity += Vector2.UnitY * -5f;

            Projectile.tileCollide = false;

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Vector2.One.RotatedBy(MathHelper.TwoPi * (i / 10f));

                Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, velocity * 2f, 0, default, 2f).noGravity = true;
            }

            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            SoundEngine.PlaySound(SoundID.Unlock with { Volume = 0.4f }, Projectile.Center);

            deathTimer = 25;
            Projectile.velocity = -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.7f, 1.5f);
            Projectile.velocity += Vector2.UnitY * -5f;

            Projectile.tileCollide = false;

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Vector2.One.RotatedBy(MathHelper.TwoPi * (i / 10f));

                Dust.NewDustPerfect(Projectile.Center, DustID.Smoke, velocity * 2f, 0, default, 2f).noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Main.instance.LoadProjectile(79);
            Main.instance.LoadProjectile(540);

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D starTex = TextureAssets.Projectile[79].Value;
            Texture2D bloomTex = TextureAssets.Projectile[540].Value;

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0f, 0f);

            if (deathTimer > 0)
            {
                float fade = deathTimer / 25f;

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(default, BlendState.Additive, default, default, default, null, Main.GameViewMatrix.TransformationMatrix);

                Main.spriteBatch.Draw(bloomTex, Projectile.Center + new Vector2(-5, -5).RotatedBy(Projectile.rotation) - Main.screenPosition, null,
                    new Color(150, 180, 50, 255) * fade * 0.35f, Projectile.rotation, bloomTex.Size() / 2f, 0.75f, 0f, 0f);
                
                Main.spriteBatch.Draw(bloomTex, Projectile.Center + new Vector2(-5, -5).RotatedBy(Projectile.rotation) - Main.screenPosition, null,
                    new Color(255, 255, 255, 255) * fade * 0.4f, Projectile.rotation, bloomTex.Size() / 2f, 1f, 0f, 0f);

                Main.spriteBatch.Draw(starTex, Projectile.Center + new Vector2(-5, -5).RotatedBy(Projectile.rotation) - Main.screenPosition, null,
                    new Color(150, 180, 50, 255) * fade, Projectile.rotation, starTex.Size() / 2f, 0.75f, 0f, 0f);
                
                Main.spriteBatch.Draw(starTex, Projectile.Center + new Vector2(-5, -5).RotatedBy(Projectile.rotation) - Main.screenPosition, null,
                    new Color(255, 255, 255, 255) * fade, Projectile.rotation, starTex.Size() / 2f, 0.5f, 0f, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(default, default, default, default, default, null, Main.GameViewMatrix.TransformationMatrix);

            }

            return false;
        }

        internal void Explode()
        {
            Player owner = Main.player[Projectile.owner];

            owner.GetModPlayer<TutorialModPlayer>().AddShake(4);

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.4f }, Projectile.Center);

            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(Projectile.GetSource_Death(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<GildedGrenadeExplosion>(),
                    Projectile.damage * 2, Projectile.knockBack * 1.5f, Projectile.owner, 90);
            }

            for (int i = 0; i < 13; i++)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(45f, 45f), DustID.Smoke,
                    Main.rand.NextVector2CircularEdge(2f, 2f), Main.rand.Next(50, 100), default, 2f).noGravity = true;
               
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(55f, 55f), DustID.Smoke,
                   Main.rand.NextVector2CircularEdge(2f, 2f), Main.rand.Next(50, 100), default, 3f).noGravity = true;
                
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2CircularEdge(75f, 75f), DustID.Smoke,
                  -Vector2.UnitY * Main.rand.NextFloat(1f, 3f), Main.rand.Next(50, 100), default, 3f).noGravity = true;
            }

            for (int i = 0; i < 2; i++)
            {
                Gore.NewGorePerfect(Projectile.GetSource_Death(), Projectile.Center
                    + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextVector2Circular(5f, 5f), GoreID.Smoke1, Main.rand.NextFloat(0.8f, 1.1f));

                Gore.NewGorePerfect(Projectile.GetSource_Death(), Projectile.Center
                    + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextVector2Circular(5f, 5f), GoreID.Smoke1, Main.rand.NextFloat(0.8f, 1.1f));

                Gore.NewGorePerfect(Projectile.GetSource_Death(), Projectile.Center
                    + Main.rand.NextVector2Circular(5f, 5f), Main.rand.NextVector2Circular(5f, 5f), GoreID.Smoke1, Main.rand.NextFloat(0.8f, 1.1f));
            }
        }
    }

    public class GildedGrenadeExplosion : ModProjectile
    {
        public override string Texture => "TutorialMod/Content/Items/Weapons/Ranged/GildedGrenade";
        private float Progress => Utils.Clamp(1 - Projectile.timeLeft / 10f, 0f, 1f);
        private float Radius => Projectile.ai[0] * Progress;

        public override void SetDefaults()
        {
            Projectile.alpha = 255;

            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 10;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
        }

        public override void AI()
        {
            for (int k = 0; k < 6; k++)
            {
                float rot = Main.rand.NextFloat(0, 6.28f);

                Dust.NewDustPerfect(Projectile.Center + Vector2.One.RotatedBy(rot) * Radius, DustID.Torch,
                    Vector2.One.RotatedBy(rot) * 0.5f, 0, default, Main.rand.NextFloat(1.5f, 3f)).noGravity = true;
                
                Dust.NewDustPerfect(Projectile.Center + Vector2.One.RotatedBy(rot) * Radius, DustID.Smoke,
                    Vector2.One.RotatedBy(rot) * 0.5f, 0, default, Main.rand.NextFloat(.5f, 1f)).noGravity = true;

                Dust.NewDustPerfect(Projectile.Center + Vector2.One.RotatedBy(rot) * Radius, DustID.Torch,
                    Vector2.One.RotatedBy(rot) * 0.5f + Main.rand.NextVector2Circular(2f, 2f), 50, default, Main.rand.NextFloat(0.5f, 1f));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 line = targetHitbox.Center.ToVector2() - Projectile.Center;
            line.Normalize();
            line *= Radius;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, Projectile.Center + line);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }
    }
}
