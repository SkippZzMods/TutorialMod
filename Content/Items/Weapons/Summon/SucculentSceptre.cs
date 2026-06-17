using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using TutorialMod.Content.Items.Weapons.Magic;

namespace TutorialMod.Content.Items.Weapons.Summon
{
    public class SucculentSceptre : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Some standard booleans for Summon weapons
            ItemID.Sets.GamepadWholeScreenUseRange[Item.type] = true;
            ItemID.Sets.LockOnIgnoresCollision[Item.type] = true;

            ItemID.Sets.StaffMinionSlotsRequired[Type] = 1f;
        }

        public override void SetDefaults()
        {           
            Item.Size = new Vector2(24, 24);

            Item.DamageType = DamageClass.Summon;
            Item.damage = 9;
            Item.knockBack = 2f;

            //Item.mana = 5; Summon staffs no longer use mana in 1.4.5, so I am electing to go that route.

            Item.UseSound = SoundID.Item44; // Summoning noise
            Item.useStyle = ItemUseStyleID.HoldUp;

            Item.useAnimation = 32;
            Item.useTime = 32;

            Item.shoot = ModContent.ProjectileType<SucculentSceptreMinion>();
            Item.shootSpeed = 2f;

            Item.rare = ItemRarityID.Blue;
            Item.value = Item.sellPrice(silver: 2);

            Item.buffType = ModContent.BuffType<SucculentSceptreSummonBuff>();

            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            position = Main.MouseWorld;
            player.LimitPointToPlayerReachableArea(ref position);

            player.AddBuff(ModContent.BuffType<SucculentSceptreSummonBuff>(), 2);

            for (int i = 0; i < 3; i++)
            {
                Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(5f, 5f), ModContent.DustType<SporebloomSparkle>(),           
                    Main.rand.NextVector2Circular(3f, 3f), 0, Color.MediumVioletRed with { A = 0 }, Main.rand.NextFloat(0.3f, 0.5f)).customData = true;
            }

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, -1);

            return false;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                 .AddIngredient(ItemID.Cactus, 50)
                 .AddIngredient(ItemID.FallenStar, 3)
                 .AddTile(TileID.WorkBenches)
                 .Register();
        }
    }

    public class SucculentSceptreSummonBuff : ModBuff
    {
        public override void Update(Player Player, ref int buffIndex)
        {
            if (Player.ownedProjectileCounts[ModContent.ProjectileType<SucculentSceptreMinion>()] > 0)
            {
                Player.buffTime[buffIndex] = 18000;
            }
            else
            {
                Player.DelBuff(buffIndex);
                buffIndex--;
            }
        }
    }

    public class SucculentSceptreMinion : ModProjectile
    {
        public enum MinionState
        {
            Idle = 0,
            FlyToTarget = 1,
            Dash = 2,
        }

        public int Timer // increments every frame
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public MinionState State // increments every frame
        {
            get => (MinionState)Projectile.ai[1];
            set => Projectile.ai[1] = (float)value;
        }
        public int TargetWhoAmI // increments every frame
        {
            get => (int)Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }

        float eyeRotation;
        float eyeDistance;

        int hitStopTimer;

        public Vector2 flyToPosition;

        public NPC Target => Owner.MinionAttackTargetNPC > 0 && Main.npc[Owner.MinionAttackTargetNPC].Distance(Projectile.Center) < 1500 ? Main.npc[Owner.MinionAttackTargetNPC] : (TargetWhoAmI < 0 ? null : Main.npc[TargetWhoAmI]);

        public Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            Main.projPet[Type] = true;

            // This is necessary for right-click targeting
            ProjectileID.Sets.MinionTargettingFeature[Projectile.type] = true;

            // This is needed so your minion can properly spawn when summoned and replaced when other minions are summoned	
            ProjectileID.Sets.MinionSacrificable[Projectile.type] = true;

            // Don't mistake this with "if this is true, then it will automatically home". It is just for damage reduction for certain NPCs
            ProjectileID.Sets.CultistIsResistantTo[Projectile.type] = true; 
            
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;

            Projectile.minionSlots = 1f;
            Projectile.ArmorPenetration = 5;
            Projectile.minion = true;

            Projectile.penetrate = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;

            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            TargetWhoAmI = -1;
        }

        public override bool MinionContactDamage()
        {
            return State == MinionState.Dash;
        }

        public override bool ShouldUpdatePosition()
        {
            return hitStopTimer <= 0;
        }

        public override bool PreAI()
        {
            if (hitStopTimer > 0)
            {
                hitStopTimer--;
                return false;
            }

            return true;
        }

        public override void AI()
        {
            if (Owner.HasBuff<SucculentSceptreSummonBuff>())
                Projectile.timeLeft = 2;

            if (Main.rand.NextBool(15))
                Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Poisoned, 0, 0, 100, default, 1.2f).noGravity = true;

            switch (State)
            {
                case MinionState.Idle:
                    Idle();
                    break;

                case MinionState.FlyToTarget:
                    FlyToTarget();
                    break;

                case MinionState.Dash:
                    Dash();
                    break;
            }
        }

        internal void Idle()
        {
            if (Target is null || !Target.active)
                TargetWhoAmI = GetTargetIndex();

            Vector2 idlePos = Owner.Center + new Vector2(-25 * Owner.direction - 25 * Projectile.minionPos * Owner.direction, (float)Math.Sin(Projectile.minionPos + Main.GlobalTimeWrappedHourly) * 20f);

            float dist = Vector2.Distance(Projectile.Center, idlePos);

            Vector2 toIdlePos = idlePos - Projectile.Center;
            if (toIdlePos.Length() < 0.0001f)
            {
                toIdlePos = Vector2.Zero;
            }
            else
            {
                float speed = 35f;
                if (dist < 1000f)
                    speed = MathHelper.Lerp(5f, 35f, dist / 1000f);

                if (dist < 100f)
                    speed = MathHelper.Lerp(0.1f, 5f, dist / 100f);

                toIdlePos.Normalize();
                toIdlePos *= speed;
            }

            Projectile.velocity = (Projectile.velocity * (25f - 1) + toIdlePos) / 25f;

            if (dist > 2000f)
            {
                Projectile.Center = idlePos;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }

            Projectile.rotation += Projectile.velocity.Length() * 0.03f;

            eyeRotation = Projectile.DirectionTo(Owner.Center).ToRotation();
            eyeDistance = MathHelper.Lerp(eyeDistance, 1.5f, 0.03f);

            if (Target is not null)
            {
                Timer = -60;
                flyToPosition = Main.rand.NextVector2CircularEdge(100f, 100f);

                State = MinionState.FlyToTarget;
            }         
        }

        internal void FlyToTarget()
        {
            if (Target is null || !Target.CanBeChasedBy(this))
            {
                Timer = 0;
                TargetWhoAmI = -1;
                State = MinionState.Idle;
                return;
            }

            Vector2 direction = Projectile.DirectionTo(Target.Center + flyToPosition);

            direction *= 5f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction, 0.075f);

            Projectile.rotation += Projectile.velocity.Length() * 0.005f;
            Projectile.rotation += MathHelper.Lerp(0.01f, 0.5f, 1f - Math.Abs(Timer) / 60f);
            
            eyeRotation = Projectile.DirectionTo(Target.Center + flyToPosition).ToRotation();
            eyeDistance = MathHelper.Lerp(eyeDistance, 2f, 0.03f);

            const int dist = 50 * 50;
            const int dist2 = 100 * 100;

            if ((Projectile.DistanceSQ(Target.Center + flyToPosition) < dist || ++Timer > 0) && Projectile.DistanceSQ(Target.Center + flyToPosition) < dist2)
            {
                Timer = 0;
                State = MinionState.Dash;
            }              
        }

        internal void Dash()
        {
            if (Target is null || !Target.CanBeChasedBy(this) || Owner.DistanceSQ(Target.Center) > dist)
            {
                Timer = 0;
                TargetWhoAmI = -1;
                State = MinionState.Idle;
                return;
            }

            if (Timer == 0)
            {
                Vector2 direction = Projectile.DirectionTo(Target.Center);

                Projectile.velocity = direction * 9f;

                eyeRotation = Projectile.DirectionTo(Target.Center).ToRotation();
                eyeDistance = 3.5f;
            }

            Projectile.rotation += Projectile.velocity.Length() * 0.05f;            
            
            Timer++;

            eyeDistance = MathHelper.Lerp(3.5f, 0f, Timer / 35f);

            if (Timer < 10)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), DustID.Poisoned, -Projectile.velocity * Main.rand.NextFloat(), 50, default, 1.2f).noGravity = true;
            }

            if (Timer > 10)
                Projectile.velocity *= 0.97f;

            if (Timer > 35)
            {
                flyToPosition = Main.rand.NextVector2CircularEdge(100f, 100f);

                State = MinionState.FlyToTarget;
                Timer = -60;
            }          
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Timer < 10)
            {
                hitStopTimer = 5;
                Projectile.timeLeft = 10;
            }

            target.AddBuff(BuffID.Poisoned, 30);

            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), ModContent.DustType<SporebloomSparkle>(),
                    -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.7f), 0, Color.MediumVioletRed with { A = 0 }, Main.rand.NextFloat(0.3f, 0.5f)).customData = true;

                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), DustID.Blood,
                    -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.3f), 50, default, Main.rand.NextFloat(1.2f, 2f)).noGravity = true;

                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(5f, 5f), DustID.Blood,
                    -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.3f), 50, default, Main.rand.NextFloat(0.9f, 1.2f));
            }
        }


        const int dist = 800 * 800;
        internal int GetTargetIndex()
        {
            NPC target = Main.npc.Where(n => n.CanBeChasedBy(this) && n.DistanceSQ(Owner.Center) < dist).OrderBy(n => n.DistanceSQ(Owner.Center)).FirstOrDefault();

            if (target is null)
                return -1;

            return target.whoAmI;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var tex = ModContent.Request<Texture2D>(Texture).Value;
            var texOutline = ModContent.Request<Texture2D>(Texture + "_Outline").Value;
            var texGlow = ModContent.Request<Texture2D>(Texture + "_Glow").Value;
            var eyeTex = ModContent.Request<Texture2D>(Texture + "_Eye").Value;
          
            var starTex = TextureAssets.Projectile[79].Value;
            var bloomTex = TextureAssets.Projectile[540].Value;
            
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f;
                float lerp = 1f - i / (float)Projectile.oldPos.Length;

                // draw afterimages here

                Color color = lightColor;

                if (State == MinionState.Dash && Timer <= 35)
                {
                    float interpolant = Timer / 35f;
                    if (Timer > 35)
                        interpolant = 1;

                    color = Color.Lerp(Color.MediumVioletRed with { A = 0 }, lightColor, interpolant);
                }

                Main.spriteBatch.Draw(tex, pos - Main.screenPosition, null, color * lerp, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0f, 0f);
            }

            Main.spriteBatch.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0f, 0f);

            Vector2 offset = Vector2.One.RotatedBy(eyeRotation) * eyeDistance;

            Main.spriteBatch.Draw(eyeTex, Projectile.Center + offset - Main.screenPosition, null, lightColor, 0f, eyeTex.Size() / 2f, Projectile.scale, 0f, 0f);
            
            if (State == MinionState.Dash && Timer <= 35)
            {
                float interpolant = 1f - Timer / 35f;
                if (Timer > 35)
                    interpolant = 0;

                Color color = Color.MediumVioletRed with { A = 0 } * interpolant;

                Main.spriteBatch.Draw(texOutline, Projectile.Center - Main.screenPosition, null, color, Projectile.rotation, texOutline.Size() / 2f, Projectile.scale, 0f, 0f);

                color = Color.Red with { A = 0 } * interpolant;
                
                Main.spriteBatch.Draw(bloomTex, Projectile.Center + offset - Main.screenPosition, null, color * 0.2f, Projectile.rotation, bloomTex.Size() / 2f, 0.3f, 0f, 0f);

                Main.spriteBatch.Draw(starTex, Projectile.Center + offset - Main.screenPosition, null, color, Projectile.rotation, starTex.Size() / 2f, 0.4f, 0f, 0f);

                Main.spriteBatch.Draw(starTex, Projectile.Center + offset - Main.screenPosition, null, Color.White with { A = 0 } * interpolant, Projectile.rotation, starTex.Size() / 2f, 0.2f, 0f, 0f);
            }

            return false;
        }
    }
}
