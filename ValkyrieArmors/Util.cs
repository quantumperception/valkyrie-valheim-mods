using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ValkyrieArmors


{
    public static class Util
    {
        public static Texture2D LoadTextureFromResources(string filename)
        {
            Texture2D newTex = new Texture2D(1, 1);
            Assembly _assembly = Assembly.GetExecutingAssembly();

            //if we get here, this is being called as a DLL, extract texture
            Stream _imageStream = null;
            try
            {
                _imageStream = _assembly.GetManifestResourceStream("MyNamespace.Tools." + filename);// this is the namespace this function lives in.
            }
            catch
            {
                Debug.LogWarning("Unable to find " + filename + " resource in DLL " + _assembly.FullName);
                return newTex;
            }
            if (_imageStream == null)//sanity check- should be "caught" above
            {
                Debug.LogWarning("Unable to find " + filename + " resource in DLL " + _assembly.FullName);
                return newTex;
            }
            byte[] imageData = new byte[_imageStream.Length];
            _imageStream.Read(imageData, 0, (int)_imageStream.Length);

            if (!newTex.LoadImage(imageData))
                Debug.LogWarning("Unable to Load " + filename + " resource from DLL" + _assembly.FullName);
            return newTex;
        }
        public static Texture2D DuplicateTexture(Texture2D source)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                        source.width,
                        source.height,
                        0,
                        RenderTextureFormat.Default,
                        RenderTextureReadWrite.Linear);

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            Texture2D readableText = new Texture2D(source.width, source.height);
            readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }
        private static int GetBoneIndex(BoneWeight boneWeight, int bone)
        {
            switch (bone)
            {
                case 0:
                    return boneWeight.boneIndex0;
                case 1:
                    return boneWeight.boneIndex1;
                case 2:
                    return boneWeight.boneIndex2;
                case 3:
                    return boneWeight.boneIndex3;
                default:
                    return -1;
            }
        }
        private static float GetBoneWeight(BoneWeight boneWeight, int bone)
        {
            switch (bone)
            {
                case 0:
                    return boneWeight.weight0;
                case 1:
                    return boneWeight.weight1;
                case 2:
                    return boneWeight.weight2;
                case 3:
                    return boneWeight.weight3;
                default:
                    return -1;
            }
        }


        public static Mesh Amputate(Mesh body, int[] bonesToHide)
        {
            BoneWeight[] boneWeights = body.boneWeights;
            for (int i = 0; i < body.subMeshCount; i++)
            {
                List<int> list = new List<int>(body.GetTriangles(i));
                int num = 0;
                while (num < list.Count)
                {
                    bool flag = false;
                    int num2 = 0;
                    for (int j = 0; j < 2; j++)
                    {
                        if (flag)
                        {
                            break;
                        }
                        BoneWeight boneWeight = boneWeights[list[num + j]];
                        float num3 = Mathf.Max(boneWeight.weight0, boneWeight.weight1, boneWeight.weight2, boneWeight.weight3);
                        for (int k = 0; k < 4; k++)
                        {
                            int boneIndex = GetBoneIndex(boneWeight, k);
                            foreach (int num4 in bonesToHide)
                            {
                                if (flag)
                                {
                                    break;
                                }
                                if (boneIndex == num4)
                                {
                                    float boneWeight2 = GetBoneWeight(boneWeight, k);
                                    if (boneWeight2 / num3 > 0.9f && ++num2 == 1)
                                    {
                                        flag = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (flag)
                    {
                        list.RemoveAt(num);
                        list.RemoveAt(num);
                        list.RemoveAt(num);
                    }
                    else
                    {
                        num += 3;
                    }
                }
                body.SetTriangles(list.ToArray(), i);
            }
            return body;
        }
        public static void ReorderNPCBones(GameObject go, int prefabHash)
        {
            GameObject itemPrefab = ObjectDB.instance.GetItemPrefab(prefabHash);
            if (itemPrefab == null)
            {
                Debug.Log("Prefab to reorder not found");
                return;
            }
            Debug.Log($"Got GO to reorder: {go.name} | Prefab: {itemPrefab.name}");
            Debug.Log($"Looking for root bone");
            for (int i = 0; i < go.transform.childCount; i++)
            {
                Transform child = go.transform.GetChild(i);
                Debug.Log($"Found child: {child.name}");
            }
            Transform visual = go.transform.Find("Visual(Clone)");
            Transform skeletonRoot = visual.Find("Armature/Hips");
            if (!visual || !skeletonRoot)
            {
                Debug.Log($"NPC missing components. Skipping {go.name} {skeletonRoot}");
                return;
            }
            Debug.Log($"Reordering bones");

            for (var i = 0; i < itemPrefab.transform.childCount; i++)
            {
                var itemPrefabChild = itemPrefab.transform.GetChild(i);
                if (itemPrefabChild.name.StartsWith("attach_skin"))
                {
                    var prefabRenderers = itemPrefabChild.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    List<SkinnedMeshRenderer> meshRenderersToReorder = new List<SkinnedMeshRenderer>();
                    foreach (var smr in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    {
                        Debug.Log("SMR NAME: " + smr.name);
                        Debug.Log("SMR GO NAME: " + smr.gameObject.name);
                        if (smr.gameObject.name == "body" || !prefabRenderers.Select(r => r.name).Contains(smr.name)) continue;
                        meshRenderersToReorder.Add(smr);
                    }
                    int j = 0;
                    foreach (var meshRenderer in prefabRenderers)
                    {
                        var meshRendererThatNeedFix = meshRenderersToReorder[j];
                        Debug.Log("Setting bones for SMR: " + meshRendererThatNeedFix.name + " using original SMR: " + meshRenderer.name);
                        meshRendererThatNeedFix.SetBones(meshRenderer.GetBoneNames(), skeletonRoot);
                        j++;
                    }
                }
            }
        }

        private static void SetBones(this SkinnedMeshRenderer skinnedMeshRenderer, string[] boneNames, Transform skeletonRoot)
        {
            var bones = new Transform[skinnedMeshRenderer.bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                bones[i] = FindInChildren(skeletonRoot, boneNames[i]);
            }

            skinnedMeshRenderer.bones = bones;
            skinnedMeshRenderer.rootBone = skeletonRoot;
        }

        private static string[] GetBoneNames(this SkinnedMeshRenderer skinnedMeshRenderer) => skinnedMeshRenderer.bones.Select(b => b.name).ToArray();

        private static Transform FindInChildren(Transform transform, string name)
        {
            Transform result;
            if (transform.name == name)
            {
                result = transform;
            }
            else
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    var childTransform = FindInChildren(transform.GetChild(i), name);
                    if (childTransform != null)
                    {
                        return childTransform;
                    }
                }

                result = null;
            }

            return result;
        }
    }
}
