﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WeaponEnchantments.Common;
using WeaponEnchantments.Common.Utility;
using static WeaponEnchantments.WEPlayer;

namespace WeaponEnchantments.Effects {
    public class MiningSpeed : StatEffect, IVanillaStat {
        public MiningSpeed(DifficultyStrength additive = null, DifficultyStrength multiplicative = null, DifficultyStrength flat = null, DifficultyStrength @base = null) : base(additive, multiplicative, flat, @base) {

        }
        public MiningSpeed(EStatModifier eStatModifier) : base(eStatModifier) { }
        public override EnchantmentEffect Clone() {
            return new MiningSpeed(EStatModifier.Clone());
        }

        public override EnchantmentStat statName => EnchantmentStat.MiningSpeed;
		public override string TooltipValue => EStatModifier.SmartTooltip;
	}
}
