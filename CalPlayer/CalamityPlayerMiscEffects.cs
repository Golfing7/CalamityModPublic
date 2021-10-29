using CalamityMod.Buffs.Alcohol;
using CalamityMod.Buffs.Cooldowns;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.Potions;
using CalamityMod.Buffs.StatBuffs;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Buffs.Summon;
using CalamityMod.CustomRecipes;
using CalamityMod.DataStructures;
using CalamityMod.Dusts;
using CalamityMod.Events;
using CalamityMod.Items;
using CalamityMod.Items.Accessories;
using CalamityMod.Items.Armor;
using CalamityMod.Items.DraedonMisc;
using CalamityMod.Items.Dyes;
using CalamityMod.Items.Fishing.AstralCatches;
using CalamityMod.Items.Fishing.BrimstoneCragCatches;
using CalamityMod.Items.Fishing.FishingRods;
using CalamityMod.Items.Mounts.Minecarts;
using CalamityMod.Items.Potions;
using CalamityMod.Items.Potions.Alcohol;
using CalamityMod.Items.Weapons.Melee;
using CalamityMod.Items.Weapons.Ranged;
using CalamityMod.Items.Weapons.Summon;
using CalamityMod.NPCs;
using CalamityMod.NPCs.AcidRain;
using CalamityMod.NPCs.Astral;
using CalamityMod.NPCs.Crags;
using CalamityMod.NPCs.NormalNPCs;
using CalamityMod.NPCs.Other;
using CalamityMod.NPCs.PlagueEnemies;
using CalamityMod.NPCs.SupremeCalamitas;
using CalamityMod.Projectiles.Boss;
using CalamityMod.Projectiles.Environment;
using CalamityMod.Projectiles.Magic;
using CalamityMod.Projectiles.Melee;
using CalamityMod.Projectiles.Rogue;
using CalamityMod.Projectiles.Summon;
using CalamityMod.Projectiles.Typeless;
using CalamityMod.UI;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.GameInput;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using ProvidenceBoss = CalamityMod.NPCs.Providence.Providence;

namespace CalamityMod.CalPlayer
{
	public class CalamityPlayerMiscEffects
	{
		#region Post Update Misc Effects
		public static void CalamityPostUpdateMiscEffects(Player player, Mod mod)
		{
			CalamityPlayer modPlayer = player.Calamity();

			// No category

			// Give the player a 24% jump speed boost while wings are equipped
			// Give the player a 10% jump speed boost while not using wings and not using a balloon
			if (player.wingsLogic > 0)
				player.jumpSpeedBoost += 1.2f;
			else if (!player.jumpBoost)
				player.jumpSpeedBoost += 0.5f;

			// Reduce balloon jump speed boosts by 15% because they'd be too powerful when stacked with wings
			// Normally gives 30%, now gives 15%
			if (player.jumpBoost)
				player.jumpSpeedBoost -= 0.75f;

			// Decrease the counter on Fearmonger set turbo regeneration
			if (modPlayer.fearmongerRegenFrames > 0)
				modPlayer.fearmongerRegenFrames--;

			// Reduce the expert debuff time multiplier to the normal mode multiplier
			if (CalamityConfig.Instance.NerfExpertDebuffs)
				Main.expertDebuffTime = 1f;

			// Bool for any existing bosses, true if any boss NPC is active
			CalamityPlayer.areThereAnyDamnBosses = CalamityUtils.AnyBossNPCS();

			// Bool for any existing events, true if any event is active
			CalamityPlayer.areThereAnyDamnEvents = CalamityGlobalNPC.AnyEvents(player);

			// Hurt the nearest NPC to the mouse if using the burning mouse.
			if (modPlayer.blazingMouseDamageEffects)
				HandleBlazingMouseEffects(player, modPlayer);

			// Revengeance effects
			RevengeanceModeMiscEffects(player, modPlayer, mod);

			// Abyss effects
			AbyssEffects(player, modPlayer);

			// Misc effects, because I don't know what else to call it
			MiscEffects(player, modPlayer, mod);

			// Max life and mana effects
			MaxLifeAndManaEffects(player, modPlayer, mod);

			// Standing still effects
			StandingStillEffects(player, modPlayer);

			// Elysian Aegis effects
			ElysianAegisEffects(player, modPlayer);

			// Other buff effects
			OtherBuffEffects(player, modPlayer);

			// Limits
			Limits(player, modPlayer);

			// Stat Meter
			UpdateStatMeter(player, modPlayer);

			// Rogue Mirrors
			RogueMirrors(player, modPlayer);

			// Double Jumps
			DoubleJumps(player, modPlayer);

			// Potions (Quick Buff && Potion Sickness)
			HandlePotions(player, modPlayer);

			// Check if schematics are present on the mouse, for the sake of registering their recipes.
			CheckIfMouseItemIsSchematic(player);

			// Update all particle sets for items.
			// This must be done here instead of in the item logic because these sets are not properly instanced
			// in the global classes. Attempting to update them there will cause multiple updates to one set for multiple items.
			CalamityGlobalItem.UpdateAllParticleSets();

			// Regularly sync player stats during multiplayer
			if (player.whoAmI == Main.myPlayer && Main.netMode == NetmodeID.MultiplayerClient)
			{
				modPlayer.packetTimer++;
				if (modPlayer.packetTimer == CalamityPlayer.GlobalSyncPacketTimer)
				{
					modPlayer.packetTimer = 0;
					modPlayer.StandardSync();
				}
			}

			// After everything, reset ranged crit if necessary.
			if (modPlayer.spiritOrigin)
			{
				modPlayer.spiritOriginConvertedCrit = player.rangedCrit - 4;
				player.rangedCrit = 4;
			}
		}
		#endregion

		#region Revengeance Effects
		private static void RevengeanceModeMiscEffects(Player player, CalamityPlayer modPlayer, Mod mod)
		{
			if (CalamityWorld.revenge || CalamityWorld.malice)
			{
				// Adjusts the life steal cap in rev/death
				float lifeStealCap = CalamityWorld.malice ? 30f : CalamityWorld.death ? 50f : 60f;
				/*if (Main.masterMode)
					lifeStealCap *= 0.75f;*/
				if (player.lifeSteal > lifeStealCap)
					player.lifeSteal = lifeStealCap;

				if (player.whoAmI == Main.myPlayer)
				{
					// Titanium Armor nerf
					if (player.onHitDodge)
					{
						for (int l = 0; l < Player.MaxBuffs; l++)
						{
							int hasBuff = player.buffType[l];
							if (player.buffTime[l] > 360 && hasBuff == BuffID.ShadowDodge)
								player.buffTime[l] = 360;
						}
					}

					// Immunity Frames nerf
					int immuneTimeLimit = 150;
					if (player.immuneTime > immuneTimeLimit)
						player.immuneTime = immuneTimeLimit;

					for (int k = 0; k < player.hurtCooldowns.Length; k++)
					{
						if (player.hurtCooldowns[k] > immuneTimeLimit)
							player.hurtCooldowns[k] = immuneTimeLimit;
					}

					// Adrenaline and Rage
					if (CalamityWorld.revenge)
						UpdateRippers(mod, player, modPlayer);
				}
			}

			// If Revengeance Mode is not active, then set rippers to zero
			else if (player.whoAmI == Main.myPlayer)
			{
				modPlayer.rage = 0;
				modPlayer.adrenaline = 0;
			}
		}

		private static void UpdateRippers(Mod mod, Player player, CalamityPlayer modPlayer)
		{
			// Figure out Rage's current duration based on boosts.
			if (modPlayer.rageBoostOne)
				modPlayer.RageDuration += CalamityPlayer.RageDurationPerBooster;
			if (modPlayer.rageBoostTwo)
				modPlayer.RageDuration += CalamityPlayer.RageDurationPerBooster;
			if (modPlayer.rageBoostThree)
				modPlayer.RageDuration += CalamityPlayer.RageDurationPerBooster;

			// Tick down "Rage Combat Frames". When they reach zero, Rage begins fading away.
			if (modPlayer.rageCombatFrames > 0)
				--modPlayer.rageCombatFrames;

			// Tick down the Rage gain cooldown.
			if (modPlayer.rageGainCooldown > 0)
				--modPlayer.rageGainCooldown;

			// This is how much Rage will be changed by this frame.
			float rageDiff = 0;

			// If the player equips multiple rage generation accessories they get the max possible effect without stacking any of them.
			{
				float rageGen = 0f;

				// Shattered Community provides constant rage generation (stronger than Heart of Darkness).
				if (modPlayer.shatteredCommunity)
				{
					float scRageGen = modPlayer.rageMax * ShatteredCommunity.RagePerSecond / 60f;
					if (rageGen < scRageGen)
						rageGen = scRageGen;
				}
				// Heart of Darkness grants constant rage generation.
				else if (modPlayer.heartOfDarkness)
				{
					float hodRageGen = modPlayer.rageMax * HeartofDarkness.RagePerSecond / 60f;
					if (rageGen < hodRageGen)
						rageGen = hodRageGen;
				}

				rageDiff += rageGen;
			}

			// Holding Gael's Greatsword grants constant rage generation.
			if (modPlayer.heldGaelsLastFrame)
				rageDiff += modPlayer.rageMax * GaelsGreatsword.RagePerSecond / 60f;

			// Calculate and grant proximity rage.
			// Regular enemies can give up to 1x proximity rage. Bosses can give up to 3x. Multiple regular enemies don't stack.
			// Proximity rage is maxed out when within 10 blocks (160 pixels) of the enemy's hitbox.
			// Its max range is 50 blocks (800 pixels), at which you get zero proximity rage.
			// Proximity rage does not generate while Rage Mode is active.
			if (!modPlayer.rageModeActive)
			{
				float bossProxRageMultiplier = 3f;
				float minProxRageDistance = 160f;
				float maxProxRageDistance = 800f;
				float enemyDistance = maxProxRageDistance + 1f;
				float bossDistance = maxProxRageDistance + 1f;

				for (int i = 0; i < Main.maxNPCs; ++i)
				{
					NPC npc = Main.npc[i];
					if (npc is null || !npc.IsAnEnemy() || npc.Calamity().DoesNotGenerateRage)
						continue;

					// Take the longer of the two directions for the NPC's hitbox to be generous.
					float generousHitboxWidth = Math.Max(npc.Hitbox.Width / 2f, npc.Hitbox.Height / 2f);
					float hitboxEdgeDist = npc.Distance(player.Center) - generousHitboxWidth;

					// If this enemy is closer than the previous, reduce the current minimum proximity distance.
					if (enemyDistance > hitboxEdgeDist)
					{
						enemyDistance = hitboxEdgeDist;

						// If they're a boss, reduce the boss distance.
						// Boss distance will always be >= enemy distance, so there's no need to do another check.
						// Worm boss body and tail segments are not counted as bosses for this calculation.
						if (npc.IsABoss() && !CalamityLists.noRageWormSegmentList.Contains(npc.type))
							bossDistance = hitboxEdgeDist;
					}
				}

				// Helper function to implement proximity rage formula
				float ProxRageFromDistance(float dist)
				{
					// Adjusted distance with the 160 grace pixels added in. If you're closer than that it counts as zero.
					float d = Math.Max(dist - minProxRageDistance, 0f);

					// The first term is exponential decay which reduces rage gain significantly over distance.
					// The second term is a linear component which allows a baseline but weak rage generation even at far distances.
					// This function takes inputs from 0.0 to 640.0 and returns a value from 1.0 to 0.0.
					float r = 1f / (0.034f * d + 2f) + (590.5f - d) / 1181f;
					return MathHelper.Clamp(r, 0f, 1f);
				}

				// If anything is close enough then provide proximity rage.
				// You can only get proximity rage from one target at a time. You gain rage from whatever target would give you the most rage.
				if (enemyDistance <= maxProxRageDistance)
				{
					// If the player is close enough to get proximity rage they are also considered to have rage combat frames.
					// This prevents proximity rage from fading away unless you run away without attacking for some reason.
					modPlayer.rageCombatFrames = Math.Max(modPlayer.rageCombatFrames, 3);

					float proxRageFromEnemy = ProxRageFromDistance(enemyDistance);
					float proxRageFromBoss = 0f;
					if (bossDistance <= maxProxRageDistance)
						proxRageFromBoss = bossProxRageMultiplier * ProxRageFromDistance(bossDistance);

					float finalProxRage = Math.Max(proxRageFromEnemy, proxRageFromBoss);

					// 300% proximity rage (max possible from a boss) will fill the Rage meter in 15 seconds.
					// 100% proximity rage (max possible from an enemy) will fill the Rage meter in 45 seconds.
					rageDiff += finalProxRage * modPlayer.rageMax / CalamityUtils.SecondsToFrames(45f);
				}
			}

			bool rageFading = modPlayer.rageCombatFrames <= 0 && !modPlayer.heartOfDarkness && !modPlayer.shatteredCommunity;

			// If Rage Mode is currently active, you smoothly lose all rage over the duration.
			if (modPlayer.rageModeActive)
				rageDiff -= modPlayer.rageMax / modPlayer.RageDuration;

			// If out of combat and NOT using Heart of Darkness or Shattered Community, Rage fades away.
			else if (!modPlayer.rageModeActive && rageFading)
				rageDiff -= modPlayer.rageMax / CalamityPlayer.RageFadeTime;

			// Apply the rage change and cap rage in both directions.
			modPlayer.rage += rageDiff;
			if (modPlayer.rage < 0)
				modPlayer.rage = 0;

			if (modPlayer.rage >= modPlayer.rageMax)
			{
				// If Rage is not active, it is capped at 100%.
				if (!modPlayer.rageModeActive)
					modPlayer.rage = modPlayer.rageMax;

				// If using the Shattered Community, Rage is capped at 200% while it's active.
				// This prevents infinitely stacking rage before a fight by standing on spikes/lava with a regen build or the Nurse handy.
				else if (modPlayer.shatteredCommunity && modPlayer.rage >= 2f * modPlayer.rageMax)
					modPlayer.rage = 2f * modPlayer.rageMax;

				// Play a sound when the Rage Meter is full
				if (modPlayer.playFullRageSound)
				{
					modPlayer.playFullRageSound = false;
					Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/FullRage"), (int)player.position.X, (int)player.position.Y);
				}
			}
			else
				modPlayer.playFullRageSound = true;

			// This is how much Adrenaline will be changed by this frame.
			float adrenalineDiff = 0;
			bool SCalAlive = NPC.AnyNPCs(ModContent.NPCType<SupremeCalamitas>());
			bool wofAndNotHell = Main.wof >= 0 && player.position.Y < (float)((Main.maxTilesY - 200) * 16);

			// If Adrenaline Mode is currently active, you smoothly lose all adrenaline over the duration.
			if (modPlayer.adrenalineModeActive)
				adrenalineDiff = -modPlayer.adrenalineMax / modPlayer.AdrenalineDuration;
			else
			{
				// If any boss is alive (or you are between DoG phases or Boss Rush is active), you gain adrenaline smoothly.
				// EXCEPTION: Wall of Flesh is alive and you are not in hell. Then you don't get anything.
				if ((CalamityPlayer.areThereAnyDamnBosses || CalamityWorld.DoGSecondStageCountdown > 0 || BossRushEvent.BossRushActive) &&
					!wofAndNotHell)
				{
					adrenalineDiff += modPlayer.adrenalineMax / modPlayer.AdrenalineChargeTime;
				}

				// If you aren't actively in a boss fight, adrenaline rapidly fades away.
				else
					adrenalineDiff = -modPlayer.adrenalineMax / modPlayer.AdrenalineFadeTime;
			}

			// In the SCal fight, adrenaline charges 33% slower (meaning it takes 50% longer to fully charge it).
			if (SCalAlive && adrenalineDiff > 0f)
				adrenalineDiff *= 0.67f;

			// Apply the adrenaline change and cap adrenaline in both directions.
			modPlayer.adrenaline += adrenalineDiff;
			if (modPlayer.adrenaline < 0)
				modPlayer.adrenaline = 0;

			if (modPlayer.adrenaline >= modPlayer.adrenalineMax)
			{
				modPlayer.adrenaline = modPlayer.adrenalineMax;

				// Play a sound when the Adrenaline Meter is full
				if (modPlayer.playFullAdrenalineSound)
				{
					modPlayer.playFullAdrenalineSound = false;
					Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/FullAdrenaline"), (int)player.position.X, (int)player.position.Y);
				}
			}
			else
				modPlayer.playFullAdrenalineSound = true;
		}
		#endregion

		#region Misc Effects

		private static void HandleBlazingMouseEffects(Player player, CalamityPlayer modPlayer)
		{
			Rectangle auraRectangle = Utils.CenteredRectangle(Main.MouseWorld, new Vector2(35f, 62f));
			modPlayer.blazingMouseAuraFade = MathHelper.Clamp(modPlayer.blazingMouseAuraFade - 0.025f, 0.25f, 1f);
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				if (!Main.npc[i].CanBeChasedBy() || !Main.npc[i].Hitbox.Intersects(auraRectangle) || !Main.rand.NextBool(2))
					continue;

				harmNPC(Main.npc[i]);
				modPlayer.blazingMouseAuraFade = MathHelper.Clamp(modPlayer.blazingMouseAuraFade + 0.15f, 0.25f, 1f);
			}

			void harmNPC(NPC npc)
			{
				int damage = (int)(player.AverageDamage() * Main.rand.Next(550, 600));
				npc.StrikeNPC(damage, 0f, 0);

				player.addDPS(damage);
				npc.AddBuff(ModContent.BuffType<VulnerabilityHex>(), 900);

				for (int i = 0; i < 4; i++)
				{
					Dust fire = Dust.NewDustDirect(npc.position, npc.width, npc.height, 267);
					fire.velocity = Vector2.UnitY * -Main.rand.NextFloat(2f, 3.45f);
					fire.scale = 1f + fire.velocity.Length() / 6f;
					fire.color = Color.Lerp(Color.Orange, Color.Red, Main.rand.NextFloat(0.85f));
					fire.noGravity = true;
				}
			}
		}

		private static void MiscEffects(Player player, CalamityPlayer modPlayer, Mod mod)
		{
			// Do a vanity/social slot check for SCal's expert drop since alternatives to get this working are a pain in the ass to create.
			int blazingMouseItem = ModContent.ItemType<Calamity>();
			for (int i = 13; i < 18 + player.extraAccessorySlots; i++)
			{
				if (player.armor[i].type == blazingMouseItem)
				{
					modPlayer.ableToDrawBlazingMouse = true;
					break;
				}
			}

			// Calculate/reset DoG cart rotations based on whether the DoG cart is in use.
			if (player.mount.Active && player.mount.Type == ModContent.MountType<DoGCartMount>())
			{
				modPlayer.SmoothenedMinecartRotation = MathHelper.Lerp(modPlayer.SmoothenedMinecartRotation, DelegateMethods.Minecart.rotation, 0.05f);

				// Initialize segments from null if necessary.
				int direction = (player.velocity.SafeNormalize(Vector2.UnitX * player.direction).X > 0f).ToDirectionInt();
				if (player.velocity.X == 0f)
					direction = player.direction;

				float idealRotation = DoGCartMount.CalculateIdealWormRotation(player);
				float minecartRotation = DelegateMethods.Minecart.rotation;
				if (Math.Abs(minecartRotation) < 0.5f)
					minecartRotation = 0f;
				Vector2 stickOffset = minecartRotation.ToRotationVector2() * player.velocity.Length() * direction * 1.25f;
				for (int i = 0; i < modPlayer.DoGCartSegments.Length; i++)
				{
					if (modPlayer.DoGCartSegments[i] is null)
					{
                        modPlayer.DoGCartSegments[i] = new DoGCartSegment
                        {
                            Center = player.Center - idealRotation.ToRotationVector2() * i * 20f
                        };
                    }
				}

				Vector2 startingStickPosition = player.Center + stickOffset + Vector2.UnitY * 12f;
				modPlayer.DoGCartSegments[0].Update(player, startingStickPosition, idealRotation);
				modPlayer.DoGCartSegments[0].Center = startingStickPosition;

				for (int i = 1; i < modPlayer.DoGCartSegments.Length; i++)
				{
					Vector2 waveOffset = DoGCartMount.CalculateSegmentWaveOffset(i, player);
					modPlayer.DoGCartSegments[i].Update(player, modPlayer.DoGCartSegments[i - 1].Center + waveOffset, modPlayer.DoGCartSegments[i - 1].Rotation);
				}
			}
			else
				modPlayer.DoGCartSegments = new DoGCartSegment[modPlayer.DoGCartSegments.Length];

			// Dust on hand when holding the phosphorescent gauntlet.
			if (player.ActiveItem().type == ModContent.ItemType<PhosphorescentGauntlet>())
				PhosphorescentGauntletPunches.GenerateDustOnOwnerHand(player);

			if (modPlayer.stealthUIAlpha > 0f && (modPlayer.rogueStealth <= 0f || modPlayer.rogueStealthMax <= 0f))
			{
				modPlayer.stealthUIAlpha -= 0.035f;
				modPlayer.stealthUIAlpha = MathHelper.Clamp(modPlayer.stealthUIAlpha, 0f, 1f);
			}
			else if (modPlayer.stealthUIAlpha < 1f)
			{
				modPlayer.stealthUIAlpha += 0.035f;
				modPlayer.stealthUIAlpha = MathHelper.Clamp(modPlayer.stealthUIAlpha, 0f, 1f);
			}

			if (player.Calamity().andromedaState == AndromedaPlayerState.LargeRobot ||
				player.ownedProjectileCounts[ModContent.ProjectileType<RelicOfDeliveranceSpear>()] > 0)
			{
				player.controlHook = player.releaseHook = false;
			}

			if (modPlayer.andromedaCripple > 0)
			{
				player.velocity = Vector2.Clamp(player.velocity, new Vector2(-11f, -8f), new Vector2(11f, 8f));
				modPlayer.andromedaCripple--;
			}

			if (player.ownedProjectileCounts[ModContent.ProjectileType<GiantIbanRobotOfDoom>()] <= 0 &&
				modPlayer.andromedaState != AndromedaPlayerState.Inactive)
			{
				modPlayer.andromedaState = AndromedaPlayerState.Inactive;
			}

			if (modPlayer.andromedaState == AndromedaPlayerState.LargeRobot)
			{
				player.width = 80;
				player.height = 212;
				player.position.Y -= 170;
				modPlayer.resetHeightandWidth = true;
			}
			else if (modPlayer.andromedaState == AndromedaPlayerState.SpecialAttack)
			{
				player.width = 24;
				player.height = 98;
				player.position.Y -= 56;
				modPlayer.resetHeightandWidth = true;
			}
			else if (!player.mount.Active && modPlayer.resetHeightandWidth)
			{
				player.width = 20;
				player.height = 42;
				modPlayer.resetHeightandWidth = false;
			}

			// Summon bullseyes on nearby targets.
			if (player.Calamity().spiritOrigin)
            {
				int bullseyeType = ModContent.ProjectileType<SpiritOriginBullseye>();
				List<int> alreadyTargetedNPCs = new List<int>();
				for (int i = 0; i < Main.maxProjectiles; i++)
				{
					if (Main.projectile[i].type != bullseyeType || !Main.projectile[i].active || Main.projectile[i].owner != player.whoAmI)
						continue;

					alreadyTargetedNPCs.Add((int)Main.projectile[i].ai[0]);
				}

				for (int i = 0; i < Main.maxNPCs; i++)
				{
					if (!Main.npc[i].active || Main.npc[i].friendly || Main.npc[i].lifeMax < 5 || alreadyTargetedNPCs.Contains(i) || Main.npc[i].realLife >= 0 || Main.npc[i].dontTakeDamage || Main.npc[i].immortal)
						continue;

					if (Main.myPlayer == player.whoAmI && Main.npc[i].WithinRange(player.Center, 2000f))
						Projectile.NewProjectile(Main.npc[i].Center, Vector2.Zero, bullseyeType, 0, 0f, player.whoAmI, i);
					if (player.Calamity().spiritOriginBullseyeShootCountdown <= 0)
						player.Calamity().spiritOriginBullseyeShootCountdown = 45;
				}
			}

			// Proficiency level ups
			if (CalamityConfig.Instance.Proficiency)
				modPlayer.GetExactLevelUp();

			// Max mana bonuses
			player.statManaMax2 +=
				(modPlayer.permafrostsConcoction ? 50 : 0) +
				(modPlayer.pHeart ? 50 : 0) +
				(modPlayer.eCore ? 50 : 0) +
				(modPlayer.cShard ? 50 : 0) +
				(modPlayer.starBeamRye ? 50 : 0);

			// Shield of Cthulhu immunity frame nerf, nerfed from 10 to 6
			if (player.eocDash > 6 && player.dashDelay > 0)
				player.eocDash = 6;

			// Life Steal nerf
			// Reduces Normal Mode life steal recovery rate from 0.6/s to 0.5/s
			// Reduces Expert Mode life steal recovery rate from 0.5/s to 0.35/s
			// Revengeance Mode recovery rate is 0.3/s
			// Death Mode recovery rate is 0.25/s
			// Malice Mode recovery rate is 0.2/s
			float lifeStealCooldown = CalamityWorld.malice ? 0.3f : CalamityWorld.death ? 0.25f : CalamityWorld.revenge ? 0.2f : Main.expertMode ? 0.15f : 0.1f;
			/*if (Main.masterMode)
				lifeStealCooldown *= 1.25f;*/
			player.lifeSteal -= lifeStealCooldown;

			// Nebula Armor nerf
			if (player.nebulaLevelMana > 0 && player.statMana < player.statManaMax2)
			{
				int num = 12;
				modPlayer.nebulaManaNerfCounter += player.nebulaLevelMana;
				if (modPlayer.nebulaManaNerfCounter >= num)
				{
					modPlayer.nebulaManaNerfCounter -= num;
					player.statMana--;
					if (player.statMana < 0)
						player.statMana = 0;
				}
			}
			else
				modPlayer.nebulaManaNerfCounter = 0;

			// Bool for drawing boss health bar small text or not
			if (Main.myPlayer == player.whoAmI)
				BossHealthBarManager.CanDrawExtraSmallText = modPlayer.shouldDrawSmallText;

			// Margarita halved debuff duration
			if (modPlayer.margarita)
			{
				if (Main.myPlayer == player.whoAmI)
				{
					for (int l = 0; l < Player.MaxBuffs; l++)
					{
						int hasBuff = player.buffType[l];
						if (player.buffTime[l] > 2 && CalamityLists.debuffList.Contains(hasBuff))
						{
							player.buffTime[l]--;
						}
					}
				}
			}

			// Update the Providence Burn effect drawer if applicable.
			float providenceBurnIntensity = 0f;
			if (Main.npc.IndexInRange(CalamityGlobalNPC.holyBoss) && Main.npc[CalamityGlobalNPC.holyBoss].active)
				providenceBurnIntensity = (Main.npc[CalamityGlobalNPC.holyBoss].modNPC as ProvidenceBoss).CalculateBurnIntensity();
			modPlayer.ProvidenceBurnEffectDrawer.ParticleSpawnRate = int.MaxValue;

			// If the burn intensity is great enough, cause the player to ignite into flames.
			if (providenceBurnIntensity > 0.45f)
				modPlayer.ProvidenceBurnEffectDrawer.ParticleSpawnRate = 1;

			// Otherwise, if the intensity is too weak, but still presernt, cause the player to release holy cinders.
			else if (providenceBurnIntensity > 0f)
			{
				int cinderCount = (int)MathHelper.Lerp(1f, 4f, Utils.InverseLerp(0f, 0.45f, providenceBurnIntensity, true));
				for (int i = 0; i < cinderCount; i++)
				{
					if (!Main.rand.NextBool(3))
						continue;

					Dust holyCinder = Dust.NewDustDirect(player.position, player.width, player.head, (int)CalamityDusts.ProfanedFire);
					holyCinder.velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
					holyCinder.velocity.Y -= Main.rand.NextFloat(1f, 3f);
					holyCinder.scale = Main.rand.NextFloat(1.15f, 1.45f);
					holyCinder.noGravity = true;
				}
			}

			modPlayer.ProvidenceBurnEffectDrawer.Update();

			// Immunity to most debuffs
			if (modPlayer.invincible)
			{
				foreach (int debuff in CalamityLists.debuffList)
					player.buffImmune[debuff] = true;
			}

			// Transformer immunity to Electrified
			if (modPlayer.aSparkRare)
				player.buffImmune[BuffID.Electrified] = true;

			// Reduce breath meter while in icy water instead of chilling
			bool canBreath = (modPlayer.sirenBoobs && NPC.downedBoss3) || player.gills || player.merman;
			if (player.arcticDivingGear || canBreath)
			{
				player.buffImmune[ModContent.BuffType<FrozenLungs>()] = true;
			}
			if (CalamityConfig.Instance.ReworkChilledWater)
			{
				if (Main.expertMode && player.ZoneSnow && player.wet && !player.lavaWet && !player.honeyWet)
				{
					player.buffImmune[BuffID.Chilled] = true;
					if (player.IsUnderwater())
					{
						if (Main.myPlayer == player.whoAmI)
						{
							player.AddBuff(ModContent.BuffType<FrozenLungs>(), 2, false);
						}
					}
				}
				if (modPlayer.iCantBreathe)
				{
					if (player.breath > 0)
						player.breath--;
				}
			}

			// Extra DoT in the lava of the crags. Negated by Abaddon.
			if (player.lavaWet)
			{
				if (modPlayer.ZoneCalamity && !modPlayer.abaddon)
					player.AddBuff(ModContent.BuffType<CragsLava>(), 2, false);
			}
			else
			{
				if (player.lavaImmune)
				{
					if (player.lavaTime < player.lavaMax)
						player.lavaTime++;
				}
			}

			// Acid rain droplets
			if (player.whoAmI == Main.myPlayer)
			{
				if (CalamityWorld.rainingAcid && modPlayer.ZoneSulphur && !CalamityPlayer.areThereAnyDamnBosses && player.Center.Y < Main.worldSurface * 16f + 800f)
				{
					int slimeRainRate = (int)(MathHelper.Clamp(Main.invasionSize * 0.4f, 13.5f, 50) * 2.25);
					Vector2 spawnPoint = new Vector2(player.Center.X + Main.rand.Next(-1000, 1001), player.Center.Y - Main.rand.Next(700, 801));

					if (player.miscCounter % slimeRainRate == 0f)
					{
						if (CalamityWorld.downedAquaticScourge && !CalamityWorld.downedPolterghast && Main.rand.NextBool(12))
						{
							NPC.NewNPC((int)spawnPoint.X, (int)spawnPoint.Y, ModContent.NPCType<IrradiatedSlime>());
						}
					}
				}
			}

			// Hydrothermal blue smoke effects but it doesn't work epicccccc
			if (player.whoAmI == Main.myPlayer)
			{
				if (modPlayer.hydrothermalSmoke)
				{
					if (Math.Abs(player.velocity.X) > 0.1f || Math.Abs(player.velocity.Y) > 0.1f)
					{
						Projectile.NewProjectile(player.Center, Vector2.Zero, ModContent.ProjectileType<HydrothermalSmoke>(), 0, 0f, player.whoAmI);
					}
				}
				// Trying to find a workaround because apparently putting the bool in ResetEffects prevents it from working
				if (!player.armorEffectDrawOutlines)
				{
					modPlayer.hydrothermalSmoke = false;
				}
			}

			// Death Mode effects
			modPlayer.caveDarkness = 0f;
			if (CalamityWorld.death)
			{
				if (player.whoAmI == Main.myPlayer)
				{
					// Thorn and spike effects
					// 10 = crimson/corruption thorns, 17 = jungle thorns, 40 = dungeon spikes, 60 = temple spikes
					Vector2 tileType;
					if (!player.mount.Active || !player.mount.Cart)
						tileType = Collision.HurtTiles(player.position, player.velocity, player.width, player.height, player.fireWalk);
					else
						tileType = Collision.HurtTiles(player.position, player.velocity, player.width, player.height - 16, player.fireWalk);
					switch ((int)tileType.Y)
					{
						case 10:
							player.AddBuff(BuffID.Weak, 300, false);
							player.AddBuff(BuffID.Bleeding, 300, false);
							break;
						case 17:
							player.AddBuff(BuffID.Poisoned, 300, false);
							break;
						case 40:
							player.AddBuff(BuffID.Bleeding, 300, false);
							break;
						case 60:
							player.AddBuff(BuffID.Venom, 300, false);
							break;
						default:
							break;
					}

					// Leech bleed
					if (player.ZoneJungle && player.wet && !player.lavaWet && !player.honeyWet)
					{
						if (player.IsUnderwater())
							player.AddBuff(BuffID.Bleeding, 300, false);
					}

					if (!BossRushEvent.BossRushActive && !CalamityPlayer.areThereAnyDamnBosses && !CalamityPlayer.areThereAnyDamnEvents)
					{
						// Astral effects
						if (modPlayer.ZoneAstral)
							player.gravity *= 0.75f;

						// Calculate underground darkness here. The effect is applied in CalamityMod.ModifyLightingBrightness.
						Point point = player.Center.ToTileCoordinates();
						if (point.Y > Main.worldSurface && !modPlayer.ZoneAbyss && !player.ZoneUnderworldHeight)
						{
							// Darkness strength scales smoothly with how deep you are.
							double totalUndergroundDepth = Main.maxTilesY - 200D - Main.worldSurface;
							double playerUndergroundDepth = point.Y - Main.worldSurface;
							double depthRatio = playerUndergroundDepth / totalUndergroundDepth;
							int lightStrength = modPlayer.GetTotalLightStrength();

							// In the last 50 blocks before hell, the darkness smoothly fades away.
							float FadeAwayStart = (float)(1D - 50D / totalUndergroundDepth);
							float darknessStrength = (float)(depthRatio / FadeAwayStart);
							if (depthRatio > FadeAwayStart)
							{
								// Varies from 1.0 to 0.0 as depthRatio varies from FadeAwayStart to 1.0.
								darknessStrength = MathHelper.Lerp(0f, 1f, (1f - (float)depthRatio) / (1f - FadeAwayStart));
							}

							// Reduce the power of cave darkness based on your light level. 5+ is enough to totally eliminate it.
							switch (lightStrength)
							{
								case 0:
									darknessStrength *= 0.75f;
									break;
								case 1:
									darknessStrength *= 0.5f;
									break;
								case 2:
									darknessStrength *= 0.25f;
									break;
								case 3:
									darknessStrength *= 0.15f;
									break;
								case 4:
									darknessStrength *= 0.05f;
									break;
								default:
									darknessStrength = 0f;
									break;
							}
							modPlayer.caveDarkness = darknessStrength;
						}

						// Ice shards, lightning and sharknadoes
						bool nearPillar = player.PillarZone();
						if (player.ZoneOverworldHeight && NPC.MoonLordCountdown == 0 && !player.InSpace())
						{
							Vector2 sharknadoSpawnPoint = new Vector2(player.Center.X - Main.rand.Next(300, 701), player.Center.Y - Main.rand.Next(700, 801));
							if (point.X > Main.maxTilesX / 2)
								sharknadoSpawnPoint.X = player.Center.X + Main.rand.Next(300, 701);

							if (Main.raining)
							{
								float frequencyMult = (1f - Main.cloudAlpha) * CalamityConfig.Instance.DeathWeatherMultiplier; // 3 to 0.055

								Vector2 spawnPoint = new Vector2(player.Center.X + Main.rand.Next(-1000, 1001), player.Center.Y - Main.rand.Next(700, 801));
								Tile tileSafely = Framing.GetTileSafely((int)(spawnPoint.X / 16f), (int)(spawnPoint.Y / 16f));

								if (player.ZoneSnow)
								{
									if (!tileSafely.active())
									{
										int divisor = (int)((Main.hardMode ? 50f : 60f) * frequencyMult);
										float windVelocity = (float)Math.Sqrt(Math.Abs(Main.windSpeed)) * Math.Sign(Main.windSpeed) * (Main.cloudAlpha + 0.5f) * 25f + Main.rand.NextFloat() * 0.2f - 0.1f;
										Vector2 velocity = new Vector2(windVelocity * 0.2f, 3f * Main.rand.NextFloat());

										if (player.miscCounter % divisor == 0 && Main.rand.NextBool(3))
											Projectile.NewProjectile(spawnPoint, velocity, ModContent.ProjectileType<IceRain>(), 20, 0f, player.whoAmI, 2f, 0f);
									}
								}
								else
								{
									if (player.ZoneBeach && !modPlayer.ZoneSulphur)
									{
										int randomFrequency = (int)(50f * frequencyMult);
										if (player.miscCounter == 280 && Main.rand.NextBool(randomFrequency) && player.ownedProjectileCounts[ProjectileID.Cthulunado] < 1)
										{
											Main.PlaySound(SoundID.NPCDeath19, (int)sharknadoSpawnPoint.X, (int)sharknadoSpawnPoint.Y);
											int y = (int)(sharknadoSpawnPoint.Y / 16f);
											int x = (int)(sharknadoSpawnPoint.X / 16f);
											int yAdjust = 100;
											if (x < 10)
												x = 10;
											if (x > Main.maxTilesX - 10)
												x = Main.maxTilesX - 10;
											if (y < 10)
												y = 10;
											if (y > Main.maxTilesY - yAdjust - 10)
												y = Main.maxTilesY - yAdjust - 10;

											int spawnAreaY = Main.maxTilesY - y;
											for (int j = y; j < y + spawnAreaY; j++)
											{
												Tile tile = Main.tile[x, j];
												if ((tile.active() && Main.tileSolid[tile.type]) || tile.liquid >= 200)
												{
													y = j;
													break;
												}
											}

											int tornado = Projectile.NewProjectile(x * 16 + 8, y * 16 - 24, 0f, 0f, ProjectileID.Cthulunado, 50, 4f, player.whoAmI, 16f, 24f);
											Main.projectile[tornado].netUpdate = true;
										}
									}

									// Death Mode random lightning strikes
									int randomFrequency2 = (int)(20f * frequencyMult);
									if (CalamityWorld.rainingAcid && player.Calamity().ZoneSulphur)
										randomFrequency2 = (int)(randomFrequency2 * 3.75);
									if (player.miscCounter % (Main.hardMode ? 90 : 120) == 0 && Main.rand.NextBool(randomFrequency2))
									{
										if (!tileSafely.active())
										{
											float randomVelocity = Main.rand.NextFloat() - 0.5f;
											Vector2 fireTo = new Vector2(spawnPoint.X + 100f * randomVelocity, spawnPoint.Y + 900f);
											Vector2 direction = fireTo - spawnPoint;
											Vector2 velocity = Vector2.Normalize(direction) * 12f;
											Projectile.NewProjectile(spawnPoint.X, spawnPoint.Y, 0f, velocity.Y, ModContent.ProjectileType<LightningMark>(), 0, 0f, player.whoAmI, 0f, 0f);
										}
									}
								}
							}
						}

						// Immunity bools
						bool hasMoltenSet = player.head == ArmorIDs.Head.MoltenHelmet && player.body == ArmorIDs.Body.MoltenBreastplate && player.legs == ArmorIDs.Legs.MoltenGreaves;

						bool immunityToHotAndCold = hasMoltenSet || player.magmaStone || player.frostArmor || modPlayer.fBulwark || modPlayer.fBarrier ||
							modPlayer.frostFlare || modPlayer.rampartOfDeities || modPlayer.cryogenSoul || modPlayer.snowman || modPlayer.blazingCore || modPlayer.permafrostsConcoction || modPlayer.profanedCrystalBuffs || modPlayer.coldDivinity || modPlayer.eGauntlet;

						bool immunityToCold = player.HasBuff(BuffID.Campfire) || Main.campfire || player.resistCold || modPlayer.eskimoSet || player.buffImmune[BuffID.Frozen] || modPlayer.aAmpoule || player.HasBuff(BuffID.Inferno) || immunityToHotAndCold || modPlayer.externalColdImmunity;

						bool immunityToHot = player.lavaImmune || player.lavaRose || player.lavaMax > 0 || immunityToHotAndCold || modPlayer.externalHeatImmunity;

						// Space effects
						if (!player.behindBackWall && player.InSpace())
						{
							if (Main.dayTime)
							{
								if (!immunityToHot)
									player.AddBuff(BuffID.Burning, 2, false);
							}
							else
							{
								if (!immunityToCold)
									player.AddBuff(BuffID.Frostburn, 2, false);
							}
						}

						// Cold timer
						if (!player.behindBackWall && Main.raining && player.ZoneSnow && !immunityToCold && player.ZoneOverworldHeight)
						{
							bool affectedByColdWater = player.wet && !player.lavaWet && !player.honeyWet && !player.arcticDivingGear;

							player.AddBuff(ModContent.BuffType<DeathModeCold>(), 2, false);

							modPlayer.deathModeBlizzardTime++;
							if (affectedByColdWater)
								modPlayer.deathModeBlizzardTime++;

							if (modPlayer.deathModeUnderworldTime > 0)
							{
								modPlayer.deathModeUnderworldTime--;
								if (affectedByColdWater)
									modPlayer.deathModeUnderworldTime--;
								if (modPlayer.deathModeUnderworldTime < 0)
									modPlayer.deathModeUnderworldTime = 0;
							}
						}
						else if (modPlayer.deathModeBlizzardTime > 0)
						{
							modPlayer.deathModeBlizzardTime--;
							if (immunityToCold)
								modPlayer.deathModeBlizzardTime--;
							if (modPlayer.deathModeBlizzardTime < 0)
								modPlayer.deathModeBlizzardTime = 0;
						}

						// Hot timer
						if (!player.behindBackWall && player.ZoneUnderworldHeight && !immunityToHot)
						{
							bool affectedByHotLava = player.lavaWet;

							player.AddBuff(ModContent.BuffType<DeathModeHot>(), 2, false);

							modPlayer.deathModeUnderworldTime++;
							if (affectedByHotLava)
								modPlayer.deathModeUnderworldTime++;

							if (modPlayer.deathModeBlizzardTime > 0)
							{
								modPlayer.deathModeBlizzardTime--;
								if (affectedByHotLava)
									modPlayer.deathModeBlizzardTime--;
								if (modPlayer.deathModeBlizzardTime < 0)
									modPlayer.deathModeBlizzardTime = 0;
							}
						}
						else if (modPlayer.deathModeUnderworldTime > 0)
						{
							modPlayer.deathModeUnderworldTime--;
							if (immunityToHot)
								modPlayer.deathModeUnderworldTime--;
							if (modPlayer.deathModeUnderworldTime < 0)
								modPlayer.deathModeUnderworldTime = 0;
						}

						// Cold effects
						if (modPlayer.deathModeBlizzardTime > 1800)
							player.AddBuff(BuffID.Frozen, 2, false);
						if (modPlayer.deathModeBlizzardTime > 1980)
							modPlayer.KillPlayer();

						// Hot effects
						if (modPlayer.deathModeUnderworldTime > 360)
							player.AddBuff(BuffID.Weak, 2, false);
						if (modPlayer.deathModeUnderworldTime > 720)
							player.AddBuff(BuffID.Slow, 2, false);
						if (modPlayer.deathModeUnderworldTime > 1080)
							player.AddBuff(BuffID.OnFire, 2, false);
						if (modPlayer.deathModeUnderworldTime > 1440)
							player.AddBuff(BuffID.Confused, 2, false);
						if (modPlayer.deathModeUnderworldTime > 1800)
							player.AddBuff(BuffID.Burning, 2, false);
					}
				}
			}

			// Increase fall speed
			if (!player.mount.Active)
			{
				if (player.IsUnderwater() && modPlayer.ironBoots)
					player.maxFallSpeed = 9f;

				if (!player.wet)
				{
					if (modPlayer.aeroSet)
						player.maxFallSpeed = 15f;
					if (modPlayer.gSabatonFall > 0 || player.PortalPhysicsEnabled)
						player.maxFallSpeed = 20f;
				}
			}

			// Omega Blue Armor bonus
			if (modPlayer.omegaBlueSet)
			{
				// Add tentacles
				if (player.ownedProjectileCounts[ModContent.ProjectileType<OmegaBlueTentacle>()] < 6 && Main.myPlayer == player.whoAmI)
				{
					bool[] tentaclesPresent = new bool[6];
					for (int i = 0; i < Main.maxProjectiles; i++)
					{
						Projectile projectile = Main.projectile[i];
						if (projectile.active && projectile.type == ModContent.ProjectileType<OmegaBlueTentacle>() && projectile.owner == Main.myPlayer && projectile.ai[1] >= 0f && projectile.ai[1] < 6f)
							tentaclesPresent[(int)projectile.ai[1]] = true;
					}

					for (int i = 0; i < 6; i++)
					{
						if (!tentaclesPresent[i])
						{
							int damage = (int)(390 * player.AverageDamage());
							Vector2 vel = new Vector2(Main.rand.Next(-13, 14), Main.rand.Next(-13, 14)) * 0.25f;
							Projectile.NewProjectile(player.Center, vel, ModContent.ProjectileType<OmegaBlueTentacle>(), damage, 8f, Main.myPlayer, Main.rand.Next(120), i);
						}
					}
				}

				float damageUp = 0.1f;
				int critUp = 10;
				if (modPlayer.omegaBlueHentai)
				{
					damageUp *= 2f;
					critUp *= 2;
				}
				player.allDamage += damageUp;
				modPlayer.AllCritBoost(critUp);
			}

			bool canProvideBuffs = modPlayer.profanedCrystalBuffs || (!modPlayer.profanedCrystal && modPlayer.pArtifact) || (modPlayer.profanedCrystal && CalamityWorld.downedSCal);
			bool attack = player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianAttack>()] > 0;
			// Guardian bonuses if not burnt out
			if (!modPlayer.bOut && canProvideBuffs)
			{
				bool healer = player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianHealer>()] > 0;
				bool defend = player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianDefense>()] > 0;
				if (healer)
				{
					if (modPlayer.healCounter > 0)
						modPlayer.healCounter--;

					if (modPlayer.healCounter <= 0)
					{
						bool enrage = player.statLife < (int)(player.statLifeMax2 * 0.5);

						modPlayer.healCounter = (!enrage && modPlayer.profanedCrystalBuffs) ? 360 : 300;

						if (player.whoAmI == Main.myPlayer)
						{
							int healAmount = 5 +
								(defend ? 5 : 0) +
								(attack ? 5 : 0);

							player.statLife += healAmount;
							player.HealEffect(healAmount);
						}
					}
				}

				if (defend)
				{
					player.moveSpeed += 0.05f +
						(attack ? 0.05f : 0f);
					player.endurance += 0.025f +
						(attack ? 0.025f : 0f);
				}

				if (attack)
				{
					player.minionDamage += 0.1f +
						(defend ? 0.05f : 0f);
				}
			}

			// You always get the max minions, even during the effect of the burnout debuff
			if (attack && canProvideBuffs)
				player.maxMinions++;

			if (modPlayer.nucleogenesis)
			{
				player.maxMinions += 4;
			}
			else
			{
				if (modPlayer.shadowMinions)
					player.maxMinions += 3;
				else if (modPlayer.holyMinions)
					player.maxMinions += 2;

				if (modPlayer.starTaintedGenerator)
					player.maxMinions += 2;
				else
				{
					if (modPlayer.starbusterCore)
						player.maxMinions++;

					if (modPlayer.voltaicJelly)
						player.maxMinions++;

					if (modPlayer.nuclearRod)
						player.maxMinions++;
				}
			}

			// Cooldowns and timers
			if (modPlayer.spiritOriginBullseyeShootCountdown > 0)
				modPlayer.spiritOriginBullseyeShootCountdown--;
			if (modPlayer.phantomicHeartRegen > 0 && modPlayer.phantomicHeartRegen < 1000)
				modPlayer.phantomicHeartRegen--;
			if (modPlayer.phantomicBulwarkCooldown > 0)
				modPlayer.phantomicBulwarkCooldown--;
			if (modPlayer.dodgeCooldownTimer > 0)
				modPlayer.dodgeCooldownTimer--;
			if (modPlayer.KameiBladeUseDelay > 0)
				modPlayer.KameiBladeUseDelay--;
			if (modPlayer.galileoCooldown > 0)
				modPlayer.galileoCooldown--;
			if (modPlayer.soundCooldown > 0)
				modPlayer.soundCooldown--;
			if (modPlayer.shadowPotCooldown > 0)
				modPlayer.shadowPotCooldown--;
			if (modPlayer.raiderCooldown > 0)
				modPlayer.raiderCooldown--;
			if (modPlayer.gSabatonCooldown > 0)
				modPlayer.gSabatonCooldown--;
			if (modPlayer.gSabatonFall > 0)
				modPlayer.gSabatonFall--;
			if (modPlayer.astralStarRainCooldown > 0)
				modPlayer.astralStarRainCooldown--;
			if (modPlayer.tarraRangedCooldown > 0)
				modPlayer.tarraRangedCooldown--;
			if (modPlayer.bloodflareMageCooldown > 0)
				modPlayer.bloodflareMageCooldown--;
			if (modPlayer.silvaMageCooldown > 0)
				modPlayer.silvaMageCooldown--;
			if (modPlayer.tarraMageHealCooldown > 0)
				modPlayer.tarraMageHealCooldown--;
			if (modPlayer.featherCrownCooldown > 0)
				modPlayer.featherCrownCooldown--;
			if (modPlayer.moonCrownCooldown > 0)
				modPlayer.moonCrownCooldown--;
			if (modPlayer.nanoFlareCooldown > 0)
				modPlayer.nanoFlareCooldown--;
			if (modPlayer.spectralVeilImmunity > 0)
				modPlayer.spectralVeilImmunity--;
			if (modPlayer.jetPackCooldown > 0)
				modPlayer.jetPackCooldown--;
			if (modPlayer.jetPackDash > 0)
				modPlayer.jetPackDash--;
			if (modPlayer.theBeeCooldown > 0)
				modPlayer.theBeeCooldown--;
			if (modPlayer.nCoreCooldown > 0)
				modPlayer.nCoreCooldown--;
			if (modPlayer.jellyDmg > 0f)
				modPlayer.jellyDmg -= 1f;
			if (modPlayer.ataxiaDmg > 0f)
				modPlayer.ataxiaDmg -= 1.5f;
			if (modPlayer.ataxiaDmg < 0f)
				modPlayer.ataxiaDmg = 0f;
			if (modPlayer.xerocDmg > 0f)
				modPlayer.xerocDmg -= 2f;
			if (modPlayer.xerocDmg < 0f)
				modPlayer.xerocDmg = 0f;
			if (modPlayer.aBulwarkRareMeleeBoostTimer > 0)
				modPlayer.aBulwarkRareMeleeBoostTimer--;
			if (modPlayer.bossRushImmunityFrameCurseTimer > 0)
				modPlayer.bossRushImmunityFrameCurseTimer--;
			if (modPlayer.gaelRageAttackCooldown > 0)
				modPlayer.gaelRageAttackCooldown--;
			if (modPlayer.projRefRareLifeRegenCounter > 0)
				modPlayer.projRefRareLifeRegenCounter--;
			if (modPlayer.hurtSoundTimer > 0)
				modPlayer.hurtSoundTimer--;
			if (modPlayer.icicleCooldown > 0)
				modPlayer.icicleCooldown--;
			if (modPlayer.statisTimer > 0 && player.dashDelay >= 0)
				modPlayer.statisTimer = 0;
			if (modPlayer.hallowedRuneCooldown > 0)
				modPlayer.hallowedRuneCooldown--;
			if (modPlayer.sulphurBubbleCooldown > 0)
				modPlayer.sulphurBubbleCooldown--;
			if (modPlayer.forbiddenCooldown > 0)
				modPlayer.forbiddenCooldown--;
			if (modPlayer.tornadoCooldown > 0)
				modPlayer.tornadoCooldown--;
			if (modPlayer.ladHearts > 0)
				modPlayer.ladHearts--;
			if (modPlayer.titanBoost > 0)
				modPlayer.titanBoost--;
			if (modPlayer.prismaticLasers > 0)
				modPlayer.prismaticLasers--;
			if (modPlayer.dogTextCooldown > 0)
				modPlayer.dogTextCooldown--;
			if (modPlayer.titanCooldown > 0)
				modPlayer.titanCooldown--;
			if (modPlayer.omegaBlueCooldown > 0)
				modPlayer.omegaBlueCooldown--;
			if (modPlayer.plagueReaperCooldown > 0)
				modPlayer.plagueReaperCooldown--;
			if (modPlayer.brimflameFrenzyTimer > 0)
				modPlayer.brimflameFrenzyTimer--;
			if (modPlayer.bloodflareSoulTimer > 0)
				modPlayer.bloodflareSoulTimer--;
			if (modPlayer.fungalSymbioteTimer > 0)
				modPlayer.fungalSymbioteTimer--;
			if (modPlayer.aBulwarkRareTimer > 0)
				modPlayer.aBulwarkRareTimer--;
			if (modPlayer.hellbornBoost > 0)
				modPlayer.hellbornBoost--;
			if (modPlayer.persecutedEnchantSummonTimer < 1800)
				modPlayer.persecutedEnchantSummonTimer++;
            else
            {
				modPlayer.persecutedEnchantSummonTimer = 0;
				if (Main.myPlayer == player.whoAmI && player.Calamity().persecutedEnchant && NPC.CountNPCS(ModContent.NPCType<DemonPortal>()) < 2)
				{
					Vector2 spawnPosition = player.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(270f, 420f);
					CalamityNetcode.NewNPC_ClientSide(spawnPosition, ModContent.NPCType<DemonPortal>(), player);
				}
			}
			if (player.miscCounter % 20 == 0)
				modPlayer.canFireAtaxiaRangedProjectile = true;
			if (player.miscCounter % 100 == 0)
				modPlayer.canFireBloodflareMageProjectile = true;
			if (player.miscCounter % 150 == 0)
			{
				modPlayer.canFireGodSlayerRangedProjectile = true;
				modPlayer.canFireBloodflareRangedProjectile = true;
				modPlayer.canFireAtaxiaRogueProjectile = true;
			}
			if (modPlayer.reaverRegenCooldown < 60 && modPlayer.reaverRegen)
				modPlayer.reaverRegenCooldown++;
			else
				modPlayer.reaverRegenCooldown = 0;
			if (modPlayer.roverDrive)
			{
				if (modPlayer.roverDriveTimer < CalamityUtils.SecondsToFrames(30f))
					modPlayer.roverDriveTimer++;
				if (modPlayer.roverDriveTimer >= CalamityUtils.SecondsToFrames(30f))
					modPlayer.roverDriveTimer = 0;
			}
			else
				modPlayer.roverDriveTimer = 616; // Doesn't reset to zero to prevent exploits
			if (modPlayer.auralisAurora > 0)
				modPlayer.auralisAurora--;
			if (modPlayer.auralisAuroraCooldown > 0)
				modPlayer.auralisAuroraCooldown--;

			// God Slayer Armor dash debuff immunity
			if (modPlayer.dashMod == 9 && player.dashDelay < 0)
			{
				foreach (int debuff in CalamityLists.debuffList)
					player.buffImmune[debuff] = true;
			}

			// Auric dye cinders.
			int auricDyeCount = player.dye.Count(dyeItem => dyeItem.type == ModContent.ItemType<AuricDye>());
			if (auricDyeCount > 0)
			{
				int sparkCreationChance = (int)MathHelper.Lerp(15f, 50f, Utils.InverseLerp(4f, 1f, auricDyeCount, true));
				if (Main.rand.NextBool(sparkCreationChance))
				{
					Dust spark = Dust.NewDustDirect(player.position, player.width, player.height, 267);
					spark.color = Color.Lerp(Color.Cyan, Color.SeaGreen, Main.rand.NextFloat(0.5f));
					spark.velocity = -Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2 * 1.33f) * Main.rand.NextFloat(2f, 5.4f);
					spark.noGravity = true;
				}
			}

			// Silva invincibility effects
			if (modPlayer.silvaCountdown > 0 && modPlayer.hasSilvaEffect && modPlayer.silvaSet)
			{
				foreach (int debuff in CalamityLists.debuffList)
					player.buffImmune[debuff] = true;

				modPlayer.silvaCountdown -= 1;
				if (modPlayer.silvaCountdown <= 0)
					Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SilvaDispel"), player.Center);

				for (int j = 0; j < 2; j++)
				{
					int green = Dust.NewDust(player.position, player.width, player.height, 157, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 2f);
					Main.dust[green].position.X += (float)Main.rand.Next(-20, 21);
					Main.dust[green].position.Y += (float)Main.rand.Next(-20, 21);
					Main.dust[green].velocity *= 0.9f;
					Main.dust[green].noGravity = true;
					Main.dust[green].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
					Main.dust[green].shader = GameShaders.Armor.GetSecondaryShader(player.cWaist, player);
					if (Main.rand.NextBool(2))
						Main.dust[green].scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
				}
			}

			// Tarragon cloak effects
			if (modPlayer.tarragonCloak)
			{
				modPlayer.tarraDefenseTime--;
				if (modPlayer.tarraDefenseTime <= 0)
				{
					modPlayer.tarraDefenseTime = 600;
					if (player.whoAmI == Main.myPlayer)
						player.AddBuff(ModContent.BuffType<TarragonCloakCooldown>(), 1800, false);
				}

				for (int j = 0; j < 2; j++)
				{
					int green = Dust.NewDust(new Vector2(player.position.X, player.position.Y), player.width, player.height, 157, 0f, 0f, 100, new Color(Main.DiscoR, 203, 103), 2f);
					Dust dust = Main.dust[green];
					dust.position.X += (float)Main.rand.Next(-20, 21);
					dust.position.Y += (float)Main.rand.Next(-20, 21);
					dust.velocity *= 0.9f;
					dust.noGravity = true;
					dust.scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
					dust.shader = GameShaders.Armor.GetSecondaryShader(player.cWaist, player);
					if (Main.rand.NextBool(2))
						dust.scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
				}
			}

			// Tarragon immunity effects
			if (modPlayer.tarraThrowing)
			{
				if (modPlayer.tarragonImmunity)
				{
					player.immune = true;
					player.immuneTime = 2;

					for (int k = 0; k < player.hurtCooldowns.Length; k++)
						player.hurtCooldowns[k] = player.immuneTime;
				}

				if (modPlayer.tarraThrowingCrits >= 25)
				{
					modPlayer.tarraThrowingCrits = 0;
					if (player.whoAmI == Main.myPlayer)
						player.AddBuff(ModContent.BuffType<TarragonImmunity>(), 180, false);
				}

				for (int l = 0; l < Player.MaxBuffs; l++)
				{
					int hasBuff = player.buffType[l];
					if (player.buffTime[l] <= 2 && hasBuff == ModContent.BuffType<TarragonImmunity>())
					{
						if (player.whoAmI == Main.myPlayer)
							player.AddBuff(ModContent.BuffType<TarragonImmunityCooldown>(), 1500, false);
					}

					bool shouldAffect = CalamityLists.debuffList.Contains(hasBuff);
					if (shouldAffect)
						modPlayer.throwingDamage += 0.1f;
				}
			}

			// Bloodflare pickup spawn cooldowns
			if (modPlayer.bloodflareSet)
			{
				if (modPlayer.bloodflareHeartTimer > 0)
					modPlayer.bloodflareHeartTimer--;
			}

			// Bloodflare frenzy effects
			if (modPlayer.bloodflareMelee)
			{
				if (modPlayer.bloodflareMeleeHits >= 15)
				{
					modPlayer.bloodflareMeleeHits = 0;
					if (player.whoAmI == Main.myPlayer)
						player.AddBuff(ModContent.BuffType<BloodflareBloodFrenzy>(), 302, false);
				}

				if (modPlayer.bloodflareFrenzy)
				{
					for (int l = 0; l < Player.MaxBuffs; l++)
					{
						int hasBuff = player.buffType[l];
						if (player.buffTime[l] <= 2 && hasBuff == ModContent.BuffType<BloodflareBloodFrenzy>())
						{
							if (player.whoAmI == Main.myPlayer)
								player.AddBuff(ModContent.BuffType<BloodflareBloodFrenzyCooldown>(), 1800, false);
						}
					}

					player.meleeCrit += 25;
					player.meleeDamage += 0.25f;

					for (int j = 0; j < 2; j++)
					{
						int blood = Dust.NewDust(player.position, player.width, player.height, DustID.Blood, 0f, 0f, 100, default, 2f);
						Dust dust = Main.dust[blood];
						dust.position.X += (float)Main.rand.Next(-20, 21);
						dust.position.Y += (float)Main.rand.Next(-20, 21);
						dust.velocity *= 0.9f;
						dust.noGravity = true;
						dust.scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
						dust.shader = GameShaders.Armor.GetSecondaryShader(player.cWaist, player);
						if (Main.rand.NextBool(2))
							dust.scale *= 1f + (float)Main.rand.Next(40) * 0.01f;
					}
				}
			}

			// Raider Talisman bonus
			if (modPlayer.raiderTalisman)
			{
				// Nanotech use to have an exclusive nerf here, but since they are currently equal, there
				// is no check to indicate such.
				float damageMult = 0.15f;
				modPlayer.throwingDamage += modPlayer.raiderStack / 150f * damageMult;
			}

			if (modPlayer.kamiBoost)
				player.allDamage += 0.15f;

			if (modPlayer.avertorBonus)
				player.allDamage += 0.1f;

			if (modPlayer.roverDriveTimer < 616)
			{
				player.statDefense += 10;
				if (modPlayer.roverDriveTimer > 606)
					player.statDefense -= modPlayer.roverDriveTimer - 606; //so it scales down when the shield dies
			}

			// Absorber bonus
			if (modPlayer.absorber)
			{
				player.moveSpeed += 0.05f;
				player.jumpSpeedBoost += 0.25f;
				player.thorns += 0.5f;
				player.endurance += modPlayer.sponge ? 0.15f : 0.1f;

				if (player.StandingStill() && player.itemAnimation == 0)
					player.manaRegenBonus += 4;
			}

			// Sea Shell bonus
			if (modPlayer.seaShell)
			{
				if (player.IsUnderwater())
				{
					player.statDefense += 3;
					player.endurance += 0.05f;
					player.moveSpeed += 0.1f;
					player.ignoreWater = true;
				}
			}

			// Affliction bonus
			if (modPlayer.affliction || modPlayer.afflicted)
			{
				player.endurance += 0.07f;
				player.statDefense += 10;
				player.allDamage += 0.1f;
			}

			// Ambrosial Ampoule bonus and other light-granting bonuses
			float[] light = new float[3];
			if ((modPlayer.rOoze && !Main.dayTime) || modPlayer.aAmpoule)
			{
				light[0] += 1f;
				light[1] += 1f;
				light[2] += 0.6f;
			}
			if (modPlayer.aAmpoule)
			{
				player.endurance += 0.07f;
				player.buffImmune[BuffID.Frozen] = true;
				player.buffImmune[BuffID.Chilled] = true;
				player.buffImmune[BuffID.Frostburn] = true;
			}
			if (modPlayer.cFreeze)
			{
				light[0] += 0.3f;
				light[1] += Main.DiscoG / 400f;
				light[2] += 0.5f;
			}
			if (modPlayer.sirenIce)
			{
				light[0] += 0.35f;
				light[1] += 1f;
				light[2] += 1.25f;
			}
			if (modPlayer.sirenBoobs)
			{
				light[0] += 0.1f;
				light[1] += 1f;
				light[2] += 1.5f;
			}
			if (modPlayer.tarraSummon)
			{
				light[0] += 0f;
				light[1] += 3f;
				light[2] += 0f;
			}
			if (modPlayer.forbiddenCirclet)
			{
				light[0] += 0.8f;
				light[1] += 0.7f;
				light[2] += 0.2f;
			}
			Lighting.AddLight((int)(player.Center.X / 16f), (int)(player.Center.Y / 16f), light[0], light[1], light[2]);

			// Blazing Core bonus
			if (modPlayer.blazingCore)
				player.endurance += 0.1f;

			//Permafrost's Concoction bonuses/debuffs
			if (modPlayer.permafrostsConcoction)
				player.manaCost *= 0.85f;

			if (modPlayer.encased)
			{
				player.statDefense += 30;
				player.frozen = true;
				player.velocity.X = 0f;
				player.velocity.Y = -0.4f; //should negate gravity

				int ice = Dust.NewDust(player.position, player.width, player.height, 88);
				Main.dust[ice].noGravity = true;
				Main.dust[ice].velocity *= 2f;

				player.buffImmune[BuffID.Frozen] = true;
				player.buffImmune[BuffID.Chilled] = true;
				player.buffImmune[ModContent.BuffType<GlacialState>()] = true;
			}

			// Cosmic Discharge Cosmic Freeze buff, gives surrounding enemies the Glacial State debuff
			if (modPlayer.cFreeze)
			{
				int buffType = ModContent.BuffType<GlacialState>();
				float freezeDist = 200f;
				if (player.whoAmI == Main.myPlayer)
				{
					if (Main.rand.NextBool(5))
					{
						for (int l = 0; l < Main.maxNPCs; l++)
						{
							NPC npc = Main.npc[l];
							if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
								continue;
							if (!npc.buffImmune[buffType] && Vector2.Distance(player.Center, npc.Center) <= freezeDist)
							{
								if (npc.FindBuffIndex(buffType) == -1)
									npc.AddBuff(buffType, 60, false);
							}
						}
					}
				}
			}

			// Remove Purified Jam and Lul accessory thorn damage exploits
			if (modPlayer.invincible || modPlayer.lol)
			{
				player.thorns = 0f;
				player.turtleThorns = false;
			}

			// Vortex Armor nerf
			if (player.vortexStealthActive)
			{
				player.rangedDamage -= (1f - player.stealth) * 0.4f; // Change 80 to 40
				player.rangedCrit -= (int)((1f - player.stealth) * 5f); // Change 20 to 15
			}

			// Polaris fish stuff
			if (modPlayer.polarisBoost)
			{
				player.endurance += 0.01f;
				player.statDefense += 2;
			}
			if (!modPlayer.polarisBoost || player.ActiveItem().type != ModContent.ItemType<PolarisParrotfish>())
			{
				modPlayer.polarisBoost = false;
				if (player.FindBuffIndex(ModContent.BuffType<PolarisBuff>()) > -1)
					player.ClearBuff(ModContent.BuffType<PolarisBuff>());

				modPlayer.polarisBoostCounter = 0;
				modPlayer.polarisBoostTwo = false;
				modPlayer.polarisBoostThree = false;
			}
			if (modPlayer.polarisBoostCounter >= 20)
			{
				modPlayer.polarisBoostTwo = false;
				modPlayer.polarisBoostThree = true;
			}
			else if (modPlayer.polarisBoostCounter >= 10)
				modPlayer.polarisBoostTwo = true;

			// Calcium Potion buff
			if (modPlayer.calcium)
				player.noFallDmg = true;

			// Ceaseless Hunger Potion buff
			if (modPlayer.ceaselessHunger)
			{
				for (int j = 0; j < Main.maxItems; j++)
				{
					Item item = Main.item[j];
					if (item.active && item.noGrabDelay == 0 && item.owner == player.whoAmI)
					{
						item.beingGrabbed = true;
						if (player.Center.X > item.Center.X)
						{
							if (item.velocity.X < 90f + player.velocity.X)
							{
								item.velocity.X += 9f;
							}
							if (item.velocity.X < 0f)
							{
								item.velocity.X += 9f * 0.75f;
							}
						}
						else
						{
							if (item.velocity.X > -90f + player.velocity.X)
							{
								item.velocity.X -= 9f;
							}
							if (item.velocity.X > 0f)
							{
								item.velocity.X -= 9f * 0.75f;
							}
						}

						if (player.Center.Y > item.Center.Y)
						{
							if (item.velocity.Y < 90f)
							{
								item.velocity.Y += 9f;
							}
							if (item.velocity.Y < 0f)
							{
								item.velocity.Y += 9f * 0.75f;
							}
						}
						else
						{
							if (item.velocity.Y > -90f)
							{
								item.velocity.Y -= 9f;
							}
							if (item.velocity.Y > 0f)
							{
								item.velocity.Y -= 9f * 0.75f;
							}
						}
					}
				}
			}

			// Spectral Veil effects
			if (modPlayer.spectralVeil && modPlayer.spectralVeilImmunity > 0)
			{
				Rectangle sVeilRectangle = new Rectangle((int)(player.position.X + player.velocity.X * 0.5f - 4f), (int)(player.position.Y + player.velocity.Y * 0.5f - 4f), player.width + 8, player.height + 8);
				for (int i = 0; i < Main.maxNPCs; i++)
				{
					NPC npc = Main.npc[i];
					if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
						continue;
					if (!npc.townNPC && npc.immune[player.whoAmI] <= 0 && npc.damage > 0)
					{
						Rectangle rect = npc.getRect();
						if (sVeilRectangle.Intersects(rect) && (npc.noTileCollide || player.CanHit(npc)))
						{
							if (player.whoAmI == Main.myPlayer)
							{
								player.noKnockback = true;
								modPlayer.rogueStealth = modPlayer.rogueStealthMax;
								modPlayer.spectralVeilImmunity = 0;

								for (int k = 0; k < player.hurtCooldowns.Length; k++)
									player.hurtCooldowns[k] = player.immuneTime;

								Vector2 sVeilDustDir = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
								sVeilDustDir.Normalize();
								sVeilDustDir *= 0.5f;

								for (int j = 0; j < 20; j++)
								{
									int sVeilDustIndex1 = Dust.NewDust(player.Center, 1, 1, 21, sVeilDustDir.X * j, sVeilDustDir.Y * j);
									int sVeilDustIndex2 = Dust.NewDust(player.Center, 1, 1, 21, -sVeilDustDir.X * j, -sVeilDustDir.Y * j);
									Main.dust[sVeilDustIndex1].noGravity = false;
									Main.dust[sVeilDustIndex1].noLight = false;
									Main.dust[sVeilDustIndex2].noGravity = false;
									Main.dust[sVeilDustIndex2].noLight = false;
								}

								Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SilvaDispel"), player.Center);
							}
							break;
						}
					}
				}

				for (int i = 0; i < Main.maxProjectiles; i++)
				{
					Projectile proj = Main.projectile[i];
					if (proj.active && !proj.friendly && proj.hostile && proj.damage > 0)
					{
						Rectangle rect = proj.getRect();
						if (sVeilRectangle.Intersects(rect))
						{
							if (player.whoAmI == Main.myPlayer)
							{
								player.noKnockback = true;
								modPlayer.rogueStealth = modPlayer.rogueStealthMax;
								modPlayer.spectralVeilImmunity = 0;

								for (int k = 0; k < player.hurtCooldowns.Length; k++)
									player.hurtCooldowns[k] = player.immuneTime;

								Vector2 sVeilDustDir = new Vector2(Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-1f, 1f));
								sVeilDustDir.Normalize();
								sVeilDustDir *= 0.5f;

								for (int j = 0; j < 20; j++)
								{
									int sVeilDustIndex1 = Dust.NewDust(player.Center, 1, 1, 21, sVeilDustDir.X * j, sVeilDustDir.Y * j);
									int sVeilDustIndex2 = Dust.NewDust(player.Center, 1, 1, 21, -sVeilDustDir.X * j, -sVeilDustDir.Y * j);
									Main.dust[sVeilDustIndex1].noGravity = false;
									Main.dust[sVeilDustIndex1].noLight = false;
									Main.dust[sVeilDustIndex2].noGravity = false;
									Main.dust[sVeilDustIndex2].noLight = false;
								}

								Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Custom, "Sounds/Custom/SilvaDispel"), player.Center);
							}
							break;
						}
					}
				}
			}

			// Plagued Fuel Pack and Blunder Booster effects
			if (modPlayer.jetPackDash > 0 && player.whoAmI == Main.myPlayer)
			{
				int velocityAmt = modPlayer.blunderBooster ? 35 : 25;
				int velocityMult = modPlayer.jetPackDash > 1 ? velocityAmt : 5;
				player.velocity = new Vector2(modPlayer.jetPackDirection, -1) * velocityMult;

				if (modPlayer.blunderBooster)
				{
					int lightningCount = Main.rand.Next(2, 7);
					for (int i = 0; i < lightningCount; i++)
					{
						Vector2 lightningVel = new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1));
						lightningVel.Normalize();
						lightningVel *= Main.rand.NextFloat(1f, 2f);
						int projectile = Projectile.NewProjectile(player.Center, lightningVel, ModContent.ProjectileType<BlunderBoosterLightning>(), (int)(30 * player.RogueDamage()), 0, player.whoAmI, Main.rand.Next(2), 0f);
						Main.projectile[projectile].timeLeft = Main.rand.Next(180, 240);
						if (projectile.WithinBounds(Main.maxProjectiles))
							Main.projectile[projectile].Calamity().forceTypeless = true;
					}

					for (int i = 0; i < 3; i++)
					{
						int dust = Dust.NewDust(player.Center, 1, 1, 60, player.velocity.X * -0.1f, player.velocity.Y * -0.1f, 100, default, 3.5f);
						Main.dust[dust].noGravity = true;
						Main.dust[dust].velocity *= 1.2f;
						Main.dust[dust].velocity.Y -= 0.15f;
					}
				}
				else if (modPlayer.plaguedFuelPack)
				{
					int numClouds = Main.rand.Next(2, 10);
					for (int i = 0; i < numClouds; i++)
					{
						Vector2 cloudVelocity = new Vector2(Main.rand.NextFloat(-1, 1), Main.rand.NextFloat(-1, 1));
						cloudVelocity.Normalize();
						cloudVelocity *= Main.rand.NextFloat(0f, 1f);
						int projectile = Projectile.NewProjectile(player.Center, cloudVelocity, ModContent.ProjectileType<PlaguedFuelPackCloud>(), (int)(20 * player.RogueDamage()), 0, player.whoAmI, 0, 0);
						Main.projectile[projectile].timeLeft = Main.rand.Next(180, 240);
						if (projectile.WithinBounds(Main.maxProjectiles))
							Main.projectile[projectile].Calamity().forceTypeless = true;
					}

					for (int i = 0; i < 3; i++)
					{
						int dust = Dust.NewDust(player.Center, 1, 1, 89, player.velocity.X * -0.1f, player.velocity.Y * -0.1f, 100, default, 3.5f);
						Main.dust[dust].noGravity = true;
						Main.dust[dust].velocity *= 1.2f;
						Main.dust[dust].velocity.Y -= 0.15f;
					}
				}
			}

			// Gravistar Sabaton effects
			if (modPlayer.gSabaton && player.whoAmI == Main.myPlayer)
			{
				if (modPlayer.gSabatonCooldown <= 0 && !player.mount.Active)
				{
					if (player.controlDown && player.releaseDown && player.position.Y != player.oldPosition.Y)
					{
						modPlayer.gSabatonFall = 300;
						modPlayer.gSabatonCooldown = 480; //8 second cooldown
						player.gravity *= 2f;
						Projectile.NewProjectile(player.Center.X, player.Center.Y + (player.height / 5f), player.velocity.X, player.velocity.Y, ModContent.ProjectileType<SabatonSlam>(), 0, 0, player.whoAmI);
					}
				}
				if (modPlayer.gSabatonCooldown == 1) //dust when ready to use again
				{
					for (int i = 0; i < 66; i++)
					{
						int d = Dust.NewDust(player.position, player.width, player.height, Main.rand.NextBool(2) ? ModContent.DustType<AstralBlue>() : ModContent.DustType<AstralOrange>(), 0, 0, 100, default, 2.6f);
						Main.dust[d].noGravity = true;
						Main.dust[d].noLight = true;
						Main.dust[d].fadeIn = 1f;
						Main.dust[d].velocity *= 6.6f;
					}
				}
			}

			if (!modPlayer.brimflameSet && modPlayer.brimflameFrenzy)
			{
				modPlayer.brimflameFrenzy = false;
				player.ClearBuff(ModContent.BuffType<BrimflameFrenzyBuff>());
				player.AddBuff(ModContent.BuffType<BrimflameFrenzyCooldown>(), BrimflameScowl.CooldownLength, true);
				modPlayer.brimflameFrenzyTimer = BrimflameScowl.CooldownLength;
			}
			if (!modPlayer.bloodflareMelee && modPlayer.bloodflareFrenzy)
			{
				modPlayer.bloodflareFrenzy = false;
				player.ClearBuff(ModContent.BuffType<BloodflareBloodFrenzy>());
				player.AddBuff(ModContent.BuffType<BloodflareBloodFrenzyCooldown>(), 1800, false);
			}
			if (!modPlayer.tarraMelee && modPlayer.tarragonCloak)
			{
				modPlayer.tarragonCloak = false;
				player.ClearBuff(ModContent.BuffType<TarragonCloak>());
				player.AddBuff(ModContent.BuffType<TarragonCloakCooldown>(), 600, false);
			}
			if (!modPlayer.tarraThrowing && modPlayer.tarragonImmunity)
			{
				modPlayer.tarragonImmunity = false;
				player.ClearBuff(ModContent.BuffType<TarragonImmunity>());
				player.AddBuff(ModContent.BuffType<TarragonImmunityCooldown>(), 600, false);
			}
			if (!modPlayer.omegaBlueSet && modPlayer.omegaBlueCooldown > 1500)
			{
				modPlayer.omegaBlueCooldown = 1500;
				player.ClearBuff(ModContent.BuffType<AbyssalMadness>());
				player.AddBuff(ModContent.BuffType<AbyssalMadnessCooldown>(), 1500, false);
			}
			if (!modPlayer.plagueReaper && modPlayer.plagueReaperCooldown > 1500)
			{
				modPlayer.plagueReaperCooldown = 1500;
				player.AddBuff(ModContent.BuffType<PlagueBlackoutCooldown>(), 1500, false);
			}
			if (!modPlayer.prismaticSet && modPlayer.prismaticLasers > 1800)
			{
				modPlayer.prismaticLasers = 1800;
				player.AddBuff(ModContent.BuffType<PrismaticCooldown>(), CalamityUtils.SecondsToFrames(30f), true);
			}
			if (!modPlayer.angelicAlliance && modPlayer.divineBless)
			{
				modPlayer.divineBless = false;
				player.ClearBuff(ModContent.BuffType<DivineBless>());
				int seconds = CalamityUtils.SecondsToFrames(60f);
				player.AddBuff(ModContent.BuffType<DivineBlessCooldown>(), seconds, false);
			}
		}
		#endregion

		#region Abyss Effects
		private static void AbyssEffects(Player player, CalamityPlayer modPlayer)
		{
			int lightStrength = modPlayer.GetTotalLightStrength();
			modPlayer.abyssLightLevelStat = lightStrength;

			if (modPlayer.ZoneAbyss)
			{
				if (Main.myPlayer == player.whoAmI)
				{
					// Abyss depth variables
					Point point = player.Center.ToTileCoordinates();
					double abyssSurface = Main.rockLayer - Main.maxTilesY * 0.05;
					double abyssLevel1 = Main.rockLayer + Main.maxTilesY * 0.03;
					double totalAbyssDepth = Main.maxTilesY - 250D - abyssSurface;
					double totalAbyssDepthFromLayer1 = Main.maxTilesY - 250D - abyssLevel1;
					double playerAbyssDepth = point.Y - abyssSurface;
					double playerAbyssDepthFromLayer1 = point.Y - abyssLevel1;
					double depthRatio = playerAbyssDepth / totalAbyssDepth;
					double depthRatioFromAbyssLayer1 = playerAbyssDepthFromLayer1 / totalAbyssDepthFromLayer1;

					// Darkness strength scales smoothly with how deep you are.
					float darknessStrength = (float)depthRatio;

					// Reduce the power of abyss darkness based on your light level.
					float multiplier = 1f;
					switch (lightStrength)
					{
						case 0:
							break;
						case 1:
							multiplier = 0.85f;
							break;
						case 2:
							multiplier = 0.7f;
							break;
						case 3:
							multiplier = 0.55f;
							break;
						case 4:
							multiplier = 0.4f;
							break;
						case 5:
							multiplier = 0.25f;
							break;
						case 6:
							multiplier = 0.15f;
							break;
						case 7:
							multiplier = 0.1f;
							break;
						default:
							multiplier = 0.05f;
							break;
					}

					// Increased darkness in Death Mode
					if (CalamityWorld.death)
						multiplier += (1f - multiplier) * 0.1f;

					// Modify darkness variable
					modPlayer.caveDarkness = darknessStrength * multiplier;

					// Nebula Headcrab darkness effect
					if (!player.headcovered)
					{
						float screenObstructionAmt = MathHelper.Clamp(modPlayer.caveDarkness, 0f, 0.95f);
						float targetValue = MathHelper.Clamp(screenObstructionAmt * 0.7f, 0.1f, 0.3f);
						ScreenObstruction.screenObstruction = MathHelper.Lerp(ScreenObstruction.screenObstruction, screenObstructionAmt, targetValue);
					}

					// Breath lost while at zero breath
					double breathLoss = point.Y > abyssLevel1 ? 50D * depthRatioFromAbyssLayer1 : 0D;

					// Breath Loss Multiplier, depending on gear
					double breathLossMult = 1D -
						(player.gills ? 0.2 : 0D) - // 0.8
						(player.accDivingHelm ? 0.25 : 0D) - // 0.75
						(player.arcticDivingGear ? 0.25 : 0D) - // 0.75
						(modPlayer.aquaticEmblem ? 0.25 : 0D) - // 0.75
						(player.accMerman ? 0.3 : 0D) - // 0.7
						(modPlayer.victideSet ? 0.2 : 0D) - // 0.85
						((modPlayer.sirenBoobs && NPC.downedBoss3) ? 0.3 : 0D) - // 0.7
						(modPlayer.abyssalDivingSuit ? 0.3 : 0D); // 0.7

					// Limit the multiplier to 5%
					if (breathLossMult < 0.05)
						breathLossMult = 0.05;

					// Reduce breath lost while at zero breath, depending on gear
					breathLoss *= breathLossMult;

					// Stat Meter stat
					modPlayer.abyssBreathLossStat = (int)breathLoss;

					// Defense loss
					int defenseLoss = (int)(120D * depthRatio);

					// Anechoic Plating reduces defense loss by 66%
					// Fathom Swarmer Breastplate reduces defense loss by 40%
					// In tandem, reduces defense loss by 80%
					if (modPlayer.anechoicPlating && modPlayer.fathomSwarmerBreastplate)
						defenseLoss = (int)(defenseLoss * 0.2f);
					else if (modPlayer.anechoicPlating)
						defenseLoss /= 3;
					else if (modPlayer.fathomSwarmerBreastplate)
						defenseLoss = (int)(defenseLoss * 0.6f);

					// Reduce defense
					player.statDefense -= defenseLoss;

					// Stat Meter stat
					modPlayer.abyssDefenseLossStat = defenseLoss;

					// Bleed effect based on abyss layer
					if (modPlayer.ZoneAbyssLayer4)
					{
						player.bleed = true;
					}
					else if (modPlayer.ZoneAbyssLayer3)
					{
						if (!modPlayer.abyssalDivingSuit)
							player.bleed = true;
					}
					else if (modPlayer.ZoneAbyssLayer2)
					{
						if (!modPlayer.depthCharm)
							player.bleed = true;
					}

					// Ticks (frames) until breath is deducted from the breath meter
					double tick = 12D * (1D - depthRatio);

					// Prevent 0
					if (tick < 1D)
						tick = 1D;

					// Tick (frame) multiplier, depending on gear
					double tickMult = 1D +
						(player.gills ? 4D : 0D) + // 5
						(player.ignoreWater ? 5D : 0D) + // 10
						(player.accDivingHelm ? 10D : 0D) + // 20
						(player.arcticDivingGear ? 10D : 0D) + // 30
						(modPlayer.aquaticEmblem ? 10D : 0D) + // 40
						(player.accMerman ? 15D : 0D) + // 55
						(modPlayer.victideSet ? 5D : 0D) + // 60
						((modPlayer.sirenBoobs && NPC.downedBoss3) ? 15D : 0D) + // 75
						(modPlayer.abyssalDivingSuit ? 15D : 0D); // 90

					// Limit the multiplier to 50
					if (tickMult > 50D)
						tickMult = 50D;

					// Increase ticks (frames) until breath is deducted, depending on gear
					tick *= tickMult;

					// Stat Meter stat
					modPlayer.abyssBreathLossRateStat = (int)tick;

					// Reduce breath over ticks (frames)
					modPlayer.abyssBreathCD++;
					if (modPlayer.abyssBreathCD >= (int)tick)
					{
						// Reset modded breath variable
						modPlayer.abyssBreathCD = 0;

						// Reduce breath
						if (player.breath > 0)
							player.breath -= (int)(modPlayer.cDepth ? breathLoss + 1D : breathLoss);
					}

					// If breath is greater than 0 and player has gills or is merfolk, balance out the effects by reducing breath
					if (player.breath > 0)
					{
						if (player.gills || player.merman)
							player.breath -= 3;
					}

					// Life loss at zero breath
					int lifeLossAtZeroBreath = (int)(12D * depthRatio);

					// Resistance to life loss at zero breath
					int lifeLossAtZeroBreathResist = 0 +
						(modPlayer.depthCharm ? 3 : 0) +
						(modPlayer.abyssalDivingSuit ? 6 : 0);

					// Reduce life loss, depending on gear
					lifeLossAtZeroBreath -= lifeLossAtZeroBreathResist;

					// Prevent negatives
					if (lifeLossAtZeroBreath < 0)
						lifeLossAtZeroBreath = 0;

					// Stat Meter stat
					modPlayer.abyssLifeLostAtZeroBreathStat = lifeLossAtZeroBreath;

					// Check breath value
					if (player.breath <= 0)
					{
						// Reduce life
						player.statLife -= lifeLossAtZeroBreath;

						// Special kill code if the life loss kills the player
						if (player.statLife <= 0)
						{
							modPlayer.abyssDeath = true;
							modPlayer.KillPlayer();
						}
					}
				}
			}
			else
			{
				modPlayer.abyssBreathCD = 0;
				modPlayer.abyssDeath = false;
			}
		}
		#endregion

		#region Calamitas Enchantment Held Item Effects
		public static void EnchantHeldItemEffects(Player player, CalamityPlayer modPlayer, Item heldItem)
		{
			if (heldItem.IsAir)
				return;

			// Exhaustion recharge effects.
			foreach (Item item in player.inventory)
			{
				if (item.IsAir)
					continue;

				if (item.Calamity().AppliedEnchantment.HasValue && item.Calamity().AppliedEnchantment.Value.ID == 600)
				{
					// Initialize the exhaustion if it is currently not defined.
					if (item.Calamity().DischargeEnchantExhaustion <= 0f)
						item.Calamity().DischargeEnchantExhaustion = CalamityGlobalItem.DischargeEnchantExhaustionCap;

					// Slowly recharge the weapon over time. This is depleted when the item is actaully used.
					else if (item.Calamity().DischargeEnchantExhaustion < CalamityGlobalItem.DischargeEnchantExhaustionCap)
						item.Calamity().DischargeEnchantExhaustion++;
				}
				else
					item.Calamity().DischargeEnchantExhaustion = 0f;
			}

			if (!heldItem.Calamity().AppliedEnchantment.HasValue || heldItem.Calamity().AppliedEnchantment.Value.HoldEffect is null)
				return;

			heldItem.Calamity().AppliedEnchantment.Value.HoldEffect(player);

			// Weak brimstone flame hold curse effect.
			if (modPlayer.flamingItemEnchant)
				player.AddBuff(ModContent.BuffType<WeakBrimstoneFlames>(), 10);
		}
		#endregion

		#region Max Life And Mana Effects
		private static void MaxLifeAndManaEffects(Player player, CalamityPlayer modPlayer, Mod mod)
		{
			// New textures
			if (Main.netMode != NetmodeID.Server && player.whoAmI == Main.myPlayer)
			{
				Texture2D rain3 = ModContent.GetTexture("CalamityMod/ExtraTextures/Rain3");
				Texture2D rainOriginal = ModContent.GetTexture("CalamityMod/ExtraTextures/RainOriginal");
				Texture2D mana2 = ModContent.GetTexture("CalamityMod/ExtraTextures/Mana2");
				Texture2D mana3 = ModContent.GetTexture("CalamityMod/ExtraTextures/Mana3");
				Texture2D mana4 = ModContent.GetTexture("CalamityMod/ExtraTextures/Mana4");
				Texture2D manaOriginal = ModContent.GetTexture("CalamityMod/ExtraTextures/ManaOriginal");
				Texture2D carpetAuric = ModContent.GetTexture("CalamityMod/ExtraTextures/AuricCarpet");
				Texture2D carpetOriginal = ModContent.GetTexture("CalamityMod/ExtraTextures/Carpet");

				int totalManaBoost =
					(modPlayer.pHeart ? 1 : 0) +
					(modPlayer.eCore ? 1 : 0) +
					(modPlayer.cShard ? 1 : 0);
				switch (totalManaBoost)
				{
					default:
						Main.manaTexture = manaOriginal;
						break;
					case 3:
						Main.manaTexture = mana4;
						break;
					case 2:
						Main.manaTexture = mana3;
						break;
					case 1:
						Main.manaTexture = mana2;
						break;
				}

				if (Main.bloodMoon)
					Main.rainTexture = rainOriginal;
				else if (Main.raining && modPlayer.ZoneSulphur)
					Main.rainTexture = rain3;
				else
					Main.rainTexture = rainOriginal;

				if (modPlayer.auricSet)
					Main.flyingCarpetTexture = carpetAuric;
				else
					Main.flyingCarpetTexture = carpetOriginal;
			}
		}
		#endregion

		#region Standing Still Effects
		private static void StandingStillEffects(Player player, CalamityPlayer modPlayer)
		{
			// Rogue Stealth
			modPlayer.UpdateRogueStealth();

			// Trinket of Chi bonus
			if (modPlayer.trinketOfChi)
			{
				if (modPlayer.trinketOfChiBuff)
				{
					player.allDamage += 0.5f;
					if (player.itemAnimation > 0)
						modPlayer.chiBuffTimer = 0;
				}

				if (player.StandingStill(0.1f) && !player.mount.Active)
				{
					if (modPlayer.chiBuffTimer < 60)
						modPlayer.chiBuffTimer++;
					else
						player.AddBuff(ModContent.BuffType<ChiBuff>(), 6);
				}
				else
					modPlayer.chiBuffTimer--;
			}
			else
				modPlayer.chiBuffTimer = 0;

			// Aquatic Emblem bonus
			if (modPlayer.aquaticEmblem)
			{
				if (player.IsUnderwater() && player.wet && !player.lavaWet && !player.honeyWet &&
					!player.mount.Active)
				{
					if (modPlayer.aquaticBoost > 0f)
					{
						modPlayer.aquaticBoost -= 2f;
						if (modPlayer.aquaticBoost <= 0f)
						{
							modPlayer.aquaticBoost = 0f;
							if (Main.netMode == NetmodeID.MultiplayerClient)
								NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
						}
					}
				}
				else
				{
					modPlayer.aquaticBoost += 2f;
					if (modPlayer.aquaticBoost > modPlayer.aquaticBoostMax)
						modPlayer.aquaticBoost = modPlayer.aquaticBoostMax;
					if (player.mount.Active)
						modPlayer.aquaticBoost = modPlayer.aquaticBoostMax;
				}

				player.statDefense += (int)((1f - modPlayer.aquaticBoost * 0.0001f) * 50f);
				player.moveSpeed -= (1f - modPlayer.aquaticBoost * 0.0001f) * 0.1f;
			}
			else
				modPlayer.aquaticBoost = modPlayer.aquaticBoostMax;

			// Auric bonus
			if (modPlayer.auricBoost)
			{
				if (player.itemAnimation > 0)
					modPlayer.modStealthTimer = 5;

				if (player.StandingStill(0.1f) && !player.mount.Active)
				{
					if (modPlayer.modStealthTimer == 0 && modPlayer.modStealth > 0f)
					{
						modPlayer.modStealth -= 0.015f;
						if (modPlayer.modStealth <= 0f)
						{
							modPlayer.modStealth = 0f;
							if (Main.netMode == NetmodeID.MultiplayerClient)
								NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
						}
					}
				}
				else
				{
					float playerVel = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y);
					modPlayer.modStealth += playerVel * 0.0075f;
					if (modPlayer.modStealth > 1f)
						modPlayer.modStealth = 1f;
					if (player.mount.Active)
						modPlayer.modStealth = 1f;
				}

				float damageBoost = (1f - modPlayer.modStealth) * 0.2f;
				player.allDamage += damageBoost;

				int critBoost = (int)((1f - modPlayer.modStealth) * 10f);
				modPlayer.AllCritBoost(critBoost);

				if (modPlayer.modStealthTimer > 0)
					modPlayer.modStealthTimer--;
			}

			// Psychotic Amulet bonus
			else if (modPlayer.pAmulet)
			{
				if (player.itemAnimation > 0)
					modPlayer.modStealthTimer = 5;

				if (player.StandingStill(0.1f) && !player.mount.Active)
				{
					if (modPlayer.modStealthTimer == 0 && modPlayer.modStealth > 0f)
					{
						modPlayer.modStealth -= 0.015f;
						if (modPlayer.modStealth <= 0f)
						{
							modPlayer.modStealth = 0f;
							if (Main.netMode == NetmodeID.MultiplayerClient)
								NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
						}
					}
				}
				else
				{
					float playerVel = Math.Abs(player.velocity.X) + Math.Abs(player.velocity.Y);
					modPlayer.modStealth += playerVel * 0.0075f;
					if (modPlayer.modStealth > 1f)
						modPlayer.modStealth = 1f;
					if (player.mount.Active)
						modPlayer.modStealth = 1f;
				}

				modPlayer.throwingDamage += (1f - modPlayer.modStealth) * 0.2f;
				modPlayer.throwingCrit += (int)((1f - modPlayer.modStealth) * 10f);
				player.aggro -= (int)((1f - modPlayer.modStealth) * 750f);
				if (modPlayer.modStealthTimer > 0)
					modPlayer.modStealthTimer--;
			}
			else
				modPlayer.modStealth = 1f;

			if (player.ActiveItem().type == ModContent.ItemType<Auralis>() && player.StandingStill(0.1f))
			{
				if (modPlayer.auralisStealthCounter < 300f)
					modPlayer.auralisStealthCounter++;

				bool usingScope = false;
				if (!Main.gameMenu && Main.netMode != NetmodeID.Server)
				{
					if (player.noThrow <= 0 && !player.lastMouseInterface || (Main.zoomX != 0f || Main.zoomY != 0f))
					{
						if (PlayerInput.UsingGamepad)
						{
							if (PlayerInput.GamepadThumbstickRight.Length() != 0f || !Main.SmartCursorEnabled)
							{
								usingScope = true;
							}
						}
						else if (Main.mouseRight)
							usingScope = true;
					}
				}

				int chargeDuration = CalamityUtils.SecondsToFrames(5f);
				int auroraDuration = CalamityUtils.SecondsToFrames(20f);

				if (usingScope && modPlayer.auralisAuroraCounter < chargeDuration + auroraDuration)
					modPlayer.auralisAuroraCounter++;

				if (modPlayer.auralisAuroraCounter > chargeDuration + auroraDuration)
				{
					modPlayer.auralisAuroraCounter = 0;
					modPlayer.auralisAuroraCooldown = CalamityUtils.SecondsToFrames(30f);
				}

				if (modPlayer.auralisAuroraCounter > 0 && modPlayer.auralisAuroraCounter < chargeDuration && !usingScope)
					modPlayer.auralisAuroraCounter--;

				if (modPlayer.auralisAuroraCounter > chargeDuration && modPlayer.auralisAuroraCounter < chargeDuration + auroraDuration && !usingScope)
					modPlayer.auralisAuroraCounter = 0;
			}
			else
			{
				modPlayer.auralisStealthCounter = 0f;
				modPlayer.auralisAuroraCounter = 0;
			}
			if (modPlayer.auralisAuroraCooldown > 0)
			{
				if (modPlayer.auralisAuroraCooldown == 1)
				{
					int dustAmt = 36;
					for (int d = 0; d < dustAmt; d++)
					{
						Vector2 source = Vector2.Normalize(player.velocity) * new Vector2((float)player.width / 2f, (float)player.height) * 1f; //0.75
						source = source.RotatedBy((double)((float)(d - (dustAmt / 2 - 1)) * MathHelper.TwoPi / (float)dustAmt), default) + player.Center;
						Vector2 dustVel = source - player.Center;
						int blue = Dust.NewDust(source + dustVel, 0, 0, 229, dustVel.X, dustVel.Y, 100, default, 1.2f);
						Main.dust[blue].noGravity = true;
						Main.dust[blue].noLight = false;
						Main.dust[blue].velocity = dustVel;
					}
					for (int d = 0; d < dustAmt; d++)
					{
						Vector2 source = Vector2.Normalize(player.velocity) * new Vector2((float)player.width / 2f, (float)player.height) * 0.75f;
						source = source.RotatedBy((double)((float)(d - (dustAmt / 2 - 1)) * MathHelper.TwoPi / (float)dustAmt), default) + player.Center;
						Vector2 dustVel = source - player.Center;
						int green = Dust.NewDust(source + dustVel, 0, 0, 107, dustVel.X, dustVel.Y, 100, default, 1.2f);
						Main.dust[green].noGravity = true;
						Main.dust[green].noLight = false;
						Main.dust[green].velocity = dustVel;
					}
				}
				modPlayer.auralisAuroraCounter = 0;
			}
		}
		#endregion

		#region Elysian Aegis Effects
		private static void ElysianAegisEffects(Player player, CalamityPlayer modPlayer)
		{
			if (modPlayer.elysianAegis)
			{
				bool spawnDust = false;

				// Activate buff
				if (modPlayer.elysianGuard)
				{
					if (player.whoAmI == Main.myPlayer)
						player.AddBuff(ModContent.BuffType<ElysianGuard>(), 2, false);

					float shieldBoostInitial = modPlayer.shieldInvinc;
					modPlayer.shieldInvinc -= 0.08f;
					if (modPlayer.shieldInvinc < 0f)
						modPlayer.shieldInvinc = 0f;
					else
						spawnDust = true;

					if (modPlayer.shieldInvinc == 0f && shieldBoostInitial != modPlayer.shieldInvinc && Main.netMode == NetmodeID.MultiplayerClient)
						NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, player.whoAmI, 0f, 0f, 0f, 0, 0, 0);

					float damageBoost = (5f - modPlayer.shieldInvinc) * 0.03f;
					player.allDamage += damageBoost;

					int critBoost = (int)((5f - modPlayer.shieldInvinc) * 2f);
					modPlayer.AllCritBoost(critBoost);

					player.aggro += (int)((5f - modPlayer.shieldInvinc) * 220f);
					player.statDefense += (int)((5f - modPlayer.shieldInvinc) * 8f);
					player.moveSpeed *= 0.85f;

					if (player.mount.Active)
						modPlayer.elysianGuard = false;
				}

				// Remove buff
				else
				{
					float shieldBoostInitial = modPlayer.shieldInvinc;
					modPlayer.shieldInvinc += 0.08f;
					if (modPlayer.shieldInvinc > 5f)
						modPlayer.shieldInvinc = 5f;
					else
						spawnDust = true;

					if (modPlayer.shieldInvinc == 5f && shieldBoostInitial != modPlayer.shieldInvinc && Main.netMode == NetmodeID.MultiplayerClient)
						NetMessage.SendData(MessageID.PlayerStealth, -1, -1, null, player.whoAmI, 0f, 0f, 0f, 0, 0, 0);
				}

				// Emit dust
				if (spawnDust)
				{
					if (Main.rand.NextBool(2))
					{
						Vector2 vector = Vector2.UnitY.RotatedByRandom(Math.PI * 2D);
						Dust dust = Main.dust[Dust.NewDust(player.Center - vector * 30f, 0, 0, (int)CalamityDusts.ProfanedFire, 0f, 0f, 0, default, 1f)];
						dust.noGravity = true;
						dust.position = player.Center - vector * (float)Main.rand.Next(5, 11);
						dust.velocity = vector.RotatedBy(Math.PI / 2D, default) * 4f;
						dust.scale = 0.5f + Main.rand.NextFloat();
						dust.fadeIn = 0.5f;
					}

					if (Main.rand.NextBool(2))
					{
						Vector2 vector2 = Vector2.UnitY.RotatedByRandom(Math.PI * 2D);
						Dust dust2 = Main.dust[Dust.NewDust(player.Center - vector2 * 30f, 0, 0, 246, 0f, 0f, 0, default, 1f)];
						dust2.noGravity = true;
						dust2.position = player.Center - vector2 * 12f;
						dust2.velocity = vector2.RotatedBy(-Math.PI / 2D, default) * 2f;
						dust2.scale = 0.5f + Main.rand.NextFloat();
						dust2.fadeIn = 0.5f;
					}
				}
			}
			else
				modPlayer.elysianGuard = false;
		}
		#endregion

		#region Other Buff Effects
		private static void OtherBuffEffects(Player player, CalamityPlayer modPlayer)
		{
			if (modPlayer.gravityNormalizer)
			{
				player.buffImmune[BuffID.VortexDebuff] = true;
				if (player.InSpace())
				{
					player.gravity = Player.defaultGravity;
					if (player.wet)
					{
						if (player.honeyWet)
							player.gravity = 0.1f;
						else if (player.merman)
							player.gravity = 0.3f;
						else
							player.gravity = 0.2f;
					}
				}
			}

			// Effigy of Decay effects
			if (modPlayer.decayEffigy)
			{
				player.buffImmune[ModContent.BuffType<SulphuricPoisoning>()] = true;
				if (!modPlayer.ZoneAbyss)
				{
					player.gills = true;
				}
			}

			if (modPlayer.astralInjection)
			{
				if (player.statMana < player.statManaMax2)
					player.statMana += 3;
				if (player.statMana > player.statManaMax2)
					player.statMana = player.statManaMax2;
			}

			if (modPlayer.armorCrumbling)
			{
				modPlayer.throwingCrit += 5;
				player.meleeCrit += 5;
			}

			if (modPlayer.armorShattering)
			{
				if (player.FindBuffIndex(ModContent.BuffType<ArmorCrumbling>()) > -1)
					player.ClearBuff(ModContent.BuffType<ArmorCrumbling>());
				modPlayer.throwingDamage += 0.08f;
				player.meleeDamage += 0.08f;
				modPlayer.throwingCrit += 8;
				player.meleeCrit += 8;
			}

			if (modPlayer.holyWrath)
			{
				if (player.FindBuffIndex(BuffID.Wrath) > -1)
					player.ClearBuff(BuffID.Wrath);
				player.allDamage += 0.12f;
				player.moveSpeed += 0.05f;
			}

			if (modPlayer.profanedRage)
			{
				if (player.FindBuffIndex(BuffID.Rage) > -1)
					player.ClearBuff(BuffID.Rage);
				modPlayer.AllCritBoost(12);
				player.moveSpeed += 0.05f;
			}

			if (modPlayer.shadow)
			{
				if (player.FindBuffIndex(BuffID.Invisibility) > -1)
					player.ClearBuff(BuffID.Invisibility);
			}

			if (modPlayer.irradiated)
			{
				player.statDefense -= 10;
				player.moveSpeed -= 0.1f;
				player.allDamage += 0.05f;
				player.minionKB += 0.5f;
			}

			if (modPlayer.rRage)
			{
				player.allDamage += 0.3f;
				player.statDefense += 5;
			}

			if (modPlayer.xRage)
				modPlayer.throwingDamage += 0.1f;

			if (modPlayer.xWrath)
				modPlayer.throwingCrit += 5;

			if (modPlayer.graxDefense)
			{
				player.statDefense += 30;
				player.endurance += 0.1f;
				player.meleeDamage += 0.2f;
			}

			if (modPlayer.tFury)
			{
				player.meleeDamage += 0.3f;
				player.meleeCrit += 10;
			}

			if (modPlayer.yPower)
			{
				player.endurance += 0.06f;
				player.statDefense += 8;
				player.pickSpeed -= 0.05f;
				player.allDamage += 0.06f;
				modPlayer.AllCritBoost(2);
				player.minionKB += 1f;
				player.moveSpeed += 0.06f;
			}

			if (modPlayer.tScale)
			{
				player.endurance += 0.05f;
				player.statDefense += 5;
				player.kbBuff = true;
				if (modPlayer.titanBoost > 0)
				{
					player.statDefense += 25;
					player.endurance += 0.1f;
				}
			}
			else
				modPlayer.titanBoost = 0;

			if (modPlayer.darkSunRing)
			{
				player.maxMinions += 2;
				player.allDamage += 0.12f;
				player.minionKB += 1.2f;
				player.pickSpeed -= 0.15f;
				if (Main.eclipse || !Main.dayTime)
					player.statDefense += 15;
			}

			if (modPlayer.eGauntlet)
			{
				player.kbGlove = true;
				player.magmaStone = true;
				player.meleeDamage += 0.15f;
				player.meleeCrit += 5;
				player.lavaMax += 240;
			}

			if (modPlayer.bloodPactBoost)
			{
				player.allDamage += 0.05f;
				player.statDefense += 20;
				player.endurance += 0.1f;
				player.longInvince = true;
				player.crimsonRegen = true;
			}

			if (modPlayer.fabsolVodka)
				player.allDamage += 0.08f;

			if (modPlayer.vodka)
			{
				player.allDamage += 0.06f;
				modPlayer.AllCritBoost(2);
			}

			if (modPlayer.grapeBeer)
				player.moveSpeed -= 0.05f;

			if (modPlayer.moonshine)
			{
				player.statDefense += 10;
				player.endurance += 0.05f;
			}

			if (modPlayer.rum)
				player.moveSpeed += 0.1f;

			if (modPlayer.whiskey)
			{
				player.allDamage += 0.04f;
				modPlayer.AllCritBoost(2);
			}

			if (modPlayer.everclear)
				player.allDamage += 0.25f;

			if (modPlayer.bloodyMary)
			{
				if (Main.bloodMoon)
				{
					player.allDamage += 0.15f;
					modPlayer.AllCritBoost(7);
					player.moveSpeed += 0.1f;
				}
			}

			if (modPlayer.tequila)
			{
				if (Main.dayTime)
				{
					player.statDefense += 5;
					player.allDamage += 0.03f;
					modPlayer.AllCritBoost(2);
					player.endurance += 0.03f;
				}
			}

			if (modPlayer.tequilaSunrise)
			{
				if (Main.dayTime)
				{
					player.statDefense += 10;
					player.allDamage += 0.07f;
					modPlayer.AllCritBoost(3);
					player.endurance += 0.07f;
				}
			}

			if (modPlayer.caribbeanRum)
				player.moveSpeed += 0.1f;

			if (modPlayer.cinnamonRoll)
			{
				player.manaRegenDelay--;
				player.manaRegenBonus += 10;
			}

			if (modPlayer.starBeamRye)
			{
				player.magicDamage += 0.08f;
				player.manaCost *= 0.9f;
			}

			if (modPlayer.moscowMule)
			{
				player.allDamage += 0.09f;
				modPlayer.AllCritBoost(3);
			}

			if (modPlayer.whiteWine)
				player.magicDamage += 0.1f;

			if (modPlayer.evergreenGin)
				player.endurance += 0.05f;

			if (modPlayer.giantPearl)
			{
				if (Main.netMode != NetmodeID.MultiplayerClient && !CalamityPlayer.areThereAnyDamnBosses)
				{
					for (int m = 0; m < Main.maxNPCs; m++)
					{
						NPC npc = Main.npc[m];
						if (!npc.active || npc.friendly || npc.dontTakeDamage)
							continue;
						float distance = (npc.Center - player.Center).Length();
						if (distance < 120f)
							npc.AddBuff(ModContent.BuffType<PearlAura>(), 20, false);
					}
				}
			}

			if (CalamityLists.scopedWeaponList.Contains(player.ActiveItem().type))
				player.scope = true;

			if (CalamityLists.highTestFishList.Contains(player.ActiveItem().type))
				player.accFishingLine = true;

			if (CalamityLists.boomerangList.Contains(player.ActiveItem().type) && player.invis)
				modPlayer.throwingDamage += 0.1f;

			if (CalamityLists.javelinList.Contains(player.ActiveItem().type) && player.invis)
				player.armorPenetration += 5;

			if (CalamityLists.flaskBombList.Contains(player.ActiveItem().type) && player.invis)
				modPlayer.throwingVelocity += 0.1f;

			if (CalamityLists.spikyBallList.Contains(player.ActiveItem().type) && player.invis)
				modPlayer.throwingCrit += 10;

			if (modPlayer.planarSpeedBoost != 0)
			{
				if (player.ActiveItem().type != ModContent.ItemType<PrideHuntersPlanarRipper>())
					modPlayer.planarSpeedBoost = 0;
			}

			if (modPlayer.brimlashBusterBoost)
			{
				if (player.ActiveItem().type != ModContent.ItemType<BrimlashBuster>() && player.ActiveItem().type != ModContent.ItemType<EvilSmasher>())
					modPlayer.brimlashBusterBoost = false;
			}
			if (modPlayer.animusBoost > 1f)
			{
				if (player.ActiveItem().type != ModContent.ItemType<Animus>())
					modPlayer.animusBoost = 1f;
			}

			// Flight time boosts
			double flightTimeMult = 1D +
				(modPlayer.ZoneAstral ? 0.05 : 0D) +
				(modPlayer.harpyRing ? 0.2 : 0D) +
				(modPlayer.aeroStone ? 0.1 : 0D) +
				(modPlayer.angelTreads ? 0.1 : 0D) +
				(modPlayer.blueCandle ? 0.1 : 0D) +
				(modPlayer.soaring ? 0.1 : 0D) +
				(modPlayer.prismaticGreaves ? 0.1 : 0D) +
				(modPlayer.plagueReaper ? 0.05 : 0D) +
				(modPlayer.draconicSurge ? 0.2 : 0D);

			if (modPlayer.harpyRing)
				player.moveSpeed += 0.1f;

			if (modPlayer.blueCandle)
				player.moveSpeed += 0.1f;

			if (modPlayer.draconicSurgeCooldown) // Weird mod conflicts with like Luiafk
			{
				modPlayer.draconicSurge = false;
				if (player.FindBuffIndex(ModContent.BuffType<DraconicSurgeBuff>()) > -1)
					player.ClearBuff(ModContent.BuffType<DraconicSurgeBuff>());
			}

			if (modPlayer.community)
			{
				float floatTypeBoost = 0.05f +
					(NPC.downedSlimeKing ? 0.01f : 0f) +
					(NPC.downedBoss1 ? 0.01f : 0f) +
					(NPC.downedBoss2 ? 0.01f : 0f) +
					(NPC.downedQueenBee ? 0.01f : 0f) +
					(NPC.downedBoss3 ? 0.01f : 0f) + // 0.1
					(Main.hardMode ? 0.01f : 0f) +
					(NPC.downedMechBossAny ? 0.01f : 0f) +
					(NPC.downedPlantBoss ? 0.01f : 0f) +
					(NPC.downedGolemBoss ? 0.01f : 0f) +
					(NPC.downedFishron ? 0.01f : 0f) + // 0.15
					(NPC.downedAncientCultist ? 0.01f : 0f) +
					(NPC.downedMoonlord ? 0.01f : 0f) +
					(CalamityWorld.downedProvidence ? 0.01f : 0f) +
					(CalamityWorld.downedDoG ? 0.01f : 0f) +
					(CalamityWorld.downedYharon ? 0.01f : 0f); // 0.2
				int integerTypeBoost = (int)(floatTypeBoost * 50f);
				int critBoost = integerTypeBoost / 2;
				float damageBoost = floatTypeBoost * 0.5f;
				player.endurance += floatTypeBoost * 0.25f;
				player.statDefense += integerTypeBoost;
				player.allDamage += damageBoost;
				modPlayer.AllCritBoost(critBoost);
				player.minionKB += floatTypeBoost;
				player.moveSpeed += floatTypeBoost;
				flightTimeMult += floatTypeBoost;
			}
			// Shattered Community gives the same wing time boost as normal Community
			if (modPlayer.shatteredCommunity)
				flightTimeMult += 0.15f;

			if (modPlayer.profanedCrystalBuffs && modPlayer.gOffense && modPlayer.gDefense)
			{
				bool offenseBuffs = (Main.dayTime && !player.wet) || player.lavaWet;
				if (offenseBuffs)
						flightTimeMult += 0.1;
				}

			// Increase wing time
			if (player.wingTimeMax > 0)
				player.wingTimeMax = (int)(player.wingTimeMax * flightTimeMult);

			if (modPlayer.vHex)
			{
				player.blind = true;
				player.statDefense -= 10;
				player.moveSpeed -= 0.1f;

				if (player.wingTimeMax < 0)
					player.wingTimeMax = 0;

				player.wingTimeMax = (int)(player.wingTimeMax * 0.75);
			}

			if (modPlayer.eGravity)
			{
				if (player.wingTimeMax < 0)
					player.wingTimeMax = 0;

				if (player.wingTimeMax > 400)
					player.wingTimeMax = 400;

				player.wingTimeMax = (int)(player.wingTimeMax * 0.66);
			}

			if (modPlayer.eGrav)
			{
				if (player.wingTimeMax < 0)
					player.wingTimeMax = 0;

				if (player.wingTimeMax > 400)
					player.wingTimeMax = 400;

				player.wingTimeMax = (int)(player.wingTimeMax * 0.75);
			}

			if (modPlayer.bounding)
			{
				player.jumpSpeedBoost += 0.25f;
				Player.jumpHeight += 10;
				player.extraFall += 25;
			}

			if (modPlayer.mushy)
				player.statDefense += 5;

			if (modPlayer.omniscience)
			{
				player.detectCreature = true;
				player.dangerSense = true;
				player.findTreasure = true;
			}

			if (modPlayer.aWeapon)
				player.moveSpeed += 0.1f;

			if (modPlayer.molten)
				player.resistCold = true;

			if (modPlayer.shellBoost)
				player.moveSpeed += 0.4f;

			if (modPlayer.tarraSet)
			{
				if (!modPlayer.tarraMelee)
					player.calmed = true;
				player.lifeMagnet = true;
			}

			if (modPlayer.cadence)
			{
				if (player.FindBuffIndex(BuffID.Regeneration) > -1)
					player.ClearBuff(BuffID.Regeneration);
				if (player.FindBuffIndex(BuffID.Lifeforce) > -1)
					player.ClearBuff(BuffID.Lifeforce);
				player.lifeMagnet = true;
				player.calmed = true;
			}

			if (player.wellFed)
				player.moveSpeed -= 0.1f;

			if (player.poisoned)
				player.moveSpeed -= 0.1f;

			if (player.venom)
				player.moveSpeed -= 0.15f;

			if (modPlayer.wDeath)
			{
				player.statDefense -= WhisperingDeath.DefenseReduction;
				player.allDamage -= 0.1f;
				player.moveSpeed -= 0.1f;
			}

			if (modPlayer.lethalLavaBurn)
				player.moveSpeed -= 0.15f;

			if (modPlayer.hInferno)
				player.moveSpeed -= 0.25f;

			if (modPlayer.aFlames)
				player.statDefense -= AbyssalFlames.DefenseReduction;

			if (modPlayer.gsInferno)
			{
				player.blackout = true;
				player.statDefense -= GodSlayerInferno.DefenseReduction;
				player.moveSpeed -= 0.15f;
			}

			if (modPlayer.astralInfection)
			{
				player.statDefense -= AstralInfectionDebuff.DefenseReduction;
				player.moveSpeed -= 0.15f;
			}

			if (modPlayer.pFlames)
			{
				player.blind = !modPlayer.alchFlask;
				player.statDefense -= Plague.DefenseReduction;
				player.moveSpeed -= 0.15f;
			}

			if (modPlayer.bBlood)
			{
				player.blind = true;
				player.statDefense -= 3;
				player.moveSpeed += 0.1f;
				player.meleeDamage += 0.05f;
				player.rangedDamage -= 0.1f;
				player.magicDamage -= 0.1f;
			}

			if (modPlayer.aCrunch && !modPlayer.laudanum)
			{
				player.statDefense -= ArmorCrunch.DefenseReduction;
				player.endurance *= 0.33f;
			}

			if (modPlayer.wCleave && !modPlayer.laudanum)
			{
				player.statDefense -= WarCleave.DefenseReduction;
				player.endurance *= 0.75f;
			}

			if (modPlayer.wither)
			{
				player.statDefense -= WitherDebuff.DefenseReduction;
			}

			if (modPlayer.gState)
			{
				player.statDefense -= GlacialState.DefenseReduction;
				player.velocity.X *= 0.5f;
				player.velocity.Y += 0.05f;
				if (player.velocity.Y > 15f)
					player.velocity.Y = 15f;
			}

			if (modPlayer.eFreeze)
			{
				player.statDefense -= GlacialState.DefenseReduction;
				player.velocity.X *= 0.5f;
				player.velocity.Y += 0.1f;
				if (player.velocity.Y > 15f)
					player.velocity.Y = 15f;
			}

			if (modPlayer.eFreeze || modPlayer.silvaStun || modPlayer.eutrophication)
				player.velocity = Vector2.Zero;

			if (modPlayer.vaporfied || modPlayer.teslaFreeze)
				player.velocity *= 0.98f;

			if (modPlayer.molluskSet)
				player.velocity.X *= 0.985f;

			if ((modPlayer.warped || modPlayer.caribbeanRum) && !player.slowFall && !player.mount.Active)
			{
				player.velocity.Y *= 1.01f;
				player.moveSpeed -= 0.1f;
			}

			if (modPlayer.corrEffigy)
			{
				player.moveSpeed += 0.1f;
				modPlayer.AllCritBoost(10);
			}

			if (modPlayer.crimEffigy)
			{
				player.allDamage += 0.15f;
				player.statDefense += 10;
			}

			if (modPlayer.badgeOfBraveryRare)
				player.meleeDamage += modPlayer.warBannerBonus;

			// The player's true max life value with Calamity adjustments
			modPlayer.actualMaxLife = player.statLifeMax2;

			if (modPlayer.thirdSageH && !player.dead && modPlayer.healToFull)
			{
				modPlayer.thirdSageH = false;
				player.statLife = modPlayer.actualMaxLife;
			}

			if (modPlayer.manaOverloader)
				player.magicDamage += 0.06f;

			if (modPlayer.rBrain)
			{
				if (player.statLife <= (int)(player.statLifeMax2 * 0.75))
					player.allDamage += 0.1f;
				if (player.statLife <= (int)(player.statLifeMax2 * 0.5))
					player.moveSpeed -= 0.05f;
			}

			if (modPlayer.bloodyWormTooth)
			{
				if (player.statLife < (int)(player.statLifeMax2 * 0.5))
				{
					player.meleeDamage += 0.1f;
					player.endurance += 0.1f;
				}
				else
				{
					player.meleeDamage += 0.05f;
					player.endurance += 0.05f;
				}
			}

			if (modPlayer.dAmulet)
				player.pStone = true;

			if (modPlayer.fBulwark)
			{
				player.noKnockback = true;
				if (player.statLife > (int)(player.statLifeMax2 * 0.25))
				{
					player.hasPaladinShield = true;
					if (player.whoAmI != Main.myPlayer && player.miscCounter % 10 == 0)
					{
						if (Main.LocalPlayer.team == player.team && player.team != 0)
						{
							Vector2 otherPlayerPos = player.position - Main.LocalPlayer.position;

							if (otherPlayerPos.Length() < 800f)
								Main.LocalPlayer.AddBuff(BuffID.PaladinsShield, 20, true);
						}
					}
				}

				if (player.statLife <= (int)(player.statLifeMax2 * 0.5))
					player.AddBuff(BuffID.IceBarrier, 5, true);
				if (player.statLife <= (int)(player.statLifeMax2 * 0.15))
					player.endurance += 0.05f;
			}

			if (modPlayer.frostFlare)
			{
				player.resistCold = true;
				player.buffImmune[BuffID.Frostburn] = true;
				player.buffImmune[BuffID.Chilled] = true;
				player.buffImmune[BuffID.Frozen] = true;

				if (player.statLife > (int)(player.statLifeMax2 * 0.75))
					player.allDamage += 0.1f;
				if (player.statLife < (int)(player.statLifeMax2 * 0.25))
					player.statDefense += 10;
			}

			if (modPlayer.vexation)
			{
				if (player.statLife < (int)(player.statLifeMax2 * 0.5))
					player.allDamage += 0.2f;
			}

			if (modPlayer.ataxiaBlaze)
			{
				if (player.statLife <= (int)(player.statLifeMax2 * 0.5))
					player.AddBuff(BuffID.Inferno, 2);
			}

			if (modPlayer.bloodflareThrowing)
			{
				if (player.statLife > (int)(player.statLifeMax2 * 0.8))
				{
					modPlayer.throwingCrit += 5;
					player.statDefense += 30;
				}
				else
					modPlayer.throwingDamage += 0.1f;
			}

			if (modPlayer.bloodflareSummon)
			{
				if (player.statLife >= (int)(player.statLifeMax2 * 0.9))
					player.minionDamage += 0.1f;
				else if (player.statLife <= (int)(player.statLifeMax2 * 0.5))
					player.statDefense += 20;

				if (modPlayer.bloodflareSummonTimer > 0)
					modPlayer.bloodflareSummonTimer--;

				if (player.whoAmI == Main.myPlayer && modPlayer.bloodflareSummonTimer <= 0)
				{
					modPlayer.bloodflareSummonTimer = 900;
					for (int I = 0; I < 3; I++)
					{
						float ai1 = I * 120;
						int projectile = Projectile.NewProjectile(player.Center.X + (float)(Math.Sin(I * 120) * 550), player.Center.Y + (float)(Math.Cos(I * 120) * 550), 0f, 0f,
							ModContent.ProjectileType<GhostlyMine>(), (int)(3750 * player.MinionDamage()), 1f, player.whoAmI, ai1, 0f);
						if (projectile.WithinBounds(Main.maxProjectiles))
							Main.projectile[projectile].Calamity().forceTypeless = true;
					}
				}
			}

			if (modPlayer.yInsignia)
			{
				player.meleeDamage += 0.1f;
				player.lavaMax += 240;
				if (player.statLife <= (int)(player.statLifeMax2 * 0.5))
					player.allDamage += 0.1f;
			}

			if (modPlayer.deepDiver && player.IsUnderwater())
			{
				player.allDamage += 0.15f;
				player.statDefense += 15;
				player.moveSpeed += 0.15f;
			}

			if (modPlayer.abyssalDivingSuit && !player.IsUnderwater())
			{
				float moveSpeedLoss = (3 - modPlayer.abyssalDivingSuitPlateHits) * 0.2f;
				player.moveSpeed -= moveSpeedLoss;
			}

			if (modPlayer.ursaSergeant)
				player.moveSpeed -= 0.35f;

			if (modPlayer.elysianGuard)
				player.moveSpeed -= 0.5f;

			if (modPlayer.coreOfTheBloodGod)
			{
				player.endurance += 0.08f;
				player.allDamage += 0.08f;
			}

			if (modPlayer.godSlayerThrowing)
			{
				if (player.statLife >= player.statLifeMax2)
				{
					modPlayer.throwingCrit += 10;
					modPlayer.throwingDamage += 0.1f;
					modPlayer.throwingVelocity += 0.1f;
				}
			}

			#region Damage Auras
			// Tarragon Summon set bonus life aura
			if (modPlayer.tarraSummon)
			{
				const int FramesPerHit = 80;

				// Constantly increment the timer every frame.
				modPlayer.tarraLifeAuraTimer = (modPlayer.tarraLifeAuraTimer + 1) % FramesPerHit;

				// If the timer rolls over, it's time to deal damage. Only run this code for the client which is wearing the armor.
				if (modPlayer.tarraLifeAuraTimer == 0 && player.whoAmI == Main.myPlayer)
				{
					const int BaseDamage = 120;
					int damage = (int)(BaseDamage * player.MinionDamage());
					float range = 300f;

					for (int i = 0; i < Main.maxNPCs; ++i)
					{
						NPC npc = Main.npc[i];
						if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
							continue;

						if (Vector2.Distance(player.Center, npc.Center) <= range)
							Projectile.NewProjectileDirect(npc.Center, Vector2.Zero, ModContent.ProjectileType<DirectStrike>(), damage, 0f, player.whoAmI, i);
					}
				}
			}

			// Navy Fishing Rod's electric aura when in-use
			if (player.ActiveItem().type == ModContent.ItemType<NavyFishingRod>() && player.ownedProjectileCounts[ModContent.ProjectileType<NavyBobber>()] != 0)
			{
				const int FramesPerHit = 120;

				// Constantly increment the timer every frame.
				modPlayer.navyRodAuraTimer = (modPlayer.navyRodAuraTimer + 1) % FramesPerHit;

				// If the timer rolls over, it's time to deal damage. Only run this code for the client which is holding the fishing rod,
				if (modPlayer.navyRodAuraTimer == 0 && player.whoAmI == Main.myPlayer)
				{
					const int BaseDamage = 10;
					int damage = (int)(BaseDamage * player.AverageDamage());
					float range = 200f;

					for (int i = 0; i < Main.maxNPCs; ++i)
					{
						NPC npc = Main.npc[i];
						if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
							continue;

						if (Vector2.Distance(player.Center, npc.Center) <= range)
							Projectile.NewProjectileDirect(npc.Center, Vector2.Zero, ModContent.ProjectileType<DirectStrike>(), damage, 0f, player.whoAmI, i);

						// Occasionally spawn cute sparks so it looks like an electrical aura
						if (Main.rand.NextBool(10))
						{
							Vector2 velocity = CalamityUtils.RandomVelocity(50f, 30f, 60f);
							int spark = Projectile.NewProjectile(npc.Center, velocity, ModContent.ProjectileType<EutrophicSpark>(), damage / 2, 0f, player.whoAmI);
							if (spark.WithinBounds(Main.maxProjectiles))
							{
								Main.projectile[spark].Calamity().forceTypeless = true;
								Main.projectile[spark].localNPCHitCooldown = -2;
								Main.projectile[spark].penetrate = 5;
							}
						}
					}
				}
			}

			// Inferno potion boost
			if (modPlayer.ataxiaBlaze && player.inferno)
			{
				const int FramesPerHit = 30;

				// Constantly increment the timer every frame.
				modPlayer.brimLoreInfernoTimer = (modPlayer.brimLoreInfernoTimer + 1) % FramesPerHit;

				// Only run this code for the client which is wearing the armor.
				// Brimstone flames is applied every single frame, but direct damage is only dealt twice per second.
				if (player.whoAmI == Main.myPlayer)
				{
					const int BaseDamage = 50;
					int damage = (int)(BaseDamage * player.AverageDamage());
					float range = 300f;

					for (int i = 0; i < Main.maxNPCs; ++i)
					{
						NPC npc = Main.npc[i];
						if (!npc.active || npc.friendly || npc.damage <= 0 || npc.dontTakeDamage)
							continue;

						if (Vector2.Distance(player.Center, npc.Center) <= range)
						{
							npc.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 120);
							if (modPlayer.brimLoreInfernoTimer == 0)
								Projectile.NewProjectileDirect(npc.Center, Vector2.Zero, ModContent.ProjectileType<DirectStrike>(), damage, 0f, player.whoAmI, i);
						}
					}
				}
			}
			#endregion

			if (modPlayer.royalGel)
			{
				player.npcTypeNoAggro[ModContent.NPCType<AeroSlime>()] = true;
				player.npcTypeNoAggro[ModContent.NPCType<BloomSlime>()] = true;
				player.npcTypeNoAggro[ModContent.NPCType<CharredSlime>()] = true;
				player.npcTypeNoAggro[ModContent.NPCType<CrimulanBlightSlime>()] = true;
				player.npcTypeNoAggro[ModContent.NPCType<CryoSlime>()] = true;
				player.npcTypeNoAggro[ModContent.NPCType<EbonianBlightSlime>()] = true;
				player.npcTypeNoAggro[ModContent.NPCType<IrradiatedSlime>()] = true;
				player.npcTypeNoAggro[ModContent.NPCType<PerennialSlime>()] = true;
				player.npcTypeNoAggro[ModContent.NPCType<PlaguedJungleSlime>()] = true;
				player.npcTypeNoAggro[ModContent.NPCType<AstralSlime>()] = true;
				player.npcTypeNoAggro[ModContent.NPCType<GammaSlime>()] = true;
				// NOTE: These don't even spawn anymore.
				player.npcTypeNoAggro[ModContent.NPCType<WulfrumSlime>()] = true;
			}

			if (modPlayer.dukeScales)
			{
				player.buffImmune[ModContent.BuffType<SulphuricPoisoning>()] = true;
				player.buffImmune[BuffID.Poisoned] = true;
				player.buffImmune[BuffID.Venom] = true;
				if (player.statLife <= (int)(player.statLifeMax2 * 0.75))
				{
					player.allDamage += 0.06f;
					modPlayer.AllCritBoost(3);
				}
				if (player.statLife <= (int)(player.statLifeMax2 * 0.5))
				{
					player.allDamage += 0.06f;
					modPlayer.AllCritBoost(3);
				}
				if (player.statLife <= (int)(player.statLifeMax2 * 0.25))
				{
					player.allDamage += 0.06f;
					modPlayer.AllCritBoost(3);
				}
				if (player.lifeRegen < 0)
				{
					player.allDamage += 0.1f;
					modPlayer.AllCritBoost(5);
				}
			}

			if (modPlayer.dArtifact)
				player.allDamage += 0.25f;

			if (modPlayer.trippy)
				player.allDamage += 0.5f;

			if (modPlayer.eArtifact)
			{
				player.manaCost *= 0.85f;
				modPlayer.throwingDamage += 0.15f;
				player.maxMinions += 2;
			}

			if (modPlayer.gArtifact)
			{
				player.maxMinions += 8;
				if (player.whoAmI == Main.myPlayer)
				{
					if (player.FindBuffIndex(ModContent.BuffType<YharonKindleBuff>()) == -1)
						player.AddBuff(ModContent.BuffType<YharonKindleBuff>(), 3600, true);

					if (player.ownedProjectileCounts[ModContent.ProjectileType<SonOfYharon>()] < 2)
						Projectile.NewProjectile(player.Center, Vector2.Zero, ModContent.ProjectileType<SonOfYharon>(), (int)(AngryChickenStaff.Damage * player.MinionDamage()), 2f, Main.myPlayer, 0f, 0f);
				}
			}

			if (modPlayer.pArtifact)
			{
				if (player.whoAmI == Main.myPlayer)
				{
					if (player.FindBuffIndex(ModContent.BuffType<ProfanedBabs>()) == -1 && !player.Calamity().profanedCrystalBuffs)
						player.AddBuff(ModContent.BuffType<ProfanedBabs>(), 3600, true);

					bool crystal = modPlayer.profanedCrystal && !modPlayer.profanedCrystalForce;
					bool summonSet = modPlayer.tarraSummon || modPlayer.bloodflareSummon || modPlayer.silvaSummon || modPlayer.dsSetBonus || modPlayer.omegaBlueSet || modPlayer.fearmongerSet;
					int guardianAmt = 1;

					if (player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianHealer>()] < guardianAmt)
						Projectile.NewProjectile(player.Center.X, player.Center.Y, 0f, -6f, ModContent.ProjectileType<MiniGuardianHealer>(), 0, 0f, Main.myPlayer, 0f, 0f);

					if (crystal || player.Calamity().minionSlotStat >= 10)
					{
						player.Calamity().gDefense = true;

						if (player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianDefense>()] < guardianAmt)
							Projectile.NewProjectile(player.Center.X, player.Center.Y, 0f, -3f, ModContent.ProjectileType<MiniGuardianDefense>(), 1, 1f, Main.myPlayer, 0f, 0f);
					}

					if (crystal || summonSet)
					{
						player.Calamity().gOffense = true;

						if (player.ownedProjectileCounts[ModContent.ProjectileType<MiniGuardianAttack>()] < guardianAmt)
							Projectile.NewProjectile(player.Center.X, player.Center.Y, 0f, -1f, ModContent.ProjectileType<MiniGuardianAttack>(), 1, 1f, Main.myPlayer, 0f, 0f);
					}
				}
			}

			if (modPlayer.profanedCrystalBuffs && modPlayer.gOffense && modPlayer.gDefense)
			{
				if (player.whoAmI == Main.myPlayer)
				{
					player.scope = false; //this is so it doesn't mess with the balance of ranged transform attacks over the others
					player.lavaImmune = true;
					player.lavaMax += 420;
					player.lavaRose = true;
					player.fireWalk = true;
					player.buffImmune[ModContent.BuffType<HolyFlames>()] = Main.dayTime;
					player.buffImmune[ModContent.BuffType<Nightwither>()] = !Main.dayTime;
					player.buffImmune[BuffID.OnFire] = true;
					player.buffImmune[BuffID.Burning] = true;
					player.buffImmune[BuffID.Daybreak] = true;
					bool offenseBuffs = (Main.dayTime && !player.wet) || player.lavaWet;
					if (offenseBuffs)
					{
						player.minionDamage += 0.15f;
						player.minionKB += 0.15f;
						player.moveSpeed += 0.1f;
						player.statDefense -= 15;
						player.ignoreWater = true;
					}
					else
					{
						player.moveSpeed -= 0.1f;
						player.endurance += 0.05f;
						player.statDefense += 15;
						player.lifeRegen += 5;
					}
					bool enrage = player.statLife <= (int)(player.statLifeMax2 * 0.5);
					bool notRetro = Lighting.NotRetro;
					if (!modPlayer.ZoneAbyss) //No abyss memes.
						Lighting.AddLight(player.Center, enrage ? 1.2f : offenseBuffs ? 1f : 0.2f, enrage ? 0.21f : offenseBuffs ? 0.2f : 0.01f, 0);
					if (enrage)
					{
						bool special = player.name == "Amber" || player.name == "Nincity" || player.name == "IbanPlay" || player.name == "Chen"; //People who either helped create the item or test it.
						for (int i = 0; i < 3; i++)
						{
							int fire = Dust.NewDust(player.position, player.width, player.height, special ? 231 : (int)CalamityDusts.ProfanedFire, 0f, 0f, 100, special ? Color.DarkRed : default, 1f);
							Main.dust[fire].scale = special ? 1.169f : 2f;
							Main.dust[fire].noGravity = true;
							Main.dust[fire].velocity *= special ? 10f : 6.9f;
						}
					}
				}
			}

			if (modPlayer.plaguebringerPistons)
			{
				//Spawn bees while sprinting or dashing
				modPlayer.pistonsCounter++;
				if (modPlayer.pistonsCounter % 12 == 0)
				{
					if (player.velocity.Length() >= 5f && player.whoAmI == Main.myPlayer)
					{
						int beeCount = 1;
						if (Main.rand.NextBool(3))
							++beeCount;
						if (Main.rand.NextBool(3))
							++beeCount;
						if (player.strongBees && Main.rand.NextBool(3))
							++beeCount;
						int damage = (int)(30 * player.MinionDamage());
						for (int index = 0; index < beeCount; ++index)
						{
							int bee = Projectile.NewProjectile(player.Center.X, player.Center.Y, Main.rand.NextFloat(-35f, 35f) * 0.02f, Main.rand.NextFloat(-35f, 35f) * 0.02f, (Main.rand.NextBool(4) ? ModContent.ProjectileType<PlagueBeeSmall>() : player.beeType()), damage, player.beeKB(0f), player.whoAmI, 0f, 0f);
							Main.projectile[bee].usesLocalNPCImmunity = true;
							Main.projectile[bee].localNPCHitCooldown = 10;
							Main.projectile[bee].penetrate = 2;
							if (bee.WithinBounds(Main.maxProjectiles))
								Main.projectile[bee].Calamity().forceTypeless = true;
						}
					}
				}
			}

			List<int> summonDeleteList = new List<int>()
			{
				ModContent.ProjectileType<BrimstoneElementalMinion>(),
				ModContent.ProjectileType<WaterElementalMinion>(),
				ModContent.ProjectileType<SandElementalHealer>(),
				ModContent.ProjectileType<SandElementalMinion>(),
				ModContent.ProjectileType<CloudElementalMinion>(),
				ModContent.ProjectileType<FungalClumpMinion>(),
				ModContent.ProjectileType<HowlsHeartHowl>(),
				ModContent.ProjectileType<HowlsHeartCalcifer>(),
				ModContent.ProjectileType<HowlsHeartTurnipHead>(),
				ModContent.ProjectileType<MiniGuardianAttack>(),
				ModContent.ProjectileType<MiniGuardianDefense>(),
				ModContent.ProjectileType<MiniGuardianHealer>()
			};
			int projAmt = 1;
			for (int i = 0; i < summonDeleteList.Count; i++)
			{
				if (player.ownedProjectileCounts[summonDeleteList[i]] > projAmt)
				{
					for (int projIndex = 0; projIndex < Main.maxProjectiles; projIndex++)
					{
						Projectile proj = Main.projectile[projIndex];
						if (proj.active && proj.owner == player.whoAmI)
						{
							if (summonDeleteList.Contains(proj.type))
							{
								proj.Kill();
							}
						}
					}
				}
			}

			if (modPlayer.blunderBooster)
			{
				if (player.whoAmI == Main.myPlayer)
				{
					if (player.ownedProjectileCounts[ModContent.ProjectileType<BlunderBoosterAura>()] < 1)
						Projectile.NewProjectile(player.Center, Vector2.Zero, ModContent.ProjectileType<BlunderBoosterAura>(), (int)(30 * player.RogueDamage()), 0f, player.whoAmI, 0f, 0f);
				}
			}
			else if (player.ownedProjectileCounts[ModContent.ProjectileType<BlunderBoosterAura>()] != 0)
			{
				if (player.whoAmI == Main.myPlayer)
				{
					for (int i = 0; i < Main.maxProjectiles; i++)
					{
						if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<BlunderBoosterAura>() && Main.projectile[i].owner == player.whoAmI)
						{
							Main.projectile[i].Kill();
							break;
						}
					}
				}
			}

			if (modPlayer.tesla)
			{
				if (player.whoAmI == Main.myPlayer)
				{
					//Reduce the buffTime of Electrified
					for (int l = 0; l < Player.MaxBuffs; l++)
					{
						bool electrified = player.buffType[l] == BuffID.Electrified;
						if (player.buffTime[l] > 2 && electrified)
						{
							player.buffTime[l]--;
						}
					}
					//Summon the aura
					if (player.ownedProjectileCounts[ModContent.ProjectileType<TeslaAura>()] < 1)
						Projectile.NewProjectile(player.Center, Vector2.Zero, ModContent.ProjectileType<TeslaAura>(), (int)(10 * player.AverageDamage()), 0f, player.whoAmI, 0f, 0f);
				}
			}
			else if (player.ownedProjectileCounts[ModContent.ProjectileType<TeslaAura>()] != 0)
			{
				if (player.whoAmI == Main.myPlayer)
				{
					int auraType = ModContent.ProjectileType<TeslaAura>();
					for (int i = 0; i < Main.maxProjectiles; i++)
					{
						if (Main.projectile[i].type != auraType || !Main.projectile[i].active || Main.projectile[i].owner != player.whoAmI)
							continue;

						Main.projectile[i].Kill();
						break;
					}
				}
			}

			if (modPlayer.CryoStone)
			{
				if (player.whoAmI == Main.myPlayer && player.ownedProjectileCounts[ModContent.ProjectileType<CryonicShield>()] == 0)
					Projectile.NewProjectile(player.Center, Vector2.Zero, ModContent.ProjectileType<CryonicShield>(), (int)(player.AverageDamage() * 70), 0f, player.whoAmI);
            }
            else if (player.whoAmI == Main.myPlayer)
			{
				int shieldType = ModContent.ProjectileType<CryonicShield>();
				for (int i = 0; i < Main.maxProjectiles; i++)
				{
					if (Main.projectile[i].type != shieldType || !Main.projectile[i].active || Main.projectile[i].owner != player.whoAmI)
						continue;

					Main.projectile[i].Kill();
					break;
				}
			}

			if (modPlayer.prismaticLasers > 1800 && player.whoAmI == Main.myPlayer)
			{
				float shootSpeed = 18f;
				int dmg = (int)(30 * player.MagicDamage());
				Vector2 startPos = player.RotatedRelativePoint(player.MountedCenter, true);
				Vector2 velocity = Main.MouseWorld - startPos;
				if (player.gravDir == -1f)
				{
					velocity.Y = Main.screenPosition.Y + Main.screenHeight - Main.mouseY - startPos.Y;
				}
				float travelDist = velocity.Length();
				if ((float.IsNaN(velocity.X) && float.IsNaN(velocity.Y)) || (velocity.X == 0f && velocity.Y == 0f))
				{
					velocity.X = player.direction;
					velocity.Y = 0f;
					travelDist = shootSpeed;
				}
				else
				{
					travelDist = shootSpeed / travelDist;
				}

				int laserAmt = Main.rand.Next(2);
				for (int index = 0; index < laserAmt; index++)
				{
					startPos = new Vector2(player.Center.X + (Main.rand.Next(201) * -(float)player.direction) + (Main.mouseX + Main.screenPosition.X - player.position.X), player.MountedCenter.Y - 600f);
					startPos.X = (startPos.X + player.Center.X) / 2f + Main.rand.Next(-200, 201);
					startPos.Y -= 100 * index;
					velocity.X = Main.mouseX + Main.screenPosition.X - startPos.X;
					velocity.Y = Main.mouseY + Main.screenPosition.Y - startPos.Y;
					if (velocity.Y < 0f)
					{
						velocity.Y *= -1f;
					}
					if (velocity.Y < 20f)
					{
						velocity.Y = 20f;
					}
					travelDist = velocity.Length();
					travelDist = shootSpeed / travelDist;
					velocity.X *= travelDist;
					velocity.Y *= travelDist;
					velocity.X += Main.rand.Next(-50, 51) * 0.02f;
					velocity.Y += Main.rand.Next(-50, 51) * 0.02f;
					int laser = Projectile.NewProjectile(startPos, velocity, ModContent.ProjectileType<MagicNebulaShot>(), dmg, 4f, player.whoAmI, 0f, 0f);
					Main.projectile[laser].localNPCHitCooldown = 5;
					if (laser.WithinBounds(Main.maxProjectiles))
						Main.projectile[laser].Calamity().forceTypeless = true;
				}
				Main.PlaySound(SoundID.Item12, player.Center);
			}
			if (modPlayer.prismaticLasers == 1800)
			{
				//Set the cooldown
				player.AddBuff(ModContent.BuffType<PrismaticCooldown>(), CalamityUtils.SecondsToFrames(30f), true);
			}
			if (modPlayer.prismaticLasers == 1)
			{
				//Spawn some dust since you can use it again
				int dustAmt = 36;
				for (int dustIndex = 0; dustIndex < dustAmt; dustIndex++)
				{
					Color color = Utils.SelectRandom(Main.rand, new Color[]
					{
						new Color(255, 0, 0, 50), //Red
						new Color(255, 128, 0, 50), //Orange
						new Color(255, 255, 0, 50), //Yellow
						new Color(128, 255, 0, 50), //Lime
						new Color(0, 255, 0, 50), //Green
						new Color(0, 255, 128, 50), //Turquoise
						new Color(0, 255, 255, 50), //Cyan
						new Color(0, 128, 255, 50), //Light Blue
						new Color(0, 0, 255, 50), //Blue
						new Color(128, 0, 255, 50), //Purple
						new Color(255, 0, 255, 50), //Fuschia
						new Color(255, 0, 128, 50) //Hot Pink
					});
					Vector2 source = Vector2.Normalize(player.velocity) * new Vector2(player.width / 2f, player.height) * 0.75f;
					source = source.RotatedBy((dustIndex - (dustAmt / 2 - 1)) * MathHelper.TwoPi / dustAmt, default) + player.Center;
					Vector2 dustVel = source - player.Center;
					int dusty = Dust.NewDust(source + dustVel, 0, 0, 267, dustVel.X * 1f, dustVel.Y * 1f, 100, color, 1f);
					Main.dust[dusty].noGravity = true;
					Main.dust[dusty].noLight = true;
					Main.dust[dusty].velocity = dustVel;
				}
			}

			if (modPlayer.angelicAlliance && Main.myPlayer == player.whoAmI)
			{
				for (int l = 0; l < Player.MaxBuffs; l++)
				{
					int hasBuff = player.buffType[l];
					if (hasBuff == ModContent.BuffType<DivineBless>())
					{
						modPlayer.angelicActivate = player.buffTime[l];
					}
					if (hasBuff == ModContent.BuffType<DivineBlessCooldown>())
					{
						if (player.buffTime[l] == 1)
							Projectile.NewProjectile(player.Center, Vector2.Zero, ModContent.ProjectileType<AllianceTriangle>(), 0, 0f, player.whoAmI);
					}
				}
				if (modPlayer.angelicActivate == 1)
				{
					int seconds = CalamityUtils.SecondsToFrames(60f);
					player.AddBuff(ModContent.BuffType<DivineBlessCooldown>(), seconds, false);
				}
				if (player.FindBuffIndex(ModContent.BuffType<DivineBless>()) == -1)
					modPlayer.angelicActivate = -1;
			}

			if (modPlayer.theBee)
			{
				if (player.statLife >= player.statLifeMax2)
				{
					float beeBoost = player.endurance / 2f;
					player.allDamage += beeBoost;
				}
			}

			if (modPlayer.badgeOfBravery)
			{
				player.meleeDamage += 0.05f;
				player.meleeCrit += 5;
			}

			if (CalamityConfig.Instance.Proficiency)
				modPlayer.GetStatBonuses();

			// True melee damage bonuses
			double damageAdd = (modPlayer.dodgeScarf ? 0.1 : 0) +
					(modPlayer.evasionScarf ? 0.05 : 0) +
					((modPlayer.aBulwarkRare && modPlayer.aBulwarkRareMeleeBoostTimer > 0) ? 0.5 : 0) +
					(modPlayer.fungalSymbiote ? 0.15 : 0) +
					((player.head == ArmorIDs.Head.MoltenHelmet && player.body == ArmorIDs.Body.MoltenBreastplate && player.legs == ArmorIDs.Legs.MoltenGreaves) ? 0.2 : 0) +
					(player.kbGlove ? 0.1 : 0) +
					(modPlayer.eGauntlet ? 0.1 : 0) +
					(modPlayer.yInsignia ? 0.1 : 0) +
					(modPlayer.badgeOfBraveryRare ? modPlayer.warBannerBonus : 0);
			modPlayer.trueMeleeDamage += damageAdd;

			// Amalgam boosts
			if (Main.myPlayer == player.whoAmI)
			{
				for (int l = 0; l < Player.MaxBuffs; l++)
				{
					int hasBuff = player.buffType[l];
					if ((hasBuff >= BuffID.ObsidianSkin && hasBuff <= BuffID.Gravitation) || hasBuff == BuffID.Tipsy || hasBuff == BuffID.WellFed ||
						hasBuff == BuffID.Honey || hasBuff == BuffID.WeaponImbueVenom || (hasBuff >= BuffID.WeaponImbueCursedFlames && hasBuff <= BuffID.WeaponImbuePoison) ||
						(hasBuff >= BuffID.Mining && hasBuff <= BuffID.Wrath) || (hasBuff >= BuffID.Lovestruck && hasBuff <= BuffID.Warmth) || hasBuff == BuffID.SugarRush ||
						hasBuff == ModContent.BuffType<AbyssalWeapon>() || hasBuff == ModContent.BuffType<AnechoicCoatingBuff>() || hasBuff == ModContent.BuffType<ArmorCrumbling>() ||
						hasBuff == ModContent.BuffType<ArmorShattering>() || hasBuff == ModContent.BuffType<AstralInjectionBuff>() || hasBuff == ModContent.BuffType<BaguetteBuff>() ||
						hasBuff == ModContent.BuffType<BloodfinBoost>() || hasBuff == ModContent.BuffType<BoundingBuff>() || hasBuff == ModContent.BuffType<Cadence>() ||
						hasBuff == ModContent.BuffType<CalciumBuff>() || hasBuff == ModContent.BuffType<CeaselessHunger>() || hasBuff == ModContent.BuffType<DraconicSurgeBuff>() ||
						hasBuff == ModContent.BuffType<GravityNormalizerBuff>() || hasBuff == ModContent.BuffType<HolyWrathBuff>() || hasBuff == ModContent.BuffType<Omniscience>() ||
						hasBuff == ModContent.BuffType<PenumbraBuff>() || hasBuff == ModContent.BuffType<PhotosynthesisBuff>() || hasBuff == ModContent.BuffType<ProfanedRageBuff>() ||
						hasBuff == ModContent.BuffType<Revivify>() || hasBuff == ModContent.BuffType<ShadowBuff>() || hasBuff == ModContent.BuffType<Soaring>() ||
						hasBuff == ModContent.BuffType<SulphurskinBuff>() || hasBuff == ModContent.BuffType<TeslaBuff>() || hasBuff == ModContent.BuffType<TitanScale>() ||
						hasBuff == ModContent.BuffType<TriumphBuff>() || hasBuff == ModContent.BuffType<YharimPower>() || hasBuff == ModContent.BuffType<Zen>() ||
						hasBuff == ModContent.BuffType<Zerg>() || hasBuff == ModContent.BuffType<BloodyMaryBuff>() || hasBuff == ModContent.BuffType<CaribbeanRumBuff>() ||
						hasBuff == ModContent.BuffType<CinnamonRollBuff>() || hasBuff == ModContent.BuffType<EverclearBuff>() || hasBuff == ModContent.BuffType<EvergreenGinBuff>() ||
						hasBuff == ModContent.BuffType<FabsolVodkaBuff>() || hasBuff == ModContent.BuffType<FireballBuff>() || hasBuff == ModContent.BuffType<GrapeBeerBuff>() ||
						hasBuff == ModContent.BuffType<MargaritaBuff>() || hasBuff == ModContent.BuffType<MoonshineBuff>() || hasBuff == ModContent.BuffType<MoscowMuleBuff>() ||
						hasBuff == ModContent.BuffType<RedWineBuff>() || hasBuff == ModContent.BuffType<RumBuff>() || hasBuff == ModContent.BuffType<ScrewdriverBuff>() ||
						hasBuff == ModContent.BuffType<StarBeamRyeBuff>() || hasBuff == ModContent.BuffType<TequilaBuff>() || hasBuff == ModContent.BuffType<TequilaSunriseBuff>() ||
						hasBuff == ModContent.BuffType<Trippy>() || hasBuff == ModContent.BuffType<VodkaBuff>() || hasBuff == ModContent.BuffType<WhiskeyBuff>() ||
						hasBuff == ModContent.BuffType<WhiteWineBuff>())
					{
						if (modPlayer.amalgam)
						{
							// Every other frame, increase the buff timer by one frame. Thus, the buff lasts twice as long.
							if (player.miscCounter % 2 == 0)
								player.buffTime[l] += 1;

							// Buffs will not go away when you die, to prevent wasting potions.
							if (!Main.persistentBuff[hasBuff])
								Main.persistentBuff[hasBuff] = true;
						}
						else
						{
							// Reset buff persistence if Amalgam is removed.
							if (Main.persistentBuff[hasBuff])
								Main.persistentBuff[hasBuff] = false;
						}
					}
				}
			}

			// Laudanum boosts
			if (modPlayer.laudanum)
			{
				if (Main.myPlayer == player.whoAmI)
				{
					for (int l = 0; l < Player.MaxBuffs; l++)
					{
						int hasBuff = player.buffType[l];
						if (hasBuff == ModContent.BuffType<ArmorCrunch>() || hasBuff == ModContent.BuffType<WarCleave>() || hasBuff == BuffID.Obstructed ||
							hasBuff == BuffID.Ichor || hasBuff == BuffID.Chilled || hasBuff == BuffID.BrokenArmor || hasBuff == BuffID.Weak ||
							hasBuff == BuffID.Slow || hasBuff == BuffID.Confused || hasBuff == BuffID.Blackout || hasBuff == BuffID.Darkness)
						{
							// Every other frame, increase the buff timer by one frame. Thus, the buff lasts twice as long.
							if (player.miscCounter % 2 == 0)
								player.buffTime[l] += 1;
						}

						// See later as Laud cancels out the normal effects
						if (hasBuff == ModContent.BuffType<ArmorCrunch>())
						{
							// +15 defense
							player.statDefense += ArmorCrunch.DefenseReduction;
						}
						if (hasBuff == ModContent.BuffType<WarCleave>())
						{
							// +10% damage reduction
							player.endurance += 0.1f;
						}

						switch (hasBuff)
						{
							case BuffID.Obstructed:
								player.headcovered = false;
								player.statDefense += 50;
								player.allDamage += 0.5f;
								modPlayer.AllCritBoost(25);
								break;
							case BuffID.Ichor:
								player.statDefense += 40;
								break;
							case BuffID.Chilled:
								player.chilled = false;
								player.moveSpeed *= 1.3f;
								break;
							case BuffID.BrokenArmor:
								player.brokenArmor = false;
								player.statDefense += (int)(player.statDefense * 0.25);
								break;
							case BuffID.Weak:
								player.meleeDamage += 0.151f;
								player.statDefense += 14;
								player.moveSpeed += 0.3f;
								break;
							case BuffID.Slow:
								player.slow = false;
								player.moveSpeed *= 1.5f;
								break;
							case BuffID.Confused:
								player.confused = false;
								player.statDefense += 30;
								player.allDamage += 0.25f;
								modPlayer.AllCritBoost(10);
								break;
							case BuffID.Blackout:
								player.blackout = false;
								player.statDefense += 30;
								player.allDamage += 0.25f;
								modPlayer.AllCritBoost(10);
								break;
							case BuffID.Darkness:
								player.blind = false;
								player.statDefense += 15;
								player.allDamage += 0.1f;
								modPlayer.AllCritBoost(5);
								break;
						}
					}
				}
			}

			// Draedon's Heart bonus
			if (modPlayer.draedonsHeart)
			{
				if (player.StandingStill() && player.itemAnimation == 0)
					player.statDefense += (int)(player.statDefense * 0.75);
			}

			// Endurance reductions
			EnduranceReductions(player, modPlayer);

			// Defense stat damage calcs
			// This was not done to prevent facetanking, but to push players to not just stand completely fucking still taking 1 or 2 damage the entire time
			if (modPlayer.defenseDamage > 0)
			{
				// Set recovery rate before everything else happens.
				// This scales with current max player defense, so that it smoothly scales with smaller and larger defense values.
				// This way, you won't be waiting a year to get defense back in early game, where it matters the most.
				// This also avoids another issue, where late game players with high defense would regen their defense stat damage way too quickly.
				int defenseDamageRecoveryRate = (int)MathHelper.Clamp(player.statDefense / modPlayer.defenseDamage, 1, 30);

				// Set current max player defense stat as the cap
				if (modPlayer.defenseDamage > player.statDefense)
					modPlayer.defenseDamage = player.statDefense;

				// Reduce player DR based on defense stat damage accumulated, this is done before defense is reduced
				if (player.statDefense > 0 && player.endurance > 0f)
					player.endurance -= player.endurance * (modPlayer.defenseDamage / (float)player.statDefense);

				// Reduce player defense based on defense stat damage accumulated
				player.statDefense -= modPlayer.defenseDamage;

				// Checks all immunity frame timers
				bool isImmune = false;
				for (int i = 0; i < player.hurtCooldowns.Length; i++)
				{
					if (player.hurtCooldowns[i] > 0)
						isImmune = true;
				}

				// Reduce defense stat damage over time, but only if the player doesn't have any active immunity frames and if the recovery timer is 0
				if (!isImmune)
				{
					if (player.miscCounter % defenseDamageRecoveryRate == 0 && modPlayer.timeBeforeDefenseDamageRecovery == 0)
						modPlayer.defenseDamage--;

					if (modPlayer.timeBeforeDefenseDamageRecovery > 0)
						modPlayer.timeBeforeDefenseDamageRecovery--;
				}
			}

			// Prevent negative defense values
			if (player.statDefense < 0)
				player.statDefense = 0;

			// Multiplicative defense reductions
			if (modPlayer.fabsolVodka)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.1);
			}

			if (modPlayer.vodka)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.05);
			}

			if (modPlayer.grapeBeer)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.03);
			}

			if (modPlayer.rum)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.05);
			}

			if (modPlayer.whiskey)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.05);
			}

			if (modPlayer.everclear)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.3);
			}

			if (modPlayer.bloodyMary)
			{
				if (Main.bloodMoon)
				{
					if (player.statDefense > 0)
						player.statDefense -= (int)(player.statDefense * 0.04);
				}
			}

			if (modPlayer.caribbeanRum)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.1);
			}

			if (modPlayer.cinnamonRoll)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.1);
			}

			if (modPlayer.margarita)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.06);
			}

			if (modPlayer.starBeamRye)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.06);
			}

			if (modPlayer.whiteWine)
			{
				if (player.statDefense > 0)
					player.statDefense -= (int)(player.statDefense * 0.06);
			}

			// Intentionally at the end: Bloodflare Core's defense reduction (after all other boosting effects and whatnot)
			// This defense still comes back over time if you take off Bloodflare Core while you're missing defense.
			// However, removing the item means you won't get healed as the defense comes back.
			ref int lostDef = ref modPlayer.bloodflareCoreLostDefense;
			if (lostDef > 0)
			{
				// Defense regeneration occurs every four frames while defense is missing
				if (player.miscCounter % 4 == 0)
				{
					--lostDef;
					if (modPlayer.bloodflareCore)
					{
						player.statLife += 1;
						player.HealEffect(1, false);

						// Produce an implosion of blood themed dust so it's obvious an effect is occurring
						for (int i = 0; i < 3; ++i)
						{
							Vector2 offset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(23f, 33f);
							Vector2 dustPos = player.Center + offset;
							Vector2 dustVel = offset * -0.08f;
							Dust d = Dust.NewDustDirect(dustPos, 0, 0, 90, 0.08f, 0.08f);
							d.velocity = dustVel;
							d.noGravity = true;
						}
					} 
				}

				// Actually apply the defense reduction
				player.statDefense -= lostDef;
			}

			if (modPlayer.spectralVeilImmunity > 0)
			{
				int numDust = 2;
				for (int i = 0; i < numDust; i++)
				{
					int dustIndex = Dust.NewDust(player.position, player.width, player.height, 21, 0f, 0f);
					Dust dust = Main.dust[dustIndex];
					dust.position.X += Main.rand.Next(-5, 6);
					dust.position.Y += Main.rand.Next(-5, 6);
					dust.velocity *= 0.2f;
					dust.noGravity = true;
					dust.noLight = true;
				}
			}
		}
		#endregion

		#region Limits
		private static void Limits(Player player, CalamityPlayer modPlayer)
		{
			//not sure where else this should go
			if (modPlayer.forbiddenCirclet)
			{
				float rogueDmg = player.thrownDamage + modPlayer.throwingDamage - 1f;
				float minionDmg = player.minionDamage;
				if (minionDmg < rogueDmg)
				{
					player.minionDamage = rogueDmg;
				}
				if (rogueDmg < minionDmg)
				{
					modPlayer.throwingDamage = minionDmg - player.thrownDamage + 1f;
				}
			}

			// 10% is converted to 9%, 25% is converted to 20%, 50% is converted to 33%, 75% is converted to 43%, 100% is converted to 50%
			if (player.endurance > 0f)
				player.endurance = 1f - (1f / (1f + player.endurance));
			
			// Do not apply reduced aggro if there are any bosses alive and it's singleplayer
			if (CalamityPlayer.areThereAnyDamnBosses && Main.netMode == NetmodeID.SinglePlayer)
			{
				if (player.aggro < 0)
					player.aggro = 0;
			}
		}
		#endregion

		#region Endurance Reductions
		private static void EnduranceReductions(Player player, CalamityPlayer modPlayer)
		{
			if (modPlayer.vHex)
				player.endurance -= 0.1f;

			if (modPlayer.irradiated)
					player.endurance -= 0.1f;

			if (modPlayer.corrEffigy)
				player.endurance -= 0.05f;
		}
		#endregion

		#region Stat Meter
		private static void UpdateStatMeter(Player player, CalamityPlayer modPlayer)
		{
			float allDamageStat = player.allDamage - 1f;
			modPlayer.damageStats[0] = (int)((player.meleeDamage + allDamageStat - 1f) * 100f);
			modPlayer.damageStats[1] = (int)((player.rangedDamage + allDamageStat - 1f) * 100f);
			modPlayer.damageStats[2] = (int)((player.magicDamage + allDamageStat - 1f) * 100f);
			modPlayer.damageStats[3] = (int)((player.minionDamage + allDamageStat - 1f) * 100f);
			modPlayer.damageStats[4] = (int)((modPlayer.throwingDamage + allDamageStat - 1f) * 100f);
			modPlayer.damageStats[5] = (int)(modPlayer.trueMeleeDamage * 100D);
			modPlayer.critStats[0] = player.meleeCrit;
			modPlayer.critStats[1] = player.rangedCrit;
			modPlayer.critStats[2] = player.magicCrit;
			modPlayer.critStats[3] = player.thrownCrit + modPlayer.throwingCrit;
			modPlayer.ammoReductionRanged = (int)(100f *
				(player.ammoBox ? 0.8f : 1f) *
				(player.ammoPotion ? 0.8f : 1f) *
				(player.ammoCost80 ? 0.8f : 1f) *
				(player.ammoCost75 ? 0.75f : 1f) *
				modPlayer.rangedAmmoCost);
			modPlayer.ammoReductionRogue = (int)(modPlayer.throwingAmmoCost * 100);
			modPlayer.defenseStat = player.statDefense + modPlayer.defenseDamage;
			modPlayer.DRStat = (int)(player.endurance * 100f);
			modPlayer.meleeSpeedStat = (int)((1f - player.meleeSpeed) * (100f / player.meleeSpeed));
			modPlayer.manaCostStat = (int)(player.manaCost * 100f);
			modPlayer.rogueVelocityStat = (int)((modPlayer.throwingVelocity - 1f) * 100f);

			// Max stealth 1f is actually "100 stealth", so multiply by 100 to get visual stealth number.
			modPlayer.stealthStat = (int)(modPlayer.rogueStealthMax * 100f);
			// Then divide by 3, because it takes 3 seconds to regen full stealth.
			// Divide by 3 again for moving, because it recharges at 1/3 speed (so divide by 9 overall).
			// Then multiply by stealthGen variables, which start at 1f and increase proportionally to your boosts.
			modPlayer.standingRegenStat = (modPlayer.rogueStealthMax * 100f / 3f) * modPlayer.stealthGenStandstill;
			modPlayer.movingRegenStat = (modPlayer.rogueStealthMax * 100f / 9f) * modPlayer.stealthGenMoving * modPlayer.stealthAcceleration;

			modPlayer.minionSlotStat = player.maxMinions;
			modPlayer.manaRegenStat = player.manaRegen;
			modPlayer.armorPenetrationStat = player.armorPenetration;
			modPlayer.moveSpeedStat = (int)((player.moveSpeed - 1f) * 100f);
			modPlayer.wingFlightTimeStat = player.wingTimeMax / 60f;
			float trueJumpSpeedBoost = player.jumpSpeedBoost + 
				(player.wereWolf ? 0.2f : 0f) +
				(player.jumpBoost ? 1.5f : 0f);
			modPlayer.jumpSpeedStat = trueJumpSpeedBoost * 20f;
			modPlayer.rageDamageStat = (int)(100D * modPlayer.RageDamageBoost);
			modPlayer.adrenalineDamageStat = (int)(100D * modPlayer.GetAdrenalineDamage());
			int extraAdrenalineDR = 0 +
				(modPlayer.adrenalineBoostOne ? 5 : 0) +
				(modPlayer.adrenalineBoostTwo ? 5 : 0) +
				(modPlayer.adrenalineBoostThree ? 5 : 0);
			modPlayer.adrenalineDRStat = 50 + extraAdrenalineDR;
		}
		#endregion

		#region Rogue Mirrors
		private static void RogueMirrors(Player player, CalamityPlayer modPlayer)
		{
			Rectangle rectangle = new Rectangle((int)(player.position.X + player.velocity.X * 0.5f - 4f), (int)(player.position.Y + player.velocity.Y * 0.5f - 4f), player.width + 8, player.height + 8);
			for (int i = 0; i < Main.maxNPCs; i++)
			{
				NPC npc = Main.npc[i];
				if (npc.active && !npc.dontTakeDamage && !npc.friendly && !npc.townNPC && npc.immune[player.whoAmI] <= 0 && npc.damage > 0)
				{
					Rectangle rect = npc.getRect();
					if (rectangle.Intersects(rect) && (npc.noTileCollide || player.CanHit(npc)))
					{
						bool isImmune = false;
						for (int j = 0; j < player.hurtCooldowns.Length; j++)
						{
							if (player.hurtCooldowns[j] > 0)
								isImmune = true;
						}

						if (Main.rand.NextBool(10) && !isImmune)
						{
							modPlayer.AbyssMirrorEvade();
							modPlayer.EclipseMirrorEvade();
						}
						break;
					}
				}
			}

			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile proj = Main.projectile[i];
				if (proj.active && proj.hostile && proj.damage > 0)
				{
					Rectangle rect = proj.getRect();
					if (rectangle.Intersects(rect))
					{
						if (Main.rand.NextBool(10))
						{
							modPlayer.AbyssMirrorEvade();
							modPlayer.EclipseMirrorEvade();
						}
						break;
					}
				}
			}
		}
		#endregion

		#region Double Jumps
		private static void DoubleJumps(Player player, CalamityPlayer modPlayer)
		{
			if (CalamityUtils.CountHookProj() > 0 || player.sliding || player.autoJump && player.justJumped)
			{
				modPlayer.jumpAgainSulfur = true;
				modPlayer.jumpAgainStatigel = true;
				return;
			}

			bool mountCheck = true;
			if (player.mount != null && player.mount.Active)
				mountCheck = player.mount.BlockExtraJumps;
			bool carpetCheck = true;
			if (player.carpet)
				carpetCheck = player.carpetTime <= 0 && player.canCarpet;

			if (player.position.Y == player.oldPosition.Y && player.wingTime == player.wingTimeMax && mountCheck && carpetCheck)
			{
				modPlayer.jumpAgainSulfur = true;
				modPlayer.jumpAgainStatigel = true;
			}
		}
		#endregion

		#region Mouse Item Checks
		public static void CheckIfMouseItemIsSchematic(Player player)
		{
			if (Main.myPlayer != player.whoAmI)
				return;

			bool shouldSync = false;

			// ActiveItem doesn't need to be checked as the other possibility involves
			// the item in question already being in the inventory.
			if (Main.mouseItem != null && !Main.mouseItem.IsAir)
			{
				if (Main.mouseItem.type == ModContent.ItemType<EncryptedSchematicSunkenSea>() && !RecipeUnlockHandler.HasFoundSunkenSeaSchematic)
				{
					RecipeUnlockHandler.HasFoundSunkenSeaSchematic = true;
					shouldSync = true;
				}

				if (Main.mouseItem.type == ModContent.ItemType<EncryptedSchematicPlanetoid>() && !RecipeUnlockHandler.HasFoundPlanetoidSchematic)
				{
					RecipeUnlockHandler.HasFoundPlanetoidSchematic = true;
					shouldSync = true;
				}

				if (Main.mouseItem.type == ModContent.ItemType<EncryptedSchematicJungle>() && !RecipeUnlockHandler.HasFoundJungleSchematic)
				{
					RecipeUnlockHandler.HasFoundJungleSchematic = true;
					shouldSync = true;
				}

				if (Main.mouseItem.type == ModContent.ItemType<EncryptedSchematicHell>() && !RecipeUnlockHandler.HasFoundHellSchematic)
				{
					RecipeUnlockHandler.HasFoundHellSchematic = true;
					shouldSync = true;
				}

				if (Main.mouseItem.type == ModContent.ItemType<EncryptedSchematicIce>() && !RecipeUnlockHandler.HasFoundIceSchematic)
				{
					RecipeUnlockHandler.HasFoundIceSchematic = true;
					shouldSync = true;
				}
			}

			if (shouldSync)
				CalamityNetcode.SyncWorld();
		}
		#endregion

		#region Potion Handling
		private static void HandlePotions(Player player, CalamityPlayer modPlayer)
		{
			if (modPlayer.potionTimer > 0)
				modPlayer.potionTimer--;
			if (modPlayer.potionTimer > 0 && player.potionDelay == 0)
				player.potionDelay = modPlayer.potionTimer;
			if (modPlayer.potionTimer == 1)
			{
				//Reduced duration than normal
				int duration = 3000;
				if (player.pStone)
					duration = (int)(duration * 0.75);
				player.ClearBuff(BuffID.PotionSickness);
				player.AddBuff(BuffID.PotionSickness, duration);
			}

			if (PlayerInput.Triggers.JustPressed.QuickBuff)
			{
				for (int i = 0; i < Main.maxInventory; ++i)
				{
					Item item = player.inventory[i];

					if (player.potionDelay > 0 || modPlayer.potionTimer > 0)
						break;
					if (item is null || item.stack <= 0)
						continue;

					if (item.type == ModContent.ItemType<SunkenStew>())
						CalamityUtils.ConsumeItemViaQuickBuff(player, item, SunkenStew.BuffType, SunkenStew.BuffDuration, true);
					if (item.type == ModContent.ItemType<Margarita>())
						CalamityUtils.ConsumeItemViaQuickBuff(player, item, Margarita.BuffType, Margarita.BuffDuration, false);
					if (item.type == ModContent.ItemType<Bloodfin>())
						CalamityUtils.ConsumeItemViaQuickBuff(player, item, Bloodfin.BuffType, Bloodfin.BuffDuration, false);
				}
			}
		}
		#endregion
	}
}
