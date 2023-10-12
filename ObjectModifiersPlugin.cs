using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

using LSFunctions;

using UnityEngine;

using SimpleJSON;

using ObjectModifiers.Modifiers;
using ObjectModifiers.Functions;
using ObjectModifiers.Functions.Components;
using ObjectModifiers.Patchers;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Managers.Networking;
using RTFunctions.Functions.Optimization;
using RTFunctions.Functions.Optimization.Objects;

using BeatmapObject = DataManager.GameData.BeatmapObject;
using Prefab = DataManager.GameData.Prefab;

namespace ObjectModifiers
{
    [BepInPlugin("com.mecha.objectmodifiers", "Object Modifiers", "1.2.1")]
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

        public static Dictionary<string, Dictionary<string, ModifierObject>> prefabModifiers = new Dictionary<string, Dictionary<string, ModifierObject>>();

        public static List<BG> backgrounds = new List<BG>();

        public static List<AnimationObject> animationObjects = new List<AnimationObject>();

        #endregion

        #region ConfigEntries
        public static ConfigEntry<bool> editorLoadLevel { get; set; }
        public static ConfigEntry<bool> editorSavesBeforeLoad { get; set; }

        public static ConfigEntry<bool> ResetVariables { get; set; }

        #endregion

        void Awake()
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

        void Update()
        {
            for (int i = 0; i < animationObjects.Count; i++)
                animationObjects[i].Update();
        }

        [HarmonyPatch(typeof(GameManager), "SpawnPlayers")]
        [HarmonyPostfix]
        static void PlayerCollisionFix()
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
        static void UpdatePatch(GameManager __instance)
        {
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
        }

        public static void DeleteKey(string id, AudioSource audioSource)
        {
            if (audioSources.ContainsKey(id))
            {
                Destroy(audioSource);
                audioSources.Remove(id);
            }
        }

        public static void SetShowable(bool _show, float _opacity, bool _highlightObjects, Color _highlightObjectsColor, Color _highlightObjectsDoubleColor) => Debug.Log($"{className}Unused SetShowable");

        [HarmonyPatch(typeof(GameManager), "EndOfLevel")]
        [HarmonyPrefix]
        static void EndOfLevelPrefix() => modifierObjects.Clear();

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

        public static void DownloadSoundAndPlay(string id, string _sound, float _pitch = 1f, float _volume = 1f, bool _loop = false)
        {
            try
            {
                var audioType = RTFile.GetAudioType(_sound);

                if (audioType != AudioType.UNKNOWN)
                    inst.StartCoroutine(AlephNetworkManager.DownloadAudioClip(_sound, RTFile.GetAudioType(_sound), delegate (AudioClip audioClip)
                    {
                        PlaySound(id, audioClip, _pitch, _volume, _loop);
                    }, delegate (string onError)
                    {
                        Debug.Log($"{className}Error! Could not download audioclip.\n{onError}");
                    }));
            }
            catch
            {

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

        //CA [DLC1] - Hub/CA [DLC1] - Regain Control

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
                inst.StartCoroutine(FileManager.inst.LoadMusicFile(audioPath.Replace(RTFile.ApplicationDirectory, ""), delegate (AudioClip _newSound)
                {
                    _newSound.name = _level;
                    saveQueue.BeatmapSong = _newSound;

                    DataManager.inst.UpdateSettingBool("IsArcade", true);

                    SceneManager.inst.LoadScene("Game");
                }));
            }
            yield break;
        }

        public static DataManager.GameData.PrefabObject AddPrefabObjectToLevel(Prefab prefab, float startTime, Vector2 pos, Vector2 sca, float rot)
        {
            DataManager.GameData.PrefabObject prefabObject = new DataManager.GameData.PrefabObject();
            prefabObject.ID = LSText.randomString(16);
            prefabObject.prefabID = prefab.ID;

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

            if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + _path + ".ses"))
            {
                string rawProfileJSON = FileManager.inst.LoadJSONFile("profile/" + _path + ".ses");

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
                constant = false,
                command = new List<string>
                {
                    "playSoundOnline",
                    "False",
                    "1",
                    "1",
                    "False"
                },
                value = "sounds/audio.wav"
            }, //playSoundOnline
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
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "setAlpha"
                },
                value = "0"
            }, //setAlpha
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "setAlphaOther",
                    "Objects Name"
                },
                value = "0"
            }, //setAlphaOther
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "addColor",
                    "0"
                },
                value = "0"
            }, //addColor
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "addColorOther",
                    "Objects Name",
                    "0"
                },
                value = "0"
            }, //addColorOther
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = true,
                command = new List<string>
                {
                    "addColorPlayerDistance",
                    "0"
                },
                value = "0"
            }, //addColorPlayerDistance
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "updateObjects"
                },
                value = "0"
            }, //updateObjects
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Action,
                constant = false,
                command = new List<string>
                {
                    "code"
                },
                value = "float x = 1f; float y = 5f; x / y;"
            }, //code
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
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "inZenMode"
                },
                value = "0"
            }, //inZenMode
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "inNormal"
                },
                value = "0"
            }, //inNormal
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "in1Life"
                },
                value = "0"
            }, //in1Life
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "inNoHit"
                },
                value = "0"
            }, //inNoHit
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "inEditor"
                },
                value = "0"
            }, //inEditor
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "randomGreater",
                    "0",
                    "1"
                },
                value = "0"
            }, //randomGreater
            new ModifierObject.Modifier
            {
                type = ModifierObject.Modifier.Type.Trigger,
                constant = true,
                command = new List<string>
                {
                    "randomLesser",
                    "0",
                    "1"
                },
                value = "0"
            }, //randomLesser
        };

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
