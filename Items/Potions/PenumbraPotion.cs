﻿using Terraria;
using Terraria.ModLoader;
using Terraria.ID;
namespace CalamityMod.Items.Potions
{
    public class PenumbraPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Penumbra Potion");
            Tooltip.SetDefault("Rogue stealth regenerates 10% faster, 20% faster at night and 30% faster during an eclipse");
        }

        public override void SetDefaults()
        {
            item.width = 28;
            item.height = 18;
            item.useTurn = true;
            item.maxStack = 999;
            item.rare = 3;
            item.useAnimation = 17;
            item.useTime = 17;
            item.useStyle = 2;
            item.UseSound = SoundID.Item3;
            item.consumable = true;
            item.buffType = ModContent.BuffType<PenumbraBuff>();
            item.buffTime = 18000;
            item.value = Item.buyPrice(0, 2, 0, 0);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BottledWater);
            recipe.AddIngredient(null, "CalamityDust", 3);
            recipe.AddIngredient(ItemID.LunarTabletFragment, 2);
            recipe.AddIngredient(null, "EssenceofChaos");
            recipe.AddTile(TileID.AlchemyTable);
            recipe.SetResult(this);
            recipe.AddRecipe();
            recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BottledWater);
            recipe.AddIngredient(null, "BloodOrb", 30);
            recipe.AddIngredient(ItemID.LunarTabletFragment);
            recipe.AddTile(TileID.AlchemyTable);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
