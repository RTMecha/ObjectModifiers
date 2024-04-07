using RTFunctions.Functions.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace ObjectModifiers.Functions.Components
{
    public class LegacyTracker
    {
        public LegacyTracker(BeatmapObject beatmapObject, Vector3 pos, Vector3 lastPos, Quaternion rot, float distance, float time)
        {
            this.beatmapObject = beatmapObject;
            this.pos = pos;
            this.lastPos = lastPos;
            this.rot = rot;
            this.distance = distance;
            this.time = time;
        }

        public float distance;
        public float time;

        public Vector3 lastPos;
        public Vector3 pos;

        public Quaternion rot;

        public BeatmapObject beatmapObject;
    }
}
