using CalamityMod.Buffs.Potions;
using CalamityMod.Items.Materials;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Potions
{
    public class CrumblingPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crumbling Potion");
            Tooltip.SetDefault("Increases melee and rogue critical strike chance by 5%\n" +
                "Melee and rogue attacks break enemy armor");
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
            item.useStyle = ItemUseStyleID.EatingUsing;
            item.UseSound = SoundID.Item3;
            item.consumable = true;
            item.buffType = ModContent.BuffType<ArmorCrumbling>();
            item.buffTime = CalamityUtils.SecondsToFrames(480f);
            item.value = Item.buyPrice(0, 2, 0, 0);
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BottledWater, 5);
            recipe.AddIngredient(ModContent.ItemType<AncientBoneDust>());
            recipe.AddIngredient(ModContent.ItemType<EssenceofCinder>());
            recipe.AddTile(TileID.AlchemyTable);
            recipe.SetResult(this, 5);
            recipe.AddRecipe();
            recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.BottledWater);
            recipe.AddIngredient(ModContent.ItemType<BloodOrb>(), 20);
            recipe.AddIngredient(ModContent.ItemType<EssenceofCinder>());
            recipe.AddTile(TileID.AlchemyTable);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
