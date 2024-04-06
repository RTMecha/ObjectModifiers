using RTFunctions.Functions.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ObjectModifiers.Functions.Components
{
    public class DestroyModifierResult : MonoBehaviour
    {
        void OnDestroy()
        {
            if (Modifier != null)
                Modifier.Result = null;
        }

        public BeatmapObject.Modifier Modifier { get; set; }
    }
}
