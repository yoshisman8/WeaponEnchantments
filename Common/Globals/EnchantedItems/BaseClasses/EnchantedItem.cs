﻿using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using Terraria.ModLoader.IO;
using Terraria.Social;
using Terraria.Utilities;
using WeaponEnchantments.Common.Utility;
using WeaponEnchantments.Effects;
using WeaponEnchantments.Items;
using WeaponEnchantments.UI;
using static WeaponEnchantments.Common.Configs.ConfigValues;
using static WeaponEnchantments.Common.EnchantingRarity;
using static WeaponEnchantments.Common.Globals.EnchantedItemStaticMethods;
using static WeaponEnchantments.Common.Globals.EnchantedWeaponStaticMethods;
using static WeaponEnchantments.Items.Enchantment;

namespace WeaponEnchantments.Common.Globals
{
    public abstract class EnchantedItem : GlobalItem {

        #region Constants

        public const int MAX_Level = 40;

        #endregion

        #region Reforge (static)

        public static Item reforgeItem = null;
        public static Item calamityAndAutoReforgePostReforgeItem = null;
        public static bool calamityReforged = false;
        public static bool cloneReforgedItem = false;
        public static bool resetLastValueBonus = false;

        #endregion

        #region Tracking (static)

        public static bool resetGlobals = false;
        public static bool skipUpdateValue = false;

        #endregion


        #region Stats

        public Dictionary<string, StatModifier> statModifiers = new Dictionary<string, StatModifier>();
        public Dictionary<string, StatModifier> appliedStatModifiers = new Dictionary<string, StatModifier>();
        public Dictionary<string, StatModifier> eStats = new Dictionary<string, StatModifier>();
        public Dictionary<string, StatModifier> appliedEStats = new Dictionary<string, StatModifier>();
        public Dictionary<int, int> buffs = new Dictionary<int, int>();
        public Dictionary<short, int> debuffs = new Dictionary<short, int>();
        public Dictionary<int, int> onHitBuffsOld = new Dictionary<int, int>();

        #endregion

        #region Enchantment

        public Item[] enchantments = new Item[EnchantingTable.maxEnchantments];

        #endregion

        #region Infusion

        public string infusedItemName = "";
        public int infusionPower = 0;
        protected int _infusionValueAdded = 0;
        public int InfusionValueAdded {
            get { return _infusionValueAdded; }
            set {
                int lastValue = _infusionValueAdded;
                _infusionValueAdded = value;

                //If value changed, upted Item Value
                if (lastValue != _infusionValueAdded) {
                    UpdateItemValue();
                }
            }
        }

        #endregion

        #region Experience

        protected int _experience = 0;
        public int Experience {
            get { return _experience; }
            set {
                int lastValue = _experience;
                _experience = value;

                //If changed, update Level/Value
                if (lastValue != _experience)
                    UpdateLevelAndValue();
            }
        }
        public int levelBeforeBooster = 0;
        protected bool _powerBoosterInstalled = false;
        public bool PowerBoosterInstalled {
            get { return _powerBoosterInstalled; }
            set {
                bool lastValue = _powerBoosterInstalled;
                _powerBoosterInstalled = value;

                //If changed, update Level/Value
                if (lastValue != _powerBoosterInstalled)
                    UpdateLevelAndValue();
            }
        }

        protected bool _ultraPowerBoosterInstalled = false;
        public bool UltraPowerBoosterInstalled {
            get { return _ultraPowerBoosterInstalled; }
            set {
                bool lastValue = _ultraPowerBoosterInstalled;
                _ultraPowerBoosterInstalled = value;

                //If changed, update Level/Value
                if (lastValue != _ultraPowerBoosterInstalled)
                    UpdateLevelAndValue();
            }
        }
        public int level = 0;
        public int lastValueBonus = 0;

        #endregion

        #region Reforge (instance)

        public int prefix = 0;
        public bool normalReforge = false;

        #endregion

        #region Minion Items

        public Projectile masterProjectile = null;

        #endregion

        #region Tracking (instance)

        public virtual EItemType ItemType { get; }
        public bool inEnchantingTable = false;
        public bool trashItem = false;
        public bool favorited = false;
        protected int _stack = 0;
        public int Stack {
            get {
                if (_stack <= 0)
                    _stack = Item.stack;
                return _stack;
            }
            set {
                int lastValue = _stack;

                //If changed, update Value
                if (lastValue != value && (lastValue > 1 || value > 1))
                    UpdateItemValue();

                _stack = value;
            }
        }

        private bool skipFirstLeftCanStackStack0Check = true;

        #endregion

        #region Properties (instance)

        public Item Item;
        public bool Enchanted => Experience != 0 || PowerBoosterInstalled || UltraPowerBoosterInstalled;
        public bool Modified => Enchanted || infusedItemName != "";

        #endregion

        public EnchantedItem() {
            for (int i = 0; i < EnchantingTable.maxEnchantments; i++) {
                enchantments[i] = new Item();
            }
        }
		public override GlobalItem Clone(Item item, Item itemClone) {
            EnchantedItem clone;

            //Set Tracked Item (similar to ModItem.Item)
            if (resetGlobals) {
                Item = itemClone;
            }
            else {
                Item = item;
            }

            if (cloneReforgedItem || resetGlobals) {
                if (itemClone.TryGetEnchantedItem(out EnchantedItem iGlobal)) {
                    clone = iGlobal;
                }
                else {
                    $"In EnchantedItem, Failed to Clone(item: {item.S()}, itemClone: {itemClone.S()}), cloneReforgedItem: {cloneReforgedItem.S()}, resetGlobals: {resetGlobals.S()}.".LogNT(ChatMessagesIDs.CloneFailGetEnchantedItem);
                    return this;
                }

                #region Stats

                clone.statModifiers = new Dictionary<string, StatModifier>(statModifiers);
                clone.appliedStatModifiers = new Dictionary<string, StatModifier>();
                clone.eStats = new Dictionary<string, StatModifier>(eStats);
                clone.appliedEStats = new Dictionary<string, StatModifier>();
                clone.buffs = new Dictionary<int, int>(buffs);
                clone.debuffs = new Dictionary<short, int>(debuffs);
                clone.onHitBuffsOld = new Dictionary<int, int>(onHitBuffsOld);

                #endregion

                #region Infusion

                clone.infusedItemName = infusedItemName;
                clone.infusionPower = infusionPower;
                clone.InfusionValueAdded = InfusionValueAdded;

                #endregion

                #region Experience

                clone.Experience = _experience;
                clone.levelBeforeBooster = levelBeforeBooster;
                clone.PowerBoosterInstalled = PowerBoosterInstalled;
                clone.UltraPowerBoosterInstalled = UltraPowerBoosterInstalled;
                clone.level = level;
                if (resetGlobals) {
                    clone.lastValueBonus = lastValueBonus;
                }

                #endregion

                #region Reforge (instance)

                if (resetGlobals) {
                    clone.prefix = prefix;
                }

                #endregion

                #region Minion weapons

                clone.masterProjectile = masterProjectile;

                #endregion

                #region Tracking (instance)

                clone.inEnchantingTable = inEnchantingTable;
                clone.favorited = favorited;

                #endregion
            }
            else {
                clone = (EnchantedItem)base.Clone(item, itemClone);
            }

            #region Enchantments

            clone.enchantments = new Item[EnchantingTable.maxEnchantments];

            if (resetGlobals) {
                for (int i = 0; i < enchantments.Length; i++)
                    clone.enchantments[i] = new Item();
            }
            else {
                //fixes enchantments being applied to all of an item instead of just the instance
                for (int i = 0; i < enchantments.Length; i++)
                    clone.enchantments[i] = enchantments[i].Clone();
            }

            #endregion

            return clone;
        }
        public override void LoadData(Item item, TagCompound tag) {
            Item = item;

            #region Debug

            if (LogMethods.debugging) ($"\\/LoadData(" + item.Name + ")").Log();

            #endregion

            #region Enchantment

            for (int i = 0; i < EnchantingTable.maxEnchantments; i++) {
                if (tag.Get<Item>("enchantments" + i.ToString()) != null) {
                    enchantments[i] = tag.Get<Item>("enchantments" + i.ToString()).Clone();
                    OldItemManager.ReplaceOldItem(ref enchantments[i]);
                }
            }

            #endregion

            #region Experience

            Experience = tag.Get<int>("experience");
            PowerBoosterInstalled = tag.Get<bool>("powerBooster");
            UltraPowerBoosterInstalled = tag.Get<bool>("ultraPowerBooster");

            #endregion

            #region Infusion

            infusedItemName = tag.Get<string>("infusedItemName");
            infusionPower = tag.Get<int>("infusedPower");

            #endregion

            if (Experience < 0)
                Experience = int.MaxValue;

            #region Debug

            if (LogMethods.debugging) ($"/\\LoadData(" + item.Name + ")").Log();

            #endregion
        }
        public override void SaveData(Item item, TagCompound tag) {

            #region Enchantment

            for (int i = 0; i < EnchantingTable.maxEnchantments; i++) {
                if (!enchantments[i].IsAir) {
                    tag["enchantments" + i.ToString()] = enchantments[i].Clone();
                }
            }

            #endregion

            #region Experience

            tag["experience"] = Experience;
            tag["powerBooster"] = PowerBoosterInstalled;
            tag["ultraPowerBooster"] = UltraPowerBoosterInstalled;

            #endregion

            #region Infusion

            tag["infusedItemName"] = infusedItemName;
            tag["infusedPower"] = infusionPower;

            #endregion
        }
        public override void NetSend(Item item, BinaryWriter writer) {

            writer.Write(Modified);

            if (!Modified)
                return;

            #region Debug

            if (LogMethods.debugging) {
                ($"\\/NetSend(" + item.Name + ")").Log();
                ($"eStats.Count: " + eStats.Count + ", statModifiers.Count: " + statModifiers.Count).Log();
            }

            #endregion

            #region Enchantment

            for (int i = 0; i < EnchantingTable.maxEnchantments; i++) {
                writer.Write((short)enchantments[i].type);

                #region Debug

                if (LogMethods.debugging) ($"e " + i + ": " + enchantments[i].Name).Log();

                #endregion
            }

            #endregion

            #region Experience

            writer.Write(Experience);
            writer.Write(PowerBoosterInstalled);
            writer.Write(UltraPowerBoosterInstalled);

            #endregion

            #region Infusion

            bool noName = infusedItemName == "";
            writer.Write(noName);
            if (!noName) {
                writer.Write(infusedItemName);
                writer.Write(infusionPower);
            }

            #endregion

            #region Debug

            if (LogMethods.debugging) {
                ($"eStats.Count: " + eStats.Count + ", statModifiers.Count: " + statModifiers.Count).Log();
                ($"/\\NetSend(" + item.Name + ")").Log();
            }

            #endregion
        }
        public override void NetReceive(Item item, BinaryReader reader) {
            Item = item;

            bool dataExistsInReader = reader.ReadBoolean();

            if (!dataExistsInReader)
                return;

            #region Debug

            if (LogMethods.debugging) {
                ($"\\/NetRecieve(" + item.Name + ")").Log();
                ($"eStats.Count: " + eStats.Count + ", statModifiers.Count: " + statModifiers.Count).Log();
            }

            #endregion

            #region Enchantment

            for (int i = 0; i < EnchantingTable.maxEnchantments; i++) {
                enchantments[i] = new Item(reader.ReadUInt16());

                #region Debug

                if (LogMethods.debugging) ($"e " + i + ": " + enchantments[i].Name).Log();

                #endregion
            }

            #endregion

            #region Experience

            Experience = reader.ReadInt32();
            PowerBoosterInstalled = reader.ReadBoolean();
            UltraPowerBoosterInstalled = reader.ReadBoolean();

            #endregion

            #region Infusion

            bool noName = reader.ReadBoolean();
            if (!noName) {
                infusedItemName = reader.ReadString();
                infusionPower = reader.ReadInt32();
            }

            #endregion

            item.SetupGlobals();

            #region Debug

            if (LogMethods.debugging) {
                ($"eStats.Count: " + eStats.Count + ", statModifiers.Count: " + statModifiers.Count).Log();
                ($"/\\NetRecieve(" + item.Name + ")").Log();
            }

            #endregion
        }
        public override void UpdateInventory(Item item, Player player) {
            if (Modified) {
                //Update Item Value if stack changed.
                Stack = item.stack;
            }

            //Track favorited
            if (item.favorited) {
                if (!favorited && WEModSystem.AltDown) {
                    favorited = true;
                }
            }
            else {
                if (favorited) {
                    if (!WEModSystem.AltDown) {
                        item.favorited = true;
                    }
                    else {
                        favorited = false;
                    }
                }
            }
        }
        public void UpdateItemValue() {
            //Fix for stack sizes not being updated until after CanStack
            if (skipUpdateValue)
                return;

            int enchantmentsValue = 0;
            for (int i = 0; i < EnchantingTable.maxEnchantments; i++) {
                enchantmentsValue += enchantments[i].value;
            }

            int powerBoosterValue = PowerBoosterInstalled ? ContentSamples.ItemsByType[ModContent.ItemType<PowerBooster>()].value : 0;
            int ultraPowerBoosterValue = UltraPowerBoosterInstalled ? ContentSamples.ItemsByType[ModContent.ItemType<UltraPowerBooster>()].value : 0;
            int valueToAdd = enchantmentsValue + (int)(EnchantmentEssence.valuePerXP * Experience) + powerBoosterValue + ultraPowerBoosterValue + InfusionValueAdded;
            valueToAdd /= Item.stack;

            if (this is EnchantedWeapon enchantedWeapon && enchantedWeapon.Stack0)
                valueToAdd -= ContentSamples.ItemsByType[Item.type].value;

            //Item.value can be reset by reforging
            if (resetLastValueBonus) {
                lastValueBonus = 0;
                resetLastValueBonus = false;
            }

            Item.value += valueToAdd - lastValueBonus / Stack;
            lastValueBonus = valueToAdd * Item.stack;
        }
        public void UpdateLevelAndValue() {
            int l;
            for (l = 0; l < MAX_Level; l++) {
                if (_experience < WEModSystem.levelXps[l]) {
                    level = l + 1;
                    break;
                }
            }

            if (l == MAX_Level) {
                levelBeforeBooster = MAX_Level;
            }
            else {
                levelBeforeBooster = l;
            }

            level = levelBeforeBooster;

            if (PowerBoosterInstalled)
                level += 10;

            if (UltraPowerBoosterInstalled)
                level += 20;

            UpdateItemValue();
        }
        public int GetLevelsAvailable() {
            int totalEnchantmentLevelCost = 0;
            for (int i = 0; i < enchantments.Length; i++) {
                if (enchantments[i] != null && !enchantments[i].IsAir) {
                    Enchantment enchantment = (Enchantment)enchantments[i].ModItem;
                    totalEnchantmentLevelCost += enchantment.GetCapacityCost();
                }
            }

            return level - totalEnchantmentLevelCost;
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips) {
            WEPlayer wePlayer = Main.LocalPlayer.GetWEPlayer();

            GetTopTooltips(item, tooltips);

            GetXPAndLevelTooltips(item, tooltips);

            //infusionTooltip
            if (infusedItemName != "") {
                string tooltip = GetInfusedItemTooltip(item);

                if (tooltip != "")
                    tooltips.Add(new TooltipLine(Mod, "infusionTooltip", tooltip) { OverrideColor = Color.DarkRed });
            }
            else if (wePlayer.usingEnchantingTable || WEMod.clientConfig.AlwaysDisplayInfusionPower) {
                string tooltip = GetInfusionTooltip(item);

                if (tooltip != "")
                    tooltips.Add(new TooltipLine(Mod, "infusionTooltip", tooltip) { OverrideColor = Color.DarkRed });
            }

            //newInfusionTooltip
            if (inEnchantingTable && wePlayer.infusionConsumeItem != null) {
                if (this.IsSameEnchantedType(wePlayer.infusionConsumeItem)) {
                    string tooltip = GetNewInfusedItemTooltip(item, wePlayer);

                    if (tooltip != "")
                        tooltips.Add(new TooltipLine(Mod, "newInfusionTooltip", tooltip) { OverrideColor = Color.DarkRed });
                }
            }

            GetEnchantementTooltips(tooltips);
        }
        protected virtual void GetTopTooltips(Item item, List<TooltipLine> tooltips) { }
        protected virtual void GetXPAndLevelTooltips(Item item, List<TooltipLine> tooltips) {
            //Xp and level Tooltips
            if (Enchanted || inEnchantingTable) {
                string pointsName = WEMod.clientConfig.UsePointsAsTooltip ? "Points" : "Enchantment Capacity";

                string levelTooltip = $"Level: {levelBeforeBooster}  {pointsName} available: {GetLevelsAvailable()}";
                if (PowerBoosterInstalled || UltraPowerBoosterInstalled) {
                    bool both = PowerBoosterInstalled && UltraPowerBoosterInstalled;
                    levelTooltip += $" (Booster Installed {(PowerBoosterInstalled ? "N" : "")}{(both ? " " : "")}{(UltraPowerBoosterInstalled ? "U" : "")})";
                }

                tooltips.Add(new TooltipLine(Mod, "level", levelTooltip) { OverrideColor = Color.LightGreen });

                //Experience tooltip
                string experienceTooltip = $"Experience: {Experience}";
                if (levelBeforeBooster < MAX_Level) {
                    experienceTooltip += $" ({WEModSystem.levelXps[levelBeforeBooster] - Experience} to next level)";
                }
                else {
                    experienceTooltip += " (Max Level)";
                }

                tooltips.Add(new TooltipLine(Mod, "experience", experienceTooltip) { OverrideColor = Color.White });
            }
        }
        protected virtual string GetInfusedItemTooltip(Item item) => "";
        protected virtual string GetInfusionTooltip(Item item) => "";
        protected virtual string GetNewInfusedItemTooltip(Item item, WEPlayer wePlayer) => "";
        protected virtual void GetEnchantementTooltips(List<TooltipLine> tooltips) {
            IEnumerable<Enchantment> enchantmentModItems = enchantments.Select(e => e.ModItem).OfType<Enchantment>();
		    int i = 0;
            foreach (Enchantment enchantment in enchantmentModItems) {
                //float effectiveness = enchantment.AllowedList[ItemType];
                //var effectTooltips = enchantment.GetEffectsTooltips();
		        string tooltip = enchantment.StoredShortTooltip;
		        tooltips.Add(new TooltipLine(Mod, $"enchantment{i}", tooltip) { OverrideColor = TierColors[enchantment.EnchantmentTier] });
                //tooltips.Add(new TooltipLine(Mod, $"enchantment:{enchantment.Name}", $"{enchantment.EnchantmentTypeName} ({effectiveness.Percent()}%):") { OverrideColor = Color.Violet });
                //foreach (var tooltipTuple in effectTooltips) {
                //    tooltips.Add(new TooltipLine(Mod, $"effects:{enchantment.Name}", $"• {tooltipTuple.Item1}") { OverrideColor = tooltipTuple.Item2 });
                //}
		        i++;
            }
        }
		public void GainXP(Item item, int xpInt) {
            WEPlayer wePlayer = Main.LocalPlayer.GetModPlayer<WEPlayer>();

            int currentLevel = levelBeforeBooster;

            //xp < 0 return
            if (xpInt < 0) {
                $"Prevented your {item.S()} from loosing experience due to a calculation error.".LogNT(ChatMessagesIDs.GainXPPreventedLoosingExperience);

                return;
            }

            if(Experience + xpInt >= 0) {
                //Add xp
                Experience += xpInt;
            }
			else {
                //Prevent overflow
                Experience = int.MaxValue;
            }

            //Already max level return
            if (currentLevel >= MAX_Level)
                return;

            //Level up message
            if (levelBeforeBooster > currentLevel && wePlayer.usingEnchantingTable) {
                if(levelBeforeBooster >= MAX_Level) {
                    SoundEngine.PlaySound(SoundID.Unlock);
                    Main.NewText($"Congratulations!  {wePlayer.Player.name}'s {item.Name} reached the maximum level, " +
						$"{levelBeforeBooster} ({WEModSystem.levelXps[levelBeforeBooster - 1]} xp).");
                }
                else {
                    Main.NewText($"{wePlayer.Player.name}'s {item.Name} reached level {levelBeforeBooster} ({WEModSystem.levelXps[levelBeforeBooster - 1]} xp).");
                }
            }
        }
        public override bool CanRightClick(Item item) {
            if (item.stack <= 1)
                return false;

            if (!Modified)
                return false;

            //Prevent global item duplication exploit
            if (Main.mouseItem.IsAir) {
                return true;
            }

            return false;
        }
        public override void RightClick(Item item, Player player) {
            if (item.stack <= 1)
                return;

            if (!Modified)
                return;

            //Prevent specific items from running RightClickStackableItem
            switch (item.Name) {
                case "Primary Zenith":
                    return;
            }

            //Prevent global item duplication exploit
            if (Main.mouseItem.IsAir) {
                RightClickStackableItem(item);
            }
        }
        public void RightClickStackableItem(Item item) {
            //Dont prevent duplication if stack is 1
            if (item.stack <= 1)
                return;

            if (!Modified)
                return;

            //Prevent global item duplication exploit
            if (Main.mouseItem.IsAir) {
                Main.mouseItem = new Item(item.type);
            }
            else if (Main.mouseItem.type == item.type) {
                Main.mouseItem.stack++;
            }

            if(Main.mouseItem.TryGetEnchantedItem(out EnchantedItem mGlobal))
                mGlobal.Stack = Main.mouseItem.stack;

            //Update Item Value if stack changed.
            Stack = item.stack;
        }
		public override bool PreReforge(Item item) {

			#region Debug

			if (LogMethods.debugging) ($"\\/PreReforge({item.S()})").Log();

            #endregion

            //Calamity
            reforgeItem = item.Clone();

			#region Debug

			if (LogMethods.debugging) {
                string s = $"reforgeItem: {reforgeItem.S()}, prefix: {reforgeItem.prefix}, Enchantments: ";
                if(reforgeItem.TryGetEnchantedItem(out EnchantedItem iGlobal)) {
                    foreach (Item enchantment in iGlobal.enchantments) {
                        s += enchantment.S();
                    }
                    s.Log();
                }

                ($"/\\PreReforge({item.S()})").Log();
            }

			#endregion

			return true;
        }
        public override void PostReforge(Item item) {

            #region Debug

            if (LogMethods.debugging) ($"\\/PostReforge({item.S()})").Log();

            #endregion

            if (!Modified)
                return;

            Item = item;

            //Vanilla
            resetLastValueBonus = true;

            //Calamity
            if (WEMod.calamityEnabled)
                calamityReforged = true;

			//Calamity deletes global data after reforge, so it is handled on the next tick in WEModSystem.
			if (!WEMod.calamityEnabled) {
                //Vanilla and AutoReforge (No Calamity)
                ReforgeItem(ref item, Main.LocalPlayer);
            }
			else {
                //Only used when both Calamity and AutoReforge are enabled.
                calamityAndAutoReforgePostReforgeItem = item;
            }

            #region Debug

            if (LogMethods.debugging) ($"/\\PostReforge({item.S()})").Log();

            #endregion
        }
        public static void ReforgeItem(ref Item item, Player player, bool needCloneGlobals = false) {
            if (item.TryGetEnchantedItem(out EnchantedItem iGlobal)) {
                //Calamity
                if (needCloneGlobals && reforgeItem.TryGetEnchantedItem(out EnchantedItem rGlobal)) {
                    cloneReforgedItem = true;
                    rGlobal.Clone(reforgeItem, item);
                    cloneReforgedItem = false;
                }

                //Vanilla
                iGlobal.UpdateItemValue();
                iGlobal.prefix = -1;
            }

            //Calamity
            reforgeItem = null;

            //Vanilla
            player.GetWEPlayer().UpdateItemStats(ref item);

            //Calamity
            calamityReforged = false;

            //Calamity and AutoReforge
            calamityAndAutoReforgePostReforgeItem = null;
        }
		public override bool ReforgePrice(Item item, ref int reforgePrice, ref bool canApplyDiscount) {
            float priceMultiplier = ((float)item.value - (float)lastValueBonus) / (float)item.value;
            float reforgePriceFloat = reforgePrice * priceMultiplier;
            reforgePrice = (int)reforgePriceFloat;

            return true;
        }
		public override void OnCreate(Item item, ItemCreationContext context) {
			if(context is RecipeCreationContext recipeCreationContext) {
                if(recipeCreationContext.ConsumedItems?.Count > 0) {
                    item.CombineEnchantedItems(ref recipeCreationContext.ConsumedItems);
                }
				else {
                    //Temporary for Magic storage
                    item.CombineEnchantedItems(ref WEMod.consumedItems);
				}
                //Temporary for Magic storage
                WEMod.consumedItems.Clear();
            }
		}
		public bool OnStack(Item item1, Item item2) {
            //Check max stack and always allow combining in the 
            if (!item1.TryGetEnchantedItem(out EnchantedItem i1Global) || item1.maxStack < 2)
                return true;

            //item1 already tested for try.
            item2.TryGetEnchantedItem(out EnchantedItem i2Global);

            bool modified1 = i1Global.Modified;
            bool modified2 = i2Global.Modified;

            if (!modified1 && !modified2) {
                //Not Modified
                return true;
            }
            else if (modified1 && modified2 && (item1.whoAmI > 0 || item2.whoAmI > 0)) {
                //Prevent stacking in the world if both modified.
                return false;
			}

            //Enchanting table loot all
            if (i2Global.inEnchantingTable && !Main.mouseLeft && !Main.mouseRight && !WeaponEnchantmentUI.pressedLootAll)
                return true;

            int maxStack = item1.maxStack;

            //Both at max stack
            if (item1.stack >= maxStack && item2.stack >= maxStack)
                return false;

            //Prevent stackable armor from merging (such as buckets)
            if (i1Global is EnchantedArmor enchantedArmor1 && i2Global is EnchantedArmor enchantedArmor2 && enchantedArmor1.infusedArmorSlot != enchantedArmor2.infusedArmorSlot)
                return false;

            //Splitting stack with right click
            if (item1.type == Main.mouseItem.type && item1.stack == Main.mouseItem.stack && Main.mouseRight && item2.stack > 1)
                return true;

            //Combine item2 into item1
            List<Item> list = new List<Item>();
            list.Add(item2);
            skipUpdateValue = true;
            item1.CombineEnchantedItems(ref list);
            skipUpdateValue = false;

            /*
            if (i1Global is EnchantedWeapon enchantedWeapon1 && i2Global is EnchantedWeapon enchantedWeapon2) {
                //Stack0
                if (enchantedWeapon1.Stack0) {
                    item1.stack--;
                }
                else if (enchantedWeapon2.Stack0) {
                    if (!WEModSystem.ShiftDown) {
                        item2.stack--;
                    }
                    else if (Main.mouseLeft) {
                        //Bug fix for both item1 and item2 having Stack0
                        if (skipFirstLeftCanStackStack0Check) {
                            skipFirstLeftCanStackStack0Check = false;
                        }
                        else {
                            item1.stack--;
                            skipFirstLeftCanStackStack0Check = true;
                        }
                    }
                }
            }
            */

            //Clear item2 if stackTotal > max stack
            int stackTotal = item1.stack + item2.stack;
            if (stackTotal > maxStack) {
                //Clear enchantments in enchanting table if item2 is in it (Will only have cleared off of the player tracked enchantments from combining)
                if (i2Global.inEnchantingTable) {
                    for (int i = 0; i < enchantments.Length; i++) {
                        Main.LocalPlayer.GetWEPlayer().enchantingTableUI.enchantmentSlotUI[i].Item = new Item();
                    }

                }

                //Reset item2 globals
                Item tempItem = new Item(item1.type);
                resetGlobals = true;
                if (tempItem.TryGetEnchantedItem(out EnchantedItem tempGlobal))
                    tempGlobal.Clone(tempItem, item2);

                resetGlobals = false;
            }

            return true;
        }
	}

    public static class EnchantedItemStaticMethods {
        public static bool IsEnchantable(Item item) {
            if (IsWeaponItem(item) || IsArmorItem(item) || IsAccessoryItem(item) || IsFishingRod(item) || IsTool(item)) {
                return true;
            }
            else {
                return false;
            }
        }
        public static bool IsWeaponItem(Item item) {
            if (item.NullOrAir())
                return false;

            bool isWeapon;
            switch (item.type) {
                case ItemID.CoinGun:
                case ItemID.Snowball:
                    isWeapon = true;
                    break;
                case ItemID.ExplosiveBunny:
                    isWeapon = false;
                    break;
                default:
                    isWeapon = item.damage > 0 && item.ammo == 0;
                    break;
            }

            return isWeapon && !item.accessory;
        }
        public static bool IsArmorItem(Item item) {
            if (item.NullOrAir())
                return false;

            return !item.vanity && (item.headSlot > -1 || item.bodySlot > -1 || item.legSlot > -1);
        }
        public static bool IsAccessoryItem(Item item) {
            if (item.NullOrAir())
                return false;

            //Check for armor item is a fix for Reforgable armor mod setting armor to accessories
            return item.accessory && !IsArmorItem(item);
        }
        public static bool IsFishingRod(Item item) {
            if (item.NullOrAir())
                return false;

            return item.fishingPole > 0;
        }
        public static bool IsTool(Item item) {
            if (item.NullOrAir())
                return false;

			switch (item.type) {
                case ItemID.Clentaminator:
                case ItemID.BugNet:
                case ItemID.GoldenBugNet:
                case ItemID.FireproofBugNet:
                    return true;
                default:
                    return false;
			}
        }
        public static bool IsSameEnchantedType(this Item item, Item otherItem) {
            if (item == null || otherItem == null)
                return false;

            if (item.TryGetEnchantedItem(out EnchantedItem enchantedItem) && otherItem.TryGetEnchantedItem(out EnchantedItem otherEnchantedItem))
                return IsSameEnchantedType(enchantedItem, otherEnchantedItem);

            return false;
        }
        public static bool IsSameEnchantedType(this Item item, EnchantedItem otherEnchantedItem) {
            if (item == null)
                return false;

            if (item.TryGetEnchantedItem(out EnchantedItem enchantedItem))
                return IsSameEnchantedType(enchantedItem, otherEnchantedItem);

            return false;
        }
        public static bool IsSameEnchantedType(this EnchantedItem enchantedItem, Item otherItem) {
            if (otherItem == null)
                return false;

            if (otherItem.TryGetEnchantedItem(out EnchantedItem otherEnchantedItem))
                return IsSameEnchantedType(enchantedItem, otherEnchantedItem);

            return false;
        }
        public static bool IsSameEnchantedType(this EnchantedItem enchantedItem, EnchantedItem otherEnchantedItem) {
            return enchantedItem.ItemType == otherEnchantedItem.ItemType;
		}
        public static void SetupGlobals(this Item item) {
            //Not EnchantedItem return
            if (!item.TryGetEnchantedItem(out EnchantedItem iGlobal))
                return;

            //Update Enchantments
            for (int i = 0; i < EnchantingTable.maxEnchantments; i++) {
                Item enchantmentItem = iGlobal.enchantments[i];
                Enchantment enchantment = (Enchantment)enchantmentItem.ModItem;
                item.UpdateEnchantment(ref enchantment, i);
            }

            //Get Global Item Stats
            iGlobal.TryGetInfusionStats();

            //Update Stats
            Main.LocalPlayer.GetWEPlayer().UpdateItemStats(ref item);
        }
        public static void ApplyEnchantment(int i) {
            WEPlayer wePlayer = Main.LocalPlayer.GetModPlayer<WEPlayer>();
            Item item = wePlayer.enchantingTableUI.itemSlotUI[0].Item;
            item.ApplyEnchantment(i);
        }
        public static void ApplyEnchantment(this Item item, int i) {

			#region Debug

			if (LogMethods.debugging) ($"\\/ApplyEnchantment(i: " + i + ")").Log();

			#endregion

			WEPlayer wePlayer = Main.LocalPlayer.GetModPlayer<WEPlayer>();
            if (item.TryGetEnchantedItem(out EnchantedItem iGlobal)) {
                Enchantment enchantment = (Enchantment)(iGlobal.enchantments[i].ModItem);
                item.UpdateEnchantment(ref enchantment, i);
                wePlayer.UpdateItemStats(ref item);
            }

			#region Debug

			if (LogMethods.debugging) ($"/\\ApplyEnchantment(i: " + i + ")").Log();

			#endregion
		}
		public static void RemoveEnchantment(int i) {

			#region Debug

			if (LogMethods.debugging) ($"\\/RemoveEnchantment(i: " + i + ")").Log();

			#endregion

			WEPlayer wePlayer = Main.LocalPlayer.GetModPlayer<WEPlayer>();
            Item item = wePlayer.enchantingTableUI.itemSlotUI[0].Item;
            if (item.TryGetEnchantedItem(out EnchantedItem iGlobal)) {
                Enchantment enchantment = (Enchantment)(iGlobal.enchantments[i].ModItem);
                iGlobal.enchantments[i] = new Item();
                item.UpdateEnchantment(ref enchantment, i, true);
                wePlayer.UpdateItemStats(ref item);
            }

            #region Debug

            if (LogMethods.debugging) ($"/\\RemoveEnchantment(i: " + i + ")").Log();

            #endregion
        }
        public static void UpdateEnchantment(this Item item, ref Enchantment enchantment, int slotNum, bool remove = false) {
            //enchantment null return
            if (enchantment == null)
                return;

            //Not EnchantedItem return
            if (!item.TryGetEnchantedItem(out EnchantedItem iGlobal))
                return;

			#region Debug

			if (LogMethods.debugging) ($"\\/UpdateEnchantment(" + item.S() + ", " + enchantment.S() + ", slotNum: " + slotNum + ", remove: " + remove).Log();

            #endregion

            //Buffs
            if (enchantment.Buff.Count > 0) {
                foreach (int buff in enchantment.Buff) {
                    if (LogMethods.debugging) (iGlobal.buffs.S(buff)).Log();

                    if (iGlobal.buffs.ContainsKey(buff)) {
                        iGlobal.buffs[buff] += (remove ? -1 : 1);

                        if (iGlobal.buffs[buff] < 1)
                            iGlobal.buffs.Remove(buff);
                    }
                    else {
                        iGlobal.buffs.Add(buff, 1);
                    }

                    if (LogMethods.debugging) (iGlobal.buffs.S(buff)).Log();
                }
            }

            //Debuffs
            if (enchantment.Debuff.Count > 0) {
                foreach (short debuff in enchantment.Debuff.Keys) {
                    if (LogMethods.debugging) (iGlobal.debuffs.S(debuff)).Log();

                    int duration = enchantment.Debuff[debuff];

                    if (iGlobal.debuffs.ContainsKey(debuff)) {
                        iGlobal.debuffs[debuff] += (remove ? -duration : duration);

                        if (iGlobal.debuffs[debuff] < 1)
                            iGlobal.debuffs.Remove(debuff);
                    }
                    else {
                        iGlobal.debuffs.Add(debuff, duration);
                    }

                    if (LogMethods.debugging) (iGlobal.debuffs.S(debuff)).Log();
                }
            }

            //OnHitBuffs
            if (enchantment.OnHitBuff.Count > 0) {
                foreach (int onHitBuff in enchantment.OnHitBuff.Keys) {
                    if (LogMethods.debugging) (iGlobal.onHitBuffsOld.S(onHitBuff)).Log();

                    int duration = enchantment.OnHitBuff[onHitBuff];
                    if (iGlobal.onHitBuffsOld.ContainsKey(onHitBuff)) {
                        iGlobal.onHitBuffsOld[onHitBuff] += (remove ? -duration : duration);

                        if (iGlobal.onHitBuffsOld[onHitBuff] < 1)
                            iGlobal.onHitBuffsOld.Remove(onHitBuff);
                    }
                    else {
                        iGlobal.onHitBuffsOld.Add(onHitBuff, duration);
                    }

                    if (LogMethods.debugging) (iGlobal.onHitBuffsOld.S(onHitBuff)).Log();
                }
            }

            //Estats
            foreach (EStat eStat in enchantment.EStats) {
                if (LogMethods.debugging) ($"eStat: " + eStat.S()).Log();

                float add = eStat.Additive * (remove ? -1f : 1f);
                float mult = remove ? 1 / eStat.Multiplicative : eStat.Multiplicative;
                float flat = eStat.Flat * (remove ? -1f : 1f);
                float @base = eStat.Base * (remove ? -1f : 1f);

                item.ApplyAllowedList(enchantment, ref add, ref mult, ref flat, ref @base);

                StatModifier statModifier = new StatModifier(1f + add, mult, flat, @base);

                if (!iGlobal.eStats.ContainsKey(eStat.StatName)) {
                    iGlobal.eStats.Add(eStat.StatName, statModifier);
                }
                else {
                    iGlobal.eStats[eStat.StatName] = iGlobal.eStats[eStat.StatName].CombineWith(statModifier);
                }
            }

            //StaticStats (StatModifiers)
            foreach (EnchantmentStaticStat staticStat in enchantment.StaticStats) {
                if (LogMethods.debugging) ($"staticStat: " + staticStat.S()).Log();

                //Magic missile and similar weapon prevent auto reuse
                if (WEMod.serverConfig.AutoReuseDisabledOnMagicMissile && staticStat.Name == "autoReuse") {
                    Item sampleItem = ContentSamples.ItemsByType[item.type];
                    if (sampleItem.mana > 0 && sampleItem.useStyle == 1 && sampleItem.channel)
                        continue;
                }

                float add = staticStat.Additive * (remove ? -1f : 1f);
                float mult = remove ? 1 / staticStat.Multiplicative : staticStat.Multiplicative;
                float flat = staticStat.Flat * (remove ? -1f : 1f);
                float @base = staticStat.Base * (remove ? -1f : 1f);

                item.ApplyAllowedList(enchantment, ref add, ref mult, ref flat, ref @base);

                StatModifier statModifier = new StatModifier(1f + add, mult, flat, @base);

                if (!iGlobal.statModifiers.ContainsKey(staticStat.Name)) {
                    iGlobal.statModifiers.Add(staticStat.Name, statModifier);
                }
                else {
                    iGlobal.statModifiers[staticStat.Name] = iGlobal.statModifiers[staticStat.Name].CombineWith(statModifier);
                }
            }

            //New Damage Type
			foreach (IPermenantStat effect in enchantment.Effects.Where(e => e is IPermenantStat).Select(e => (IPermenantStat)e)) {
                effect.Update(ref item, remove);
			}

            enchantment.ItemTypeAppliedOn = remove ? EItemType.None : iGlobal.ItemType;

            //Update item Value
            iGlobal.UpdateItemValue();

            if (iGlobal is EnchantedHeldItem)
                Main.LocalPlayer.GetWEPlayer().Equipment.UpdateHeldItemEnchantmentEffects(item);

            #region Debug

            if (LogMethods.debugging) ($"/\\UpdateEnchantment(" + item.S() + ", " + enchantment.S() + ", slotNum: " + slotNum + ", remove: " + remove).Log();

			#endregion
		}
		public static void DamageNPC(this Item item, Player player, NPC target, int damage, bool crit, bool melee = false) {

            #region Debug

            if (LogMethods.debugging) ($"\\/DamageNPC").Log();

            #endregion
            
            //dummy goto debug
            if (target.netID == NPCID.TargetDummy || target.FullName == "Super Dummy")
                goto debugBeforeReturn;

            //friendly goto debug
            if (target.friendly || target.townNPC)
                goto debugBeforeReturn;

            //value
            float value;
            if (Main.netMode == NetmodeID.MultiplayerClient) {
                value = ContentSamples.NpcsByNetId[target.netID].value;
            }
            else {
                value = target.value;
            }

            //value or life max goto debug
            if (value <= 0 && (target.SpawnedFromStatue || target.lifeMax <= 10))
                goto debugBeforeReturn;

            //NPC Characteristics Factors
            float noGravityFactor = target.noGravity ? 0.2f : 0f;
            float noTileCollideFactor = target.noTileCollide ? 0.2f : 0f;
            float knockBackResistFactor = 0.2f * (1f - target.knockBackResist);
            float npcCharacteristicsFactor = noGravityFactor + noTileCollideFactor + knockBackResistFactor;

            //Config Multiplier
            float configMultiplier = target.boss ? BossXPMultiplier : NormalXPMultiplier;

            float balanceMultiplier = target.boss ? 0.25f : 1f;

            //Experience Multiplier
            float experienceMultiplier = (1f + npcCharacteristicsFactor) * configMultiplier * balanceMultiplier;

            //Armor Penetration
            float armorPenetration = 0;
            if (item != null) {
                armorPenetration = target.checkArmorPenetration(player.GetWeaponArmorPenetration(item));
            }

            //Actual Defense
            float actualDefense = target.defense / 2f - armorPenetration;
            float actualDamage = damage;
            if (!melee) {
                actualDamage -= actualDefense;

                if (crit)
                    actualDamage *= 2f;
            }

            //life vs damage check
            int xpDamage = (int)actualDamage;
            if (target.life < 0)
                xpDamage += target.life;

            //XP Damage <= 0 check
            if (xpDamage <= 0)
                goto debugBeforeReturn;

            //Low damage per hit xp boost
            float lowDamagePerHitXPBoost;
            if (item != null && target.defense > 0) {
                //Remove 2x from crit
                float effectiveDamagePerHit = actualDamage;
                if (crit)
                    effectiveDamagePerHit /= 2f;

                //Apply affective crit multiplier
                float critMultiplier = 1f + (player.GetWeaponCrit(item) % 100) / 100f;
                effectiveDamagePerHit *= critMultiplier;

                float effectiveBaseDamagePerHit = effectiveDamagePerHit + actualDefense;

                lowDamagePerHitXPBoost = effectiveBaseDamagePerHit / effectiveDamagePerHit;
            }
            else {
                lowDamagePerHitXPBoost = 1f;
            }

            if(lowDamagePerHitXPBoost < 1f) {
                ($"Prevented an issue that would cause your xp do be reduced.  (xpInt < 0) item: {item.S()}, target: {target.S()}, damage: {damage}, crit: {crit.S()}, " +
                    $"melee: {melee.S()}, Main.GameMode: {Main.GameMode},\n" +
					$"target.defense: {target.defense}, armorPenetration: {armorPenetration} xpDamage: {xpDamage}, lowDamagePerHitXPBoost: {lowDamagePerHitXPBoost}, actualDefense: {actualDefense}, actualDamage: {actualDamage}").LogNT(ChatMessagesIDs.LowDamagePerHitXPBoost);
                lowDamagePerHitXPBoost = 1f;
			}

            //Low Damage help multiplier
            float xp = xpDamage * lowDamagePerHitXPBoost * experienceMultiplier;

            //Reduction Factor (Life Max)
            float reductionFactor = GetReductionFactor(target.lifeMax);
            xp /= reductionFactor;

            //XP <= 0 check
            int xpInt = (int)Math.Round(xp);
            if (xpInt <= 0) {
                xpInt = 1;
            }
            else if (xpInt < 0) {
                ($"Prevented an issue that would cause you to loose experience. (xpInt < 0) item: {item.S()}, target: {target.S()}, damage: {damage}, crit: {crit.S()}, " +
                    $"melee: {melee.S()}, Main.GameMode: {Main.GameMode}, xpDamage: {xpDamage}, xpInt: {xpInt}, lowDamagePerHitXPBoost: {lowDamagePerHitXPBoost}, " +
                    $"actualDefense: {actualDefense}, actualDamage: {actualDamage}").LogNT(ChatMessagesIDs.DamageNPCPreventLoosingXP2);
                xpInt = 1;
            }

            //Gain XP (Item)
            if (item.TryGetEnchantedItem(out EnchantedItem iGlobal))
                iGlobal.GainXP(item, xpInt);

            //Gain XP (Armor)
            player.AllArmorGainXp(xpInt);

			#region Debug

            debugBeforeReturn:
			if (LogMethods.debugging) ($"/\\DamageNPC").Log();

            #endregion
        }
        public static void AllArmorGainXp(this Player player, int xp) {
            int vanillaArmorLength = player.armor.Length / 2;
            var loader = LoaderManager.Get<AccessorySlotLoader>();
            for (int j = 0; j < player.GetWEPlayer().equipArmor.Length; j++) {
                Item armor;

                //Check if armorslot is functional
                if(j < vanillaArmorLength) {
                    armor = player.armor[j];
				}
				else {
                    int num = j - vanillaArmorLength;
                    if (loader.ModdedIsAValidEquipmentSlotForIteration(num, player)) {
                        armor = loader.Get(num).FunctionalItem;
                    }
					else {
                        armor = new Item();
                    }
                }

                //Gain xp on each armor
                if (!armor.vanity && armor.TryGetEnchantedItem(out EnchantedItem aGlobal)) {
                    float reductionFactor;
                    if (IsArmorItem(armor)) {
                        reductionFactor = 2f;
                    }
                    else {
                        reductionFactor = 4f;
                    }

                    int xpInt = (int)Math.Round(xp / reductionFactor);

                    if (xpInt <= 0)
                        xpInt = 1;

                    aGlobal.GainXP(armor, xpInt);
                }
            }
        }
        public static void CombineEnchantedItems(this Item item, ref List<Item> consumedItems) {
            if (consumedItems.Count <= 0)
                return;

            for (int c = 0; c < consumedItems.Count; c++) {
                Item consumedItem = consumedItems[c];
                if (consumedItem.IsAir)
                    continue;

                if (!consumedItem.TryGetEnchantedItem(out EnchantedItem cGlobal))
                    continue;

                if (!cGlobal.Modified)
                    continue;

                if (item.TryGetEnchantedItem(out EnchantedItem iGlobal)) {
                    item.CheckConvertExcessExperience(consumedItem);
                    if (iGlobal.infusionPower < cGlobal.infusionPower && item.GetWeaponInfusionPower() < cGlobal.infusionPower) {
                        item.TryInfuseItem(consumedItem);
                        item.TryInfuseItem(consumedItem, false, true);
                    }

                    if (cGlobal.PowerBoosterInstalled) {
                        if (!iGlobal.PowerBoosterInstalled) {
                            iGlobal.PowerBoosterInstalled = true;
                        }
						else {
                            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("PlayerDropItemCheck"), ModContent.ItemType<PowerBooster>(), 1);
                        }
                    }

                    if (cGlobal.UltraPowerBoosterInstalled) {
                        if (!iGlobal.UltraPowerBoosterInstalled) {
                            iGlobal.UltraPowerBoosterInstalled = true;
                        }
						else {
                            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("PlayerDropItemCheck"), ModContent.ItemType<UltraPowerBooster>(), 1);
                        }
                    }

                    int j;
                    for (j = 0; j <= EnchantingTable.maxEnchantments; j++) {
                        if (j > 4)
                            break;
                        if (iGlobal.enchantments[j].IsAir)
                            break;
                    }
                    for (int k = 0; k < EnchantingTable.maxEnchantments; k++) {
                        if (!cGlobal.enchantments[k].IsAir) {
                            Enchantment enchantment = ((Enchantment)cGlobal.enchantments[k].ModItem);
                            int uniqueItemSlot = WEUIItemSlot.FindSwapEnchantmentSlot(enchantment, item);
                            bool cantFit = false;
                            if (enchantment.GetCapacityCost() <= iGlobal.GetLevelsAvailable()) {
                                if (uniqueItemSlot == -1) {
                                    if (enchantment.Utility && iGlobal.enchantments[4].IsAir && (IsWeaponItem(item) || IsArmorItem(item))) {
                                        iGlobal.enchantments[4] = cGlobal.enchantments[k].Clone();
                                        item.ApplyEnchantment(j);
                                    }
                                    else if (j < 4) {
                                        iGlobal.enchantments[j] = cGlobal.enchantments[k].Clone();
                                        item.ApplyEnchantment(j);
                                        j++;
                                    }
                                    else {
                                        cantFit = true;
                                    }
                                }
                                else {
                                    cantFit = true;
                                }
                            }
                            else {
                                cantFit = true;
                            }

                            if (cantFit)
                                Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("PlayerDropItemCheck"), cGlobal.enchantments[k].type, 1);
                        }
                        cGlobal.enchantments[k] = new Item();
                    }
                }
                else {
                    item.CheckConvertExcessExperience(consumedItem);
                    int numberEssenceRecieved;
                    int xpCounter = iGlobal.Experience;
                    for (int tier = EnchantingTable.maxEssenceItems - 1; tier >= 0; tier--) {
                        numberEssenceRecieved = xpCounter / (int)EnchantmentEssenceBasic.xpPerEssence[tier] * 4 / 5;
                        xpCounter -= (int)EnchantmentEssenceBasic.xpPerEssence[tier] * numberEssenceRecieved;
                        if (xpCounter < (int)EnchantmentEssenceBasic.xpPerEssence[0] && xpCounter > 0 && tier == 0) {
                            xpCounter = 0;
                            numberEssenceRecieved += 1;
                        }

                        Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("PlayerDropItemCheck"), EnchantmentEssenceBasic.IDs[tier], 1);
                    }

                    if (cGlobal.PowerBoosterInstalled)
                        Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("PlayerDropItemCheck"), ModContent.ItemType<PowerBooster>(), 1);

                    if (cGlobal.UltraPowerBoosterInstalled)
                        Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("PlayerDropItemCheck"), ModContent.ItemType<UltraPowerBooster>(), 1);

                    for (int k = 0; k < EnchantingTable.maxEnchantments; k++) {
                        if (!cGlobal.enchantments[k].IsAir) {
                            Main.LocalPlayer.QuickSpawnItem(Main.LocalPlayer.GetSource_Misc("PlayerDropItemCheck"), cGlobal.enchantments[k].type, 1);
                        }
                    }
                }
            }

            consumedItems.Clear();
        }
        public static void CheckRemoveEnchantments(this Item item, Player player) {
            if (!item.TryGetEnchantedItem(out EnchantedItem iGlobal))
                return;

            //Check config
            for (int i = EnchantingTable.maxEnchantments - 1; i >= 0 ; i--) {
                Item enchantment = iGlobal.enchantments[i];
                if (enchantment.IsAir)
                    continue;

                bool slotAllowedByConfig = WEUIItemSlot.SlotAllowedByConfig(item, i);
                if (!slotAllowedByConfig)
                    RemoveEnchantmentNoUpdate(ref iGlobal.enchantments[i], player, $"Slot {i} disabled by config.  Removed {enchantment.Name} from your {item.Name}.");
            }

            //Check enchantment limitations
            List<string> enchantmentTypeNames = new List<string>();
            bool unique = false;
            for (int i = 0; i < EnchantingTable.maxEnchantments; i++) {
                Item enchantmentItem = iGlobal.enchantments[i];

                if (enchantmentItem != null && !enchantmentItem.IsAir && player != null) {
                    ModItem modItem = enchantmentItem.ModItem;
                    if (modItem is UnloadedItem unloadedItem) {
                        RemoveEnchantmentNoUpdate(ref iGlobal.enchantments[i], player, $"Removed Unloaded Item:{unloadedItem.ItemName} from your {item.S()}.  Please inform andro951(WeaponEnchantments).");
                        continue;
                    }

                    if (modItem is not Enchantment enchantment) {
                        RemoveEnchantmentNoUpdate(ref iGlobal.enchantments[i], player, $"Detected a non-enchantment item:{enchantmentItem.S()} on your {item.S()}.  It has been returned to your inventory.");
                        continue;
                    }

                    if (IsWeaponItem(item) && !enchantment.AllowedList.ContainsKey(EItemType.Weapons)) {
                        RemoveEnchantmentNoUpdate(ref iGlobal.enchantments[i], player, enchantmentItem.Name + " is no longer allowed on weapons and has been removed from your " + item.Name + ".");
                    }
                    else if (IsArmorItem(item) && !enchantment.AllowedList.ContainsKey(EItemType.Armor)) {
                        RemoveEnchantmentNoUpdate(ref iGlobal.enchantments[i], player, enchantmentItem.Name + " is no longer allowed on armor and has been removed from your " + item.Name + ".");
                    }
                    else if (IsAccessoryItem(item) && !enchantment.AllowedList.ContainsKey(EItemType.Accessories)) {
                        RemoveEnchantmentNoUpdate(ref iGlobal.enchantments[i], player, enchantmentItem.Name + " is no longer allowed on acessories and has been removed from your " + item.Name + ".");
                    }

                    if (i == EnchantingTable.maxEnchantments - 1 && !enchantment.Utility)
                        RemoveEnchantmentNoUpdate(ref iGlobal.enchantments[i], player, enchantmentItem.Name + " is no longer a utility enchantment and has been removed from your " + item.Name + ".");

                    if (enchantment.RestrictedClass.Count > 0 && enchantment.RestrictedClass.Contains(ContentSamples.ItemsByType[item.type].DamageType.Type))
                        RemoveEnchantmentNoUpdate(ref iGlobal.enchantments[i], player, enchantmentItem.Name + $" is no longer allowed on {item.DamageType.Name} weapons and has removed from your " + item.Name + ".");

                    if (enchantment.Max1 && enchantmentTypeNames.Contains(enchantment.EnchantmentTypeName))
                        RemoveEnchantmentNoUpdate(ref iGlobal.enchantments[i], player, enchantment.EnchantmentTypeName + $" Enchantments are now limmited to 1 per item.  {enchantmentItem.Name} has been removed from your " + item.Name + ".");

                    if (enchantment.Unique) {
                        if (unique) {
                            RemoveEnchantmentNoUpdate(ref iGlobal.enchantments[i], player, enchantment.EnchantmentTypeName + $" Detected multiple uniques on your {item.Name}.  {enchantmentItem.Name} has been removed from your " + item.Name + ".");
                        }
                        else {
                            unique = true;
                        }
                    }

                    enchantmentTypeNames.Add(enchantment.EnchantmentTypeName);
                }
            }

            //Check too many enchantments on item
            if (iGlobal.GetLevelsAvailable() < 0) {
                for (int k = EnchantingTable.maxEnchantments - 1; k >= 0 && iGlobal.GetLevelsAvailable() < 0; k--) {
                    if (!iGlobal.enchantments[k].IsAir)
                        iGlobal.enchantments[k] = player.GetItem(player.whoAmI, iGlobal.enchantments[k], GetItemSettings.LootAllSettings);

                    if (!iGlobal.enchantments[k].IsAir) {
                        player.QuickSpawnItem(player.GetSource_Misc("PlayerDropItemCheck"), iGlobal.enchantments[k]);
                        iGlobal.enchantments[k] = new Item();
                    }
                }

                Main.NewText("Your " + item.Name + "' level is too low to use that many enchantments.");
            }
        }
        private static void RemoveEnchantmentNoUpdate(ref Item enchantmentItem, Player player, string msg) {
            enchantmentItem = player.GetItem(player.whoAmI, enchantmentItem, GetItemSettings.LootAllSettings);
            if (!enchantmentItem.IsAir)
                player.QuickSpawnItem(player.GetSource_Misc("PlayerDropItemCheck"), enchantmentItem);

            enchantmentItem = new Item();
            Main.NewText(msg);
        }
        public static bool IsSameEnchantedItem(this Item item1, Item item2) {
            if (!item1.TryGetEnchantedItem(out EnchantedItem global1) || !item2.TryGetEnchantedItem(out EnchantedItem global2))
                return false;

            if (item1.type != item2.type || item1.prefix != item2.prefix)
                return false;

            if (global1.PowerBoosterInstalled != global2.PowerBoosterInstalled || global1.UltraPowerBoosterInstalled != global2.UltraPowerBoosterInstalled || global1.infusedItemName != global2.infusedItemName)
                return false;

            for (int i = 0; i < EnchantingTable.maxEnchantments; i++) {
                if (global1.enchantments[i].type != global2.enchantments[i].type)
                    return false;
            }

            return true;
        }
        public static void RoundCheck(this StatModifier statModifier, ref float value, int baseValue, StatModifier appliedStatModifier, int contentSample) {
            if (LogMethods.debugging) ($"\\/RoundCheck").Log();
            if (value > baseValue) {
                float checkValue = (float)((int)value + 1) * 1f / statModifier.ApplyTo(1f);
                if ((int)Math.Round(checkValue) == baseValue) {
                    float sampleValue = WEPlayer.CombineStatModifier(appliedStatModifier, statModifier, true).ApplyTo(contentSample);
                    if ((int)Math.Round(sampleValue) == baseValue)
                        value += 0.5f;
                }
            }
            if (LogMethods.debugging) ($"/\\RoundCheck").Log();
        }
        public static void ApplyAllowedList(this Item item, Enchantment enchantment, ref float add, ref float mult, ref float flat, ref float @base) {
            if (IsWeaponItem(item)) {
                if (enchantment.AllowedList.ContainsKey(EItemType.Weapons)) {
                    add *= enchantment.AllowedList[EItemType.Weapons];
                    mult = 1f + (mult - 1f) * enchantment.AllowedList[EItemType.Weapons];
                    flat *= enchantment.AllowedList[EItemType.Weapons];
                    @base *= enchantment.AllowedList[EItemType.Weapons];
                    return;
                }
                else {
                    add = 1f;
                    mult = 1f;
                    flat = 0f;
                    @base = 0f;
                }
            }
            if (IsArmorItem(item)) {
                if (enchantment.AllowedList.ContainsKey(EItemType.Armor)) {
                    add *= enchantment.AllowedList[EItemType.Armor];
                    mult = 1f + (mult - 1f) * enchantment.AllowedList[EItemType.Armor];
                    flat *= enchantment.AllowedList[EItemType.Armor];
                    @base *= enchantment.AllowedList[EItemType.Armor];
                    return;
                }
                else {
                    add = 1f;
                    mult = 1f;
                    flat = 0f;
                    @base = 0f;
                }
            }
            if (IsAccessoryItem(item)) {
                if (enchantment.AllowedList.ContainsKey(EItemType.Accessories)) {
                    add *= enchantment.AllowedList[EItemType.Accessories];
                    mult = 1f + (mult - 1f) * enchantment.AllowedList[EItemType.Accessories];
                    flat *= enchantment.AllowedList[EItemType.Accessories];
                    @base *= enchantment.AllowedList[EItemType.Accessories];
                    return;
                }
                else {
                    add = 1f;
                    mult = 1f;
                    flat = 0f;
                    @base = 0f;
                }
            }
        }//d
    }
}
