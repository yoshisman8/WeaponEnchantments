﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace WeaponEnchantments.Items
{
    public class Stabilizer : ModItem
    {
        public static string[] sizes = new string[] { "", "Superior" };
        public static int[,] ingredientTypes = {{177, 178, 179, 180, 181}, {182, 999, -1, -1, -1}}; 
        public int size = 0;
        public static int[] IDs = new int[sizes.Length];//Make drop from bosses and ofering
        public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
        public override void SetDefaults()
        {
            if(size == 0)
            {
                Tooltip.SetDefault("Used to create Superior Enchantment Containments");
            }
            else
            {
                Tooltip.SetDefault("Used to create Ultra Rare Enchantments");
            }
            Item.width = 8;
            Item.height = 8;
        }
        public override void AddRecipes()
        {
            for (int i = 0; i < ingredientTypes.Length / 2; i++)
            {
                Recipe recipie = CreateRecipe();
                if(size == 0)
                {
                    recipie.AddTile(TileID.Hellforge);
                }
                else
                {
                    recipie.AddTile(TileID.AdamantiteForge);
                }
                if(ingredientTypes[size, i] != -1)
                {
                    recipie.AddIngredient(ingredientTypes[size, i], 1);
                }
                recipie.Register();
            }
            IDs[size] = Item.type;
        }
        public class SuperiorStabilizer : Stabilizer
        {
            SuperiorStabilizer() { size = 1; }
        }
    }
    public class ContainmentFragment : ModItem
    {
        public static int ID;//Make drop from bosses and ofering
        public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
        public override void SetDefaults()
        {
            Tooltip.SetDefault("Used to create Enchantment Containments");
            Item.width = 10;
            Item.height = 10;
        }
        public override void AddRecipes()
        {
            for (int i = 0; i < Containment.sizes.Length; i++)
            {
                Recipe recipie = CreateRecipe(Containment.fragments[i]);
                recipie.AddTile(TileID.WorkBenches);
                recipie.AddIngredient(Mod, Containment.sizes[i] + "Containment", 1);
                recipie.Register();
            }
            ID = Item.type;
        }
    }
    public class Containment : ModItem
    {
        public static string[] sizes = new string[] { "", "Medium", "Superior" };
        public static int[] glass = new int[] { 1, 4, 0};
        public static int[] fragments = new int[] { 4, 8, 16 };
        public static int[] IDs = new int[sizes.Length];
        public int size = 0;
        public override string Texture => (GetType().Namespace + ".Sprites." + Name).Replace('.', '/');
        public override void SetDefaults()
        {
            Item.value = 100000;
            if (size < 2)
            {
                Item.width = 10 + 4 * (size);
                Item.height = 10 + 4 * (size);
            }
            else
            {
                Item.width = 40;
                Item.height = 40;
            }
            Tooltip.SetDefault("Used to store " + Enchantments.rarity[size] + " enchantments");
        }
        public override void AddRecipes()
        {
            Recipe recipie = CreateRecipe();
            recipie.AddTile(TileID.WorkBenches);
            if (size > 0)
            {
                recipie.AddIngredient(Mod, sizes[size - 1] + "Containment", 1);
            }
            if(size == 2)
            {
                recipie.AddIngredient(Stabilizer.IDs[0], 4);
            }
            else
            {
                recipie.AddIngredient(ItemID.Glass, glass[size]);
            }
            recipie.AddIngredient(ContainmentFragment.ID, fragments[size]);
            recipie.AddIngredient(ContainmentFragment.ID, fragments[size]);
            recipie.Register();
            IDs[size] = Item.type;
        }
        public class MediumContainment : Containment
        {
            MediumContainment() { size = 1; }
        }
        public class SuperiorContainment : Containment
        {
            SuperiorContainment() { size = 2; }
        }
    }
}
