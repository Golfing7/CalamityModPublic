using Terraria.ModLoader;
namespace CalamityMod.Items.Placeables
{
    public class AstrumDeusTrophy : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Astrum Deus Trophy");
        }

        public override void SetDefaults()
        {
            item.width = 30;
            item.height = 30;
            item.maxStack = 99;
            item.useTurn = true;
            item.autoReuse = true;
            item.useAnimation = 15;
            item.useTime = 10;
            item.useStyle = 1;
            item.consumable = true;
            item.value = 50000;
            item.rare = 1;
            item.createTile = ModContent.TileType<BossTrophy>();
            item.placeStyle = 20;
        }
    }
}
