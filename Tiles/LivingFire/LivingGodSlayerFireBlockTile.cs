using CalamityMod.Dusts;
using CalamityMod.Items.Placeables.LivingFire;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
namespace CalamityMod.Tiles.LivingFire
{
    public class LivingGodSlayerFireBlockTile : ModTile
    {
        public override void SetDefaults()
        {
            Main.tileLighted[Type] = true;
            soundType = SoundID.Dig;
            drop = ModContent.ItemType<LivingGodSlayerFireBlock>();
            AddMapEntry(new Color(186, 85, 211));
            animationFrameHeight = 90;
			TileObjectData.newTile.AnchorTop = new AnchorData(AnchorType.EmptyTile, TileObjectData.newTile.Width, 0);
			TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.EmptyTile, TileObjectData.newTile.Width, 0);
			TileObjectData.newTile.AnchorLeft = new AnchorData(AnchorType.EmptyTile, TileObjectData.newTile.Width, 0);
			TileObjectData.newTile.AnchorRight = new AnchorData(AnchorType.EmptyTile, TileObjectData.newTile.Width, 0);
        }

        public override void AnimateTile(ref int frame, ref int frameCounter)
        {
			frame = Main.tileFrame[TileID.LivingFire];
        }

        public override void ModifyLight(int i, int j, ref float r, ref float g, ref float b)
        {
			r = 1f;
			g = 0f;
			b = 1f;
        }

        public override bool CreateDust(int i, int j, ref int type)
        {
            Dust.NewDust(new Vector2(i, j) * 16f, 16, 16, (int)CalamityDusts.PurpleCosmolite, 0f, 0f, 1, default, 1f);
            return false;
        }
    }
}
