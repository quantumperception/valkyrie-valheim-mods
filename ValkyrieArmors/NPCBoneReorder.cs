using BlacksmithTools;
using System.Collections.Generic;
using UnityEngine;

namespace ValkyrieArmors


{
    public class NPCBoneReorder : MonoBehaviour
    {
        ZNetView m_nview;
        string chestPrefab;
        string legsPrefab;
        string overrideModel;

        void Start()
        {
            m_nview = GetComponent<ZNetView>();
            
            HideBonesAndReorder();
        }
        void OnTransformChildrenChanged()
        {
            HideBonesAndReorder();
        }

        void HideBonesAndReorder()
        {
            if (m_nview == null) return;
            ZDO zdo = m_nview.GetZDO();
            chestPrefab = zdo.GetString("KGchestItem");
            legsPrefab = zdo.GetString("KGlegsItem");
            if (chestPrefab == null && legsPrefab == null) return;
            Util.ReorderNPCBones(gameObject, chestPrefab.GetStableHashCode());
            Util.ReorderNPCBones(gameObject, legsPrefab.GetStableHashCode());
            UpdateBodyModel();
        }
        public void SetOverrideModel(string model) => overrideModel = model;

        public void UpdateBodyModel()
        {
            if (BodypartSystem.bodypartSettingsAsBones.Keys.Count != BodypartSystem.bodypartSettings.Keys.Count)
            {
                BodypartSystem.PartCfgToBoneindexes();
                BodypartSystem.CleanupCfgs();
            }
            List<int> list = new List<int>();
            int[] equippedHashes = new int[] { chestPrefab.GetStableHashCode(), legsPrefab.GetStableHashCode() };
            foreach (int hash in equippedHashes)
            {
                foreach (string key in BodypartSystem.bodypartSettingsAsBones.Keys)
                {
                    if (key.GetStableHashCode() != hash) continue;
                    list.AddRange(BodypartSystem.bodypartSettingsAsBones[key].ToArray());
                }
            }
            if (list.Count == 0) return;
            SkinnedMeshRenderer bodySMR = gameObject.transform.Find("Visual(Clone)/body").GetComponent<SkinnedMeshRenderer>();
            Mesh mesh = bodySMR.sharedMesh;
            Mesh mesh2 = Util.Amputate(Instantiate(mesh), list.ToArray());
            mesh2.name = mesh.name;
            bodySMR.sharedMesh = mesh2;
        }
    }
}
