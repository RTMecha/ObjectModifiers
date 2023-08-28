using System;
using System.Reflection.Emit;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using HarmonyLib;

using TMPro;
using DG.Tweening;

using ObjectModifiers.Functions;
using RTFunctions.Functions;

namespace ObjectModifiers.Patchers
{
    [HarmonyPatch(typeof(ObjectManager))]
    public class ObjectManagerPatch : MonoBehaviour
    {
        [HarmonyPatch("Update")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> UpdateTranspilerFixed(IEnumerable<CodeInstruction> instructions)
        {
            var match = new CodeMatcher(instructions);

            match = match.Start();
            match = match.Advance(522);
            match = match.ThrowIfNotMatch("Is not 0.0005f 1", new CodeMatch(OpCodes.Ldc_R4));
            match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 21));
            match = match.Insert(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "DummyNumber")));

            match = match.Start();
            match = match.Advance(1138); //1137
            match = match.ThrowIfNotMatch("Is not 0.0005f 2", new CodeMatch(OpCodes.Ldc_R4));
            match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 50));
            match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesZ1", new[] { typeof(DataManager.GameData.EventKeyframe) })));

            match = match.Start();
            match = match.Advance(1186); //1184
            match = match.ThrowIfNotMatch("Is not 0.0005f 3", new CodeMatch(OpCodes.Ldc_R4));
            match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 50));
            match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesZ1", new[] { typeof(DataManager.GameData.EventKeyframe) })));

            match = match.Start();
            match = match.Advance(1800); //1797
            match = match.ThrowIfNotMatch("Is not 0.1f 1", new CodeMatch(OpCodes.Ldc_R4));
            match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 80));
            match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesZ2", new[] { typeof(DataManager.GameData.EventKeyframe) })));

            match = match.Start();
            match = match.Advance(1832); //1828
            match = match.ThrowIfNotMatch("Is not 0.1f 2", new CodeMatch(OpCodes.Ldc_R4));
            match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc_S, 80));
            match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesZ2", new[] { typeof(DataManager.GameData.EventKeyframe) })));

            match = match.Start();
            match = match.Advance(1834);
            match = match.ThrowIfNotMatch("Is not DataManager.inst at 1834", new CodeMatch(OpCodes.Ldsfld));
            match = match.RemoveInstructions(10);

            match = match.Start();
            match = match.Advance(1802);
            match = match.ThrowIfNotMatch("Is not DataManager.inst at 1802", new CodeMatch(OpCodes.Ldsfld));
            match = match.RemoveInstructions(10);

            match = match.Start();
            match = match.Advance(1188);
            match = match.ThrowIfNotMatch("Is not DataManager.inst at 1188", new CodeMatch(OpCodes.Ldsfld));
            match = match.RemoveInstructions(10);

            match = match.Start();
            match = match.Advance(1140);
            match = match.ThrowIfNotMatch("Is not DataManager.inst at 1140", new CodeMatch(OpCodes.Ldsfld));
            match = match.RemoveInstructions(10);

            match = match.Start();
            match = match.Advance(524);
            match = match.ThrowIfNotMatch("Is not DataManager.inst at 524", new CodeMatch(OpCodes.Ldsfld));
            match = match.RemoveInstructions(10);

            //??? + 5 - 50
            //match = match.Start();
            //match = match.Advance(2290);
            //match = match.ThrowIfNotMatch("is not ldc.i4.3", new CodeMatch(OpCodes.Ldc_I4_3));
            //match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc, 100));
            //match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesRMode", new[] { typeof(DataManager.GameData.EventKeyframe) })));

            //1623 + 3 - 30 = 1596
            //match = match.Start();
            //match = match.Advance(1596);
            //match = match.ThrowIfNotMatch("is not ldc.i4.3", new CodeMatch(OpCodes.Ldc_I4_3));
            //match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc, 72));
            //match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesRMode", new[] { typeof(DataManager.GameData.EventKeyframe) })));

            //843 + 1 - 10
            //match = match.Start();
            //match = match.Advance(834);
            //match = match.ThrowIfNotMatch("is not ldc.i4.3", new CodeMatch(OpCodes.Ldc_I4_3));
            //match = match.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc, 37));
            //match = match.InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Triggers), "EventValuesRMode", new[] { typeof(DataManager.GameData.EventKeyframe) })));


            return match.InstructionEnumeration();
        }

        [HarmonyPatch("updateObjects", new Type[] { })]
        [HarmonyPostfix]
        private static void updateObjects1Postfix()
        {
            foreach (var sequence in ObjectModifiersPlugin.customSequences.Values)
            {
                sequence.sequence.Kill();
            }

            if (GameObject.Find("BepInEx_Manager").GetComponentByName("CatalystBase"))
            {
                foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        foreach (var keyframe in beatmapObject.events[i])
                        {
                            keyframe.active = false;
                        }
                    }
                }
            }

            ObjectModifiersPlugin.customSequences.Clear();
        }

        [HarmonyPatch("updateObjects", new Type[] { typeof(ObjEditor.ObjectSelection), typeof(bool) })]
        [HarmonyPostfix]
        private static void updateObjects2Postfix(ObjEditor.ObjectSelection __0, bool __1)
        {
            if (ObjectModifiersPlugin.customSequences.ContainsKey(__0.ID))
            {
                ObjectModifiersPlugin.customSequences[__0.ID].sequence.Kill();
                ObjectModifiersPlugin.customSequences.Remove(__0.ID);

                if (GameObject.Find("BepInEx_Manager").GetComponentByName("CatalystBase"))
                {
                    for (int i = 0; i < 4; i++)
                    {
                        foreach (var keyframe in __0.GetObjectData().events[i])
                        {
                            keyframe.active = false;
                        }
                    }
                }
            }
        }

        [HarmonyPatch("updateObjects", new Type[] { typeof(string) })]
        [HarmonyPostfix]
        private static void updateObjects3Postfix(string __0)
        {
            if (__0 != null)
            {
                if (ObjectModifiersPlugin.customSequences.ContainsKey(__0))
                {
                    ObjectModifiersPlugin.customSequences[__0].sequence.Kill();
                    ObjectModifiersPlugin.customSequences.Remove(__0);

                    for (int i = 0; i < 4; i++)
                    {
                        foreach (var keyframe in DataManager.inst.gameData.beatmapObjects.ID(__0).events[i])
                        {
                            keyframe.active = false;
                        }
                    }
                }
            }
        }

        [HarmonyPatch("terminateObject", new Type[] { typeof(ObjEditor.ObjectSelection) })]
        [HarmonyPostfix]
        private static void terminateObjectsPostfix(ObjEditor.ObjectSelection __0)
        {
            if (__0 != null && !string.IsNullOrEmpty(__0.ID) && ObjectModifiersPlugin.customSequences.ContainsKey(__0.ID) && __0.IsObject() && __0.GetObjectData() != null)
            {
                ObjectModifiersPlugin.customSequences[__0.ID].sequence.Kill(false);
                ObjectModifiersPlugin.customSequences.Remove(__0.ID);

                for (int i = 0; i < 4; i++)
                {
                    foreach (var keyframe in __0.GetObjectData().events[i])
                    {
                        keyframe.active = false;
                    }
                }
            }
        }
    }
}
