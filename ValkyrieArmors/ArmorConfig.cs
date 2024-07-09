using System.Collections.Generic;


namespace ValkyrieArmors
{
    public class ArmorConfig
    {
        public string verboseSetName;
        public string setName;
        public string helmetPrefab;
        public string chestPrefab;
        public string legsPrefab;
        public string capePrefab;
        public int hp;
        public float hpRegen;
        public int stamina;
        public float staminaRegen;
        public int eitr;
        public float eitrRegen;
        public int armor;
        public int armorPerLevel;
        public float movementSpeed;
        public int stealthLevels;
        public float carryWeight;
        public Dictionary<string, long> weaponDamageMultipliers;
        public float staminaRunJumpReduction;
        public float damageReduction;
    }
}
