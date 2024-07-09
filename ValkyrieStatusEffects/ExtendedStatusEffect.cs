using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ValkyrieStatusEffects
{
    public class ExtendedStatusEffect : SE_Stats
    {
        public float m_maxHealth;
        public float m_maxStamina;
        public float m_maxEitr;
        public float m_damageReduction;
        public static Dictionary<Skills.SkillType, float> m_weaponDamageModifiers = new Dictionary<Skills.SkillType, float>();
        public float m_healingMultiplier;

        public static string GetWeaponDamageModifiersTooltip()
        {
            if (m_weaponDamageModifiers.Count == 0)
                return "";
            string text = "";
            foreach (KeyValuePair<Skills.SkillType, float> wdm in m_weaponDamageModifiers)
            {
                if (wdm.Value != 0)
                {
                    switch (wdm.Key)
                    {
                        case Skills.SkillType.Axes:
                            text += "\n$skill_axes: ";
                            break;
                        case Skills.SkillType.BloodMagic:
                            text += "\n$skill_bloodmagic: ";
                            break;
                        case Skills.SkillType.Bows:
                            text += "\n$skill_bows: ";
                            break;
                        case Skills.SkillType.Clubs:
                            text += "\n$skill_clubs: ";
                            break;
                        case Skills.SkillType.Crossbows:
                            text += "\n$skill_crossbows: ";
                            break;
                        case Skills.SkillType.ElementalMagic:
                            text += "\n$skill_elementalmagic: ";
                            break;
                        case Skills.SkillType.Knives:
                            text += "\n$skill_knives: ";
                            break;
                        case Skills.SkillType.Pickaxes:
                            text += "\n$skill_pickaxes: ";
                            break;
                        case Skills.SkillType.Polearms:
                            text += "\n$skill_polearms: ";
                            break;
                        case Skills.SkillType.Spears:
                            text += "\n$skill_spears: ";
                            break;
                        case Skills.SkillType.Swords:
                            text += "\n$skill_swords: ";
                            break;
                        case Skills.SkillType.Unarmed:
                            text += "\n$skill_unarmed: ";
                            break;
                    }
                    text += $"<color=orange>{(wdm.Value > 0 ? '+' : '-')}{wdm.Value * 100}% DMG</color>";
                }
            }
            return text;
        }

        public override string GetTooltipString()
        {
            StringBuilder stringBuilder = new StringBuilder(256);
            if (m_tooltip.Length > 0)
            {
                stringBuilder.AppendFormat("{0}\n", m_tooltip);
            }
            if (m_runStaminaDrainModifier != 0f)
            {
                stringBuilder.AppendFormat("$se_runstamina: <color=orange>{0}%</color>\n", (m_runStaminaDrainModifier * 100f).ToString("+0;-0"));
            }
            if (m_maxHealth != 0f)
            {
                stringBuilder.AppendFormat("HP: <color=orange>{0}</color>\n", m_maxHealth.ToString());
            }
            if (m_healthOverTime != 0f)
            {
                stringBuilder.AppendFormat("$se_health: <color=orange>{0}</color>\n", m_healthOverTime.ToString());
            }
            if (m_maxStamina != 0f)
            {
                stringBuilder.AppendFormat("Stamina: <color=orange>{0}</color>\n", m_maxStamina.ToString());
            }
            if (m_staminaOverTime != 0f)
            {
                stringBuilder.AppendFormat("$se_stamina: <color=orange>{0}</color>\n", m_staminaOverTime.ToString());
            }
            if (m_maxEitr != 0f)
            {
                stringBuilder.AppendFormat("Eitr: <color=orange>{0}</color>\n", m_maxEitr.ToString());
            }
            if (m_eitrOverTime != 0f)
            {
                stringBuilder.AppendFormat("$se_eitr: <color=orange>{0}</color>\n", m_eitrOverTime.ToString());
            }
            if (m_healthRegenMultiplier != 1f)
            {
                stringBuilder.AppendFormat("$se_healthregen: <color=orange>{0}%</color>\n", ((m_healthRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
            }
            if (m_staminaRegenMultiplier != 1f)
            {
                stringBuilder.AppendFormat("$se_staminaregen: <color=orange>{0}%</color>\n", ((m_staminaRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
            }
            if (m_eitrRegenMultiplier != 1f)
            {
                stringBuilder.AppendFormat("$se_eitrregen: <color=orange>{0}%</color>\n", ((m_eitrRegenMultiplier - 1f) * 100f).ToString("+0;-0"));
            }
            if (m_addMaxCarryWeight != 0f)
            {
                stringBuilder.AppendFormat("$se_max_carryweight: <color=orange>{0}</color>\n", m_addMaxCarryWeight.ToString("+0;-0"));
            }
            if (m_mods.Count > 0)
            {
                stringBuilder.Append(GetDamageModifiersTooltipString(m_mods));
                stringBuilder.Append("\n");
            }
            if (m_noiseModifier != 0f)
            {
                stringBuilder.AppendFormat("$se_noisemod: <color=orange>{0}%</color>\n", (m_noiseModifier * 100f).ToString("+0;-0"));
            }
            if (m_stealthModifier != 0f)
            {
                stringBuilder.AppendFormat("$se_sneakmod: <color=orange>{0}%</color>\n", (m_stealthModifier * 100f).ToString("+0;-0"));
            }
            if (m_speedModifier != 0f)
            {
                stringBuilder.AppendFormat("$item_movement_modifier: <color=orange>{0}%</color>\n", (m_speedModifier * 100f).ToString("+0;-0"));
            }
            if (m_maxMaxFallSpeed != 0f)
            {
                stringBuilder.AppendFormat("$item_limitfallspeed: <color=orange>{0}m/s</color>\n", m_maxMaxFallSpeed.ToString("0"));
            }
            if (m_fallDamageModifier != 0f)
            {
                stringBuilder.AppendFormat("$item_falldamage: <color=orange>{0}%</color>\n", (m_fallDamageModifier * 100f).ToString("+0;-0"));
            }
            if (m_jumpModifier.y != 0f)
            {
                stringBuilder.AppendFormat("$se_jumpheight: <color=orange>{0}%</color>\n", (m_jumpModifier.y * 100f).ToString("+0;-0"));
            }
            if (m_jumpModifier.x != 0f || m_jumpModifier.z != 0f)
            {
                stringBuilder.AppendFormat("$se_jumplength: <color=orange>{0}%</color>\n", (Mathf.Max(m_jumpModifier.x, m_jumpModifier.z) * 100f).ToString("+0;-0"));
            }
            if (m_jumpStaminaUseModifier != 0f)
            {
                stringBuilder.AppendFormat("$se_jumpstamina: <color=orange>{0}%</color>\n", (m_jumpStaminaUseModifier * 100f).ToString("+0;-0"));
            }
            if (m_attackStaminaUseModifier != 0f)
            {
                stringBuilder.AppendFormat("$se_attackstamina: <color=orange>{0}%</color>\n", (m_attackStaminaUseModifier * 100f).ToString("+0;-0"));
            }
            if (m_blockStaminaUseModifier != 0f)
            {
                stringBuilder.AppendFormat("$se_blockstamina: <color=orange>{0}%</color>\n", (m_blockStaminaUseModifier * 100f).ToString("+0;-0"));
            }
            if (m_dodgeStaminaUseModifier != 0f)
            {
                stringBuilder.AppendFormat("$se_dodgestamina: <color=orange>{0}%</color>\n", (m_dodgeStaminaUseModifier * 100f).ToString("+0;-0"));
            }
            if (m_swimStaminaUseModifier != 0f)
            {
                stringBuilder.AppendFormat("$se_swimstamina: <color=orange>{0}%</color>\n", (m_dodgeStaminaUseModifier * 100f).ToString("+0;-0"));
            }
            if (m_homeItemStaminaUseModifier != 0f)
            {
                stringBuilder.AppendFormat("$base_item_modifier: <color=orange>{0}%</color>\n", (m_homeItemStaminaUseModifier * 100f).ToString("+0;-0"));
            }
            if (m_sneakStaminaUseModifier != 0f)
            {
                stringBuilder.AppendFormat("$se_sneakstamina: <color=orange>{0}%</color>\n", (m_sneakStaminaUseModifier * 100f).ToString("+0;-0"));
            }
            if (m_runStaminaUseModifier != 0f)
            {
                stringBuilder.AppendFormat("$se_runstamina: <color=orange>{0}%</color>\n", (m_runStaminaUseModifier * 100f).ToString("+0;-0"));
            }
            if (m_skillLevel != 0 && m_skillLevelModifier != 0)
            {
                stringBuilder.AppendFormat("{0} <color=orange>{1}</color>\n", Localization.instance.Localize("$skill_" + m_skillLevel.ToString().ToLower()), m_skillLevelModifier.ToString("+0;-0"));
            }
            if (m_skillLevel2 != 0 && m_skillLevelModifier2 != 0)
            {
                stringBuilder.AppendFormat("{0} <color=orange>{1}</color>\n", Localization.instance.Localize("$skill_" + m_skillLevel2.ToString().ToLower()), m_skillLevelModifier2.ToString("+0;-0"));
            }
            if (m_percentigeDamageModifiers.m_blunt != 0f)
            {
                stringBuilder.AppendFormat("$inventory_blunt: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_blunt * 100f).ToString("+0;-0"));
            }
            if (m_percentigeDamageModifiers.m_slash != 0f)
            {
                stringBuilder.AppendFormat("$inventory_slash: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_slash * 100f).ToString("+0;-0"));
            }
            if (m_percentigeDamageModifiers.m_pierce != 0f)
            {
                stringBuilder.AppendFormat("$inventory_pierce: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_pierce * 100f).ToString("+0;-0"));
            }
            if (m_percentigeDamageModifiers.m_chop != 0f)
            {
                stringBuilder.AppendFormat("$inventory_chop: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_chop * 100f).ToString("+0;-0"));
            }
            if (m_percentigeDamageModifiers.m_pickaxe != 0f)
            {
                stringBuilder.AppendFormat("$inventory_pickaxe: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_pickaxe * 100f).ToString("+0;-0"));
            }
            if (m_percentigeDamageModifiers.m_fire != 0f)
            {
                stringBuilder.AppendFormat("$inventory_fire: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_fire * 100f).ToString("+0;-0"));
            }
            if (m_percentigeDamageModifiers.m_frost != 0f)
            {
                stringBuilder.AppendFormat("$inventory_frost: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_frost * 100f).ToString("+0;-0"));
            }
            if (m_percentigeDamageModifiers.m_lightning != 0f)
            {
                stringBuilder.AppendFormat("$inventory_lightning: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_lightning * 100f).ToString("+0;-0"));
            }
            if (m_percentigeDamageModifiers.m_poison != 0f)
            {
                stringBuilder.AppendFormat("$inventory_poison: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_poison * 100f).ToString("+0;-0"));
            }
            if (m_percentigeDamageModifiers.m_spirit != 0f)
            {
                stringBuilder.AppendFormat("$inventory_spirit: <color=orange>{0}%</color>\n", (m_percentigeDamageModifiers.m_spirit * 100f).ToString("+0;-0"));
            }
            if (m_damageReduction != 0)
            {
                stringBuilder.AppendFormat("Reducción de daño: <color=orange>{0}%</color>\n", (m_damageReduction * 100f).ToString("+0;-0"));
            }
            if (m_weaponDamageModifiers.Count > 0)
            {
                stringBuilder.Append(GetWeaponDamageModifiersTooltip());
                stringBuilder.Append("\n");
            }
            return stringBuilder.ToString();
        }

    }
}
