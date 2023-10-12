using System.Collections.Generic;

using UnityEngine;

using BeatmapObject = DataManager.GameData.BeatmapObject;

namespace ObjectModifiers.Functions.Components
{
    public class CameraParent : MonoBehaviour
    {
        Dictionary<string, bool> canParent = new Dictionary<string, bool>();

        public BeatmapObject parentObject;

        public bool positionParent;
        public bool scaleParent;
        public bool rotationParent;

        public float positionParentOffset;
        public float scaleParentOffset;
        public float rotationParentOffset;

        void LateUpdate()
        {
            if (positionParent)
            {
                var x = EventManager.inst.cam.transform.position.x;
                var y = EventManager.inst.cam.transform.position.y;

                transform.position = new Vector3(x, y, 0f) * positionParentOffset;
            }
            else
                transform.position = Vector3.zero;

            if (scaleParent)
            {
                float camOrthoZoom = EventManager.inst.cam.orthographicSize / 20f;

                transform.localScale = new Vector3(camOrthoZoom, camOrthoZoom, 1f) * scaleParentOffset;
            }
            else
                transform.localScale = Vector3.one;

            if (rotationParent)
            {
                var camRot = EventManager.inst.camParent.transform.rotation.eulerAngles;

                transform.rotation = Quaternion.Euler(camRot * rotationParentOffset);
            }
            else
                transform.rotation = Quaternion.identity;
        }

        //void UpdateChildren()
        //{
        //    foreach (var obj in DataManager.inst.gameData.beatmapObjects)
        //    {
        //        bool flag = true;
        //        if (!string.IsNullOrEmpty(obj.parent))
        //        {
        //            string parentID = parentObject.id;
        //            while (!string.IsNullOrEmpty(parentID))
        //            {
        //                if (parentID == obj.parent)
        //                {
        //                    flag = false;
        //                    break;
        //                }
        //                int num2 = DataManager.inst.gameData.beatmapObjects.FindIndex(x => x.parent == parentID);
        //                if (num2 != -1)
        //                {
        //                    parentID = DataManager.inst.gameData.beatmapObjects[num2].id;
        //                }
        //                else
        //                {
        //                    parentID = null;
        //                }
        //            }
        //        }

        //        if (!canParent.ContainsKey(obj.id))
        //            canParent.Add(obj.id, flag);
        //        else
        //            canParent[obj.id] = flag;
        //    }
        //}

        //void FixedUpdate()
        //{
        //    UpdateChildren();

        //    foreach (var item in canParent)
        //    {
        //        var bm = DataManager.inst.gameData.beatmapObjects.ID(item.Key);
        //        if (!item.Value && bm != null && Objects.beatmapObjects.ContainsKey(item.Key) && Objects.beatmapObjects[item.Key].transformChain != null && Objects.beatmapObjects[item.Key].transformChain.Count > 1 && Objects.beatmapObjects[item.Key].transformChain[0].name != "CAMERA_PARENT [" + parentObject.id + "]")
        //        {
        //            var tf1 = Objects.beatmapObjects[item.Key].transformChain[0];
        //            var ogScale = tf1.localScale;
        //            tf1.SetParent(gameObject.transform);

        //            tf1.localScale = ogScale;

        //            Objects.beatmapObjects[item.Key].transformChain.Insert(0, gameObject.transform);
        //        }
        //    }
        //}
    }
}
