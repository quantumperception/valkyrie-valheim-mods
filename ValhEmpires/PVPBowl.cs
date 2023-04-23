using UnityEngine;

namespace ValkyrieUtils
{
    class PVPBowl : MonoBehaviour
    {
        string m_name = "Entrega tu Ticket PVP";
        string m_useItemText;

        public string GetHoverText()
        {
            return Localization.instance.Localize(this.m_name + "\n[<color=yellow><b>1-8</b></color>] " + this.m_useItemText);
        }

        public string GetHoverName()
        {
            return this.m_name;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            if (hold)
            {
                return false;
            }
            //if (this.IsBossSpawnQueued())
            //{
            //	return false;
            //}
            //if (this.m_useItemStands)
            //{
            //	List<ItemStand> list = this.FindItemStands();
            //	using (List<ItemStand>.Enumerator enumerator = list.GetEnumerator())
            //	{
            //		while (enumerator.MoveNext())
            //		{
            //			if (!enumerator.Current.HaveAttachment())
            //			{
            //				user.Message(MessageHud.MessageType.Center, "$msg_incompleteoffering", 0, null);
            //				return false;
            //			}
            //		}
            //	}
            //	if (this.SpawnBoss(this.GetSpawnPosition()))
            //	{
            //		user.Message(MessageHud.MessageType.Center, "$msg_offerdone", 0, null);
            //		foreach (ItemStand itemStand in list)
            //		{
            //			itemStand.DestroyAttachment();
            //		}
            //		if (this.m_itemSpawnPoint)
            //		{
            //			this.m_fuelAddedEffects.Create(this.m_itemSpawnPoint.position, base.transform.rotation, null, 1f, -1);
            //		}
            //	}
            //	return true;
            //}
            return false;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            //if (this.m_useItemStands)
            //{
            //	return false;
            //}
            //if (this.IsBossSpawnQueued())
            //{
            //	return true;
            //}
            //if (!(this.m_bossItem != null))
            //{
            //	return false;
            //}
            //if (!(item.m_shared.m_name == this.m_bossItem.m_itemData.m_shared.m_name))
            //{
            //	user.Message(MessageHud.MessageType.Center, "$msg_offerwrong", 0, null);
            //	return true;
            //}
            //int num = user.GetInventory().CountItems(this.m_bossItem.m_itemData.m_shared.m_name, -1);
            //if (num < this.m_bossItems)
            //{
            //	user.Message(MessageHud.MessageType.Center, string.Concat(new string[]
            //	{
            //	"$msg_incompleteoffering: ",
            //	this.m_bossItem.m_itemData.m_shared.m_name,
            //	" ",
            //	num.ToString(),
            //	" / ",
            //	this.m_bossItems.ToString()
            //	}), 0, null);
            //	return true;
            //}
            //if (this.m_bossPrefab != null)
            //{
            //	if (this.SpawnBoss(this.GetSpawnPosition()))
            //	{
            //		user.GetInventory().RemoveItem(item.m_shared.m_name, this.m_bossItems, -1);
            //		user.ShowRemovedMessage(this.m_bossItem.m_itemData, this.m_bossItems);
            //		user.Message(MessageHud.MessageType.Center, "$msg_offerdone", 0, null);
            //		if (this.m_itemSpawnPoint)
            //		{
            //			this.m_fuelAddedEffects.Create(this.m_itemSpawnPoint.position, base.transform.rotation, null, 1f, -1);
            //		}
            //	}
            //}
            //else if (this.m_itemPrefab != null && this.SpawnItem(this.m_itemPrefab, user as Player))
            //{
            //	user.GetInventory().RemoveItem(item.m_shared.m_name, this.m_bossItems, -1);
            //	user.ShowRemovedMessage(this.m_bossItem.m_itemData, this.m_bossItems);
            //	user.Message(MessageHud.MessageType.Center, "$msg_offerdone", 0, null);
            //	this.m_fuelAddedEffects.Create(this.m_itemSpawnPoint.position, base.transform.rotation, null, 1f, -1);
            //}
            //if (!string.IsNullOrEmpty(this.m_setGlobalKey))
            //{
            //	ZoneSystem.instance.SetGlobalKey(this.m_setGlobalKey);
            //}
            return true;
        }
    }
}
