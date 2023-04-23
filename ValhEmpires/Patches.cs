using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;

namespace ValkyrieUtils
{
    class Patches
    {
		[HarmonyPatch(typeof(Character), "SetHealth")]
		private class InterceptDeath
		{
			// Token: 0x06000019 RID: 25 RVA: 0x000027D0 File Offset: 0x000009D0
			[<8050e37c-e25e-4f87-8c12-6679b01943f2>NullableContext(1)]
			private static void Prefix(Character __instance, float health)
			{
				if (__instance == Player.m_localPlayer && __instance.GetHealth() > 0f && health <= 0f)
				{
					if (__instance.IsSwiming())
					{
						Patches.BroadcastDeath(Patches.getRandomText(new string[]
						{
							"death by drowning"
						}));
						return;
					}
					if (Patches.SaveCauseOfDeath.deathHit == null)
					{
						return;
					}
					HashSet<string> hashSet = new HashSet<string>();
					if (Patches.SaveCauseOfDeath.deathHit.GetAttacker() != null)
					{
						if (Patches.SaveCauseOfDeath.deathHit.GetAttacker().IsBoss())
						{
							hashSet.Add("death by boss");
						}
						else if (Patches.SaveCauseOfDeath.deathHit.GetAttacker().IsPlayer())
						{
							hashSet.Add("death by player");
						}
						else
						{
							hashSet.Add("death by creature");
						}
					}
					if (Patches.SaveCauseOfDeath.deathHit.m_damage.m_blunt > 0f)
					{
						hashSet.Add("death by blunt");
						hashSet.Add("death by physical");
					}
					if (Patches.SaveCauseOfDeath.deathHit.m_damage.m_pierce > 0f)
					{
						hashSet.Add("death by pierce");
						hashSet.Add("death by physical");
					}
					if (Patches.SaveCauseOfDeath.deathHit.m_damage.m_slash > 0f)
					{
						hashSet.Add("death by slash");
						hashSet.Add("death by physical");
					}
					if (Patches.SaveCauseOfDeath.deathHit.m_damage.m_fire > 0f)
					{
						hashSet.Add("death by fire");
						hashSet.Add("death by elemental");
					}
					if (Patches.SaveCauseOfDeath.deathHit.m_damage.m_frost > 0f)
					{
						hashSet.Add("death by frost");
						hashSet.Add("death by elemental");
					}
					if (Patches.SaveCauseOfDeath.deathHit.m_damage.m_lightning > 0f)
					{
						hashSet.Add("death by lightning");
						hashSet.Add("death by elemental");
					}
					if (Patches.SaveCauseOfDeath.deathHit.m_damage.m_poison > 0f)
					{
						hashSet.Add("death by poison");
						hashSet.Add("death by elemental");
					}
					if (Patches.SaveCauseOfDeath.deathHit.m_skill == Skills.SkillType.WoodCutting)
					{
						hashSet.Add("death by tree");
					}
					if (Patches.SetGravityFlag.fallDamageTaken)
					{
						hashSet.Add("death by gravity");
					}
					if (Patches.SetFreezingFlag.freezingDamageTaken)
					{
						hashSet.Add("death by frost");
					}
					Character attacker = Patches.SaveCauseOfDeath.deathHit.GetAttacker();
					if (attacker != null)
					{
						ValkyrieUtils.BroadcastDeath(hashSet.ToArray<string>()));
						return;
					}
					Patches.BroadcastDeath(Patches.getRandomText(hashSet.ToArray<string>()));
				}
			}
		}

		// Token: 0x0200000B RID: 11
		[HarmonyPatch(typeof(SE_Stats), "UpdateStatusEffect")]
		[<8050e37c-e25e-4f87-8c12-6679b01943f2>NullableContext(0)]
		public class SetFreezingFlag
		{
			// Token: 0x0600001B RID: 27 RVA: 0x00002A54 File Offset: 0x00000C54
			[<8050e37c-e25e-4f87-8c12-6679b01943f2>NullableContext(1)]
			private static void Prefix(SE_Stats __instance)
			{
				if (__instance.name == "Freezing")
				{
					Patches.SetFreezingFlag.freezingDamageTaken = true;
				}
			}

			// Token: 0x0600001C RID: 28 RVA: 0x00002A70 File Offset: 0x00000C70
			private static void Finalizer()
			{
				Patches.SetFreezingFlag.freezingDamageTaken = false;
			}

			// Token: 0x04000032 RID: 50
			public static bool freezingDamageTaken;
		}

		// Token: 0x0200000C RID: 12
		[<8050e37c-e25e-4f87-8c12-6679b01943f2>NullableContext(0)]
		[HarmonyPatch(typeof(Character), "UpdateGroundContact")]
		public class SetGravityFlag
		{
			// Token: 0x0600001E RID: 30 RVA: 0x00002A80 File Offset: 0x00000C80
			private static void Prefix()
			{
				Patches.SetGravityFlag.fallDamageTaken = true;
			}

			// Token: 0x0600001F RID: 31 RVA: 0x00002A88 File Offset: 0x00000C88
			private static void Finalizer()
			{
				Patches.SetGravityFlag.fallDamageTaken = false;
			}

			// Token: 0x04000033 RID: 51
			public static bool fallDamageTaken;
		}

		// Token: 0x0200000D RID: 13
		[<8050e37c-e25e-4f87-8c12-6679b01943f2>NullableContext(0)]
		[HarmonyPatch(typeof(ImpactEffect), "OnCollisionEnter")]
		public class SetTreeFlag
		{
			// Token: 0x06000021 RID: 33 RVA: 0x00002A98 File Offset: 0x00000C98
			[<8050e37c-e25e-4f87-8c12-6679b01943f2>NullableContext(1)]
			private static void Prefix(ImpactEffect __instance)
			{
				if (__instance.GetComponent<TreeLog>())
				{
					Patches.SetTreeFlag.hitByTree = true;
				}
			}

			// Token: 0x06000022 RID: 34 RVA: 0x00002AB0 File Offset: 0x00000CB0
			private static void Finalizer()
			{
				Patches.SetTreeFlag.hitByTree = false;
			}

			// Token: 0x04000034 RID: 52
			public static bool hitByTree;
		}

		// Token: 0x0200000E RID: 14
		[<8050e37c-e25e-4f87-8c12-6679b01943f2>NullableContext(0)]
		[HarmonyPatch(typeof(Character), "Damage")]
		public class HitByTree
		{
			// Token: 0x06000024 RID: 36 RVA: 0x00002AC0 File Offset: 0x00000CC0
			[<8050e37c-e25e-4f87-8c12-6679b01943f2>NullableContext(1)]
			private static void Prefix(Character __instance, HitData hit)
			{
				if (Patches.SetTreeFlag.hitByTree)
				{
					Player player = __instance as Player;
					if (player != null && player.GetHealth() > 0f)
					{
						hit.m_skill = Skills.SkillType.WoodCutting;
					}
				}
			}
		}

		// Token: 0x0200000F RID: 15
		[<8050e37c-e25e-4f87-8c12-6679b01943f2>NullableContext(0)]
		[HarmonyPatch(typeof(Character), "ApplyDamage")]
		private class SaveCauseOfDeath
		{
			// Token: 0x06000026 RID: 38 RVA: 0x00002AFC File Offset: 0x00000CFC
			[<8050e37c-e25e-4f87-8c12-6679b01943f2>NullableContext(1)]
			private static void Prefix(HitData hit)
			{
				Patches.SaveCauseOfDeath.deathHit = hit;
			}

			// Token: 0x06000027 RID: 39 RVA: 0x00002B04 File Offset: 0x00000D04
			private static void Finalizer()
			{
				Patches.SaveCauseOfDeath.deathHit = null;
			}

			// Token: 0x04000035 RID: 53
			[<db975f0c-cbc4-4111-b9b0-908a57b487b7>Nullable(2)]
			public static HitData deathHit;
		}
	}
}
