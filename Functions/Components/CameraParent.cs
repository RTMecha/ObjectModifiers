using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using BeatmapObject = DataManager.GameData.BeatmapObject;

namespace ObjectModifiers.Functions.Components
{
    public class CameraParent : MonoBehaviour
    {
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
    }
}
