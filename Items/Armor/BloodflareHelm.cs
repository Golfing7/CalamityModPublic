using CalamityMod.CalPlayer;
using CalamityMod.Items.Materials;
using CalamityMod.World;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Items.Armor
{
    [AutoloadEquip(EquipType.Head)]
    public class BloodflareHelm : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Bloodflare Imp Mask");
            Tooltip.SetDefault("You can move freely through liquids and have temporary immunity to lava\n" +
                "10% increased rogue damage and critical strike chance, 5% increased movement speed");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.value = Item.buyPrice(0, 60, 0, 0);
            item.defense = 28; //85
            item.Calamity().customRarity = CalamityRarity.PureGreen;
        }

        public override void ModifyTooltips(List<TooltipLine> list)
        {
			if (CalamityWorld.death)
			{
				foreach (TooltipLine line2 in list)
				{
					if (line2.mod == "Terraria" && line2.Name == "Tooltip1")
					{
						line2.text = "10% increased rogue damage and critical strike chance, 15% increased movement speed\n" +
						"Provides heat protection in Death Mode";
					}
				}
			}
        }

        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<BloodflareBodyArmor>() && legs.type == ModContent.ItemType<BloodflareCuisses>();
        }

        public override void ArmorSetShadows(Player player)
        {
            player.armorEffectDrawShadowSubtle = true;
        }

        public override void UpdateArmorSet(Player player)
        {
            CalamityPlayer modPlayer = player.Calamity();
            modPlayer.bloodflareSet = true;
            modPlayer.bloodflareThrowing = true;
            modPlayer.rogueStealthMax += 1.2f;
            modPlayer.wearingRogueArmor = true;
            player.setBonus = "Greatly increases life regen\n" +
				"Enemies below 50% life drop a heart when struck\n" +
				"This effect has a 5 second cooldown\n" +
				"Enemies killed during a Blood Moon have a much higher chance to drop Blood Orbs\n" +
                "Being over 80% life boosts your defense by 30 and rogue crit by 5%\n" +
                "Being below 80% life boosts your rogue damage by 10%\n" +
                "Rogue critical strikes have a 50% chance to heal you\n" +
                "Rogue stealth builds while not attacking and slower while moving, up to a max of 120\n" +
                "Once you have built max stealth, you will be able to perform a Stealth Strike\n" +
                "Rogue stealth only reduces when you attack, it does not reduce while moving\n" +
                "The higher your rogue stealth the higher your rogue damage, crit, and movement speed";
            player.crimsonRegen = true;
        }

        public override void UpdateEquip(Player player)
        {
            player.lavaMax += 240;
            player.ignoreWater = true;
            player.Calamity().throwingDamage += 0.1f;
            player.Calamity().throwingCrit += 10;
            player.moveSpeed += 0.05f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<BloodstoneCore>(), 11);
            recipe.AddIngredient(ModContent.ItemType<RuinousSoul>(), 2);
			recipe.AddTile(TileID.LunarCraftingStation);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
