using System;
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

using RTFunctions.Functions;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Managers.Networking;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;
using BasePrefab = DataManager.GameData.Prefab;
using RTFunctions.Functions.Optimization;

namespace ObjectModifiers
{
    [BepInPlugin("com.mecha.objectmodifiers", "Object Modifiers", "1.4.1")]
    [BepInDependency("com.mecha.rtfunctions")]
    [BepInProcess("Project Arrhythmia.exe")]
    public class ObjectModifiersPlugin : BaseUnityPlugin
    {
        //TODO
        //Modifiers:
        //Animation Action (Makes an object play a preset animation depending on if it's been activated.
        //Chain Object Action (With chain following and more)

        #region Variables

        public static Material blur;
        public static Material GetBlur()
        {
            var assetBundle = GetAssetBundle(RTFile.ApplicationDirectory + "BepInEx/plugins/Assets", "objectmaterials.asset");
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

        readonly Harmony harmony = new Harmony("ObjectModifiers");
        public static ObjectModifiersPlugin inst;
        public static string className = "[<color=#F5501B>ObjectModifiers</color>]\n";

        #endregion

        #region ConfigEntries
        public static ConfigEntry<bool> EditorLoadLevel { get; set; }
        public static ConfigEntry<bool> EditorSavesBeforeLoad { get; set; }

        public static ConfigEntry<bool> ResetVariables { get; set; }

        #endregion

        void Awake()
        {
            inst = this;

            EditorLoadLevel = Config.Bind("Editor", "Modifier Loads Level", false, "Any modifiers with the \"loadLevel\" function will load the level whilst in the editor. This is only to prevent the loss of progress.");
            EditorSavesBeforeLoad = Config.Bind("Editor", "Saves Before Load", false, "The level will be saved before a level is loaded using a loadLevel modifier.");

            ResetVariables = Config.Bind("Editor", "Reset Variable", false, "Resets the variables of every object when not in preview mode.");

            harmony.PatchAll(typeof(ObjectModifiersPlugin));

            blur = GetBlur();

            SetModifierTypes();

            if (!ModCompatibility.mods.ContainsKey("ObjectModifiers"))
            {
                var mod = new ModCompatibility.Mod(inst, GetType());
                mod.Methods.Add("AddModifierToObject", (Action<BeatmapObject, int>)AddModifierToObject);
                ModCompatibility.mods.Add("ObjectModifiers", mod);
            }

            // Plugin startup logic
            Logger.LogInfo($"Plugin Object Modifiers is loaded!");
        }

        [HarmonyPatch(typeof(GameManager), "SpawnPlayers")]
        [HarmonyPostfix]
        static void PlayerCollisionFix()
        {
            foreach (var player in InputDataManager.inst.players)
            {
                if (player.player)
                {
                    var p = player.player.gameObject.GetComponentInChildren<Collider2D>();
                    p.isTrigger = false;
                    p.attachedRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                    //player.player.gameObject.GetComponentInChildren<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                }
            }
        }

        [HarmonyPatch(typeof(GameManager), "Update")]
        [HarmonyPostfix]
        static void UpdatePatch(GameManager __instance)
        {
            foreach (var b in DataManager.inst.gameData.beatmapObjects.OrderBy(x => x.StartTime))
            {
                var beatmapObject = (BeatmapObject)b;

                if (beatmapObject.modifiers.Count > 0)
                {
                    beatmapObject.modifiers.Where(x => x.Action == null || x.Trigger == null || x.Inactive == null).ToList().ForEach(delegate (BeatmapObject.Modifier modifier)
                    {
                        modifier.Action = ModifierMethods.Action;
                        modifier.Trigger = ModifierMethods.Trigger;
                        modifier.Inactive = ModifierMethods.Inactive;
                    });

                    var actions = beatmapObject.modifiers.Where(x => x.type == BeatmapObject.Modifier.Type.Action);
                    var triggers = beatmapObject.modifiers.Where(x => x.type == BeatmapObject.Modifier.Type.Trigger);

                    if (beatmapObject.TimeWithinLifespan())
                    {
                        if (triggers.Count() > 0)
                        {
                            if (triggers.All(x => !x.active && (x.Trigger(x) && !x.not || !x.Trigger(x) && x.not)))
                            {
                                foreach (var act in actions)
                                {
                                    if (!act.active)
                                    {
                                        if (!act.constant)
                                            act.active = true;

                                        act.Action?.Invoke(act);
                                    }
                                }

                                foreach (var trig in triggers)
                                {
                                    if (!trig.constant)
                                        trig.active = true;
                                }
                            }
                            else
                            {
                                foreach (var act in actions)
                                {
                                    act.active = false;
                                    act.Inactive?.Invoke(act);
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
                                    act.Action?.Invoke(act);
                                }
                            }
                        }
                    }
                    else
                    {
                        foreach (var act in actions)
                        {
                            act.active = false;
                            act.Inactive?.Invoke(act);
                        }

                        foreach (var trig in triggers)
                        {
                            trig.active = false;
                            trig.Inactive?.Invoke(trig);
                        }
                    }
                }

                if (EditorManager.inst && EditorManager.inst.isEditing && ResetVariables.Value)
                    beatmapObject.integerVariable = 0;
            }

            foreach (var audioSource in audioSources)
            {
                try
                {
                    if (DataManager.inst.gameData.beatmapObjects.ID(audioSource.Key) == null || !DataManager.inst.gameData.beatmapObjects.ID(audioSource.Key).TimeWithinLifespan())
                        DeleteKey(audioSource.Key, audioSource.Value);
                }
                catch
                {

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

        #region Modifier Functions

        public static void GetSoundPath(string id, string _sound, bool fromSoundLibrary = false, float _pitch = 1f, float _volume = 1f, bool _loop = false)
        {
            string text = RTFile.ApplicationDirectory + "beatmaps/soundlibrary/" + _sound;

            if (!fromSoundLibrary)
                text = RTFile.BasePath + _sound;

            if (!_sound.Contains(".ogg") && RTFile.FileExists(text + ".ogg"))
                text += ".ogg";
            
            if (!_sound.Contains(".wav") && RTFile.FileExists(text + ".wav"))
                text += ".wav";

            //Debug.LogFormat("{0}Filepath: {1}", className, text);
            if (RTFile.FileExists(text))
            {
                //Debug.LogFormat("{0}File exists so play", className);
                //inst.StartCoroutine(AlephNetworkManager.DownloadAudioClip("file://" + text, RTFile.GetAudioType(text), delegate (AudioClip audioClip)
                //{
                //    audioClip.name = _sound;
                //    PlaySound(id, audioClip, _pitch, _volume, _loop);
                //}));
                inst.StartCoroutine(LoadMusicFileRaw(text, delegate (AudioClip _newSound)
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
            audioSource.pitch = _pitch * AudioManager.inst.CurrentAudioSource.pitch;
            audioSource.volume = Mathf.Clamp(_volume, 0f, 2f) * AudioManager.inst.sfxVol;
            audioSource.Play();

            float x = _pitch * AudioManager.inst.CurrentAudioSource.pitch;
            if (x == 0f)
                x = 1f;
            if (x < 0f)
                x = -x;

            if (!_loop)
                inst.StartCoroutine(AudioManager.inst.DestroyWithDelay(audioSource, _clip.length / x));
            else if (!audioSources.ContainsKey(id))
                audioSources.Add(id, audioSource);
        }

        public static IEnumerator LoadMusicFileRaw(string _filepath, Action<AudioClip> callback)
        {
            if (!File.Exists(_filepath))
            {
                Debug.LogFormat("{0}Could not load Music file [{1}]", new object[]
                {
                    FileManager.className,
                    _filepath
                });
            }
            else
            {
                var www = new WWW("file://" + _filepath);
                while (!www.isDone)
                    yield return null;

                AudioClip beatmapAudio = www.GetAudioClip(false, false);
                while (beatmapAudio.loadState != AudioDataLoadState.Loaded)
                {
                    yield return null;
                }
                callback(beatmapAudio);
                beatmapAudio = null;
                www = null;
            }
            yield break;
        }

        public static Dictionary<string, AudioSource> audioSources = new Dictionary<string, AudioSource>();

        //CA [DLC1] - Hub/CA [DLC1] - Regain Control

        public static IEnumerator ParseStoryLevel(string _level)
        {
            Debug.LogFormat("{0}Parsing {1}...", className, _level);

            string audioPath = RTFile.ApplicationDirectory + $"beatmaps/story/{_level}/level.ogg";
            string beatmapJSON = FileManager.inst.LoadJSONFile($"beatmaps/story/{_level}/level.lsb");

            string rawMetadataJSON = FileManager.inst.LoadJSONFile($"beatmaps/story/{_level}/metadata.lsb");

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

        public static PrefabObject AddPrefabObjectToLevel(BasePrefab prefab, float startTime, Vector2 pos, Vector2 sca, float rot, int repeatCount, float repeatOffsetTime, float speed)
        {
            var prefabObject = new PrefabObject();
            prefabObject.ID = LSText.randomString(16);
            prefabObject.prefabID = prefab.ID;

            prefabObject.StartTime = startTime;

            prefabObject.events[0].eventValues[0] = pos.x;
            prefabObject.events[0].eventValues[1] = pos.y;
            prefabObject.events[1].eventValues[0] = sca.x;
            prefabObject.events[1].eventValues[1] = sca.y;
            prefabObject.events[2].eventValues[0] = rot;

            prefabObject.RepeatCount = repeatCount;
            prefabObject.RepeatOffsetTime = repeatOffsetTime;
            prefabObject.speed = speed;

            prefabObject.fromModifier = true;

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

        public static IEnumerator ActivateModifier(BeatmapObject beatmapObject, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (beatmapObject.modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == BeatmapObject.Modifier.Type.Trigger, out BeatmapObject.Modifier modifier))
            {
                modifier.Result = "death hd";
            }
        }

        #endregion

        public static void AddModifierToObject(BeatmapObject beatmapObject, int index)
        {
            var copy = BeatmapObject.Modifier.DeepCopy(modifierTypes[index]);
            copy.modifierObject = beatmapObject;
            beatmapObject.modifiers.Add(copy);
        }

        public static void SetModifierTypes() => ModCompatibility.sharedFunctions.Add("DefaultModifierList", modifierTypes);

        public static List<BeatmapObject.Modifier> modifierTypes = new List<BeatmapObject.Modifier>
        {
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "setPitch"
                },
                value = "1"
            }, //setPitch
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "addPitch"
                },
                value = "0.1"
            }, //addPitch
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "setMusicTime"
                },
                value = "0"
            }, //setMusicTime
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playSound",
                    "False",
                    "1",
                    "1",
                    "False"
                },
                value = "sounds/audio.wav"
            }, //playSound
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playSoundOnline",
                    "False",
                    "1",
                    "1",
                    "False"
                },
                value = "sounds/audio.wav"
            }, //playSoundOnline
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "loadLevel"
                },
                value = "level name"
            }, //loadLevel
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "loadLevelInternal"
                },
                value = "level name"
            }, //loadLevelInternal
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "blur",
                    "False"
                },
                value = "0.5"
            }, //blur
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
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
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
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
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "spawnPrefab",
                    "0", // Pos X
                    "0", // Pos Y
                    "1", // Sca X
                    "1", // Sca Y
                    "0", // Rot
                    "0", // Repeat Count
                    "0", // Repeat Offset Time
                    "1", // Speed
                },
                value = "0" // Index
            }, //spawnPrefab
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerHeal"
                },
                value = "1"
            }, //playerHeal
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerHealAll"
                },
                value = "1"
            }, //playerHealAll
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerHit"
                },
                value = "1"
            }, //playerHit
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerHitAll"
                },
                value = "1"
            }, //playerHitAll
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerKill"
                },
                value = ""
            }, //playerKill
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerKillAll"
                },
                value = ""
            }, //playerKillAll
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerMove",
                    "1",
                    "0",
                    "False"
                },
                value = "0,0"
            }, //playerMove
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerMoveAll",
                    "1",
                    "0",
                    "False"
                },
                value = "0,0"
            }, //playerMoveAll
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerMoveX",
                    "1",
                    "0",
                    "False"
                },
                value = "0"
            }, //playerMoveX
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerMoveXAll",
                    "1",
                    "0",
                    "False"
                },
                value = "0"
            }, //playerMoveXAll
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerMoveY",
                    "1",
                    "0",
                    "False"
                },
                value = "0"
            }, //playerMoveY
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerMoveYAll",
                    "1",
                    "0",
                    "False"
                },
                value = "0"
            }, //playerMoveYAll
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerRotate",
                    "1",
                    "0",
                    "False"
                },
                value = "0"
            }, //playerRotate
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerRotateAll",
                    "1",
                    "0",
                    "False"
                },
                value = "0"
            }, //playerRotateAll
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerBoost"
                },
                value = "0"
            }, //playerBoost
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerBoostAll"
                },
                value = "0"
            }, //playerBoostAll
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerDisableBoost"
                },
                value = "0"
            }, //playerDisableBoost
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "playerDisableBoostAll"
                },
                value = "0"
            }, //playerDisableBoost
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "showMouse"
                },
                value = "0"
            }, //showMouse
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "hideMouse"
                },
                value = "0"
            }, //hideMouse
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "addVariable",
                    "Object Group"
                },
                value = "1"
            }, //addVariable
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "subVariable",
                    "Object Group"
                },
                value = "1"
            }, //subVariable
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "setVariable",
                    "Object Group"
                },
                value = "1"
            }, //setVariable
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "setVariableRandom",
                    "0",
                    "2",
                },
                value = "Object Group"
            }, //setVariableRandom
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "quitToMenu"
                },
                value = "0"
            }, //quitToMenu
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "quitToArcade"
                },
                value = "0"
            }, //quitToArcade
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "disableObject"
                },
                value = "0"
            }, //disableObject
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "disableObjectTree"
                },
                value = "0"
            }, //disableObjectTree
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "save",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //save
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "saveVariable",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //saveVariable
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "reactivePos",
                    "0",
                    "0",
                    "0",
                    "0"
                },
                value = "1"
            }, //reactivePos
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "reactiveSca",
                    "0",
                    "0",
                    "1",
                    "1"
                },
                value = "1"
            }, //reactiveSca
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "reactiveRot",
                    "0"
                },
                value = "1"
            }, //reactiveRot
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "reactiveCol",
                    "0",
                    "0"
                },
                value = "1"
            }, //reactiveCol
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "reactiveColLerp",
                    "0",
                    "0"
                },
                value = "1"
            }, //reactiveColLerp
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "reactivePosChain",
                    "0",
                    "0",
                    "0",
                    "0"
                },
                value = "1"
            }, //reactivePosChain
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "reactiveScaChain",
                    "0",
                    "0",
                    "1",
                    "1"
                },
                value = "1"
            }, //reactiveScaChain
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "reactiveRotChain",
                    "0"
                },
                value = "1"
            }, //reactiveRotChain
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "setPlayerModel",
                    "0"
                },
                value = "0"
            }, //setPlayerModel
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "blackHole",
                },
                value = "0.01"
            }, //blackHole
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "eventOffset",
                    "0", // Type (Move, Zoom, Rotate, etc)
                    "0", // Value Index (X, Y, etc)
                },
                value = "1"
            }, //eventOffset
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "eventOffsetAnimate",
                    "0", // Type (Move, Zoom, Rotate, etc)
                    "0", // Value Index (X, Y, etc)
                    "1", // Time
                    "Linear", // Ease
                    "False", // Relative
                },
                value = "1" // Value
            }, //eventOffsetAnimate
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "setAlpha"
                },
                value = "0"
            }, //setAlpha
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "setAlphaOther",
                    "Object Group",
                },
                value = "0"
            }, //setAlphaOther
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "addColor",
                    "0"
                },
                value = "0"
            }, //addColor
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "addColorOther",
                    "Object Group",
                    "0", // Lerp
                },
                value = "0"
            }, //addColorOther
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "addColorPlayerDistance",
                    "0"
                },
                value = "0"
            }, //addColorPlayerDistance
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "lerpColor",
                    "0"
                },
                value = "0"
            }, //lerpColor
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "lerpColorOther",
                    "Object Group",
                    "0", // Lerp
                },
                value = "0"
            }, //lerpColorOther
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "copyColor",
                },
                value = "Object Group"
            }, //copyColor
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "copyColorOther",
                },
                value = "Object Group"
            }, //copyColorOther
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "updateObjects"
                },
                value = "0"
            }, //updateObjects
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "updateObject",
                },
                value = "Object Group"
            }, //updateObject
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "signalModifier",
                    "Object Group", // Objects with tag
                },
                value = "0.5"
            }, //signalModifier
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "editorNotify",
                    "2",
                    "1"
                },
                value = "Modifier triggered!"
            }, //editorNotify
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "setText"
                },
                value = "Text"
            }, //setText
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "setTextOther",
                    "Objects Name", // Objects with tag
                },
                value = "Text"
            }, //setTextOther
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "addText"
                },
                value = "Text"
            }, //addText
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "addTextOther",
                    "Objects Name", // Objects with tag
                },
                value = "Text"
            }, //addTextOther
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "removeText"
                },
                value = "1"
            }, //removeText
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "removeTextOther",
                    "Objects Name", // Objects with tag
                },
                value = "1"
            }, //removeTextOther
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "removeTextAt"
                },
                value = "1"
            }, //removeTextAt
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "removeTextOtherAt",
                    "Objects Name", // Objects with tag
                },
                value = "1"
            }, //removeTextOtherAt
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "animateObject",
                    "0", // Pos / Sca / Rot
                    "0", // X
                    "0", // Y
                    "0", // Z
                    "True", // Relative
                    "Linear", // Easing
                },
                value = "1"
            }, //animateObject
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = false,
                commands = new List<string>
                {
                    "animateObjectOther",
                    "0", // Pos / Sca / Rot
                    "0", // X
                    "0", // Y
                    "0", // Z
                    "True", // Relative
                    "Linear", // Easing
                    "Object Group", // Objects with tag
                },
                value = "1"
            }, //animateObjectOther
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Action,
                constant = true,
                commands = new List<string>
                {
                    "copyAxis",
                    "0", // From Type
                    "0", // From Axis
                    "0", // To Type
                    "0", // To Axis
                    "0", // Delay
                    "1", // Multiply
                },
                value = "Object Group"
            }, //copyAxis
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerCollide"
                },
                value = ""
            }, //playerCollide
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerHealthEquals"
                },
                value = "3"
            }, //playerHealthEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerHealthLesserEquals"
                },
                value = "3"
            }, //playerHealthLesserEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerHealthGreaterEquals"
                },
                value = "3"
            }, //playerHealthGreaterEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerHealthLesser"
                },
                value = "3"
            }, //playerHealthLesser
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerHealthGreater"
                },
                value = "3"
            }, //playerHealthGreater
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerDeathsEquals"
                },
                value = "1"
            }, //playerDeathsEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerDeathsLesserEquals"
                },
                value = "1"
            }, //playerDeathsLesserEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerDeathsGreaterEquals"
                },
                value = "1"
            }, //playerDeathsGreaterEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerDeathsLesser"
                },
                value = "1"
            }, //playerDeathsLesser
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerDeathsGreater"
                },
                value = "1"
            }, //playerDeathsGreater
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerMoving"
                },
                value = "0"
            }, //playerMoving
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerBoosting"
                },
                value = "0"
            }, //playerBoosting
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerAlive"
                },
                value = "0"
            }, //playerAlive
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerDistanceLesser"
                },
                value = "5"
            }, //playerDistanceLesser
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "playerDistanceGreater"
                },
                value = "5"
            }, //playerDistanceGreater
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "onPlayerHit"
                },
                value = ""
            }, //onPlayerHit
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "bulletCollide"
                },
                value = ""
            }, //bulletCollide
            //new BeatmapObject.Modifier
            //{
            //    type = BeatmapObject.Modifier.Type.Trigger,
            //    constant = true,
            //    commands = new List<string>
            //    {
            //        "objectCollide"
            //    },
            //    value = "Object Group"
            //}, //bulletCollide
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "keyPressDown"
                },
                value = "0"
            }, //keyPressDown
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "keyPress"
                },
                value = "0"
            }, //keyPress
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "keyPressUp"
                },
                value = "0"
            }, //keyPressUp
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "mouseButtonDown"
                },
                value = "0"
            }, //mouseButtonDown
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "mouseButton"
                },
                value = "0"
            }, //mouseButton
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "mouseButtonUp"
                },
                value = "0"
            }, //mouseButtonUp
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "mouseOver"
                },
                value = "0"
            }, //mouseOver
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "mouseOverSignalModifier",
                    "Objects Name"
                },
                value = "0"
            }, //mouseOverSignalModifier
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "loadEquals",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "loadLesserEquals",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadLesserEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "loadGreaterEquals",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadGreaterEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "loadLesser",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadLesser
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "loadGreater",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadGreater
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "loadExists",
                    "save_file",
                    "chapter",
                    "data"
                },
                value = "0"
            }, //loadExists
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "variableEquals"
                },
                value = "1"
            }, //variableEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "variableLesserEquals"
                },
                value = "1"
            }, //variableLesserEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "variableGreaterEquals"
                },
                value = "1"
            }, //variableGreaterEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "variableLesser"
                },
                value = "1"
            }, //variableLesser
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "variableGreater"
                },
                value = "1"
            }, //variableGreater
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "variableOtherEquals",
                    "Object Name"
                },
                value = "1"
            }, //variableOtherEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "variableOtherLesserEquals",
                    "Object Name"
                },
                value = "1"
            }, //variableOtherLesserEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "variableOtherGreaterEquals",
                    "Object Name"
                },
                value = "1"
            }, //variableOtherGreaterEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "variableOtherLesser",
                    "Object Name"
                },
                value = "1"
            }, //variableOtherLesser
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "variableOtherGreater",
                    "Object Name"
                },
                value = "1"
            }, //variableOtherGreater
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "pitchEquals"
                },
                value = "1"
            }, //pitchEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "pitchLesserEquals"
                },
                value = "1"
            }, //pitchLesserEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "pitchGreaterEquals"
                },
                value = "1"
            }, //pitchGreaterEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "pitchLesser"
                },
                value = "1"
            }, //pitchLesser
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "pitchGreater"
                },
                value = "1"
            }, //pitchGreater
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "inZenMode"
                },
                value = "0"
            }, //inZenMode
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "inNormal"
                },
                value = "0"
            }, //inNormal
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "in1Life"
                },
                value = "0"
            }, //in1Life
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "inNoHit"
                },
                value = "0"
            }, //inNoHit
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "inEditor"
                },
                value = "0"
            }, //inEditor
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "randomGreater",
                    "0",
                    "1"
                },
                value = "0"
            }, //randomGreater
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "randomLesser",
                    "0",
                    "1"
                },
                value = "0"
            }, //randomLesser
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "randomEquals",
                    "0",
                    "1"
                },
                value = "0"
            }, //randomEquals
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "requireSignal"
                },
                value = "0"
            }, //requireSignal
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "musicTimeGreater"
                },
                value = "0"
            }, //musicTimeGreater
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "musicTimeLesser"
                },
                value = "0"
            }, //musicTimeLesser
            new BeatmapObject.Modifier
            {
                type = BeatmapObject.Modifier.Type.Trigger,
                constant = true,
                commands = new List<string>
                {
                    "musicPlaying"
                },
                value = "0"
            }, //musicPlaying
        };
    }
}
