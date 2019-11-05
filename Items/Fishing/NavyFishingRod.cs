using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityMod.Items.Placeables;
namespace CalamityMod.Items.Fishing
{
    public class NavyFishingRod : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Navy Fishing Rod");
            Tooltip.SetDefault("While held, slowly electrifies nearby enemies.\n" +
				"The sea is a city.\n" + //Life of Pi ref Ch.59
				"Just below are highways, boulevards, streets and roundabouts bustling with submarine traffic.");
        }

        public override void SetDefaults()
        {
			//item.CloneDefaults(2289); //Wooden Fishing Pole
			item.width = 24;
			item.height = 28;
			item.useAnimation = 8;
			item.useTime = 8;
			item.useStyle = 1;
			item.UseSound = SoundID.Item1;
			item.fishingPole = 25;
			item.shootSpeed = 13f;
			item.shoot = ModContent.ProjectileType<NavyBobber>();
            item.value = Item.buyPrice(0, 2, 0, 0);
            item.rare = 2;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<SeaPrism>(), 5);
            recipe.AddIngredient(ModContent.ItemType<Navystone>(), 8);
            recipe.AddTile(TileID.Anvils);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
