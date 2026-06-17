using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
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
            Item.value = Item.sellPrice(silver: 5);

            Item.buffType = ModContent.BuffType<SucculentSceptreSummonBuff>();

            Item.noMelee = true;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            position = Main.MouseWorld;
            player.LimitPointToPlayerReachableArea(ref position);

            player.AddBuff(ModContent.BuffType<SucculentSceptreSummonBuff>(), 2);

            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI, -1);
            
            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(15f, 15f), ModContent.DustType<SporebloomSparkle>(), Main.rand.NextVector2Circular(5f, 5f), 0, new Color(202, 68, 128, 0), Main.rand.NextFloat(0.3f, 0.5f)).customData = true;
            }

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
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = true;
        }

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
        const int MAX_DIST = 900 * 900;

        public enum MinionState
        {
            Idle = 0,
            FlyToTarget = 1,
            Dash = 2
        }

        public int Timer
        {
            get => (int)Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public MinionState State
        {
            get => (MinionState)Projectile.ai[1];
            set => Projectile.ai[1] = (float)value;
        }

        public int TargetWhoAmI
        {
            get => (int)Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }

        Vector2 attackOffset;

        float eyeRotation;

        public NPC Target => Owner.MinionAttackTargetNPC > 0 && Main.npc[Owner.MinionAttackTargetNPC].CanBeChasedBy(this) && Owner.DistanceSQ(Main.npc[Owner.MinionAttackTargetNPC].Center) < MAX_DIST ? Main.npc[Owner.MinionAttackTargetNPC] : (TargetWhoAmI < 0 ? null : Main.npc[TargetWhoAmI]);

        public Player Owner => Main.player[Projectile.owner];

        public override void SetStaticDefaults()
        {
            Main.projPet[Type] = true;

            ProjectileID.Sets.MinionTargettingFeature[Type] = true;
            ProjectileID.Sets.MinionSacrificable[Type] = true;

            ProjectileID.Sets.CultistIsResistantTo[Type] = true;

            ProjectileID.Sets.TrailCacheLength[Type] = 7;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.Size = new(16);

            Projectile.friendly = true;

            Projectile.minion = true;
            Projectile.minionSlots = 1f;

            Projectile.DamageType = DamageClass.Summon;
            Projectile.ArmorPenetration = 5;
            Projectile.penetrate = -1;

            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;

            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
        }

        public override bool MinionContactDamage()
        {
            return State == MinionState.Dash;
        }

        public override void AI()
        {
            if (Owner.HasBuff<SucculentSceptreSummonBuff>())
                Projectile.timeLeft = 2;

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

            Vector2 idlePosition = Owner.Center + new Vector2(-25f * Owner.direction - 25 * Projectile.minionPos * Owner.direction, (float)Math.Sin(Main.GlobalTimeWrappedHourly * 0.5f + Projectile.minionPos) * 12f);

            float distance = Vector2.Distance(Projectile.Center, idlePosition);

            Vector2 toIdlePosition = idlePosition - Projectile.Center;

            if (toIdlePosition.Length() < 0.0001f)
            {
                toIdlePosition = Vector2.Zero;
            }
            else
            {
                float speed = 15f;

                if (distance < 1000f)
                    speed = MathHelper.Lerp(3f, 15f, distance / 1000f);

                if (distance < 100f)
                    speed = MathHelper.Lerp(0.1f, 3f, distance / 100f);

                toIdlePosition.Normalize();
                toIdlePosition *= speed;
            }

            Projectile.velocity = (Projectile.velocity * 24f + toIdlePosition) / 25f;

            if (distance > 2000f)
            {
                Projectile.Center = idlePosition;
                Projectile.velocity = Main.rand.NextVector2Circular(1f, 1f);
                Projectile.netUpdate = true;
            }

            Projectile.rotation += Projectile.velocity.Length() * 0.03f;

            eyeRotation = Utils.AngleLerp(eyeRotation, Projectile.DirectionTo(Owner.Center).ToRotation(), 0.1f);

            if (Main.rand.NextBool(20))
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), DustID.Poisoned, Main.rand.NextVector2Circular(3f, 3f), 150, default, Main.rand.NextFloat(0.8f, 1.5f)).noGravity = true;
            }

            if (Target is not null)
            {
                Timer = -60;
                attackOffset = Main.rand.NextVector2CircularEdge(100f, 100f);

                State = MinionState.FlyToTarget;
            }
        }

        internal void FlyToTarget()
        {
            if (Target is null || !Target.CanBeChasedBy(this) || Target.DistanceSQ(Projectile.Center) > MAX_DIST)
            {
                Timer = 0;
                TargetWhoAmI = -1;
                State = MinionState.Idle;
                return;
            }

            Vector2 targetPosition = Target.Center + attackOffset;

            Vector2 direction = Projectile.DirectionTo(targetPosition);

            direction *= 5f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction, 0.09f);

            Projectile.rotation += Projectile.velocity.Length() * 0.03f;
            Projectile.rotation += MathHelper.Lerp(0.01f, 0.5f, 1f - Math.Abs(Timer) / 60f);
            
            eyeRotation = Utils.AngleLerp(eyeRotation, Projectile.DirectionTo(targetPosition).ToRotation(), 0.1f);

            const int dist = 50 * 50;

            if ((Projectile.DistanceSQ(targetPosition) < dist || ++Timer > 0) && Projectile.DistanceSQ(targetPosition) < dist * 2)
            {
                Timer = 0;
                State = MinionState.Dash;

                Vector2 dashDirection = Projectile.DirectionTo(Target.Center);

                Projectile.velocity = dashDirection * Main.rand.NextFloat(9f, 12f);

                eyeRotation = Projectile.DirectionTo(Target.Center).ToRotation();

                SoundEngine.PlaySound(SoundID.DD2_MonkStaffSwing with { Volume = 0.75f, PitchVariance = 0.3f}, Projectile.Center);
            }
        }

        internal void Dash()
        {
            if (Target is null || !Target.CanBeChasedBy(this) || Target.DistanceSQ(Projectile.Center) > MAX_DIST)
            {
                Timer = 0;
                TargetWhoAmI = -1;
                State = MinionState.Idle;
                return;
            }

            Projectile.rotation += Projectile.velocity.Length() * 0.05f;

            Timer++;

            if (Timer > 10)
                Projectile.velocity *= 0.97f;
            else
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), DustID.Poisoned, -Projectile.velocity * Main.rand.NextFloat(0.2f), 150, default, Main.rand.NextFloat(0.8f, 1.5f)).noGravity = true;

            if (Timer > 35)
            {
                attackOffset = Main.rand.NextVector2CircularEdge(100f, 100f);

                State = MinionState.FlyToTarget;
                Timer = -60;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 30);

            for (int i = 0; i < 5; i++)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), ModContent.DustType<SporebloomSparkle>(), -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.4f), 0, new Color(202, 68, 128, 0), Main.rand.NextFloat(0.3f, 0.5f)).customData = true;

                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), DustID.Blood, -Projectile.velocity.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.4f), 0, default, Main.rand.NextFloat(1.1f, 1.5f)).noGravity = true;

                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(15f, 15f), DustID.Blood, -Projectile.velocity.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.7f), 0, default, Main.rand.NextFloat(0.8f, 1.2f)).fadeIn = 1f;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            var tex = ModContent.Request<Texture2D>(Texture).Value;
            var texEye = ModContent.Request<Texture2D>(Texture + "_Eye").Value;
            var texOutline = ModContent.Request<Texture2D>(Texture + "_Outline").Value;

            var starTex = TextureAssets.Projectile[79].Value;
            var bloomTex = TextureAssets.Projectile[540].Value;

            SpriteBatch sb = Main.spriteBatch;
           
            if (State == MinionState.Dash || State == MinionState.FlyToTarget)
            {
                for (int i = 0; i < Projectile.oldPos.Length; i++)
                {
                    Vector2 pos = Projectile.oldPos[i] + Projectile.Size / 2f;
                    float lerp = 1f - i / (float)Projectile.oldPos.Length;

                    float fadeIn;

                    if (Timer < 0)
                        fadeIn = 1f - Math.Abs(Timer) / 60f;
                    else
                        fadeIn = 1f - Timer / 35f;

                    Color color = lightColor;

                    if (Timer > 0 && State == MinionState.Dash)
                        color = Color.Lerp(new Color(202, 68, 128, 0), lightColor, 1f - fadeIn);

                    color *= lerp * fadeIn;

                    sb.Draw(tex, pos - Main.screenPosition, null, color, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0f, 0f);
                }
            }

            sb.Draw(tex, Projectile.Center - Main.screenPosition, null, lightColor, Projectile.rotation, tex.Size() / 2f, Projectile.scale, 0f, 0f);

            Vector2 offset = Vector2.One.RotatedBy(eyeRotation) * 3f;

            sb.Draw(texEye, Projectile.Center + offset - Main.screenPosition, null, lightColor, 0f, texEye.Size() / 2f, Projectile.scale, 0f, 0f);

            if (State == MinionState.Dash && Timer < 35)
            {
                float interpolant = 1f - Timer / 35f;

                Color color = new Color(202, 68, 128, 0);

                sb.Draw(texOutline, Projectile.Center - Main.screenPosition, null, color * interpolant, Projectile.rotation, texOutline.Size() / 2f, Projectile.scale, 0f, 0f);

                sb.Draw(bloomTex, Projectile.Center + offset - Main.screenPosition, null, color * interpolant * 0.25f, 0f, bloomTex.Size() / 2f, 0.3f, 0f, 0f);

                sb.Draw(starTex, Projectile.Center + offset - Main.screenPosition, null, color, Projectile.rotation, starTex.Size() / 2f, 0.4f * interpolant, 0f, 0f);

                sb.Draw(starTex, Projectile.Center + offset - Main.screenPosition, null, Color.White with { A = 0 }, Projectile.rotation, starTex.Size() / 2f, 0.2f * interpolant, 0f, 0f);
            }

            return false;
        }

        internal int GetTargetIndex()
        {
            NPC target = Main.npc.Where(n => n.CanBeChasedBy(this) && n.DistanceSQ(Projectile.Center) < MAX_DIST).OrderBy(n => n.DistanceSQ(Projectile.Center)).FirstOrDefault();

            if (target is null)
                return -1;

            return target.whoAmI;
        }
    }
}
