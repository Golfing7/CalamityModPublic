using Terraria.ModLoader; using WallTiles = CalamityMod.Walls;
namespace CalamityMod.Items.Placeables.Walls
{
    public class EutrophicSandWallSafe : ModItem
    {
        public override void SetDefaults()
        {
            item.width = 12;
            item.height = 12;
            item.maxStack = 999;
            item.useTurn = true;
            item.autoReuse = true;
            item.useAnimation = 15;
            item.useTime = 7;
            item.useStyle = 1;
            item.consumable = true;
            item.createWall = ModContent.WallType<WallTiles.EutrophicSandWallSafe>();
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<EutrophicSand>());
            recipe.AddTile(18);
            recipe.SetResult(this, 4);
            recipe.AddRecipe();
        }
    }
}
