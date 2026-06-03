using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using TutorialMod.Content.Items.Weapons.Ranged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Audio;
using Terraria.GameContent;

namespace TutorialMod.Content.Items.Weapons.Magic
{
    public class SporebloomStaff : ModItem
    {
        public override bool CanUseItem(Player player) => player.ownedProjectileCounts[Item.shoot] <= 0;
        public override void SetDefaults()
        {
            Item.Size = new Vector2(16, 16);

            Item.DamageType = DamageClass.Magic;
            Item.damage = 5;
            Item.knockBack = 1f;
            Item.mana = 5;

            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Shoot;

            Item.useAnimation = 60;
            Item.useTime = 60;

            Item.shoot = ModContent.ProjectileType<SporebloomStaffHoldout>();
            Item.shootSpeed = 2f;

            Item.rare = ItemRarityID.White;

            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true; // hold weapon
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                 .AddIngredient(ItemID.Wood, 35)
                 .AddIngredient(ItemID.Mushroom, 5)
                 .AddIngredient(ItemID.Daybloom, 1)
                 .AddTile(TileID.WorkBenches)
                 .Register();
        }
    }

    public class SporebloomStaffHoldout : ModProjectile
    {
        public bool Dying;
        public int _recoilTimer;
        public int Timer // increments every frame
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }
        public int UseTime // equivalent to item.usetime
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }
        public int AnimationTime // used for the shoot animation. Decreases by 1 when greater than zero in AI()
        {
            get => (int)Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }

        // if the player can hold the held projectile
        public bool CanHold => Owner.HeldItem.ModItem is SporebloomStaff && Owner.channel && !Owner.CCed && !Owner.noItems;

        public Vector2 ArmPosition => Owner.RotatedRelativePoint(Owner.MountedCenter, true) + new Vector2(20f, 0f).RotatedBy(Projectile.rotation) + ArmOffset;
        public Vector2 ArmOffset;
        public Player Owner => Main.player[Projectile.owner];
        public override string Texture => "TutorialMod/Content/Items/Weapons/Magic/SporebloomStaff";

        public override bool? CanDamage() => false;
        public override void SetDefaults()
        {
            Projectile.width = 42;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            if (!CanHold && !Dying && Timer > 10f)
            {
                Dying = true;
                Projectile.timeLeft = 10;
            }

            if (AnimationTime > 0)
                AnimationTime--;

            if (_recoilTimer > 0)
            {
                int offset = (int)MathHelper.Min(60, _recoilTimer);

                ArmOffset = new Vector2(-15f * (offset / 60f), 0f).RotatedBy(Projectile.rotation);

                _recoilTimer--;
            }

            if (Timer == 0f)
            {
                // we would like to run code that deals with the players mouse only on the players client (for multiplayer purposes)
                if (Main.myPlayer == Projectile.owner)
                    Projectile.velocity = Owner.DirectionTo(Main.MouseWorld);
                
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.netUpdate = true;
                UseTime = CombinedHooks.TotalUseTime(Owner.itemTime, Owner, Owner.HeldItem);
            }

            if (!Dying)
            {
                // looping attack behavior whilst held
                if (Timer % UseTime == 0)
                    AnimationTime = 20;

                UpdateHeldProjectile();

                Timer++;

                // this code runs once every five ticks when animation time is greater than zero
                const int ticks = 5;
                if (AnimationTime > 0 && AnimationTime % ticks == 0)
                {
                    SpawnProjectiles();
                    Projectile.velocity = Projectile.velocity.RotatedByRandom(0.15f);
                    _recoilTimer += Main.rand.Next(7, 15);
                }
            }
            else
            {
                UpdateHeldProjectile(false, false);
            }
        }

        public void SpawnProjectiles()
        {
            if (Owner.statMana < 5)
            {
                Dying = true;
                Projectile.timeLeft = 10;
                return;
            }
            else
                Owner.statMana -= 5;
                
            SoundEngine.PlaySound(SoundID.Item43 with { PitchVariance = 0.3f, Volume = 0.5f, Pitch = -0.3f}, Projectile.Center);

            for (int i = 0; i < 2; i++)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromAI(), Projectile.Center, Projectile.velocity.RotatedByRandom(0.25f) * Main.rand.NextFloat(8f, 12f), ModContent.ProjectileType<SporebloomStaffSpore>(), Projectile.damage, Projectile.knockBack, Projectile.owner);
            }

            for (int i = 0; i < 1 + Main.rand.Next(3); i++)
            {
                Dust.NewDustPerfect(Projectile.Center + new Vector2(15f, 0f).RotatedBy(Projectile.rotation) + Main.rand.NextVector2Circular(5f, 5f), ModContent.DustType<SporebloomSmoke>(),
                    Projectile.velocity.RotatedByRandom(0.25f) * Main.rand.NextFloat(8f, 12f), 0, Color.Lerp(new Color(201, 45, 45), new Color(195, 105, 39), Main.rand.NextFloat()) * Main.rand.NextFloat(0.2f, 0.5f), Main.rand.NextFloat(1.5f, 2.3f));

                Dust.NewDustPerfect(Projectile.Center + new Vector2(15f, 0f).RotatedBy(Projectile.rotation) + Main.rand.NextVector2Circular(5f, 5f), ModContent.DustType<SporebloomSparkle>(),
                    Projectile.velocity.RotatedByRandom(0.25f) * Main.rand.NextFloat(8f, 12f), 0, Color.Lerp(new Color(201, 45, 45), new Color(195, 105, 39), Main.rand.NextFloat()) with { A = 0 }, Main.rand.NextFloat(0.5f, 0.6f)).customData = true;
            }
        }

        /// <summary>
        /// Updates the basic variables needed for a held projectile
        /// </summary>
        public void UpdateHeldProjectile(bool updateTimeleft = true, bool updateVelocity = true)
        {
            Owner.ChangeDir(Projectile.direction);
            Owner.heldProj = Projectile.whoAmI;

            Owner.itemTime = 2;
            Owner.itemAnimation = 2;

            if (updateTimeleft)
                Projectile.timeLeft = 2;

            Projectile.rotation = Projectile.velocity.ToRotation();
            Owner.itemRotation = Utils.ToRotation(Projectile.velocity * Projectile.direction);

            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.ToRadians(90f));

            Projectile.position = ArmPosition - Projectile.Size * 0.5f;

            if (Main.myPlayer == Projectile.owner && updateVelocity)
            {
                Vector2 oldVelocity = Projectile.velocity;

                Projectile.velocity = Vector2.Lerp(Projectile.velocity, Owner.DirectionTo(Main.MouseWorld), 0.05f);

                if (Projectile.velocity != oldVelocity)
                {
                    Projectile.netSpam = 0;
                    Projectile.netUpdate = true;
                }
            }

            Projectile.spriteDirection = Projectile.direction;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var tex = ModContent.Request<Texture2D>(Texture).Value;
            var texOutline = ModContent.Request<Texture2D>(Texture + "_Outline").Value;
            
            SpriteEffects spriteEffects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            Vector2 position = ArmPosition - Main.screenPosition;

            float rotation = Projectile.rotation + (spriteEffects == SpriteEffects.FlipHorizontally ? MathHelper.Pi : 0f) + MathHelper.PiOver4 * Projectile.spriteDirection;

            float fadeIn = 1f;

            if (Timer < 10f)
                fadeIn = Timer / 10f;
            else if (Dying)
                fadeIn = Projectile.timeLeft / 10f;

            Main.spriteBatch.Draw(tex, position, null, lightColor * fadeIn, rotation, tex.Size() / 2f, Projectile.scale, spriteEffects, 0f);

            if (_recoilTimer > 0)
            {
                float opacity = MathHelper.Min(60, _recoilTimer) / 60f;

                Main.spriteBatch.Draw(texOutline, position, null, new Color(254, 67, 58) * opacity, rotation, texOutline.Size() / 2f, Projectile.scale, spriteEffects, 0f);
            }

            return false;
        }
    }

    public class SporebloomStaffSpore : ModProjectile
    {
        internal Color drawColor;
        public override void SetDefaults()
        {
            Projectile.width = 25;
            Projectile.height = 25;
            Projectile.friendly = true;

            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = false;

            Projectile.scale *= Main.rand.NextFloat(1.25f, 2.5f);
            Projectile.rotation = Main.rand.NextFloat(6.28f);

            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 15;

            Projectile.frame = Main.rand.Next(3);

            drawColor = Color.Lerp(new Color(201, 45, 45), new Color(195, 105, 39), Main.rand.NextFloat());
        }

        public override void AI()
        {
            Projectile.velocity *= 0.96f;
            Projectile.rotation += Projectile.velocity.Length() * 0.02f;

            if (Main.rand.NextBool(110))
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), ModContent.DustType<SporebloomSmoke>(),
                    Main.rand.NextVector2Circular(5f, 5f), 0, Color.Lerp(new Color(201, 45, 45), new Color(195, 105, 39), Main.rand.NextFloat()) * Main.rand.NextFloat(0.2f, 0.5f), Main.rand.NextFloat(0.5f, 1.2f));
            }

            if (Main.rand.NextBool(90))
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f) * Projectile.scale, DustID.Poisoned, Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.Next(150, 220), default, Main.rand.NextFloat(0.8f, 1.65f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(target.Center, DustID.Poisoned, Main.rand.NextVector2Circular(1.5f, 1.5f), Main.rand.Next(50, 200), default, Main.rand.NextFloat(0.8f, 1.65f));
            }

            target.AddBuff(BuffID.Poisoned, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;

            Rectangle frame = tex.Frame(1, 3, 0, Projectile.frame);

            float progress = 1f - Projectile.timeLeft / 240f;

            float fadeIn;
            if (progress < 0.25f)
                fadeIn = progress / 0.25f;
            else
                fadeIn = 1f - (progress - 0.25f) / 0.75f;

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, frame, drawColor * 0.5f * fadeIn, Projectile.rotation, frame.Size() / 2f, Projectile.scale * MathHelper.Lerp(1f, 1.5f, progress), SpriteEffects.None, 0f);

            return false;
        }
    }

    public class SporebloomSmoke : ModDust
    {
        public override string Texture => "TutorialMod/Content/Items/Weapons/Magic/SporebloomStaffSpore";
        public override void OnSpawn(Dust dust)
        {
            dust.noGravity = true;
            dust.frame = new Rectangle(0, Main.rand.Next(3), 32, 32);
            dust.rotation = Main.rand.NextFloat(6.28f);
        }

        public override bool Update(Dust dust)
        {
            dust.velocity *= 0.95f;

            dust.scale += 0.02f;
            dust.alpha += 3;

            dust.position += dust.velocity;
            dust.rotation += dust.velocity.Length() * 0.02f;

            if (dust.alpha >= 255)
                dust.active = false;

            return false;
        }

        public override bool PreDraw(Dust dust)
        {
            Color color = dust.color;

            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;

            Rectangle frame = tex.Frame(1, 3, 0, dust.frame.Y);

            float progress = 1f - dust.alpha / 255f;

            float fadeIn;
            if (progress < 0.25f)
                fadeIn = progress / 0.25f;
            else
                fadeIn = 1f - (progress - 0.25f) / 0.75f;

            Main.spriteBatch.Draw(tex, dust.position - Main.screenPosition, frame, color * 0.5f * fadeIn, dust.rotation, frame.Size() / 2f, dust.scale * MathHelper.Lerp(1f, 1.5f, progress), SpriteEffects.None, 0f);

            return false;
        }
    }

    // set customData to true for rotation
    public class SporebloomSparkle : ModDust
    {
        public override string Texture => "TutorialMod/Assets/Invisible";

        public override void OnSpawn(Dust dust)
        {
            dust.frame = new Rectangle(0, 0, 4, 4);
        }

        public override bool Update(Dust dust)
        {
            dust.position += dust.velocity;
            dust.velocity *= 0.95f;

            if (dust.customData is not null && (bool)dust.customData)
                dust.rotation += dust.velocity.Length() * 0.05f;

            dust.scale *= 0.95f;

            if (dust.scale < 0.02f)
                dust.active = false;

            Lighting.AddLight(dust.position, dust.color.ToVector3() * 0.15f);

            return false;
        }

        public override bool PreDraw(Dust dust)
        {
            Color color = dust.color;

            float lerper = 1f - dust.alpha / 255f;

            Texture2D starTex = TextureAssets.Projectile[79].Value;
            Texture2D bloomTex = TextureAssets.Projectile[540].Value;

            Main.spriteBatch.Draw(bloomTex, dust.position - Main.screenPosition, null, color * lerper * 0.05f, dust.rotation, bloomTex.Size() / 2f, dust.scale * 0.5f * lerper, 0f, 0f);

            Main.spriteBatch.Draw(starTex, dust.position - Main.screenPosition, null, color * lerper, dust.rotation, starTex.Size() / 2f, dust.scale * lerper, 0f, 0f);
            
            Main.spriteBatch.Draw(starTex, dust.position - Main.screenPosition, null, Color.White with { A = 0 } * lerper, dust.rotation, starTex.Size() / 2f, dust.scale * 0.5f * lerper, 0f, 0f);

            return false;
        }
    }
}
