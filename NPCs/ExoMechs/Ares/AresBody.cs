using CalamityMod.Events;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Potions;
using CalamityMod.Items.TreasureBags;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Rogue;
using CalamityMod.NPCs.ExoMechs.Apollo;
using CalamityMod.NPCs.ExoMechs.Artemis;
using CalamityMod.NPCs.ExoMechs.Thanatos;
using CalamityMod.Projectiles.Boss;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.NPCs.ExoMechs.Ares
{
	[AutoloadBossHead]
	public class AresBody : ModNPC
    {
		public enum Phase
		{
			Normal = 0,
			Deathrays = 1
		}

		public float AIState
		{
			get => npc.Calamity().newAI[0];
			set => npc.Calamity().newAI[0] = value;
		}

		public enum SecondaryPhase
		{
			Nothing = 0,
			Passive = 1,
			PassiveAndImmune = 2
		}

		public float SecondaryAIState
		{
			get => npc.Calamity().newAI[1];
			set => npc.Calamity().newAI[1] = value;
		}

		// Number of frames on the X and Y axis
		private const int maxFramesX = 6;
		private const int maxFramesY = 8;

		// Counters for frames on the X and Y axis
		private int frameX = 0;
		private int frameY = 0;

		// Frame limit per animation, these are the specific frames where each animation ends
		private const int normalFrameLimit = 11;
		private const int firstStageDeathrayChargeFrameLimit = 23;
		private const int secondStageDeathrayChargeFrameLimit = 35;
		private const int finalStageDeathrayChargeFrameLimit = 47;

		// Default life ratio for the other mechs
		private const float defaultLifeRatio = 5f;

		// Variable used to stop the arm spawning loop
		private bool armsSpawned = false;

		// Total duration of the deathray telegraph
		public const float deathrayTelegraphDuration = 240f;

		// Total duration of the deathrays
		public const float deathrayDuration = 600f;

		public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("XF-09 Ares");
		}

        public override void SetDefaults()
        {
			npc.npcSlots = 5f;
			npc.damage = 100;
			npc.width = 220;
            npc.height = 252;
            npc.defense = 100;
			npc.DR_NERD(0.35f);
			npc.LifeMaxNERB(1000000, 1150000, 500000);
			double HPBoost = CalamityConfig.Instance.BossHealthBoost * 0.01;
			npc.lifeMax += (int)(npc.lifeMax * HPBoost);
			npc.aiStyle = -1;
            aiType = -1;
			npc.Opacity = 0f;
            npc.knockBackResist = 0f;
            npc.value = Item.buyPrice(3, 33, 0, 0);
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.HitSound = SoundID.NPCHit4;
            npc.DeathSound = SoundID.NPCDeath14;
            npc.netAlways = true;
			npc.boss = true;
			music = /*CalamityMod.Instance.GetMusicFromMusicMod("AdultEidolonWyrm") ??*/ MusicID.Boss3;
			bossBag = ModContent.ItemType<DraedonTreasureBag>();
		}

        public override void SendExtraAI(BinaryWriter writer)
        {
			writer.Write(frameX);
			writer.Write(frameY);
			writer.Write(armsSpawned);
			writer.Write(npc.chaseable);
            writer.Write(npc.dontTakeDamage);
			writer.Write(npc.localAI[0]);
			for (int i = 0; i < 4; i++)
				writer.Write(npc.Calamity().newAI[i]);
		}

        public override void ReceiveExtraAI(BinaryReader reader)
        {
			frameX = reader.ReadInt32();
			frameY = reader.ReadInt32();
			armsSpawned = reader.ReadBoolean();
			npc.chaseable = reader.ReadBoolean();
			npc.dontTakeDamage = reader.ReadBoolean();
			npc.localAI[0] = reader.ReadSingle();
			for (int i = 0; i < 4; i++)
				npc.Calamity().newAI[i] = reader.ReadSingle();
		}

        public override void AI()
        {
			CalamityGlobalNPC calamityGlobalNPC = npc.Calamity();

			CalamityGlobalNPC.draedonExoMechPrime = npc.whoAmI;

			// Difficulty modes
			bool malice = CalamityWorld.malice || BossRushEvent.BossRushActive;
			bool death = CalamityWorld.death || malice;
			bool revenge = CalamityWorld.revenge || malice;
			bool expertMode = Main.expertMode || malice;

			if (npc.ai[2] > 0f)
				npc.realLife = (int)npc.ai[2];

			// Spawn arms
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				if (!armsSpawned && npc.ai[0] == 0f)
				{
					int totalArms = 4;
					int Previous = npc.whoAmI;
					for (int i = 0; i < totalArms; i++)
					{
						int lol = 0;
						switch (i)
						{
							case 0:
								lol = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<AresLaserCannon>(), npc.whoAmI);
								break;
							case 1:
								lol = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<AresPlasmaFlamethrower>(), npc.whoAmI);
								break;
							case 2:
								lol = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<AresTeslaCannon>(), npc.whoAmI);
								break;
							case 3:
								lol = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<AresGaussNuke>(), npc.whoAmI);
								break;
							default:
								break;
						}

						Main.npc[lol].realLife = npc.whoAmI;
						Main.npc[lol].ai[2] = npc.whoAmI;
						Main.npc[lol].ai[1] = Previous;
						Main.npc[Previous].ai[0] = lol;
						NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, lol, 0f, 0f, 0f, 0);
						Previous = lol;
					}
					armsSpawned = true;
				}
			}

			if (npc.life > Main.npc[(int)npc.ai[0]].life)
				npc.life = Main.npc[(int)npc.ai[0]].life;

			// Percent life remaining
			float lifeRatio = npc.life / (float)npc.lifeMax;

			// Check if the other exo mechs are alive
			int otherExoMechsAlive = 0;
			bool exoWormAlive = false;
			bool exoTwinsAlive = false;
			if (CalamityGlobalNPC.draedonExoMechWorm != -1)
			{
				if (Main.npc[CalamityGlobalNPC.draedonExoMechWorm].active)
				{
					otherExoMechsAlive++;
					exoWormAlive = true;
				}
			}

			// There is no point in checking for the other twin because they have linked HP
			if (CalamityGlobalNPC.draedonExoMechTwinGreen != -1)
			{
				if (Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].active)
				{
					otherExoMechsAlive++;
					exoTwinsAlive = true;
				}
			}

			// These are 5 by default to avoid triggering passive phases after the other mechs are dead
			float exoWormLifeRatio = defaultLifeRatio;
			float exoTwinsLifeRatio = defaultLifeRatio;
			if (exoWormAlive)
				exoWormLifeRatio = Main.npc[CalamityGlobalNPC.draedonExoMechWorm].life / (float)Main.npc[CalamityGlobalNPC.draedonExoMechWorm].lifeMax;
			if (exoTwinsAlive)
				exoTwinsLifeRatio = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].life / (float)Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].lifeMax;
			float totalOtherExoMechLifeRatio = exoWormLifeRatio + exoTwinsLifeRatio;

			// Check if any of the other mechs are passive
			bool exoWormPassive = false;
			bool exoTwinsPassive = false;
			if (exoWormAlive)
				exoWormPassive = Main.npc[CalamityGlobalNPC.draedonExoMechWorm].Calamity().newAI[1] == (float)ThanatosHead.SecondaryPhase.Passive;
			if (exoTwinsAlive)
				exoTwinsPassive = Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].Calamity().newAI[1] == (float)Apollo.Apollo.SecondaryPhase.Passive;
			bool anyOtherExoMechPassive = exoWormPassive || exoTwinsPassive;

			// Phases
			bool spawnOtherExoMechs = lifeRatio > 0.4f && otherExoMechsAlive == 0 && lifeRatio < 0.7f;
			bool berserk = lifeRatio < 0.4f || (otherExoMechsAlive == 0 && lifeRatio < 0.7f);
			bool lastMechAlive = berserk && otherExoMechsAlive == 0;

			// If Ares doesn't go berserk
			bool otherMechIsBerserk = exoWormLifeRatio < 0.4f || exoTwinsLifeRatio < 0.4f;

			// Get a target
			if (npc.target < 0 || npc.target == Main.maxPlayers || Main.player[npc.target].dead || !Main.player[npc.target].active)
				npc.TargetClosest();

			// Despawn safety, make sure to target another player if the current player target is too far away
			if (Vector2.Distance(Main.player[npc.target].Center, npc.Center) > CalamityGlobalNPC.CatchUpDistance200Tiles)
				npc.TargetClosest();

			// Target variable
			Player player = Main.player[npc.target];

			// Despawn if target is dead
			bool targetDead = false;
			if (player.dead)
			{
				npc.TargetClosest(false);
				player = Main.player[npc.target];
				if (player.dead)
				{
					targetDead = true;

					AIState = (float)Phase.Normal;
					calamityGlobalNPC.newAI[2] = 0f;
					calamityGlobalNPC.newAI[3] = 0f;

					npc.velocity.Y -= 2f;
					if ((double)npc.position.Y < Main.topWorld + 16f)
						npc.velocity.Y -= 2f;

					if ((double)npc.position.Y < Main.topWorld + 16f)
					{
						for (int a = 0; a < Main.maxNPCs; a++)
						{
							if (Main.npc[a].type == npc.type || Main.npc[a].type == ModContent.NPCType<Artemis.Artemis>() || Main.npc[a].type == ModContent.NPCType<Apollo.Apollo>() ||
								Main.npc[a].type == ModContent.NPCType<AresLaserCannon>() || Main.npc[a].type == ModContent.NPCType<AresPlasmaFlamethrower>() ||
								Main.npc[a].type == ModContent.NPCType<AresTeslaCannon>() || Main.npc[a].type == ModContent.NPCType<AresGaussNuke>() ||
								Main.npc[a].type == ModContent.NPCType<ThanatosHead>() || Main.npc[a].type == ModContent.NPCType<ThanatosBody1>() ||
								Main.npc[a].type == ModContent.NPCType<ThanatosBody2>() || Main.npc[a].type == ModContent.NPCType<ThanatosTail>())
								Main.npc[a].active = false;
						}
					}
				}
			}

			// General AI pattern
			// 0 - Fly above target
			// 1 - Fly towards the target, slow down when close enough
			// 2 - Fire deathrays from telegraph locations to avoid cheap hits and rotate them around for 10 seconds while the plasma and tesla arms fire projectiles to make dodging difficult
			// 3 - Go passive and fly above the target while firing less projectiles
			// 4 - Go passive, immune and invisible; fly above the target and do nothing until next phase

			// Attack patterns
			// If spawned first
			// Phase 1 - 0
			// Phase 2 - 4
			// Phase 3 - 3

			// If berserk, this is the last phase of Ares
			// Phase 4 - 1, 2

			// If not berserk
			// Phase 4 - 4
			// Phase 5 - 0

			// If berserk, this is the last phase of Ares
			// Phase 6 - 1, 2

			// If not berserk
			// Phase 6 - 4

			// Berserk, final phase of Ares
			// Phase 7 - 1, 2

			// Adjust opacity
			bool invisiblePhase = SecondaryAIState == (float)SecondaryPhase.PassiveAndImmune;
			npc.dontTakeDamage = invisiblePhase;
			if (!invisiblePhase)
			{
				npc.Opacity += 0.2f;
				if (npc.Opacity > 1f)
					npc.Opacity = 1f;
			}
			else
			{
				npc.Opacity -= 0.05f;
				if (npc.Opacity < 0f)
					npc.Opacity = 0f;
			}

			// Rotation
			npc.rotation = npc.velocity.X * 0.003f;

			// Light
			float lightScale = 765f;
			Lighting.AddLight(npc.Center, Main.DiscoR / lightScale, Main.DiscoG / lightScale, Main.DiscoB / lightScale);

			// Default vector to fly to
			Vector2 destination = SecondaryAIState == (float)SecondaryPhase.PassiveAndImmune ? new Vector2(player.Center.X, player.Center.Y - 800f) : AIState != (float)Phase.Deathrays ? new Vector2(player.Center.X, player.Center.Y - 425f) : player.Center;

			// Velocity and acceleration values
			float baseVelocityMult = malice ? 1.3f : death ? 1.2f : revenge ? 1.15f : expertMode ? 1.1f : 1f;
			float baseVelocity = 14f * baseVelocityMult;
			float baseAcceleration = 1f;
			float decelerationVelocityMult = 0.85f;
			if (berserk)
			{
				baseVelocity *= 1.5f;
				baseAcceleration *= 1.5f;
			}
			Vector2 distanceFromDestination = destination - npc.Center;
			Vector2 desiredVelocity = Vector2.Normalize(distanceFromDestination) * baseVelocity;

			// Distance from target
			float distanceFromTarget = Vector2.Distance(npc.Center, player.Center);

			// Distance where Ares stops moving
			float movementDistanceGateValue = 50f;

			// Gate values
			float deathrayPhaseGateValue = lastMechAlive ? 630f : 900f;
			float deathrayDistanceGateValue = 480f;

			// Passive and Immune phases
			switch ((int)SecondaryAIState)
			{
				case (int)SecondaryPhase.Nothing:

					// Spawn the other mechs if Ares is first
					if (otherExoMechsAlive == 0)
					{
						if (spawnOtherExoMechs)
						{
							// Reset everything
							SecondaryAIState = (float)SecondaryPhase.PassiveAndImmune;
							npc.TargetClosest();

							if (Main.netMode != NetmodeID.MultiplayerClient)
							{
								// Spawn code here
								NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<ThanatosHead>());
								NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<Artemis.Artemis>());
								NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<Apollo.Apollo>());
							}
						}
					}
					else
					{
						// If not spawned first, go to passive state if any other mech is passive or if Ares is under 70% life
						// Do not run this if berserk
						// Do not run this if any exo mech is dead
						if ((anyOtherExoMechPassive || lifeRatio < 0.7f) && !berserk && totalOtherExoMechLifeRatio < 5f)
						{
							// Tells Ares to return to the battle in passive state and reset everything
							SecondaryAIState = (float)SecondaryPhase.Passive;
							npc.TargetClosest();
						}

						// Go passive and immune if one of the other mechs is berserk
						// This is only called if two exo mechs are alive
						if (otherMechIsBerserk)
						{
							// Reset everything
							SecondaryAIState = (float)SecondaryPhase.PassiveAndImmune;
							npc.TargetClosest();
						}
					}

					break;

				// Fire projectiles less often
				case (int)SecondaryPhase.Passive:

					// Enter passive and invincible phase if one of the other exo mechs is berserk
					if (otherMechIsBerserk)
					{
						// Reset everything
						SecondaryAIState = (float)SecondaryPhase.PassiveAndImmune;
						npc.TargetClosest();
					}

					// If Ares is the first mech to go berserk
					if (berserk)
					{
						// Reset everything
						npc.TargetClosest();

						// Never be passive if berserk
						SecondaryAIState = (float)SecondaryPhase.Nothing;
					}

					break;

				// Fly above target and become immune
				case (int)SecondaryPhase.PassiveAndImmune:

					// Enter the fight again if any of the other exo mechs is below 70% and the other mechs aren't berserk
					if ((exoWormLifeRatio < 0.7f || exoTwinsLifeRatio < 0.7f) && !otherMechIsBerserk)
					{
						// Tells Ares to return to the battle in passive state and reset everything
						// Return to normal phases if one or more mechs have been downed
						SecondaryAIState = totalOtherExoMechLifeRatio > 5f ? (float)SecondaryPhase.Nothing : (float)SecondaryPhase.Passive;
						npc.TargetClosest();
					}

					if (berserk)
					{
						// Reset everything
						npc.TargetClosest();

						// Never be passive if berserk
						SecondaryAIState = (float)SecondaryPhase.Nothing;
					}

					break;
			}

			// Attacking phases
			switch ((int)AIState)
			{
				// Fly above the target
				case (int)Phase.Normal:

					if (!targetDead)
					{
						// Inverse lerp returns the percentage of progress between A and B
						float lerpValue = Utils.InverseLerp(movementDistanceGateValue, 2400f, distanceFromDestination.Length(), true);

						// Min velocity
						float minVelocity = distanceFromDestination.Length();
						float minVelocityCap = baseVelocity;
						if (minVelocity > minVelocityCap)
							minVelocity = minVelocityCap;

						// Max velocity
						Vector2 maxVelocity = distanceFromDestination / 24f;
						float maxVelocityCap = minVelocityCap * 3f;
						if (maxVelocity.Length() > maxVelocityCap)
							maxVelocity = distanceFromDestination.SafeNormalize(Vector2.Zero) * maxVelocityCap;

						npc.velocity = Vector2.Lerp(distanceFromDestination.SafeNormalize(Vector2.Zero) * minVelocity, maxVelocity, lerpValue);
					}

					if (berserk)
					{
						calamityGlobalNPC.newAI[2] += 1f;
						if (calamityGlobalNPC.newAI[2] > deathrayPhaseGateValue)
						{
							calamityGlobalNPC.newAI[2] = 0f;
							AIState = (float)Phase.Deathrays;
						}
					}

					break;

				// Move close to target, reduce velocity when close enough, create telegraph beams, fire deathrays
				case (int)Phase.Deathrays:

					if (!targetDead)
					{
						if (distanceFromTarget > deathrayDistanceGateValue && calamityGlobalNPC.newAI[3] == 0f)
						{
							Vector2 desiredVelocity2 = Vector2.Normalize(distanceFromDestination) * baseVelocity;
							npc.SimpleFlyMovement(desiredVelocity2, baseAcceleration);
						}
						else
						{
							calamityGlobalNPC.newAI[3] = 1f;
							npc.velocity *= decelerationVelocityMult;

							int totalProjectiles = malice ? 12 : expertMode ? 10 : 8;
							float radians = MathHelper.TwoPi / totalProjectiles;
							Vector2 laserSpawnPoint = new Vector2(npc.Center.X, npc.Center.Y);
							bool normalLaserRotation = npc.localAI[0] % 2f == 0f;
							float velocity = 6f;
							double angleA = radians * 0.5;
							double angleB = MathHelper.ToRadians(90f) - angleA;
							float velocityX2 = (float)(velocity * Math.Sin(angleA) / Math.Sin(angleB));
							Vector2 spinningPoint = normalLaserRotation ? new Vector2(0f, -velocity) : new Vector2(-velocityX2, -velocity);
							spinningPoint.Normalize();

							calamityGlobalNPC.newAI[2] += 1f;
							if (calamityGlobalNPC.newAI[2] < deathrayTelegraphDuration)
							{
								// Fire deathray telegraph beams
								if (calamityGlobalNPC.newAI[2] == 1f)
								{
									// Set frames to deathray charge up frames, which begin on frame 12
									// Reset the frame counter
									npc.frameCounter = 0D;

									// X = 1 sets to frame 8
									frameX = 1;

									// Y = 4 sets to frame 12
									frameY = 4;

									if (Main.netMode != NetmodeID.MultiplayerClient)
									{
										Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/LaserCannon"), npc.Center);
										int type = ModContent.ProjectileType<AresDeathBeamTelegraph>();
										Vector2 spawnPoint = npc.Center + new Vector2(-1f, 23f);
										for (int k = 0; k < totalProjectiles; k++)
										{
											Vector2 laserVelocity = spinningPoint.RotatedBy(radians * k);
											Projectile.NewProjectile(spawnPoint + Vector2.Normalize(laserVelocity) * 17f, laserVelocity, type, 0, 0f, Main.myPlayer, 0f, npc.whoAmI);
										}
									}
								}
							}
							else
							{
								// Fire deathrays
								if (calamityGlobalNPC.newAI[2] == deathrayTelegraphDuration)
								{
									if (Main.netMode != NetmodeID.MultiplayerClient)
									{
										Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/TeslaCannonFire"), npc.Center);
										int type = ModContent.ProjectileType<AresDeathBeamStart>();
										int damage = npc.GetProjectileDamage(type);
										Vector2 spawnPoint = npc.Center + new Vector2(-1f, 23f);
										for (int k = 0; k < totalProjectiles; k++)
										{
											Vector2 laserVelocity = spinningPoint.RotatedBy(radians * k);
											Projectile.NewProjectile(spawnPoint + Vector2.Normalize(laserVelocity) * 35f, laserVelocity, type, damage, 0f, Main.myPlayer, 0f, npc.whoAmI);
										}
									}
								}
							}

							if (calamityGlobalNPC.newAI[2] >= deathrayTelegraphDuration + deathrayDuration)
							{
								AIState = (float)Phase.Normal;
								calamityGlobalNPC.newAI[2] = 0f;
								calamityGlobalNPC.newAI[3] = 0f;
								npc.localAI[0] += 1f;
								npc.TargetClosest();
							}
						}
					}

					break;
			}
		}

		public override bool CanHitPlayer(Player target, ref int cooldownSlot) => false;

		public override bool StrikeNPC(ref double damage, int defense, ref float knockback, int hitDirection, ref bool crit) => !CalamityUtils.AntiButcher(npc, ref damage, 0.5f);

		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			scale = 2f;
			return null;
		}

		public override void FindFrame(int frameHeight)
		{
			// Use telegraph frames when using deathrays
			npc.frameCounter += 1D;
			if (AIState == (float)Phase.Normal || npc.Calamity().newAI[3] == 0f)
			{
				if (npc.frameCounter >= 10D)
				{
					// Reset frame counter
					npc.frameCounter = 0D;

					// Increment the Y frame
					frameY++;

					// Reset the Y frame if greater than 8
					if (frameY == maxFramesY)
					{
						frameX++;
						frameY = 0;
					}

					// Reset the frames to frame 0
					if ((frameX * maxFramesY) + frameY > normalFrameLimit)
						frameX = frameY = 0;
				}
			}
			else
			{
				if (npc.frameCounter >= 10D)
				{
					// Reset frame counter
					npc.frameCounter = 0D;

					// Increment the Y frame
					frameY++;

					// Reset the Y frame if greater than 8
					if (frameY == maxFramesY)
					{
						frameX++;
						frameY = 0;
					}

					// Reset the frames to frame 36, the start of the deathray firing animation loop
					if ((frameX * maxFramesY) + frameY > finalStageDeathrayChargeFrameLimit)
						frameX = frameY = 4;
				}
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			Texture2D texture = Main.npcTexture[npc.type];
			Rectangle frame = new Rectangle(npc.width * frameX, npc.height * frameY, npc.width, npc.height);
			Vector2 vector = new Vector2(npc.width / 2, npc.height / 2);
			Color afterimageBaseColor = Color.White;
			int numAfterimages = 5;

			if (CalamityConfig.Instance.Afterimages)
			{
				for (int i = 1; i < numAfterimages; i += 2)
				{
					Color afterimageColor = drawColor;
					afterimageColor = Color.Lerp(afterimageColor, afterimageBaseColor, 0.5f);
					afterimageColor = npc.GetAlpha(afterimageColor);
					afterimageColor *= (numAfterimages - i) / 15f;
					Vector2 afterimageCenter = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
					afterimageCenter -= new Vector2(texture.Width, texture.Height / Main.npcFrameCount[npc.type]) * npc.scale / 2f;
					afterimageCenter += vector * npc.scale + new Vector2(0f, npc.gfxOffY);
					spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.rotation, vector, npc.scale, SpriteEffects.None, 0f);
				}
			}

			Vector2 center = npc.Center - Main.screenPosition;
			spriteBatch.Draw(texture, center, frame, npc.GetAlpha(drawColor), npc.rotation, vector, npc.scale, SpriteEffects.None, 0f);

			texture = ModContent.GetTexture("CalamityMod/NPCs/ExoMechs/Ares/AresBodyGlow");

			if (CalamityConfig.Instance.Afterimages)
			{
				for (int i = 1; i < numAfterimages; i += 2)
				{
					Color afterimageColor = drawColor;
					afterimageColor = Color.Lerp(afterimageColor, afterimageBaseColor, 0.5f);
					afterimageColor = npc.GetAlpha(afterimageColor);
					afterimageColor *= (numAfterimages - i) / 15f;
					Vector2 afterimageCenter = npc.oldPos[i] + new Vector2(npc.width, npc.height) / 2f - Main.screenPosition;
					afterimageCenter -= new Vector2(texture.Width, texture.Height / Main.npcFrameCount[npc.type]) * npc.scale / 2f;
					afterimageCenter += vector * npc.scale + new Vector2(0f, npc.gfxOffY);
					spriteBatch.Draw(texture, afterimageCenter, npc.frame, afterimageColor, npc.rotation, vector, npc.scale, SpriteEffects.None, 0f);
				}
			}

			spriteBatch.Draw(texture, center, frame, Color.White * npc.Opacity, npc.rotation, vector, npc.scale, SpriteEffects.None, 0f);

			return false;
		}

		public override void BossLoot(ref string name, ref int potionType)
		{
			potionType = ModContent.ItemType<OmegaHealingPotion>();
		}

		public override void NPCLoot()
        {
			// DropHelper.DropItemChance(npc, ModContent.ItemType<AresTrophy>(), 10);

			// Check if the other exo mechs are alive
			bool otherExoMechsAlive = false;
			if (CalamityGlobalNPC.draedonExoMechWorm != -1)
			{
				if (Main.npc[CalamityGlobalNPC.draedonExoMechWorm].active)
					otherExoMechsAlive = true;
			}
			if (CalamityGlobalNPC.draedonExoMechTwinGreen != -1)
			{
				if (Main.npc[CalamityGlobalNPC.draedonExoMechTwinGreen].active)
					otherExoMechsAlive = true;
			}

			// Mark Exo Mechs as dead
			if (!otherExoMechsAlive)
				DropExoMechLoot(npc);
		}

		public static void DropExoMechLoot(NPC npc)
		{
			DropHelper.DropBags(npc);

			// DropHelper.DropItemCondition(npc, ModContent.ItemType<KnowledgeExoMechs>(), true, !CalamityWorld.downedExoMechs);

			// Materials
			int minCrystalQuantity = Main.expertMode ? 15 : 12;
			int maxCrystalQuantity = Main.expertMode ? 18 : 15;
			DropHelper.DropItem(npc, ModContent.ItemType<ExoCrystal>(), true, minCrystalQuantity, maxCrystalQuantity);

			// All other drops are contained in the bag, so they only drop directly on Normal
			if (!Main.expertMode)
			{
				// Weapons
				float w = DropHelper.NormalWeaponDropRateFloat;
				DropHelper.DropEntireWeightedSet(npc,
					DropHelper.WeightStack<SpineOfThanatos>(w),
					DropHelper.WeightStack<PhotonRipper>(w),
					DropHelper.WeightStack<SurgeDriver>(w),
					DropHelper.WeightStack<TheJailor>(w),
					DropHelper.WeightStack<RefractionRotor>(w),
					DropHelper.WeightStack<TheAtomSplitter>(w)
				);

				// Vanity
				// DropHelper.DropItemChance(npc, ModContent.ItemType<ThanatosMask>(), 7);
				// DropHelper.DropItemChance(npc, ModContent.ItemType<ArtemisMask>(), 7);
				// DropHelper.DropItemChance(npc, ModContent.ItemType<ApolloMask>(), 7);
				// DropHelper.DropItemChance(npc, ModContent.ItemType<AresMask>(), 7);
			}

			CalamityWorld.downedExoMechs = true;
			CalamityNetcode.SyncWorld();
		}

		public override void HitEffect(int hitDirection, double damage)
		{
			for (int k = 0; k < 3; k++)
				Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1f);

			if (npc.life <= 0)
			{
				for (int num193 = 0; num193 < 2; num193++)
				{
					Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
				}
				for (int num194 = 0; num194 < 20; num194++)
				{
					int num195 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 107, 0f, 0f, 0, new Color(0, 255, 255), 2.5f);
					Main.dust[num195].noGravity = true;
					Main.dust[num195].velocity *= 3f;
					num195 = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 107, 0f, 0f, 100, new Color(0, 255, 255), 1.5f);
					Main.dust[num195].velocity *= 2f;
					Main.dust[num195].noGravity = true;
				}

				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ares/AresBody1"), 1f);
				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ares/AresBody2"), 1f);
				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ares/AresBody3"), 1f);
				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ares/AresBody4"), 1f);
				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ares/AresBody5"), 1f);
				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ares/AresBody6"), 1f);
				Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/Ares/AresBody7"), 1f);
			}
		}

		public override bool CheckActive() => false;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax * 0.8f * bossLifeScale);
			npc.damage = (int)(npc.damage * 0.8f);
		}
    }
}
