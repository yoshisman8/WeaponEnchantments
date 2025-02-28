﻿using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using WeaponEnchantments.Common.Utility;
using static WeaponEnchantments.WEPlayer;

namespace WeaponEnchantments.Common
{
	public class EStatModifier {
		//public static readonly EStatModifier Default = new EStatModifier(1f, 1f, 0f, 0f);
		public EnchantmentStat StatType { get; private set; }

		/// <summary>
		/// Increase to the base value of the stat. Directly added to the stat before multipliers are applied.
		/// </summary>
		public float Base {
			get {
				if (_waitingForEnterWorld)
					SetUpAutomaticStrengthFromWorldDificulty();

				return _base;
			}
			set {
				if (value != originalBase) {
					originalBase = value;
					_base = value * EfficiencyMultiplier;
					_strength = 0f;
				}
			}
		}
		private float originalBase;
		private float _base;

		/// <summary>
		/// The combination of all additive multipliers. Starts at 1
		/// </summary>
		public float Additive {
			get {
				if (_waitingForEnterWorld)
					SetUpAutomaticStrengthFromWorldDificulty();

				return _additive;
			}
			set {
				if (value != originalAdditive) {
					originalAdditive = value;
					_additive = 1f + value * EfficiencyMultiplier;
					_strength = 0f;
				}
			}
		}
		private float originalAdditive;
		private float _additive;

		/// <summary>
		/// The combination of all multiplicative multipliers. Starts at 1. Applies 'after' all additive bonuses have been accumulated.
		/// </summary>
		public float Multiplicative {
			get {
				if (_waitingForEnterWorld)
					SetUpAutomaticStrengthFromWorldDificulty();

				return _multiplicative;
			}
			set {
				if (value != originalMultiplicative) {
					originalMultiplicative = value;
					_multiplicative = 1f + (value - 1f) * EfficiencyMultiplier;
					_strength = 0f;
				}
			}
		}
		private float originalMultiplicative;
		private float _multiplicative;

		/// <summary>
		/// Increase to the final value of the stat. Directly added to the stat after multipliers are applied.
		/// </summary>
		public float Flat {
			get {
				if (_waitingForEnterWorld)
					SetUpAutomaticStrengthFromWorldDificulty();

				return _flat;
			}
			set {
				if (value != originalFlat) {
					originalFlat = value;
					_flat = value * EfficiencyMultiplier;
					_strength = 0f;
				}
			}
		}
		private float originalFlat;
		private float _flat;

		/// <summary>
		/// Modifies Additive, Multiplicative, Base and Flat.<br/>
		/// 1f is subtracted from Additive and Multiplicative before being modfied and added back after.<br/>
		/// </summary>
		public float EfficiencyMultiplier {
			get => _efficiencyMultiplier;
			set {
				if (value != _efficiencyMultiplier) {
					if (_waitingForEnterWorld)
						SetUpAutomaticStrengthFromWorldDificulty();

					_efficiencyMultiplier = value;
					_additive = 1f + originalAdditive * value;
					_multiplicative = 1f + (originalMultiplicative - 1f) * _efficiencyMultiplier;
					_flat = originalFlat * value;
					_base = originalBase * value;
					_strength = 0f;
				}
			}
		}
		private float _efficiencyMultiplier;

		private DifficultyStrength _automaticStrengthData;
		byte _statTypeID;
		private bool _waitingForEnterWorld;

		/// <summary>
		/// The total of the EStatModifier
		/// </summary>
		public float Strength {
			get {
				if (_waitingForEnterWorld)
					SetUpAutomaticStrengthFromWorldDificulty();

				if (_strength != 0f)
					return _strength;

				if (_base > 0f || _flat > 0f) {
					_strength = ApplyTo(0f);
				}
				else {
					_strength = ApplyTo(1f);
				}

				return _strength;
			}
			private set {
				_strength = value;
				tooltip = null;
			}
		}
		private float _strength;
		private CombineModeID _combineModeID;

		public string SmartTooltip {
			get {
				if (_waitingForEnterWorld)
					SetUpAutomaticStrengthFromWorldDificulty();

				if (_additive != 1f)
					return SignPercentMult100Minus1Tooltip;

				return SignTooltip;
			}
		}

		private string FlatTooltip => _flat > 0f ? _additive > 0f || _multiplicative > 0f || _base > 0f ? ", +" : "" + $"{(float)Math.Round(_flat, 3)}" : "";

		private string tooltip;
		public string SignPercentMult100Tooltip => GetTootlip(true, true, true);
		public string SignPercentTooltip => GetTootlip(true, true, false);
		public string SignMult100Tooltip => GetTootlip(true, false, true);
		public string SignTooltip => GetTootlip(true, false, false);
		public string PercentMult100Tooltip => GetTootlip(false, true, true);
		public string PercentTooltip => GetTootlip(false, true, false);
		public string Mult100Tooltip => GetTootlip(false, false, true);
		public string NoneTooltip => GetTootlip(false, false, false);
		public string SignPercentMult100Minus1Tooltip => GetTootlip(true, true, true, true);
		public string SignPercentMinus1Tooltip => GetTootlip(true, true, false, true);
		public string SignMult100Minus1Tooltip => GetTootlip(true, false, true, true);
		public string SignMinus1Tooltip => GetTootlip(true, false, false, true);
		public string PercentMult100Minus1Tooltip => GetTootlip(false, true, true, true);
		public string PercentMinus1Tooltip => GetTootlip(false, true, false, true);
		public string Mult100Minus1Tooltip => GetTootlip(false, false, true, true);
		public string Minus1Tooltip => GetTootlip(false, false, false, true);

		private string GetTootlip(bool sign, bool percent, bool multiply100, bool minusOne = false) {
			if (_waitingForEnterWorld)
				SetUpAutomaticStrengthFromWorldDificulty();

			if (tooltip == null || _strength == 0) {
				float baseTooltip;
				if (minusOne && _base == 0f && _additive != 1f) {
					baseTooltip = (float)Math.Round(_additive * _multiplicative - 1f, 3);
				}
				else {
					baseTooltip = (float)Math.Round(Strength - _flat, 3);
				}

				tooltip = "";
				if (_base > 0f || _additive != 1f && minusOne) {
					if (sign)
						tooltip += "+";

					tooltip += $"{(multiply100 ? baseTooltip * 100f : baseTooltip)}";
				}
				else {
					tooltip += $"{baseTooltip}";
					if (sign)
						tooltip += "x";
				}

				if (percent)
					tooltip += "%";

				tooltip += FlatTooltip;
			}

			return tooltip;
		}
		/*
		string final = "";
        float mult = EStatModifier.Multiplicative + EStatModifier.Additive - 2;
        float flats = EStatModifier.Base * mult + EStatModifier.Flat;

        if (flats > 0f) {
            final += $"{s(flats)}{flats}";
        }

        if (mult > 0f) {
            if (final != "") final += ' ';
            final += $"{s(mult)}{mult.Percent()}%";
        }

        return final;
		*/

		public StatModifier StatModifier => new StatModifier(_additive, _multiplicative, _flat, _base);
		public EStatModifier(EnchantmentStat statType, float additive = 0f, float multiplicative = 1f, float flat = 0f, float @base = 0f, float baseEfficiencyMultiplier = 1f, CombineModeID combineModeID = CombineModeID.Normal) {
			StatType = statType;
			originalAdditive = additive;
			originalMultiplicative = multiplicative;
			originalFlat = flat;
			originalBase = @base;
			_efficiencyMultiplier = baseEfficiencyMultiplier;
			_additive = 1f + additive * _efficiencyMultiplier;
			_multiplicative = 1f + (multiplicative - 1f) * _efficiencyMultiplier;
			_flat = flat * _efficiencyMultiplier;
			_base = @base * _efficiencyMultiplier;
			_strength = 0f;
			tooltip = null;
			_combineModeID = combineModeID;
		}
		public EStatModifier(EnchantmentStat statType, DifficultyStrength additive = null, DifficultyStrength multiplicative = null, DifficultyStrength flat = null, DifficultyStrength @base = null, float baseEfficiencyMultiplier = 1f, CombineModeID combineModeID = CombineModeID.Normal) {
			_waitingForEnterWorld = true;
			DifficultyStrength[] arr = { additive?.Clone(), multiplicative?.Clone(), flat?.Clone(), @base?.Clone() };
			for (byte i = 0; i < arr.Length; i++) {
				if (arr[i] != null) {
					_automaticStrengthData = arr[i];
					_statTypeID = i;
					break;
				}
			}

			StatType = statType;
			originalAdditive = 0f;
			originalMultiplicative = 1f;
			originalFlat = 0f;
			originalBase = 0f;
			_efficiencyMultiplier = baseEfficiencyMultiplier;
			_additive = 1f;
			_multiplicative = 1f;
			_flat = 0f;
			_base = 0f;
			_strength = 0f;
			tooltip = null;
			_combineModeID = combineModeID;
		}
		public EStatModifier(EnchantmentStat statType, DifficultyStrength automaticStrengthData, byte statTypeID, float baseEfficiencyMultiplier = 1f, CombineModeID combineModeID = CombineModeID.Normal) {
			_waitingForEnterWorld = true;
			_automaticStrengthData = automaticStrengthData;
			_statTypeID = statTypeID;
			StatType = statType;
			originalAdditive = 0f;
			originalMultiplicative = 1f;
			originalFlat = 0f;
			originalBase = 0f;
			_efficiencyMultiplier = baseEfficiencyMultiplier;
			_additive = 1f;
			_multiplicative = 1f;
			_flat = 0f;
			_base = 0f;
			_strength = 0f;
			tooltip = null;
			_combineModeID = combineModeID;
		}

		private void SetUpAutomaticStrengthFromWorldDificulty() {
			int index = Main.gameMenu ? 0 : _automaticStrengthData.AllValues.Length == 4 ? Main.GameMode : 0;
			switch (_statTypeID) {
				case 0:
					originalAdditive = _automaticStrengthData.AllValues[index];
					_additive = 1f + originalAdditive * _efficiencyMultiplier;
					break;
				case 1:
					originalMultiplicative = _automaticStrengthData.AllValues[index];
					_multiplicative = 1f + (originalMultiplicative - 1f) * _efficiencyMultiplier;
					break;
				case 2:
					originalFlat = _automaticStrengthData.AllValues[index];
					_flat = originalFlat * _efficiencyMultiplier;
					break;
				case 3:
					originalBase = _automaticStrengthData.AllValues[index];
					_base = originalBase * _efficiencyMultiplier;
					break;
			}

			if (!Main.gameMenu) {
				_waitingForEnterWorld = false;
			}
		}

		/*public override bool Equals(object obj) {
			if (obj is not EStatModifier m)
				return false;

			return this == m;
		}

		public override int GetHashCode() {
			int hashCode = 1713062080;
			hashCode = hashCode * -1521134295 + _additive.GetHashCode();
			hashCode = hashCode * -1521134295 + _multiplicative.GetHashCode();
			hashCode = hashCode * -1521134295 + _flat.GetHashCode();
			hashCode = hashCode * -1521134295 + _base.GetHashCode();
			return hashCode;
		}

		/// <summary>
		/// By using the add operator, the supplied additive modifier is combined with the existing modifiers. For example, adding 0.12f would be equivalent to a typical 12% damage boost. For 99% of effects used in the game, this approach is used.
		/// </summary>
		/// <param name="m"></param>
		/// <param name="add">The additive modifier to add, where 0.01f is equivalent to 1%</param>
		/// <returns></returns>
		public static EStatModifier operator +(EStatModifier m, float add)
			=> new EStatModifier(m.StatType, m._additive + add, m._multiplicative, m._flat, m._base);

		/// <summary>
		/// By using the subtract operator, the supplied subtractive modifier is combined with the existing modifiers. For example, subtracting 0.12f would be equivalent to a typical 12% damage decrease. For 99% of effects used in the game, this approach is used.
		/// </summary>
		/// <param name="m"></param>
		/// <param name="sub">The additive modifier to subtract, where 0.01f is equivalent to 1%</param>
		/// <returns></returns>
		public static EStatModifier operator -(EStatModifier m, float sub)
			=> new EStatModifier(m.StatType, m._additive - sub, m._multiplicative, m._flat, m._base);

		/// <summary>
		/// The multiply operator applies a multiplicative effect to the resulting multiplicative modifier. This effect is very rarely used, typical effects use the add operator.
		/// </summary>
		/// <param name="m"></param>
		/// <param name="mul">The factor by which the multiplicative modifier is scaled</param>
		/// <returns></returns>
		public static EStatModifier operator *(EStatModifier m, float mul)
			=> new EStatModifier(m.StatType, m._additive, m._multiplicative * mul, m._flat, m._base);

		public static EStatModifier operator /(EStatModifier m, float div)
			=> new EStatModifier(m.StatType, m._additive, m._multiplicative / div, m._flat, m._base);

		public static EStatModifier operator +(float add, EStatModifier m)
			=> m + add;

		public static EStatModifier operator *(float mul, EStatModifier m)
			=> m * mul;

		public static bool operator ==(EStatModifier m1, EStatModifier m2)
			=> m1.Additive == m2.Additive && m1.Multiplicative == m2.Multiplicative && m1.Flat == m2.Flat && m1.Base == m2.Base;

		public static bool operator !=(EStatModifier m1, EStatModifier m2)
			=> m1.Additive != m2.Additive || m1.Multiplicative != m2.Multiplicative || m1.Flat != m2.Flat || m1.Base != m2.Base;*/

		public float ApplyTo(float baseValue) {
			if (_waitingForEnterWorld)
				SetUpAutomaticStrengthFromWorldDificulty();
			
			return (baseValue + _base) * _additive * _multiplicative + _flat;
		}
		public float InvertApplyTo(float baseValue) {
			if (_waitingForEnterWorld)
				SetUpAutomaticStrengthFromWorldDificulty();

			return (baseValue - _base) / _additive / _multiplicative - _flat;
		}
		public void ApplyTo(ref float baseValue) {
			if (_waitingForEnterWorld)
				SetUpAutomaticStrengthFromWorldDificulty();

			baseValue = (baseValue + _base) * _additive * _multiplicative + _flat;
		}
		public void ApplyTo(ref int baseValue) {
			if (_waitingForEnterWorld)
				SetUpAutomaticStrengthFromWorldDificulty();

			baseValue = (int)Math.Round(((float)baseValue + _base) * _additive * _multiplicative + _flat);
		}
		public void ApplyTo(ref float flat, ref float mult, Item item) {
			if (_waitingForEnterWorld)
				SetUpAutomaticStrengthFromWorldDificulty();

			flat += _base;
			mult *= _additive * _multiplicative;

			if (_flat != 0f) {
				float sampleMana = (float)ContentSamples.ItemsByType[item.type].mana;
				mult += _flat / sampleMana;
			}
		}
		public void CombineWith(EStatModifier m) {
			if (_waitingForEnterWorld)
				SetUpAutomaticStrengthFromWorldDificulty();

			_additive += m.Additive - 1f;
			_multiplicative *= m.Multiplicative;

			switch (_combineModeID) {
				case CombineModeID.MultiplicativePartOf1:
					_flat = 1f - ((1f - _flat) * (1f - m.Flat));
					_base = 1f - ((1f - _base) * (1f - m.Base));
					break;
				case CombineModeID.Normal:
				default:
					_flat += m.Flat;
					_base += m.Base;
					break;
			}

			_strength = 0f;
			tooltip = null;
		}

		public StatModifier CombineWith(StatModifier m) {
			if (_waitingForEnterWorld)
				SetUpAutomaticStrengthFromWorldDificulty();

			return new StatModifier(_additive + m.Additive - 1f, _multiplicative * m.Multiplicative, _flat + m.Flat, _base + m.Base);
		}

		public StatModifier Scale(float scale) {
			if (_waitingForEnterWorld)
				SetUpAutomaticStrengthFromWorldDificulty();

			return new StatModifier(1f + (_additive - 1f) * scale, 1f + (_multiplicative - 1f) * scale, _flat * scale, _base * scale);
		}

		/*
		public EStatModifier Clone() {
			if (_waitingForEnterWorld)
				SetUpAutomaticStrengthFromWorldDificulty();

			return new EStatModifier(StatType, _additive - 1f, _multiplicative, _flat, _base, combineModeID: _combineModeID);
		}
		*/
		public EStatModifier Clone() {
			if (_waitingForEnterWorld)
				return new EStatModifier(StatType, _automaticStrengthData, _statTypeID, EfficiencyMultiplier, _combineModeID);

			return new EStatModifier(StatType, _additive - 1f, _multiplicative, _flat, _base, EfficiencyMultiplier, _combineModeID);
		}

		public override string ToString() {
			return $"{StatType}, A: {_additive}, M: {_multiplicative}, F: {_flat}, B: {_base}, combineMode: {_combineModeID}";
		}
	}
}
