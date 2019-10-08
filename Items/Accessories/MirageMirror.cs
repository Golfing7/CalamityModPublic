﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod.CalPlayer;

namespace CalamityMod.Items.Accessories
{
    public class MirageMirror : ModItem
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Mirage Mirror");
			Tooltip.SetDefault("Bend light around you\n" +
                "Reduces enemy aggression outside of the abyss\n" +
                "10% increased stealth regeneration while moving");
		}

		public override void SetDefaults()
		{
			item.width = 20;
			item.height = 20;
            item.value = Item.buyPrice(0, 2, 0, 0);
            item.rare = 2;
			item.accessory = true;
		}

		public override void UpdateAccessory(Player player, bool hideVisual)
		{
            CalamityPlayer modPlayer = player.GetModPlayer<CalamityPlayer>(mod);
            modPlayer.stealthGenMoving += 0.1f;
            player.aggro -= 200;
		}

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MagicMirror);
            recipe.AddIngredient(ItemID.BlackLens);
            recipe.AddIngredient(ItemID.Bone, 50);
            recipe.AddTile(TileID.TinkerersWorkbench);
            recipe.SetResult(this);
            recipe.AddRecipe();

            recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IceMirror);
            recipe.AddIngredient(ItemID.BlackLens);
            recipe.AddIngredient(ItemID.Bone, 50);
            recipe.AddTile(TileID.TinkerersWorkbench);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
