using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

using LSFunctions;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

using TMPro;
using DG.Tweening;
using DG.Tweening.Core;
using SimpleJSON;

using ObjectModifiers.Modifiers;
using ObjectModifiers.Functions;
using ObjectModifiers.Functions.Components;
using ObjectModifiers.Patchers;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

using BeatmapObject = DataManager.GameData.BeatmapObject;
using Prefab = DataManager.GameData.Prefab;

namespace ObjectModifiers
{
    [BepInPlugin("com.mecha.objectmodifiers", "Object Modifiers", "1.1.2")]
    [BepInDependency("com.mecha.rtfunctions")]
    [BepInProcess("Project Arrhythmia.exe")]
    public class ObjectModifiersPlugin : BaseUnityPlugin
    {
        //TODO
        //Modifiers:
        //-Action Modifier that sets an updates the player model in CreativePlayers.
        //Homing Objects need to use the random keyframe system. (Worry about this wayy later)

        //Feature List:
        //New and Improved Modifier system based on the ObjectTags mod.
        //Integrated camera parent.
        //Moved pos Z axis to here and added opacity value to color keyframes.
        //Added Solid object type, players will not be able to pass through the object but should not take damage.
        //

        #region Variables

        public static Material blur;
        public static Material GetBlur()
        {
            var assetBundle = GetAssetBundle(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets", "objectmaterials");
            var assetToLoad = assetBundle.LoadAsset<Material>("blur.mat");
            var blurMat = Instantiate(assetToLoad);
            assetBundle.Unload(false);

            return blurMat;
        }

        public static Shader GetShader(string _shader)
        {
            var assetBundle = GetAssetBundle(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets", "objectmaterials");
            var shaderToLoad = assetBundle.LoadAsset<Shader>(_shader);
            var shader = Instantiate(shaderToLoad);
            assetBundle.Unload(false);
            return shader;
        }

        public static Material GetMaterial(string _shader)
        {
            var assetBundle = GetAssetBundle(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets", "objectmaterials");
            var materialToLoad = assetBundle.LoadAsset<Material>(_shader);
            var material = Instantiate(materialToLoad);
            assetBundle.Unload(false);
            return material;
        }

        public static AssetBundle GetAssetBundle(string _filepath, string _bundle)
        {
            return AssetBundle.LoadFromFile(Path.Combine(_filepath, _bundle));
        }

        public static Harmony harmony = new Harmony("ObjectModifiers");
        public static ObjectModifiersPlugin inst;
        public static string className = "[<color=#F5501B>ObjectModifiers</color>]\n";

        public static List<HomingObject> homingObjects = new List<HomingObject>();

        public static HomingObject homingSelection;
        public static List<HomingObject> selectedHomingObjects = new List<HomingObject>();

        public static Dictionary<string, ModifierObject> modifierObjects = new Dictionary<string, ModifierObject>();

        public static List<BG> backgrounds = new List<BG>();

        #endregion

        #region ConfigEntries
        public static ConfigEntry<bool> editorLoadLevel { get; set; }
        public static ConfigEntry<bool> editorSavesBeforeLoad { get; set; }

        public static ConfigEntry<bool> ResetVariables { get; set; }

        #endregion

        private void Awake()
        {
            inst = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin Object Modifiers is loaded!");

            GameObject gameObject = new GameObject("ObjectModifiers PluginThing");
            DontDestroyOnLoad(gameObject);

            editorLoadLevel = Config.Bind("Editor", "Modifier Loads Level", false, "Any modifiers with the \"loadLevel\" function will load the level whilst in the editor. This is only to prevent the loss of progress.");
            editorSavesBeforeLoad = Config.Bind("Editor", "Saves Before Load", true, "The level will be saved before a level is loaded using a loadLevel modifier.");

            ResetVariables = Config.Bind("Editor", "Reset Variable", false, "Resets the variables of every object when not in preview mode.");

            harmony.PatchAll(typeof(ObjectModifiersPlugin));
            //harmony.PatchAll(typeof(BackgroundManagerPatch));
            harmony.PatchAll(typeof(DataManagerPatch));
            harmony.PatchAll(typeof(ObjectManagerPatch));

            blur = GetBlur();

            SetModifierTypes();
        }

        [HarmonyPatch(typeof(ObjectManager), "Update")]
        [HarmonyPostfix]
        private static void ObjectUpdate(ObjectManager __instance)
        {
            if (ModCompatibility.catalyst == null)
            {
                foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
                {
                    if (!customSequences.ContainsKey(beatmapObject.id))
                    {
                        customSequences.Add(beatmapObject.id, new CustomSequence(beatmapObject));
                    }
                }

                //var beatmapGameObjects = __instance.beatmapGameObjects;
                foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
                {
                    float startTime = Mathf.Clamp(beatmapObject.StartTime, 0.0001f, AudioManager.inst.CurrentAudioSource.clip.length);
                    string sequenceIDAssignment = beatmapObject.id;

                    int currentIndex = 0;
                    var colorKeyframes = beatmapObject.events[3].OrderBy(x => x.eventTime).ToList();
                    foreach (DataManager.GameData.EventKeyframe currentKF in colorKeyframes)
                    {
                        if (!currentKF.active)
                        {
                            currentKF.active = true;
                            int previousIndex = currentIndex - 1;
                            if (previousIndex < 0)
                                previousIndex = 0;
                            DataManager.GameData.EventKeyframe previousKF = colorKeyframes[previousIndex];
                            float atPosition = startTime + previousKF.eventTime;
                            float duration = currentKF.eventTime - previousKF.eventTime;
                            if (duration == 0.0)
                            {
                                atPosition = Mathf.Clamp(atPosition - 0.0001f, 0.0001f, float.PositiveInfinity);
                                duration = 0.0001f;
                            }

                            float a = currentKF.eventValues[1];
                            float h = 0f;
                            float s = 0f;
                            float v = 0f;

                            if (currentKF.eventValues.Length > 2)
                            {
                                h = currentKF.eventValues[2];
                                s = currentKF.eventValues[3];
                                v = currentKF.eventValues[4];
                            }

                            if (currentKF.random != 0)
                                a = ObjectManager.inst.RandomFloatParser(currentKF);

                            if (currentIndex == 0)
                            {
                                if (customSequences.ContainsKey(sequenceIDAssignment))
                                {
                                    customSequences[sequenceIDAssignment].sequence.Insert(0f, DOTween.To(x => customSequences[sequenceIDAssignment].opacity = x, a, a, 0f).SetEase(EventManager.inst.customInstantEase));
                                    //customSequences[sequenceIDAssignment].sequence.Insert(0f, DOTween.To(x => customSequences[sequenceIDAssignment].hue = x, h, h, 0f).SetEase(EventManager.inst.customInstantEase));
                                    //customSequences[sequenceIDAssignment].sequence.Insert(0f, DOTween.To(x => customSequences[sequenceIDAssignment].sat = x, s, s, 0f).SetEase(EventManager.inst.customInstantEase));
                                    //customSequences[sequenceIDAssignment].sequence.Insert(0f, DOTween.To(x => customSequences[sequenceIDAssignment].val = x, v, v, 0f).SetEase(EventManager.inst.customInstantEase));
                                }
                            }
                            if (customSequences.ContainsKey(sequenceIDAssignment))
                            {
                                customSequences[sequenceIDAssignment].sequence.Insert(atPosition, DOTween.To(x => customSequences[sequenceIDAssignment].opacity = x, previousKF.eventValues[1], a, duration).SetEase(currentKF.curveType.Animation));
                                if (previousKF.eventValues.Length > 2)
                                {
                                    //customSequences[sequenceIDAssignment].sequence.Insert(atPosition, DOTween.To(x => customSequences[sequenceIDAssignment].hue = x, previousKF.eventValues[2], h, duration).SetEase(currentKF.curveType.Animation));
                                    //customSequences[sequenceIDAssignment].sequence.Insert(atPosition, DOTween.To(x => customSequences[sequenceIDAssignment].sat = x, previousKF.eventValues[3], s, duration).SetEase(currentKF.curveType.Animation));
                                    //customSequences[sequenceIDAssignment].sequence.Insert(atPosition, DOTween.To(x => customSequences[sequenceIDAssignment].val = x, previousKF.eventValues[4], v, duration).SetEase(currentKF.curveType.Animation));
                                }
                            }
                            ++currentIndex;
                        }
                    }

                    if (Objects.beatmapObjects.ContainsKey(beatmapObject.id) && Objects.beatmapObjects[beatmapObject.id].gameObject != null && beatmapObject.objectType == (BeatmapObject.ObjectType)4)
                    {
                        var modifierObject = Objects.beatmapObjects[beatmapObject.id];

                        if (modifierObject.gameObject != null && modifierObject.collider != null && modifierObject.gameObject.tag != "Helper")
                        {
                            modifierObject.gameObject.tag = "Helper";

                            if (modifierObject.transformChain != null && modifierObject.transformChain.Count > 0)
                                foreach (var tf in modifierObject.transformChain)
                                {
                                    tf.tag = "Helper";
                                }

                            if (modifierObject.collider != null)
                                modifierObject.collider.isTrigger = false;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(GameManager), "SpawnPlayers")]
        [HarmonyPostfix]
        private static void PlayerCollisionFix()
        {
            foreach (var player in InputDataManager.inst.players)
            {
                if (player.player != null)
                {
                    player.player.gameObject.GetComponentInChildren<Collider2D>().isTrigger = false;
                    player.player.gameObject.GetComponentInChildren<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                }
            }
        }

        [HarmonyPatch(typeof(GameManager), "Update")]
        [HarmonyPostfix]
        private static void UpdatePatch(GameManager __instance)
        {
            if (DataManager.inst.gameData != null && DataManager.inst.gameData.beatmapObjects.Count > 0 && ObjectManager.inst != null)
            {
                foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
                {
                    if (beatmapObject.parent == "CAMERA_PARENT")
                    {
                        GameObject gameObject = null;
                        if (!ObjectManager.inst.objectParent.transform.Find("CAMERA_PARENT [" + beatmapObject.id + "]"))
                        {
                            gameObject = new GameObject("CAMERA_PARENT [" + beatmapObject.id + "]");
                            gameObject.transform.SetParent(ObjectManager.inst.objectParent.transform);
                            gameObject.transform.localScale = Vector3.zero;
                            var camParent = gameObject.AddComponent<CameraParent>();

                            camParent.positionParent = beatmapObject.GetParentType(0);
                            camParent.scaleParent = beatmapObject.GetParentType(1);
                            camParent.rotationParent = beatmapObject.GetParentType(2);

                            camParent.positionParentOffset = beatmapObject.getParentOffset(0);
                            camParent.scaleParentOffset = beatmapObject.getParentOffset(1);
                            camParent.rotationParentOffset = beatmapObject.getParentOffset(2);

                            foreach (var cc in beatmapObject.GetChildChain())
                            {
                                for (int i = 0; i < cc.Count; i++)
                                {
                                    var obj = cc[i];
                                    if (obj.objectType != BeatmapObject.ObjectType.Empty && Objects.beatmapObjects.ContainsKey(obj.id) && Objects.beatmapObjects[obj.id].transformChain != null && Objects.beatmapObjects[obj.id].transformChain.Count > 1 && Objects.beatmapObjects[obj.id].transformChain[0].name != "CAMERA_PARENT [" + beatmapObject.id + "]")
                                    {
                                        var tf1 = Objects.beatmapObjects[obj.id].transformChain[0];
                                        var ogScale = tf1.localScale;
                                        tf1.SetParent(gameObject.transform);

                                        tf1.localScale = ogScale;

                                        Objects.beatmapObjects[obj.id].transformChain.Insert(0, gameObject.transform);
                                    }
                                }
                            }
                        }
                        //if (ObjectManager.inst.objectParent.transform.Find("CAMERA_PARENT [" + beatmapObject.id + "]"))
                        //{
                        //    gameObject = ObjectManager.inst.objectParent.transform.Find("CAMERA_PARENT [" + beatmapObject.id + "]").gameObject;
                        //}

                        //if (gameObject != null)
                        //{
                        //    var top = gameObject.transform;

                        //    if (beatmapObject.GetParentType(0))
                        //    {
                        //        float camPosX = EventManager.inst.camParent.transform.position.x;
                        //        float camPosY = EventManager.inst.camParent.transform.position.y;

                        //        top.position = new Vector3(camPosX, camPosY, 0f) * beatmapObject.getParentOffset(0);
                        //    }
                        //    else
                        //    {
                        //        top.position = Vector3.zero;
                        //    }

                        //    if (beatmapObject.GetParentType(1))
                        //    {
                        //        float camOrthoZoom = EventManager.inst.cam.orthographicSize / 20f;
                        //        top.localScale = new Vector3(camOrthoZoom, camOrthoZoom, 1f) * beatmapObject.getParentOffset(1);
                        //    }
                        //    else
                        //    {
                        //        top.localScale = Vector3.one;
                        //    }

                        //    if (beatmapObject.GetParentType(2))
                        //    {
                        //        var camRot = EventManager.inst.camParent.transform.rotation.eulerAngles;

                        //        top.rotation = Quaternion.Euler(camRot * beatmapObject.getParentOffset(2));
                        //    }
                        //    else
                        //    {
                        //        top.rotation = Quaternion.identity;
                        //    }
                        //}
                    }

                    if (Objects.beatmapObjects.ContainsKey(beatmapObject.id) && modifierObjects.ContainsKey(beatmapObject.id))
                    {
                        var modifierObject = modifierObjects[beatmapObject.id];
                        var functionObject = Objects.beatmapObjects[beatmapObject.id];

                        if (functionObject.otherComponents.ContainsKey("ModifierObject") && functionObject.otherComponents["ModifierObject"] == null)
                        {
                            functionObject.otherComponents["ModifierObject"] = modifierObject;
                        }
                        if (!functionObject.otherComponents.ContainsKey("ModifierObject"))
                        {
                            functionObject.otherComponents.Add("ModifierObject", modifierObject);
                        }
                        if (!functionObject.otherComponents.ContainsKey("ObjectOptimization"))
                        {
                            functionObject.otherComponents.Add("ObjectOptimization", modifierObject);
                        }
                    }

                    //if (beatmapObject.parent == "CAMERA_PARENT")
                    //{
                    //    foreach (var cc in beatmapObject.GetChildChain())
                    //    {
                    //        for (int i = 0; i < cc.Count; i++)
                    //        {
                    //            var obj = cc[i];

                    //            if (obj.TimeWithinLifespan() && obj.GetGameObject() != null)
                    //            {
                    //                var top = obj.GetTransformChain()[0];

                    //                if (beatmapObject.GetParentType(0))
                    //                {
                    //                    float camPosX = EventManager.inst.camParent.transform.position.x;
                    //                    float camPosY = EventManager.inst.camParent.transform.position.y;

                    //                    top.position = new Vector3(camPosX, camPosY, 0f) * beatmapObject.getParentOffset(0);
                    //                }
                    //                else
                    //                {
                    //                    top.position = Vector3.zero;
                    //                }

                    //                if (beatmapObject.GetParentType(1))
                    //                {
                    //                    float camOrthoZoom = EventManager.inst.cam.orthographicSize / 20f;
                    //                    top.localScale = new Vector3(camOrthoZoom, camOrthoZoom, 1f) * beatmapObject.getParentOffset(1);
                    //                }
                    //                else
                    //                {
                    //                    top.localScale = Vector3.one;
                    //                }

                    //                if (beatmapObject.GetParentType(2))
                    //                {
                    //                    //var q = EventManager.inst.camParent.transform.rotation;
                    //                    //var a = gameObjectRef.rotation;

                    //                    //gameObjectRef.rotation = new Quaternion(a.x + q.x * beatmapObject.getParentOffset(2), a.y + q.y * beatmapObject.getParentOffset(2), a.z + q.z * beatmapObject.getParentOffset(2), a.w + q.w * beatmapObject.getParentOffset(2));

                    //                    var camRot = EventManager.inst.camParent.transform.rotation.eulerAngles;

                    //                    top.rotation = Quaternion.Euler(camRot * beatmapObject.getParentOffset(2));
                    //                }
                    //                else
                    //                {
                    //                    top.rotation = Quaternion.identity;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    //if (beatmapObject.parent == "PLAYER_PARENT")
                    //{
                    //    foreach (var cc in beatmapObject.GetChildChain())
                    //    {
                    //        for (int i = 0; i < cc.Count; i++)
                    //        {
                    //            var obj = cc[i];

                    //            var modifierObject = modifierObjects[obj.id];

                    //            if (obj.TimeWithinLifespan() && modifierObject.gameObject != null && modifierObject.top != null && modifierObject.delayTracker != null)
                    //            {
                    //                var top = modifierObject.top;

                    //                var delayTracker = modifierObject.delayTracker;
                    //                delayTracker.move = beatmapObject.GetParentType(0);
                    //                delayTracker.rotate = beatmapObject.GetParentType(2);
                    //                delayTracker.active = true;

                    //                delayTracker.moveSharpness = beatmapObject.getParentOffset(0);
                    //                delayTracker.rotateSharpness = beatmapObject.getParentOffset(2);

                    //                var j = ObjectExtensions.ClosestPlayer(modifierObject.top.gameObject);

                    //                var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", j + 1));

                    //                if (player != null && delayTracker.leader != player)
                    //                {
                    //                    delayTracker.leader = player;
                    //                }
                    //            }
                    //        }
                    //    }
                    //}

                    if (ModCompatibility.catalyst == null && customSequences.ContainsKey(beatmapObject.id) && beatmapObject.objectType != BeatmapObject.ObjectType.Empty)
                    {
                        if (ObjectManager.inst.beatmapGameObjects.ContainsKey(beatmapObject.id) && ObjectManager.inst.beatmapGameObjects[beatmapObject.id].mat != null)
                        {
                            var gameObjectRef = ObjectManager.inst.beatmapGameObjects[beatmapObject.id];

                            if (gameObjectRef.rend != null && gameObjectRef.rend.enabled)
                            {
                                DataManager.BeatmapTheme beatmapTheme = __instance.LiveTheme;
                                if (EditorManager.inst != null && EventEditor.inst.showTheme)
                                {
                                    beatmapTheme = EventEditor.inst.previewTheme;
                                }

                                var col = Color.Lerp(beatmapTheme.GetObjColor(gameObjectRef.sequence.LastColor), beatmapTheme.GetObjColor(gameObjectRef.sequence.NewColor), gameObjectRef.sequence.ColorValue);

                                var sequence = customSequences[beatmapObject.id];

                                float a = sequence.opacity;

                                float b = a - 1f;

                                b = -b;

                                float opacity = 1f;

                                if (beatmapObject.objectType != BeatmapObject.ObjectType.Helper)
                                {
                                    if (b >= 0f && b <= 1f)
                                    {
                                        if (showOnlyOnLayer && EditorManager.inst != null && beatmapObject.editorData.Layer != EditorManager.inst.layer)
                                        {
                                            opacity = col.a * b * layerOpacityOffset;
                                        }
                                        else
                                        {
                                            opacity = col.a * b;
                                        }
                                    }
                                    else
                                    {
                                        if (showOnlyOnLayer && EditorManager.inst != null && beatmapObject.editorData.Layer != EditorManager.inst.layer)
                                        {
                                            opacity = col.a * layerOpacityOffset;
                                        }
                                        else
                                        {
                                            opacity = col.a;
                                        }
                                    }
                                }
                                else
                                {
                                    if (b >= 0f && b <= 1f)
                                    {
                                        if (showOnlyOnLayer && EditorManager.inst != null && beatmapObject.editorData.Layer != EditorManager.inst.layer)
                                        {
                                            opacity = col.a * b * 0.35f * layerOpacityOffset;
                                        }
                                        else
                                        {
                                            opacity = col.a * b * 0.35f;
                                        }
                                    }
                                    else
                                    {
                                        if (showOnlyOnLayer && EditorManager.inst != null && beatmapObject.editorData.Layer != EditorManager.inst.layer)
                                        {
                                            opacity = col.a * 0.35f * layerOpacityOffset;
                                        }
                                        else
                                        {
                                            opacity = col.a * 0.35f;
                                        }
                                    }
                                }

                                col = LSColors.fadeColor(col, opacity);
                                //col = LSColors.fadeColor(RTHelpers.ChangeColorHSV(col, sequence.hue, sequence.sat, sequence.val), opacity);

                                if (gameObjectRef.obj.GetComponentInChildren<TextMeshPro>())
                                {
                                    gameObjectRef.obj.GetComponentInChildren<TextMeshPro>().color = col;
                                }
                                if (gameObjectRef.obj.GetComponentInChildren<SpriteRenderer>())
                                {
                                    gameObjectRef.obj.GetComponentInChildren<SpriteRenderer>().material.color = col;
                                }
                                else
                                {
                                    if (gameObjectRef.mat.HasProperty("_Color"))
                                    {
                                        if (beatmapObject.GetGameObject().GetComponentByName("RTObject"))
                                        {
                                            var rt = beatmapObject.GetGameObject().GetComponentByName("RTObject");
                                            var selected = (bool)rt.GetType().GetField("selected", BindingFlags.Public | BindingFlags.Instance).GetValue(rt);

                                            if (selected)
                                            {
                                                if (Input.GetKey(KeyCode.LeftShift))
                                                {
                                                    Color colorHover = new Color(highlightObjectsDoubleColor.r, highlightObjectsDoubleColor.g, highlightObjectsDoubleColor.b);

                                                    if (col.r > 0.9f && col.g > 0.9f && col.b > 0.9f)
                                                    {
                                                        colorHover = new Color(-highlightObjectsDoubleColor.r, -highlightObjectsDoubleColor.g, -highlightObjectsDoubleColor.b);
                                                    }

                                                    gameObjectRef.mat.color = col + new Color(colorHover.r, colorHover.g, colorHover.b, 0f);
                                                }
                                                else
                                                {
                                                    Color colorHover = new Color(highlightObjectsColor.r, highlightObjectsColor.g, highlightObjectsColor.b);

                                                    if (col.r > 0.95f && col.g > 0.95f && col.b > 0.95f)
                                                    {
                                                        colorHover = new Color(-highlightObjectsColor.r, -highlightObjectsColor.g, -highlightObjectsColor.b);
                                                    }

                                                    gameObjectRef.mat.color = col + new Color(colorHover.r, colorHover.g, colorHover.b, 0f);
                                                }
                                            }
                                            else
                                                gameObjectRef.mat.color = col;
                                        }
                                        else
                                            gameObjectRef.mat.color = col;
                                    }
                                }
                            }
                        }

                        var sequencer = customSequences[beatmapObject.id];

                        sequencer.sequence.Goto(AudioManager.inst.CurrentAudioSource.time, false);
                    }

                    if (beatmapObject.name.Contains("object.collision"))
                    {
                        beatmapObject.name = beatmapObject.name.Replace("object.collsion", "");
                        beatmapObject.objectType = (BeatmapObject.ObjectType)4;
                    }
                }
            }

            foreach (var modifierObject in modifierObjects.Values)
            {
                if (modifierObject.modifiers.Count > 0)
                {
                    List<ModifierObject.Modifier> actions = new List<ModifierObject.Modifier>();

                    foreach (var act in modifierObject.modifiers)
                    {
                        if (act.type == ModifierObject.Modifier.Type.Action && !actions.Contains(act))
                        {
                            actions.Add(act);
                        }
                    }

                    List<ModifierObject.Modifier> triggers = new List<ModifierObject.Modifier>();
                    foreach (var act in modifierObject.modifiers)
                    {
                        if (act.type == ModifierObject.Modifier.Type.Trigger && !triggers.Contains(act))
                        {
                            triggers.Add(act);
                        }
                    }

                    if (modifierObject.beatmapObject != null && modifierObject.beatmapObject.TimeWithinLifespan())
                    {
                        if (triggers.Count > 0)
                        {
                            //foreach (var trig in triggers)
                            //{
                            //    foreach (var act in actions)
                            //    {
                            //        var t = trig.Trigger();
                            //        if (!act.active && (t && !trig.not || !t && trig.not) && (!trig.active))
                            //        {
                            //            if (!trig.constant)
                            //            {
                            //                trig.active = true;
                            //            }

                            //            if (!act.constant)
                            //            {
                            //                act.active = true;
                            //            }
                            //            act.Action();
                            //        }
                            //        else if (!t && !trig.not || t && trig.not)
                            //        {
                            //            act.active = false;
                            //        }
                            //    }
                            //}

                            if (triggers.All(x => !x.active && (x.Trigger() && !x.not || !x.Trigger() && x.not)))
                            {
                                foreach (var act in actions)
                                {
                                    if (!act.active)
                                    {
                                        if (!act.constant)
                                        {
                                            act.active = true;
                                        }
                                        act.Action();
                                    }
                                }

                                foreach (var trig in triggers)
                                {
                                    if (!trig.constant)
                                    {
                                        trig.active = true;
                                    }
                                }
                            }
                            else
                            {
                                foreach (var act in actions)
                                {
                                    act.active = false;
                                }
                            }
                        }
                        else
                        {
                            foreach (var act in actions)
                            {
                                if (!act.active)
                                {
                                    if (!act.constant)
                                    {
                                        act.active = true;
                                    }
                                    act.Action();
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var act in actions)
                        {
                            act.active = false;
                            act.Inactive();
                        }

                        foreach (var trig in triggers)
                        {
                            trig.active = false;
                            trig.Inactive();
                        }
                    }
                }

                if (EditorManager.inst != null && EditorManager.inst.isEditing && ResetVariables.Value)
                {
                    modifierObject.variable = 0;
                }
            }

            foreach (var audioSource in audioSources)
            {
                if (DataManager.inst.gameData.beatmapObjects.ID(audioSource.Key) == null || !DataManager.inst.gameData.beatmapObjects.ID(audioSource.Key).TimeWithinLifespan())
                {
                    DeleteKey(audioSource.Key, audioSource.Value);
                }
            }

            if (homingObjects.Count > 0)
            {
                foreach (var homingObject in homingObjects)
                {
                    if (!homingObject.active && homingObject.startTime >= AudioManager.inst.CurrentAudioSource.time)
                    {
                        homingObject.active = true;
                        GameObject bullet = Instantiate(ObjectManager.inst.objectPrefabs[homingObject.shape].options[homingObject.shapeOption]);
                        homingObject.gameObject = bullet;
                        bullet.transform.SetParent(GameObject.Find("GameObjects").transform);
                        bullet.transform.localScale = Vector3.one;
                        var logic = bullet.AddComponent<HomingLogic>();
                        logic.angleChangingSpeed = 200f;
                        logic.movementSpeed = 20f;
                        logic.target = InputDataManager.inst.players[0].player.transform.Find("Player");

                        float z = 0.1f * homingObject.depth;
                        float calc = homingObject.events[0][0].eventValues[2] / 10f;
                        z += calc;

                        bullet.transform.GetChild(0).position = new Vector3(homingObject.events[0][0].eventValues[0], homingObject.events[0][0].eventValues[1], z);
                        bullet.transform.GetChild(0).localScale = new Vector3(homingObject.events[1][0].eventValues[0], homingObject.events[1][0].eventValues[1], 1f);
                        bullet.transform.GetChild(0).eulerAngles = new Vector3(0f, 0f, homingObject.events[2][0].eventValues[0]);

                        if (bullet.transform.GetChild(0).GetComponent<SelectObjectInEditor>())
                        {
                            Destroy(bullet.transform.GetChild(0).GetComponent<SelectObjectInEditor>());
                        }
                        if (bullet.transform.GetChild(0).gameObject.GetComponentByName("RTObject"))
                        {
                            Destroy(bullet.transform.GetChild(0).gameObject.GetComponentByName("RTObject"));
                        }

                        if (bullet.transform.GetChild(0).GetComponent<Collider2D>())
                        {
                            var collider = bullet.transform.GetChild(0).GetComponent<Collider2D>();
                            collider.enabled = true;
                            collider.isTrigger = !homingObject.collide;
                            if (homingObject.deco)
                            {
                                collider.tag = "Helper";
                            }
                        }

                        float time = Time.time;
                        var listTime = new List<float>();

                        if (bullet.transform.GetChild(0).GetComponent<Renderer>())
                        {
                            var bulletRenderer = bullet.transform.GetChild(0).GetComponent<Renderer>();
                            bulletRenderer.enabled = true;
                            bulletRenderer.material.color = new Color(homingObject.events[3][0].eventValues[0], homingObject.events[3][0].eventValues[1], homingObject.events[3][0].eventValues[2], homingObject.events[3][0].eventValues[3]);

                            logic.mat = bulletRenderer.material;
                            for (int i = 0; i < homingObject.events[3].Count;)
                            {
                                switch (homingObject.events[3][i].homingType)
                                {
                                    case HomingObject.HomingKeyframe.HomingType.Static:
                                        {
                                            var color = bulletRenderer.material.DOColor(GameManager.inst.LiveTheme.objectColors[(int)homingObject.events[3][i].eventValues[0]], homingObject.events[3][i].eventTime).SetEase(homingObject.events[3][i].curveType.Animation);
                                            color.Play();
                                            homingObject.homingLogic.followColor = false;
                                            break;
                                        }
                                    case HomingObject.HomingKeyframe.HomingType.Dynamic:
                                        {
                                            homingObject.homingLogic.colRange = homingObject.events[3][i].range;
                                            homingObject.homingLogic.followColor = true;
                                            break;
                                        }
                                }
                                if (homingObject.events[3][i].eventTime > AudioManager.inst.CurrentAudioSource.time - homingObject.startTime + homingObject.events[3][i].eventTime)
                                {
                                    i++;
                                }
                            }
                        }

                        if (homingObject.events[0].Count > 1)
                        {
                            for (int i = 0; i < homingObject.events[0].Count;)
                            {
                                switch (homingObject.events[0][i].homingType)
                                {
                                    case HomingObject.HomingKeyframe.HomingType.Static:
                                        {
                                            if (!homingObject.events[0][i].targetPlayer)
                                            {
                                                var position = bullet.transform.GetChild(0).DOLocalMove(new Vector3(homingObject.events[0][i].eventValues[0], homingObject.events[0][i].eventValues[1]), homingObject.events[0][i].eventTime).SetEase(homingObject.events[0][i].curveType.Animation);
                                                position.Play();
                                            }
                                            else
                                            {
                                                if (!homingObject.multiple)
                                                {
                                                    var position = bullet.transform.GetChild(0).DOLocalMove(InputDataManager.inst.players[(int)homingObject.events[0][i].eventValues[0]].player.transform.Find("Player").position, homingObject.events[0][i].eventTime).SetEase(homingObject.events[0][i].curveType.Animation);
                                                    position.Play();
                                                }
                                            }
                                            homingObject.homingLogic.followPos = false;
                                            break;
                                        }
                                    case HomingObject.HomingKeyframe.HomingType.Dynamic:
                                        {
                                            homingObject.homingLogic.posRange = homingObject.events[0][i].range;
                                            homingObject.homingLogic.followPos = true;
                                            break;
                                        }
                                }
                                if (homingObject.events[0][i].eventTime > AudioManager.inst.CurrentAudioSource.time - homingObject.startTime + homingObject.events[0][i].eventTime)
                                {
                                    i++;
                                }
                            }
                        }

                        if (homingObject.events[1].Count > 1)
                        {
                            for (int i = 0; i < homingObject.events[1].Count;)
                            {
                                var scale = bullet.transform.GetChild(0).DOScale(new Vector3(homingObject.events[1][i].eventValues[0], homingObject.events[1][i].eventValues[1], 1f), homingObject.events[1][i].eventTime).SetEase(homingObject.events[1][i].curveType.Animation);
                                scale.Play();

                                if (homingObject.events[1][i].eventTime > AudioManager.inst.CurrentAudioSource.time - homingObject.startTime + homingObject.events[1][i].eventTime)
                                {
                                    i++;
                                }
                            }
                        }

                        if (homingObject.events[2].Count > 1)
                        {
                            for (int i = 0; i < homingObject.events[2].Count;)
                            {
                                var rotation = bullet.transform.GetChild(0).DORotate(new Vector3(0f, 0f, homingObject.events[2][i].eventValues[0]), homingObject.events[2][i].eventTime, RotateMode.LocalAxisAdd).SetEase(homingObject.events[2][i].curveType.Animation);
                                rotation.Play();

                                if (homingObject.events[1][i].eventTime > AudioManager.inst.CurrentAudioSource.time - homingObject.startTime + homingObject.events[1][i].eventTime)
                                {
                                    i++;
                                }
                            }
                        }

                        listTime = (from x in listTime
                                    orderby x
                                    select x).ToList();

                        homingObject.totalDuration = listTime[listTime.Count - 1];

                        Tweener tw = GameObject.Find("ObjectModifiers PluginThing").transform.DOScale(1f, homingObject.totalDuration);

                        tw.OnComplete(delegate ()
                        {
                            bullet.transform.DOKill();
                            if (bullet.transform.GetChild(0).GetComponent<Renderer>())
                            {
                                bullet.transform.GetChild(0).GetComponent<Renderer>().material.DOKill();
                            }
                            Destroy(bullet);
                        });
                    }
                    else
                    {
                        if (homingObject.gameObject != null)
                        {
                            Destroy(homingObject.gameObject);
                        }
                    }
                }
            }
        }

        public static void DeleteKey(string id, AudioSource audioSource)
        {
            if (audioSources.ContainsKey(id))
            {
                Destroy(audioSource);
                audioSources.Remove(id);
            }
        }

        public static void SetShowable(bool _show, float _opacity, bool _highlightObjects, Color _highlightObjectsColor, Color _highlightObjectsDoubleColor)
        {
            showOnlyOnLayer = _show;
            layerOpacityOffset = _opacity;
            highlightObjects = _highlightObjects;
            highlightObjectsColor = _highlightObjectsColor;
            highlightObjectsDoubleColor = _highlightObjectsDoubleColor;
        }

        public static bool showOnlyOnLayer = false;
        public static float layerOpacityOffset = 0.2f;
        public static bool highlightObjects = false;
        public static Color highlightObjectsColor;
        public static Color highlightObjectsDoubleColor;

        [HarmonyPatch(typeof(GameManager), "EndOfLevel")]
        [HarmonyPrefix]
        private static void EndOfLevelPrefix()
        {
            modifierObjects.Clear();
        }

        #region Homing Tests

        //[HarmonyPatch(typeof(ObjectManager), "Update")]
        //[HarmonyTranspiler]
        //private static IEnumerable<CodeInstruction> ObjectManagerUpdateTranspiler(IEnumerable<CodeInstruction> instructions)
        //{
        //    var matcher = new CodeMatcher(instructions);

        //    matcher = matcher.Start();

        //    matcher = matcher.MatchForward(false, new CodeMatch(OpCodes.Ldstr, "top"));

        //    matcher = matcher.Insert(new CodeInstruction(OpCodes.Ldloc_0));
        //    Debug.LogFormat("{0}Opcode: {1} \nOperand: {2}", className, matcher.Opcode, matcher.Operand);
        //    matcher = matcher.SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ObjectExtensions), "GetParentName", new[] { typeof(DataManager.GameData.BeatmapObject) })));
        //    Debug.LogFormat("{0}Opcode: {1} \nOperand: {2}", className, matcher.Opcode, matcher.Operand);

        //    return matcher.InstructionEnumeration();
        //}

        //ObjectModifiers.ObjectModifiersPlugin.PlayTest()

        public static void PlayTest()
        {
            if (homingObjects.Count < 1)
            {
                CreateNewHomingObject();
            }    
            Play(homingObjects[0]);
        }

        public static IEnumerator LoadBackground(BackgroundManager __instance)
        {
            float delay = 0f;
            while (GameManager.inst.gameState != GameManager.State.Playing)
            {
                yield return new WaitForSeconds(delay);
                delay += 0.0001f;
            }

            BackgroundManagerPatch.audio = AudioManager.inst.CurrentAudioSource;

            __instance.samples = new float[256];

            var clip = BackgroundManagerPatch.audio.clip;
            if (clip != null)
            {
                clip.GetData(__instance.samples, 0);
            }

            Debug.LogFormat("{0}Begin loading BG Objects: {1}", className, DataManager.inst.gameData.backgroundObjects);
            foreach (var background in DataManager.inst.gameData.backgroundObjects)
            {
                var bg = new BG(new BG.Reactive(new int[2], new float[2]), background);
                Debug.LogFormat("{0}Loading new BG: {1} {2}", className, background, bg);
                backgrounds.Add(bg);
            }

            foreach (var background in DataManager.inst.gameData.backgroundObjects)
            {
                __instance.CreateBackgroundObject(background);
            }
            yield break;
        }

        public static void Play(HomingObject _homingObject)
        {
            _homingObject.ICreateHomingObject();
        }

        public static HomingObject CreateNewHomingObject(bool select = false)
        {
            var homingObject = new HomingObject();
            homingObject.id = LSText.randomNumString(16);
            homingObject.homingName = "Homer";
            Debug.LogFormat("{0}{1}", className, homingObject.homingName + " | " + homingObject.id);
            homingObject.startTime = Mathf.Clamp(AudioManager.inst.CurrentAudioSource.time, 0f, AudioManager.inst.CurrentAudioSource.clip.length);
            homingObject.depth = 15;
            homingObject.editorData.Bin = 0;
            if (EditorManager.inst.layer != 5)
            {
                homingObject.editorData.Layer = EditorManager.inst.layer;
            }
            else
            {
                homingObject.editorData.Layer = EditorManager.inst.lastLayer;
            }
            homingObject.playerTarget = 0;
            homingObject.shape = 0;
            homingObject.shapeOption = 0;
            homingObject.events = new List<List<HomingObject.HomingKeyframe>>
            {
                new List<HomingObject.HomingKeyframe>
                {
                    new HomingObject.HomingKeyframe
                    {
                        eventValues = new float[3],
                        curveType = DataManager.inst.AnimationList[0],
                        eventTime = 0f,
                        homingType = HomingObject.HomingKeyframe.HomingType.Static
                    },

                    new HomingObject.HomingKeyframe
                    {
                        eventValues = new float[3],
                        curveType = DataManager.inst.AnimationList[0],
                        eventTime = 2f,
                        homingType = HomingObject.HomingKeyframe.HomingType.Static
                    },

                    new HomingObject.HomingKeyframe
                    {
                        eventValues = new float[3],
                        curveType = DataManager.inst.AnimationList[0],
                        eventTime = 4f,
                        homingType = HomingObject.HomingKeyframe.HomingType.Dynamic
                    }
                },

                new List<HomingObject.HomingKeyframe>
                {
                    new HomingObject.HomingKeyframe
                    {
                        eventValues = new float[2]
                        {
                            1f,
                            1f
                        },
                        curveType = DataManager.inst.AnimationList[0],
                        eventTime = 0f
                    }
                },

                new List<HomingObject.HomingKeyframe>
                {
                    new HomingObject.HomingKeyframe
                    {
                        eventValues = new float[1],
                        curveType = DataManager.inst.AnimationList[0],
                        eventTime = 0f
                    }
                },

                new List<HomingObject.HomingKeyframe>
                {
                    new HomingObject.HomingKeyframe
                    {
                        eventValues = new float[4]
                        {
                            1f,
                            1f,
                            1f,
                            1f
                        },
                        curveType = DataManager.inst.AnimationList[0],
                        eventTime = 0f
                    }
                },
            };

            homingObject.collide = false;
            homingObject.deco = false;
            homingObject.followPlayer = new List<bool>
            {
                false,
                true
            };

            Debug.LogFormat("{0}Homing State: {1}", className, homingObject == null);

            homingObjects.Add(homingObject);
            if (select)
            {
                homingSelection = homingObject;
                selectedHomingObjects.Add(homingObject);
            }

            return homingObject;
        }

        #endregion

        #region Modifier Functions

        public static void GetSoundPath(string id, string _sound, bool fromSoundLibrary = false, float _pitch = 1f, float _volume = 1f, bool _loop = false)
        {
            string text = "beatmaps/soundlibrary/" + _sound;

            if (!fromSoundLibrary)
            {
                text = RTFile.basePath + _sound;
            }

            Debug.LogFormat("{0}Filepath: {1}", className, text);
            if (RTFile.FileExists(RTFile.ApplicationDirectory + text))
            {
                Debug.LogFormat("{0}File exists so play", className);
                inst.StartCoroutine(FileManager.inst.LoadMusicFile(text, delegate (AudioClip _newSound)
                {
                    _newSound.name = _sound;
                    PlaySound(id, _newSound, _pitch, _volume, _loop);
                }));
            }
        }

        public static void PlaySound(string id, AudioClip _clip, float _pitch, float _volume, bool _loop)
        {
            AudioSource audioSource = Camera.main.gameObject.AddComponent<AudioSource>();
            audioSource.clip = _clip;
            audioSource.playOnAwake = true;
            audioSource.loop = _loop;
            audioSource.pitch = _pitch * AudioManager.inst.pitch;
            audioSource.volume = Mathf.Clamp(_volume, 0f, 2f) * AudioManager.inst.sfxVol;
            audioSource.Play();

            float x = _pitch * AudioManager.inst.pitch;
            if (x == 0f)
                x = 1f;
            if (x < 0f)
                x = -x;

            if (!_loop)
                inst.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, _clip.length / x));
            else if (!audioSources.ContainsKey(id))
                audioSources.Add(id, audioSource);
        }

        public static Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

        public static IEnumerator ParseStoryLevel(string _level)
        {
            Debug.LogFormat("{0}Parsing {1}...", className, _level);
            string audioPath = RTFile.ApplicationDirectory + "beatmaps/story/" + _level + "/level.ogg";
            string beatmapJSON = FileManager.inst.LoadJSONFile("beatmaps/story/" + _level + "/level.lsb");

            string rawMetadataJSON = FileManager.inst.LoadJSONFile("beatmaps/story/" + _level + "/metadata.lsb");

            var metadata = DataManager.inst.ParseMetadata(rawMetadataJSON);

            var saveQueue = SaveManager.inst.ArcadeQueue;

            saveQueue.AudioFileStr = audioPath;
            saveQueue.BeatmapJsonStr = beatmapJSON;
            saveQueue.MetadataJsonStr = rawMetadataJSON;
            saveQueue.MetaData = metadata;

            if (RTFile.FileExists(audioPath))
            {
                Debug.LogFormat("{0}File exists so play", className);
                inst.StartCoroutine(FileManager.inst.LoadMusicFile(audioPath, delegate (AudioClip _newSound)
                {
                    _newSound.name = _level;
                    saveQueue.BeatmapSong = _newSound;

                    DataManager.inst.UpdateSettingBool("IsArcade", true);

                    SceneManager.inst.LoadScene("Game");
                }));
            }
            yield break;
        }

        public static DataManager.GameData.PrefabObject AddPrefabObjectToLevel(Prefab _prefab, float startTime, Vector2 pos, Vector2 sca, float rot)
        {
            DataManager.GameData.PrefabObject prefabObject = new DataManager.GameData.PrefabObject();
            prefabObject.ID = LSText.randomString(16);
            prefabObject.prefabID = _prefab.ID;

            prefabObject.StartTime = startTime;

            prefabObject.events[0].eventValues[0] = pos.x;
            prefabObject.events[0].eventValues[1] = pos.y;
            prefabObject.events[1].eventValues[0] = sca.x;
            prefabObject.events[1].eventValues[1] = sca.y;
            prefabObject.events[2].eventValues[0] = rot;

            if (EditorManager.inst != null)
                prefabObject.editorData.Layer = EditorManager.inst.layer;

            DataManager.inst.gameData.prefabObjects.Add(prefabObject);
            ObjectManager.inst.updateObjects(prefabObject.ID);
            if (EditorManager.inst != null && !EditorManager.inst.isEditing && prefabObject.editorData.Layer != EditorManager.inst.layer)
            {
                EditorManager.inst.SetLayer(prefabObject.editorData.Layer);
            }
            if (EditorManager.inst != null)
            {
                ObjEditor.inst.RenderTimelineObject(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Prefab, prefabObject.ID));
            }

            return prefabObject;
        }

        public static void SaveProgress(string _path, string _chapter, string _level, float _data)
        {
            if (!RTFile.DirectoryExists(RTFile.ApplicationDirectory + "profile"))
            {
                Directory.CreateDirectory(RTFile.ApplicationDirectory + "profile");
            }

            string rawProfileJSON = FileManager.inst.LoadJSONFile("profile/" + _path + ".ses");

            if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + _path + ".ses"))
            {
                var jsonnode = JSON.Parse(rawProfileJSON);

                jsonnode[_chapter][_level] = _data.ToString();

                RTFile.WriteToFile("profile/" + _path + ".ses", jsonnode.ToString(3));
            }
            else
            {
                JSONNode jsonnode = JSON.Parse("{}");

                jsonnode[_chapter][_level] = _data.ToString();

                RTFile.WriteToFile("profile/" + _path + ".ses", jsonnode.ToString(3));
            }
        }

        #endregion

        #region ModCompatibility Reference

        public static void AddModifierObject(BeatmapObject _beatmapObject)
        {
            if (!modifierObjects.ContainsKey(_beatmapObject.id))
            {
                _beatmapObject.AddModifierObject();
            }
        }

        public static void RemoveModifierObject(BeatmapObject _beatmapObject)
        {
            if (modifierObjects.ContainsKey(_beatmapObject.id))
            {
                _beatmapObject.RemoveModifierObject();
            }
        }

        public static void ClearModifierObjects()
        {
            if (modifierObjects == null)
            {
                modifierObjects = new Dictionary<string, ModifierObject>();
            }
            else
            {
                modifierObjects.Clear();
            }
        }

        public static ModifierObject GetModifierObject(BeatmapObject _beatmapObject)
        {
            if (modifierObjects.ContainsKey(_beatmapObject.id))
                return modifierObjects[_beatmapObject.id];
            return null;
        }

        public static ModifierObject.Modifier GetModifierIndex(BeatmapObject _beatmapObject, int index)
        {
            if (GetModifierObject(_beatmapObject) != null)
                return GetModifierObject(_beatmapObject).modifiers[index];
            return null;
        }

        public static int GetModifierCount(BeatmapObject _beatmapObject)
        {
            if (GetModifierObject(_beatmapObject) != null)
                return GetModifierObject(_beatmapObject).modifiers.Count;
            return 0;
        }

        public static void RemoveModifierIndex(BeatmapObject _beatmapObject, int index)
        {
            if (GetModifierObject(_beatmapObject) != null)
                GetModifierObject(_beatmapObject).modifiers.RemoveAt(index);
        }

        public static void AddModifierObjectWithValues(BeatmapObject _beatmapObject, Dictionary<string, object> dictionary)
        {
            var modifierObject = GetModifierObject(_beatmapObject);
            if (modifierObject != null)
            {
                var list = (List<Dictionary<string, object>>)dictionary["modifiers"];

                modifierObject.modifiers.Clear();

                for (int i = 0; i < list.Count; i++)
                {
                    var modifier = new ModifierObject.Modifier(_beatmapObject);

                    var list2 = list[i];

                    if (!string.IsNullOrEmpty((string)list2["value"]))
                    {
                        var value = (string)list2["value"];

                        var constant = (bool)list2["constant"];

                        var type = (int)list2["type"];

                        var commands = (List<string>)list2["commands"];

                        var notGate = (bool)list2["not"];

                        modifier.type = (ModifierObject.Modifier.Type)type;
                        modifier.constant = constant;
                        modifier.value = value;
                        modifier.command = commands;
                        modifier.not = notGate;
                    }

                    modifier.refModifier = modifierObject;
                    modifier.modifierObject = _beatmapObject;
                    modifierObject.modifiers.Add(modifier);
                }
            }
        }

        public static void AddModifierToObject(BeatmapObject _beatmapObject, int index)
        {
            var copy = ModifierObject.Modifier.DeepCopy(modifierTypes[index]);
            if (GetModifierObject(_beatmapObject) != null)
            {
                var modifierObject = GetModifierObject(_beatmapObject);

                copy.modifierObject = _beatmapObject;
                copy.refModifier = modifierObject;

                modifierObject.modifiers.Add(copy);
            }
        }

        public static int GetModifierVariable(BeatmapObject _beatmapObject)
        {
            if (GetModifierObject(_beatmapObject) != null)
            {
                var modifier = GetModifierObject(_beatmapObject);

                return modifier.variable;
            }
            return 0;
        }

        #endregion

        public static void SetModifierTypes()
        {
            modifierTypesDictionary.Clear();
            foreach (var modifierType in modifierTypes)
            {
                modifierTypesDictionary.Add(modifierType.command[0], modifierType);
            }

            var list = new List<string>();

            foreach (var modifierType in modifierTypes)
            {
                list.Add(modifierType.command[0] + " (" + modifierType.type.ToString() + ")");
            }

            ModCompatibility.sharedFunctions.Add("ObjectModifiersModifierList", list);
        }

        //Modifer ideas:
        //Animation Trigger (Makes an object play a preset animation depending on if it's been activated.
        //Chain Object (With chain following and more)
        //Homing Object (Duplicates the original object, removes the animation stuff and replaces it with homing types.)

        public static Dictionary<string, ModifierObject.Modifier> modifierTypesDictionary = new Dictionary<string, ModifierObject.Modifier>();

        public static List<ModifierObject.Modifier> modifierTypes = new List<ModifierObject.Modifier>
        {
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "setPitch"
                },
                value = "1"
            }, //setPitch
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "addPitch"
                },
                value = "0.1"
            }, //addPitch
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "setMusicTime"
                },
                value = "0"
            }, //setMusicTime
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playSound",
                    "False",
                    "1",
                    "1",
                    "False"
                },
                value = "sounds/audio.wav"
            }, //playSound
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "loadLevel"
                },
                value = "level name"
            }, //loadLevel
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "blur",
                    "False"
                },
                value = "0.5"
            }, //blur
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "particleSystem",
                    "0", //S - 1
                    "0", //SO - 2
                    "0", //Color - 3
                    "1", //Start Opacity - 4
                    "0", //End Opacity - 5
                    "1", //Start Scale - 6
                    "0", //End Scale - 7
                    "0", //Rotation - 8
                    "5", //Speed - 9
                    "1", //Amount - 10
                    "1", //Duration - 11
                    "0", //Force X - 12
                    "0", //Force Y - 13
                    "True", //Trails - 14
                },
                value = "5"
            }, //particleSystem
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "trailRenderer",
                    "1", //StartWidth
                    "0", //EndWidth
                    "0", //StartColor
                    "1", //StartOpacity
                    "0", //EndColor
                    "0", //EndOpacity
                },
                value = "1"
            }, //trailRenderer
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "spawnPrefab",
                    "0",
                    "0",
                    "1",
                    "1",
                    "0"
                },
                value = "0"
            }, //spawnPrefab
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerHeal"
                },
                value = "1"
            }, //playerHeal
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerHealAll"
                },
                value = "1"
            }, //playerHealAll
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerHit"
                },
                value = "1"
            }, //playerHit
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerHitAll"
                },
                value = "1"
            }, //playerHitAll
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerKill"
                },
                value = ""
            }, //playerKill
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerKillAll"
                },
                value = ""
            }, //playerKillAll
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerMove",
                    "1",
                    "0"
                },
                value = "0.0"
            }, //playerMove
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerMoveAll",
                    "1",
                    "0"
                },
                value = "0.0"
            }, //playerMoveAll
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerMoveX",
                    "1",
                    "0"
                },
                value = "0"
            }, //playerMoveX
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerMoveXAll",
                    "1",
                    "0"
                },
                value = "0"
            }, //playerMoveXAll
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerMoveY",
                    "1",
                    "0"
                },
                value = "0"
            }, //playerMoveY
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerMoveYAll",
                    "1",
                    "0"
                },
                value = "0"
            }, //playerMoveYAll
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerRotate",
                    "1",
                    "0"
                },
                value = "0"
            }, //playerRotate
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerRotateAll",
                    "1",
                    "0"
                },
                value = "0"
            }, //playerRotateAll
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerBoost"
                },
                value = "0"
            }, //playerBoost
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerBoostAll"
                },
                value = "0"
            }, //playerBoostAll
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "playerDisableBoost"
                },
                value = "0"
            }, //playerDisableBoost
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "showMouse"
                },
                value = "0"
            }, //showMouse
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "hideMouse"
                },
                value = "0"
            }, //hideMouse
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "addVariable",
                    "Object Name"
                },
                value = "1"
            }, //addVariable
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "subVariable",
                    "Object Name"
                },
                value = "1"
            }, //subVariable
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "setVariable",
                    "Object Name"
                },
                value = "1"
            }, //setVariable
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "quitToMenu"
                },
                value = "0"
            }, //quitToMenu
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "quitToArcade"
                },
                value = "0"
            }, //quitToArcade
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "disableObject"
                },
                value = "0"
            }, //disableObject
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "disableObjectTree"
                },
                value = "0"
            }, //disableObjectTree
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "save",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //save
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "saveVariable",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //saveVariable
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "reactivePos",
                    "0",
                    "0",
                    "0",
                    "0"
                },
                value = "1"
            }, //reactivePos
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "reactiveSca",
                    "0",
                    "0",
                    "1",
                    "1"
                },
                value = "1"
            }, //reactiveSca
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "reactiveRot",
                    "0"
                },
                value = "1"
            }, //reactiveRot
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "reactiveCol",
                    "0",
                    "0"
                },
                value = "1"
            }, //reactiveCol
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "setPlayerModel",
                    "0"
                },
                value = "0"
            }, //setPlayerModel
            //new ModifierObject.Modifier
            //{
            //    type = ModifierObject.Modifier.Type.Action,
            //    constant = false,
            //    command = new List<string>
            //    {
            //        "legacyTail",
            //        "3",
            //        "200",
            //        "12"
            //    },
            //    value = "2"
            //}, //legacyTail
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "blackHole",
                },
                value = "0.01"
            }, //blackHole
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "eventOffset",
                    "0",
                    "0",
                },
                value = "1"
            }, //blackHole
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerCollide"
                },
                value = ""
            }, //playerCollide
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerHealthEquals"
                },
                value = "3"
            }, //playerHealthEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerHealthLesserEquals"
                },
                value = "3"
            }, //playerHealthLesserEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerHealthGreaterEquals"
                },
                value = "3"
            }, //playerHealthGreaterEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerHealthLesser"
                },
                value = "3"
            }, //playerHealthLesser
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerHealthGreater"
                },
                value = "3"
            }, //playerHealthGreater
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerDeathsEquals"
                },
                value = "1"
            }, //playerDeathsEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerDeathsLesserEquals"
                },
                value = "1"
            }, //playerDeathsLesserEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerDeathsGreaterEquals"
                },
                value = "1"
            }, //playerDeathsGreaterEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerDeathsLesser"
                },
                value = "1"
            }, //playerDeathsLesser
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerDeathsGreater"
                },
                value = "1"
            }, //playerDeathsGreater
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerMoving"
                },
                value = "0"
            }, //playerMoving
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerBoosting"
                },
                value = "0"
            }, //playerBoosting
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerAlive"
                },
                value = "0"
            }, //playerAlive
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerDistanceLesser"
                },
                value = "5"
            }, //playerDistanceLesser
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "playerDistanceGreater"
                },
                value = "5"
            }, //playerDistanceGreater
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "onPlayerHit"
                },
                value = ""
            }, //onPlayerHit
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "bulletCollide"
                },
                value = ""
            }, //bulletCollide
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "keyPressDown"
                },
                value = "0"
            }, //keyPressDown
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "keyPress"
                },
                value = "0"
            }, //keyPress
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "keyPressUp"
                },
                value = "0"
            }, //keyPressUp
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "mouseButtonDown"
                },
                value = "0"
            }, //mouseButtonDown
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "mouseButton"
                },
                value = "0"
            }, //mouseButton
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "mouseButtonUp"
                },
                value = "0"
            }, //mouseButtonUp
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "mouseOver"
                },
                value = "0"
            }, //mouseOver
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "loadEquals",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "loadLesserEquals",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadLesserEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "loadGreaterEquals",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadGreaterEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "loadLesser",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadLesser
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "loadGreater",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadGreater
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "variableEquals"
                },
                value = "1"
            }, //variableEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "variableLesserEquals"
                },
                value = "1"
            }, //variableLesserEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "variableGreaterEquals"
                },
                value = "1"
            }, //variableGreaterEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "variableLesser"
                },
                value = "1"
            }, //variableLesser
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "variableGreater"
                },
                value = "1"
            }, //variableGreater
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "variableOtherEquals",
                    "Object Name"
                },
                value = "1"
            }, //variableOtherEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "variableOtherLesserEquals",
                    "Object Name"
                },
                value = "1"
            }, //variableOtherLesserEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "variableOtherGreaterEquals",
                    "Object Name"
                },
                value = "1"
            }, //variableOtherGreaterEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "variableOtherLesser",
                    "Object Name"
                },
                value = "1"
            }, //variableOtherLesser
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "variableOtherGreater",
                    "Object Name"
                },
                value = "1"
            }, //variableOtherGreater
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "pitchEquals"
                },
                value = "1"
            }, //pitchEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "pitchLesserEquals"
                },
                value = "1"
            }, //pitchLesserEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "pitchGreaterEquals"
                },
                value = "1"
            }, //pitchGreaterEquals
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "pitchLesser"
                },
                value = "1"
            }, //pitchLesser
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "pitchGreater"
                },
                value = "1"
            }, //pitchGreater
        };

        public static Dictionary<string, CustomSequence> customSequences = new Dictionary<string, CustomSequence>();

        public class CustomSequence
        {
            public CustomSequence(BeatmapObject _bm)
            {
                bm = _bm;
                sequence = DOTween.Sequence();
            }

            public BeatmapObject bm;
            public float opacity;
            //public float hue;
            //public float sat;
            //public float val;

            public Sequence sequence;
        }

        public static List<AnimationPreset> animationLibrary = new List<AnimationPreset>();

        public class AnimationPreset
        {
            public AnimationPreset(string name)
            {
                this.name = name;
            }

            public string id;
            public string name;
            public List<List<DataManager.GameData.EventKeyframe>> eventKeyframes = new List<List<DataManager.GameData.EventKeyframe>>
            {
                new List<DataManager.GameData.EventKeyframe>
                {
                    new DataManager.GameData.EventKeyframe
                    {
                        eventTime = 0f,
                        eventValues = new float[3]
                    }
                },
                new List<DataManager.GameData.EventKeyframe>
                {
                    new DataManager.GameData.EventKeyframe
                    {
                        eventTime = 0f,
                        eventValues = new float[2]
                    }
                },
                new List<DataManager.GameData.EventKeyframe>
                {
                    new DataManager.GameData.EventKeyframe
                    {
                        eventTime = 0f,
                        eventValues = new float[1]
                    }
                },
                new List<DataManager.GameData.EventKeyframe>
                {
                    new DataManager.GameData.EventKeyframe
                    {
                        eventTime = 0f,
                        eventValues = new float[2]
                    }
                },
            };
        }
    }
}
