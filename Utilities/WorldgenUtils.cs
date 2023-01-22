﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace CalamityMod
{
    public static partial class CalamityUtils
    {
        /// <summary>
        /// Generates clusters of ore across the world based on various requirements and with various strengths/frequencies.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="frequency"></param>
        /// <param name="verticalStartFactor"></param>
        /// <param name="verticalEndFactor"></param>
        /// <param name="strengthMin"></param>
        /// <param name="strengthMax"></param>
        /// <param name="convertibleTiles"></param>
        public static void SpawnOre(int type, double frequency, float verticalStartFactor, float verticalEndFactor, int strengthMin, int strengthMax, params int[] convertibleTiles)
        {
            int x = Main.maxTilesX;
            int y = Main.maxTilesY;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int k = 0; k < (int)(x * y * frequency); k++)
                {
                    int tilesX = WorldGen.genRand.Next(0, x);
                    int tilesY = WorldGen.genRand.Next((int)(y * verticalStartFactor), (int)(y * verticalEndFactor));
                    if (convertibleTiles.Length <= 0 || convertibleTiles.Contains(ParanoidTileRetrieval(tilesX, tilesY).TileType))
                        WorldGen.OreRunner(tilesX, tilesY, WorldGen.genRand.Next(strengthMin, strengthMax), WorldGen.genRand.Next(3, 8), (ushort)type);
                }
            }
        }

        //place a circular clump of tiles
        public static void NaturalCircle(int i, int j, int size, int tileType)
		{
			int BaseRadius = size;
			int radius = BaseRadius;

			for (int y = j - radius; y <= j + radius; y++)
			{
				for (int x = i - radius; x <= i + radius + 1; x++)
				{
					if ((int)Vector2.Distance(new Vector2(x, y), new Vector2(i, j)) <= radius && WorldGen.InWorld(x, y))
                    {
						Tile tile = Framing.GetTileSafely(x, y);

                        WorldGen.KillTile(x, y);
                        WorldGen.PlaceTile(x, y, tileType);
                        tile.Slope = 0;
                    }
				}

				radius = BaseRadius - WorldGen.genRand.Next(-1, 2);
			}
		}

        //for some reason orerunner wasnt working with clouds, so just have a seperate thing for aerialite here
        public static void SpawnAerialiteOre()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int x = 5; x < Main.maxTilesX - 5; x++) 
                {
                    for (int y = 5; y < 300; y++) 
                    {
                        if (WorldGen.genRand.Next(350) == 0 && Main.tile[x, y].TileType == TileID.Cloud) 
                        {
                            NaturalCircle(x, y, WorldGen.genRand.Next(1, 4), ModContent.TileType<Tiles.Ores.AerialiteOre>());
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Settles all liquids in the world.
        /// </summary>
        public static void SettleWater()
        {
            Liquid.worldGenTilesIgnoreWater(true);
            Liquid.QuickWater(3);
            WorldGen.WaterCheck();

            Liquid.quickSettle = true;

            for (int i = 0; i < 10; i++)
            {
                int maxLiquid = Liquid.numLiquid + LiquidBuffer.numLiquidBuffer;
                int m = maxLiquid * 5;
                double maxLiquidDifferencePercentage = 0D;
                while (Liquid.numLiquid > 0)
                {
                    m--;
                    if (m < 0)
                        break;

                    double liquidDifferencePercentage = (maxLiquid - Liquid.numLiquid - LiquidBuffer.numLiquidBuffer) / (double)maxLiquid;
                    if (Liquid.numLiquid + LiquidBuffer.numLiquidBuffer > maxLiquid)
                        maxLiquid = Liquid.numLiquid + LiquidBuffer.numLiquidBuffer;
                    
                    if (liquidDifferencePercentage > maxLiquidDifferencePercentage)
                        maxLiquidDifferencePercentage = liquidDifferencePercentage;

                    Liquid.UpdateLiquid();
                }
                WorldGen.WaterCheck();
            }
            Liquid.quickSettle = false;
            Liquid.worldGenTilesIgnoreWater(false);
        }
    }

    //tile runner, useful for caves and other stuff
    public class TileRunner
    {
        public Vector2 pos;
        public Vector2 speed;
        public Point16 hRange;
        public Point16 vRange;
        public double strength;
        public double str;
        public int steps;
        public int stepsLeft;
        public ushort type;
        public bool addTile;
        public bool overRide;

        public TileRunner(Vector2 pos, Vector2 speed, Point16 hRange, Point16 vRange, double strength, int steps, ushort type, bool addTile, bool overRide)
        {
            this.pos = pos;
            if (speed.X == 0 && speed.Y == 0)
            {
                this.speed = new Vector2(WorldGen.genRand.Next(hRange.X, hRange.Y + 1) * 0.1f, WorldGen.genRand.Next(vRange.X, vRange.Y + 1) * 0.1f);
            }
            else
            {
                this.speed = speed;
            }
            this.hRange = hRange;
            this.vRange = vRange;
            this.strength = strength;
            str = strength;
            this.steps = steps;
            stepsLeft = steps;
            this.type = type;
            this.addTile = addTile;
            this.overRide = overRide;
        }

        public void Start()
        {
            while (str > 0 && stepsLeft > 0)
            {
                str = strength * (double)stepsLeft / steps;

                int a = (int)Math.Max(pos.X - str * 0.5, 1);
                int b = (int)Math.Min(pos.X + str * 0.5, Main.maxTilesX - 1);
                int c = (int)Math.Max(pos.Y - str * 0.5, 1);
                int d = (int)Math.Min(pos.Y + str * 0.5, Main.maxTilesY - 1);

                for (int i = a; i < b; i++)
                {
                    for (int j = c; j < d; j++)
                    {
                        if (Math.Abs(i - pos.X) + Math.Abs(j - pos.Y) >= strength * StrengthRange())
                        {
                            continue;
                        }
                        
                        ChangeTile(Main.tile[i, j]);
                    }
                }

                str += 50;
                while (str > 50)
                {
                    pos += speed;
                    stepsLeft--;
                    str -= 50;
                    speed.X += WorldGen.genRand.Next(hRange.X, hRange.Y + 1) * 0.05f;
                    speed.Y += WorldGen.genRand.Next(vRange.X, vRange.Y + 1) * 0.05f;
                }

                speed = Vector2.Clamp(speed, new Vector2(-1, -1), new Vector2(1, 1));
            }
        }

        public virtual void ChangeTile(Tile tile)
        {
            if (!addTile)
            {
                tile.HasTile = false;
            }
            else
            {
                tile.TileType = type;
            }
        }

        public virtual double StrengthRange()
        {
            return 0.5 + WorldGen.genRand.Next(-10, 11) * 0.0075;
        }
    }

    //probably might be useful to have for later, just places clumps of water
	class WaterTileRunner : TileRunner
    {
        public WaterTileRunner(Vector2 pos, Vector2 speed, Point16 hRange, Point16 vRange, double strength, int steps, ushort type, bool addTile, bool overRide) : base(pos, speed, hRange, vRange, strength, steps, type, addTile, overRide)
        {
        }
        public override void ChangeTile(Tile tile)
        {
            tile.HasTile = false;
            tile.LiquidType = LiquidID.Water;
			tile.LiquidAmount = 255;
        }
    }

    class LavaTileRunner : TileRunner
    {
        public LavaTileRunner(Vector2 pos, Vector2 speed, Point16 hRange, Point16 vRange, double strength, int steps, ushort type, bool addTile, bool overRide) : base(pos, speed, hRange, vRange, strength, steps, type, addTile, overRide)
        {
        }
        public override void ChangeTile(Tile tile)
        {
            tile.HasTile = false;
            tile.LiquidType = LiquidID.Lava;
			tile.LiquidAmount = 255;
        }
    }
}
