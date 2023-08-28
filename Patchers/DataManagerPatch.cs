using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using HarmonyLib;

using RTFunctions.Enums;

namespace ObjectModifiers.Patchers
{
    [HarmonyPatch(typeof(DataManager))]
    public class DataManagerPatch : MonoBehaviour
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        private static void AwakePostfix()
        {
            EnumPatcher.AddEnumValue(typeof(DataManager.GameData.BeatmapObject.ObjectType), "Solid");
            EnumPatcher.AddEnumValue(typeof(DataManager.GameData.BackgroundObject.ReactiveType), "CUSTOM");
        }
    }
}
