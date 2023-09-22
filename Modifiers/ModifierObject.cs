using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using ObjectModifiers.Functions;
using ObjectModifiers.Functions.Components;

using SimpleJSON;
using DG.Tweening;

using RTFunctions.Functions;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;

using BeatmapObject = DataManager.GameData.BeatmapObject;
using PrefabObject = DataManager.GameData.PrefabObject;
using Prefab = DataManager.GameData.Prefab;
using Ease = RTFunctions.Functions.Animation.Ease;

namespace ObjectModifiers.Modifiers
{
    public class ModifierObject
    {
        public ModifierObject()
        {
        }

        public ModifierObject(BeatmapObject _beatmapObject, List<Modifier> _modifiers)
        {
            beatmapObject = _beatmapObject;
            modifiers = _modifiers;
        }

        public int variable = 0;

        public BeatmapObject beatmapObject;

        //public DelayTracker delayTracker;

        //public GameObject gameObject;

        //public GameObject top;

        //public Renderer renderer;

        public ObjectOptimization optimization;

        //public Collider2D collider;

        public ParticleSystem ps;
        public TrailRenderer tr;

        public bool logged = false;

        public List<Modifier> modifiers = new List<Modifier>();

        public class Modifier
        {
            public static Modifier DeepCopy(Modifier _orig)
            {
                var modifier = new Modifier();
                modifier.type = _orig.type;
                modifier.command = new List<string>();
                foreach (var l in _orig.command)
                {
                    modifier.command.Add(l);
                }
                modifier.value = _orig.value;
                modifier.modifierObject = _orig.modifierObject;
                modifier.not = _orig.not;
                modifier.constant = _orig.constant;

                return modifier;
            }
            
            public Modifier()
            {
            }

            public Modifier(Type _type, string _value)
            {
                type = _type;
                value = _value;
            }

            public Modifier(Type _type, string _command, string _value, BeatmapObject _beatmapObject)
            {
                command[0] = _command;
                type = _type;
                value = _value;
                modifierObject = _beatmapObject;
            }

            public Modifier(BeatmapObject _beatmapObject)
            {
                modifierObject = _beatmapObject;
            }

            public bool constant = true;

            public enum Type
            {
                Trigger,
                Action
            }

            public ModifierObject refModifier;
            public BeatmapObject modifierObject;

            public Type type = Type.Action;
            public string value;
            public bool active = false;
            public List<string> command = new List<string>
            {
                ""
            };

            public bool not = false;

            private object result;

            private List<int> currentHealths = new List<int>
            {
                -1,
                -1,
                -1,
                -1
            };

            Vector3 playerPos;

            public bool Trigger()
            {
                switch (command[0])
                {
                    case "playerCollide":
                        {
                            if (modifierObject.IsTouchingPlayer())
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerHealthEquals":
                        {
                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.All(x => x.health == int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerHealthLesserEquals":
                        {
                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.All(x => x.health <= int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerHealthGreaterEquals":
                        {
                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.All(x => x.health >= int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerHealthLesser":
                        {
                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.All(x => x.health < int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerHealthGreater":
                        {
                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.All(x => x.health > int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerMoving":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                    if (playerPos == null)
                                        playerPos = player.position;

                                    if (player.position != playerPos)
                                    {
                                        playerPos = player.position;
                                        return true;
                                    }
                                }
                            }
                            break;
                        }
                    case "playerBoosting":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var hit = int.Parse(value);

                                    var rt = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");

                                    if (rt != null)
                                        return (bool)rt.GetType().GetField("isBoosting", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(rt);
                                    else
                                    {
                                        if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Count > i)
                                        {
                                            var p = InputDataManager.inst.players[i].player;

                                            return (bool)p.GetType().GetField("isBoosting", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(p);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "playerAlive":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var hit = int.Parse(value);

                                    if (i == hit)
                                    {
                                        var rt = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");

                                        if (rt != null)
                                            return (bool)rt.GetType().GetProperty("PlayerAlive", BindingFlags.Public | BindingFlags.Instance).GetValue(rt);
                                        else
                                        {
                                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Count > i)
                                            {
                                                var p = InputDataManager.inst.players[i].player;

                                                return (bool)p.GetType().GetProperty("PlayerAlive", BindingFlags.Public | BindingFlags.Instance).GetValue(p);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "playerDeathsEquals":
                        {
                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count == int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerDeathsLesserEquals":
                        {
                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count <= int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerDeathsGreaterEquals":
                        {
                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count >= int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerDeathsLesser":
                        {
                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count < int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerDeathsGreater":
                        {
                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count > int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "playerDistanceGreater":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                                    return Vector2.Distance(player.transform.position, Objects.beatmapObjects[modifierObject.id].gameObject.transform.position) > float.Parse(value);
                                }
                            }

                            break;
                        }
                    case "playerDistanceLesser":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                                    return Vector2.Distance(player.transform.position, Objects.beatmapObjects[modifierObject.id].gameObject.transform.position) < float.Parse(value);
                                }
                            }

                            break;
                        }
                    case "playerCountEquals":
                        {
                            if (InputDataManager.inst.players.Count == int.Parse(value))
                                return true;

                            break;
                        }
                    case "playerCountLesserEquals":
                        {
                            if (InputDataManager.inst.players.Count <= int.Parse(value))
                                return true;

                            break;
                        }
                    case "playerCountGreaterEquals":
                        {
                            if (InputDataManager.inst.players.Count >= int.Parse(value))
                                return true;

                            break;
                        }
                    case "playerCountLesser":
                        {
                            if (InputDataManager.inst.players.Count < int.Parse(value))
                                return true;

                            break;
                        }
                    case "playerCountGreater":
                        {
                            if (InputDataManager.inst.players.Count > int.Parse(value))
                                return true;

                            break;
                        }

                    case "keyPressDown":
                        {
                            if (Input.GetKeyDown((KeyCode)int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "keyPress":
                        {
                            if (Input.GetKey((KeyCode)int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "keyPressUp":
                        {
                            if (Input.GetKeyUp((KeyCode)int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "mouseButtonDown":
                        {
                            if (Input.GetMouseButtonDown(int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "mouseButton":
                        {
                            if (Input.GetMouseButton(int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "mouseButtonUp":
                        {
                            if (Input.GetMouseButtonUp(int.Parse(value)))
                            {
                                return true;
                            }
                            break;
                        }
                    case "mouseOver":
                        {
                            if (modifierObject != null && refModifier != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                            {
                                if (refModifier.optimization == null)
                                {
                                    refModifier.optimization = Objects.beatmapObjects[modifierObject.id].gameObject.AddComponent<ObjectOptimization>();
                                }

                                if (refModifier.optimization != null)
                                {
                                    return refModifier.optimization.hovered;
                                }
                            }
                            break;
                        }
                    case "bulletCollide":
                        {
                            if (modifierObject != null && refModifier != null && modifierObject.TimeWithinLifespan() && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                            {
                                if (refModifier.optimization == null)
                                {
                                    refModifier.optimization = Objects.beatmapObjects[modifierObject.id].gameObject.AddComponent<ObjectOptimization>();
                                    refModifier.optimization.beatmapObject = modifierObject;
                                    refModifier.optimization.modifierObject = refModifier;
                                }

                                if (refModifier.optimization != null)
                                    return refModifier.optimization.bulletOver;
                            }
                            break;
                        }
                    case "loadEquals":
                        {
                            if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + command[1] + ".ses"))
                            {
                                string rawProfileJSON = FileManager.inst.LoadJSONFile("profile/" + command[1] + ".ses");

                                if (!string.IsNullOrEmpty(rawProfileJSON))
                                {
                                    JSONNode jsonnode = JSON.Parse(rawProfileJSON);

                                    if (!string.IsNullOrEmpty(jsonnode[command[2]][command[3]]))
                                    {
                                        if (float.Parse(jsonnode[command[2]][command[3]]) == float.Parse(value))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "loadLesserEquals":
                        {
                            if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + command[1] + ".ses"))
                            {
                                string rawProfileJSON = FileManager.inst.LoadJSONFile("profile/" + command[1] + ".ses");

                                if (!string.IsNullOrEmpty(rawProfileJSON))
                                {
                                    JSONNode jsonnode = JSON.Parse(rawProfileJSON);

                                    if (!string.IsNullOrEmpty(jsonnode[command[2]][command[3]]))
                                    {
                                        if (float.Parse(jsonnode[command[2]][command[3]]) <= float.Parse(value))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "loadGreaterEquals":
                        {
                            if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + command[1] + ".ses"))
                            {
                                string rawProfileJSON = FileManager.inst.LoadJSONFile("profile/" + command[1] + ".ses");

                                if (!string.IsNullOrEmpty(rawProfileJSON))
                                {
                                    JSONNode jsonnode = JSON.Parse(rawProfileJSON);

                                    if (!string.IsNullOrEmpty(jsonnode[command[2]][command[3]]))
                                    {
                                        if (float.Parse(jsonnode[command[2]][command[3]]) >= float.Parse(value))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "loadLesser":
                        {
                            if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + command[1] + ".ses"))
                            {
                                string rawProfileJSON = FileManager.inst.LoadJSONFile("profile/" + command[1] + ".ses");

                                if (!string.IsNullOrEmpty(rawProfileJSON))
                                {
                                    JSONNode jsonnode = JSON.Parse(rawProfileJSON);

                                    if (!string.IsNullOrEmpty(jsonnode[command[2]][command[3]]))
                                    {
                                        if (float.Parse(jsonnode[command[2]][command[3]]) < float.Parse(value))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "loadGreater":
                        {
                            if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + command[1] + ".ses"))
                            {
                                string rawProfileJSON = FileManager.inst.LoadJSONFile("profile/" + command[1] + ".ses");

                                if (!string.IsNullOrEmpty(rawProfileJSON))
                                {
                                    JSONNode jsonnode = JSON.Parse(rawProfileJSON);

                                    if (!string.IsNullOrEmpty(jsonnode[command[2]][command[3]]))
                                    {
                                        if (float.Parse(jsonnode[command[2]][command[3]]) > float.Parse(value))
                                        {
                                            return true;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "variableEquals":
                        {
                            if (refModifier != null && refModifier.variable == int.Parse(value))
                            {
                                return true;
                            }
                            break;
                        }
                    case "variableLesserEquals":
                        {
                            if (refModifier != null && refModifier.variable <= int.Parse(value))
                            {
                                return true;
                            }
                            break;
                        }
                    case "variableGreaterEquals":
                        {
                            if (refModifier != null && refModifier.variable >= int.Parse(value))
                            {
                                return true;
                            }
                            break;
                        }
                    case "variableLesser":
                        {
                            if (refModifier != null && refModifier.variable < int.Parse(value))
                            {
                                return true;
                            }
                            break;
                        }
                    case "variableGreater":
                        {
                            if (refModifier != null && refModifier.variable > int.Parse(value))
                            {
                                return true;
                            }
                            break;
                        }
                    case "variableOtherEquals":
                        {
                            if (refModifier != null && !string.IsNullOrEmpty(command[1]) && DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]) != null)
                            {
                                if (DataManager.inst.gameData.beatmapObjects.Any(x => x.name == command[1] && x.GetModifierObject() != null && x.GetModifierObject().variable == int.Parse(value)))
                                    return true;
                            }
                            break;
                        }
                    case "variableOtherLesserEquals":
                        {
                            if (refModifier != null && !string.IsNullOrEmpty(command[1]) && DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]) != null)
                            {
                                if (DataManager.inst.gameData.beatmapObjects.Any(x => x.name == command[1] && x.GetModifierObject() != null && x.GetModifierObject().variable <= int.Parse(value)))
                                    return true;
                            }
                            break;
                        }
                    case "variableOtherGreaterEquals":
                        {
                            if (refModifier != null && !string.IsNullOrEmpty(command[1]) && DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]) != null)
                            {
                                if (DataManager.inst.gameData.beatmapObjects.Any(x => x.name == command[1] && x.GetModifierObject() != null && x.GetModifierObject().variable >= int.Parse(value)))
                                    return true;
                            }
                            break;
                        }
                    case "variableOtherLesser":
                        {
                            if (refModifier != null && !string.IsNullOrEmpty(command[1]) && DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]) != null)
                            {
                                if (DataManager.inst.gameData.beatmapObjects.Any(x => x.name == command[1] && x.GetModifierObject() != null && x.GetModifierObject().variable < int.Parse(value)))
                                    return true;
                            }
                            break;
                        }
                    case "variableOtherGreater":
                        {
                            if (refModifier != null && !string.IsNullOrEmpty(command[1]) && DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]) != null)
                            {
                                if (DataManager.inst.gameData.beatmapObjects.Any(x => x.name == command[1] && x.GetModifierObject() != null && x.GetModifierObject().variable > int.Parse(value)))
                                    return true;
                            }
                            break;
                        }
                    case "pitchEquals":
                        {
                            if (AudioManager.inst.pitch == float.Parse(value))
                            {
                                return true;
                            }
                            break;
                        }
                    case "pitchLesserEquals":
                        {
                            if (AudioManager.inst.pitch <= float.Parse(value))
                            {
                                return true;
                            }
                            break;
                        }
                    case "pitchGreaterEquals":
                        {
                            if (AudioManager.inst.pitch >= float.Parse(value))
                            {
                                return true;
                            }
                            break;
                        }
                    case "pitchLesser":
                        {
                            if (AudioManager.inst.pitch < float.Parse(value))
                            {
                                return true;
                            }
                            break;
                        }
                    case "pitchGreater":
                        {
                            if (AudioManager.inst.pitch > float.Parse(value))
                            {
                                return true;
                            }
                            break;
                        }

                    case "onPlayerHit":
                        {
                            if (InputDataManager.inst.players.Count > 0)
                            {
                                for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                                {
                                    if (currentHealths[i] != -1 && currentHealths[i] > InputDataManager.inst.players[i].health)
                                    {
                                        Debug.LogFormat("{0}Player has been hit!", ObjectModifiersPlugin.className);
                                        return true;
                                    }
                                    currentHealths[i] = InputDataManager.inst.players[i].health;
                                }
                            }

                            break;
                        }

                    case "inZenMode":
                        {
                            return DataManager.inst.GetSettingInt("ArcadeDifficulty", 0) == 0;
                        }
                    case "inNormal":
                        {
                            return DataManager.inst.GetSettingInt("ArcadeDifficulty", 0) == 1;
                        }
                    case "in1Life":
                        {
                            return DataManager.inst.GetSettingInt("ArcadeDifficulty", 0) == 2;
                        }
                    case "inNoHit":
                        {
                            return DataManager.inst.GetSettingInt("ArcadeDifficulty", 0) == 3;
                        }
                    case "inEditor":
                        {
                            return EditorManager.inst != null;
                        }
                    case "randomGreater":
                        {
                            if (float.TryParse(command[1], out float x) && float.TryParse(command[2], out float y) && float.TryParse(value, out float z))
                            {
                                return Random.Range(x, y) > z;
                            }

                            break;
                        }
                    case "randomLesser":
                        {
                            if (float.TryParse(command[1], out float x) && float.TryParse(command[2], out float y) && float.TryParse(value, out float z))
                            {
                                return Random.Range(x, y) < z;
                            }

                            break;
                        }
                }
                return false;
            }

            public void Action()
            {
                switch (command[0])
                {
                    case "setPitch":
                        {
                            if (float.TryParse(value, out float num))
                            {
                                if (GameObject.Find("Game Systems/EventManager").GetComponentByName("RTEventManager"))
                                {
                                    var rt = GameObject.Find("Game Systems/EventManager").GetComponentByName("RTEventManager");

                                    rt.GetType().GetField("pitchOffset", BindingFlags.Public | BindingFlags.Instance).SetValue(rt, num);
                                }
                                else
                                {
                                    AudioManager.inst.pitch = num * GameManager.inst.getPitch();
                                }
                            }

                            break;
                        }
                    case "addPitch":
                        {
                            if (float.TryParse(value, out float num))
                            {
                                if (GameObject.Find("Game Systems/EventManager").GetComponentByName("RTEventManager"))
                                {
                                    var rt = GameObject.Find("Game Systems/EventManager").GetComponentByName("RTEventManager");

                                    var pitchCurrent = (float)rt.GetType().GetField("pitchOffset", BindingFlags.Public | BindingFlags.Instance).GetValue(rt);

                                    rt.GetType().GetField("pitchOffset", BindingFlags.Public | BindingFlags.Instance).SetValue(rt, pitchCurrent + num);
                                }
                                else
                                {
                                    AudioManager.inst.pitch = (AudioManager.inst.pitch + num) * GameManager.inst.getPitch();
                                }
                            }

                            break;
                        }
                    case "setMusicTime":
                        {
                            if (float.TryParse(value, out float num))
                                AudioManager.inst.SetMusicTime(num);
                            break;
                        }
                    case "playSound":
                        {
                            if (command.Count > 1 && bool.TryParse(command[1], out bool global) && float.TryParse(command[2], out float pitch) && float.TryParse(command[3], out float vol) && bool.TryParse(command[4], out bool loop))
                            {
                                if (command.Count < 4)
                                {
                                    command.Add("1");
                                    command.Add("False");
                                }
                                if (command.Count > 4)
                                    ObjectModifiersPlugin.GetSoundPath(modifierObject.id, value, global, pitch, vol, loop);
                            }
                            else
                            {
                                ObjectModifiersPlugin.GetSoundPath(modifierObject.id, value);
                            }
                            break;
                        }
                    case "playSoundOnline":
                        {
                            if (command.Count > 1 && bool.TryParse(command[1], out bool global) && float.TryParse(command[2], out float pitch) && float.TryParse(command[3], out float vol) && bool.TryParse(command[4], out bool loop))
                            {
                                if (command.Count < 4)
                                {
                                    command.Add("1");
                                    command.Add("False");
                                }
                                if (command.Count > 4 && !string.IsNullOrEmpty(value))
                                {
                                    ObjectModifiersPlugin.DownloadSoundAndPlay(modifierObject.id, value, pitch, vol, loop);
                                }
                            }
                            else if (!string.IsNullOrEmpty(value))
                            {
                                ObjectModifiersPlugin.DownloadSoundAndPlay(modifierObject.id, value);
                            }
                            break;
                        }
                    case "loadLevel":
                        {
                            if (EditorManager.inst != null && EditorManager.inst.isEditing)
                            {
                                if (ObjectModifiersPlugin.editorLoadLevel.Value)
                                {
                                    if (ObjectModifiersPlugin.editorSavesBeforeLoad.Value)
                                    {
                                        EditorManager.inst.SaveBeatmap();
                                    }

                                    string str = RTFile.basePath;
                                    string modBackup = RTFile.ApplicationDirectory + str + "level-modifier-backup.lsb";
                                    if (RTFile.FileExists(modBackup))
                                    {
                                        System.IO.File.Delete(modBackup);
                                    }

                                    string lvl = RTFile.ApplicationDirectory + str + "level.lsb";
                                    if (RTFile.FileExists(lvl))
                                        System.IO.File.Copy(lvl, modBackup);

                                    EditorManager.inst.StartCoroutine(EditorManager.inst.LoadLevel(value));
                                }
                            }
                            else if (EditorManager.inst == null)
                            {
                                ObjectModifiersPlugin.inst.StartCoroutine(ObjectModifiersPlugin.ParseStoryLevel(value));
                            }
                            break;
                        }
                    case "quitToMenu":
                        {
                            if (EditorManager.inst != null && !EditorManager.inst.isEditing)
                            {
                                if (ObjectModifiersPlugin.editorLoadLevel.Value)
                                {
                                    if (ObjectModifiersPlugin.editorSavesBeforeLoad.Value)
                                    {
                                        EditorManager.inst.SaveBeatmap();
                                    }

                                    string str = RTFile.basePath;
                                    if (RTFile.FileExists(RTFile.ApplicationDirectory + str + "/level-modifier-backup.lsb"))
                                    {
                                        System.IO.File.Delete(RTFile.ApplicationDirectory + str + "/level-modifier-backup.lsb");
                                    }

                                    if (RTFile.FileExists(RTFile.ApplicationDirectory + str + "/level.lsb"))
                                        System.IO.File.Copy(RTFile.ApplicationDirectory + str + "/level.lsb", RTFile.ApplicationDirectory + str + "/level-modifier-backup.lsb");

                                    EditorManager.inst.QuitToMenu();
                                }
                            }
                            else if (EditorManager.inst == null)
                            {
                                DOTween.KillAll();
                                DOTween.Clear(true);
                                DataManager.inst.gameData = null;
                                DataManager.inst.gameData = new DataManager.GameData();
                                DiscordController.inst.OnIconChange("");
                                DiscordController.inst.OnStateChange("");
                                Debug.Log("Quit to Main Menu");
                                InputDataManager.inst.players.Clear();
                                SceneManager.inst.LoadScene("Main Menu");
                            }
                            break;
                        }
                    case "quitToArcade":
                        {
                            if (EditorManager.inst != null && !EditorManager.inst.isEditing)
                            {
                                if (ObjectModifiersPlugin.editorLoadLevel.Value)
                                {
                                    if (ObjectModifiersPlugin.editorSavesBeforeLoad.Value)
                                    {
                                        EditorManager.inst.SaveBeatmap();
                                    }

                                    string str = RTFile.basePath;
                                    if (RTFile.FileExists(RTFile.ApplicationDirectory + str + "/level-modifier-backup.lsb"))
                                    {
                                        System.IO.File.Delete(RTFile.ApplicationDirectory + str + "/level-modifier-backup.lsb");
                                    }

                                    if (RTFile.FileExists(RTFile.ApplicationDirectory + str + "/level.lsb"))
                                        System.IO.File.Copy(RTFile.ApplicationDirectory + str + "/level.lsb", RTFile.ApplicationDirectory + str + "/level-modifier-backup.lsb");

                                    GameManager.inst.QuitToArcade();
                                }
                            }
                            else if (EditorManager.inst == null)
                            {
                                GameManager.inst.QuitToArcade();
                            }
                            break;
                        }
                    case "blur":
                        {
                            if (modifierObject != null && modifierObject.objectType != BeatmapObject.ObjectType.Empty && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null && Objects.beatmapObjects[modifierObject.id].renderer != null)
                            {
                                var rend = Objects.beatmapObjects[modifierObject.id].renderer;
                                rend.material = ObjectModifiersPlugin.blur;
                                if (command.Count > 1 && bool.Parse(command[1]) == true)
                                {
                                    //float a = ObjectModifiersPlugin.customSequences[modifierObject.id].opacity - 1f;
                                    //a = -a;

                                    rend.material.SetFloat("_blurSizeXY", -(Interpolate() - 1f) * float.Parse(value));
                                }
                                else
                                    rend.material.SetFloat("_blurSizeXY", float.Parse(value));
                            }
                            break;
                        }
                    case "particleSystem":
                        {
                            if (refModifier != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                            {
                                var mod = Objects.beatmapObjects[modifierObject.id].gameObject;

                                if (refModifier.ps == null)
                                {
                                    refModifier.ps = mod.AddComponent<ParticleSystem>();

                                    var mat = mod.GetComponent<ParticleSystemRenderer>();
                                    mat.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                                    mat.material.color = Color.white;
                                    mat.trailMaterial = mat.material;
                                    mat.renderMode = ParticleSystemRenderMode.Mesh;

                                    var s = int.Parse(command[1]);
                                    var so = int.Parse(command[2]);

                                    s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                                    so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                                    if (s != 4 && s != 6)
                                    {
                                        mat.mesh = ObjectManager.inst.objectPrefabs[s].options[so].GetComponentInChildren<MeshFilter>().mesh;
                                    }
                                    else
                                    {
                                        mat.mesh = ObjectManager.inst.objectPrefabs[0].options[0].GetComponentInChildren<MeshFilter>().mesh;
                                    }

                                    var psMain = refModifier.ps.main;
                                    var psEmission = refModifier.ps.emission;

                                    psMain.simulationSpace = ParticleSystemSimulationSpace.World;

                                    psMain.startSpeed = float.Parse(command[9]);

                                    if (constant)
                                        refModifier.ps.emissionRate = float.Parse(command[10]);
                                    else
                                    {
                                        refModifier.ps.emissionRate = 0f;
                                        psMain.loop = false;
                                        psEmission.burstCount = int.Parse(command[10]);
                                        psMain.duration = float.Parse(command[11]);
                                    }

                                    var rotationOverLifetime = refModifier.ps.rotationOverLifetime;
                                    rotationOverLifetime.enabled = true;
                                    rotationOverLifetime.separateAxes = true;
                                    rotationOverLifetime.xMultiplier = 0f;
                                    rotationOverLifetime.yMultiplier = 0f;
                                    rotationOverLifetime.zMultiplier = float.Parse(command[8]);

                                    var forceOverLifetime = refModifier.ps.forceOverLifetime;
                                    forceOverLifetime.enabled = true;
                                    forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                                    forceOverLifetime.xMultiplier = float.Parse(command[12]);
                                    forceOverLifetime.yMultiplier = float.Parse(command[13]);

                                    var particlesTrail = refModifier.ps.trails;
                                    particlesTrail.enabled = bool.Parse(command[14]);
                                    
                                    var colorOverLifetime = refModifier.ps.colorOverLifetime;
                                    colorOverLifetime.enabled = true;
                                    var psCol = colorOverLifetime.color;

                                    float alphaStart = float.Parse(command[4]);
                                    float alphaEnd = float.Parse(command[5]);

                                    var gradient = new Gradient();
                                    gradient.alphaKeys = new GradientAlphaKey[2]
                                    {
                                        new GradientAlphaKey(alphaStart, 0f),
                                        new GradientAlphaKey(alphaEnd, 1f)
                                    };
                                    gradient.colorKeys = new GradientColorKey[2]
                                    {
                                        new GradientColorKey(Color.white, 0f),
                                        new GradientColorKey(Color.white, 1f)
                                    };

                                    psCol.gradient = gradient;

                                    colorOverLifetime.color = psCol;

                                    var sizeOverLifetime = refModifier.ps.sizeOverLifetime;
                                    sizeOverLifetime.enabled = true;

                                    var ssss = sizeOverLifetime.size;

                                    var sizeStart = float.Parse(command[6]);
                                    var sizeEnd = float.Parse(command[7]);

                                    var curve = new AnimationCurve(new Keyframe[2]
                                    {
                                            new Keyframe(0f, sizeStart),
                                            new Keyframe(1f, sizeEnd)
                                    });

                                    ssss.curve = curve;

                                    sizeOverLifetime.size = ssss;
                                }

                                if (refModifier.ps != null)
                                {
                                    var psMain = refModifier.ps.main;
                                    var psEmission = refModifier.ps.emission;

                                    psMain.startLifetime = float.Parse(value);
                                    psEmission.enabled = !(mod.transform.lossyScale.x < 0.001f && mod.transform.lossyScale.x > -0.001f || mod.transform.lossyScale.y < 0.001f && mod.transform.lossyScale.y > -0.001f) && mod.activeSelf && mod.activeInHierarchy;

                                    var beatmapTheme = GameManager.inst.LiveTheme;
                                    if (EventEditor.inst != null && EventEditor.inst.showTheme)
                                    {
                                        beatmapTheme = EventEditor.inst.previewTheme;
                                    }

                                    psMain.startColor = beatmapTheme.GetObjColor(int.Parse(command[3]));

                                    if (!constant && !psMain.loop)
                                    {
                                        refModifier.ps.Play();
                                    }
                                }
                            }

                            break;
                        }
                    case "trailRenderer":
                        {
                            if (refModifier != null && modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                            {
                                var mod = Objects.beatmapObjects[modifierObject.id].gameObject;

                                if (refModifier.tr == null)
                                {
                                    refModifier.tr = mod.AddComponent<TrailRenderer>();

                                    refModifier.tr.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                                    refModifier.tr.material.color = Color.white;
                                }
                                else
                                {
                                    if (float.TryParse(value, out float time))
                                    {
                                        refModifier.tr.time = time;
                                    }

                                    refModifier.tr.emitting = !(mod.transform.lossyScale.x < 0.001f && mod.transform.lossyScale.x > -0.001f || mod.transform.lossyScale.y < 0.001f && mod.transform.lossyScale.y > -0.001f) && mod.activeSelf && mod.activeInHierarchy;

                                    if (float.TryParse(command[1], out float startWidth) && float.TryParse(command[2], out float endWidth))
                                    {
                                        var t = mod.transform.lossyScale.magnitude * 0.576635f;
                                        refModifier.tr.startWidth = startWidth * t;
                                        refModifier.tr.endWidth = endWidth * t;
                                    }

                                    var beatmapTheme = GameManager.inst.LiveTheme;
                                    if (EventEditor.inst != null && EventEditor.inst.showTheme)
                                    {
                                        beatmapTheme = EventEditor.inst.previewTheme;
                                    }

                                    if (int.TryParse(command[3], out int startColor) && float.TryParse(command[4], out float startOpacity))
                                        refModifier.tr.startColor = LSFunctions.LSColors.fadeColor(beatmapTheme.GetObjColor(startColor), startOpacity);
                                    if (int.TryParse(command[5], out int endColor) && float.TryParse(command[6], out float endOpacity))
                                        refModifier.tr.endColor = LSFunctions.LSColors.fadeColor(beatmapTheme.GetObjColor(endColor), endOpacity);
                                }
                            }
                            break;
                        }
                    case "spawnPrefab":
                        {
                            if (!constant && int.TryParse(value, out int num) && DataManager.inst.gameData.prefabs.Count > num
                                && float.TryParse(command[1], out float posX) && float.TryParse(command[2], out float posY)
                                && float.TryParse(command[3], out float scaX) && float.TryParse(command[4], out float scaY) && float.TryParse(command[5], out float rot))
                            {
                                result = ObjectModifiersPlugin.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[num],
                                    AudioManager.inst.CurrentAudioSource.time,
                                    new Vector2(posX, posY),
                                    new Vector2(scaX, scaY),
                                    rot);
                            }

                            break;
                        }
                    case "playerHit":
                        {
                            if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !constant)
                                if (Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                                {
                                    var i = ObjectExtensions.ClosestPlayer(Objects.beatmapObjects[modifierObject.id].gameObject);

                                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && int.TryParse(value, out int hit))
                                    {
                                        var rt = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");

                                        if (rt != null)
                                        {
                                            rt.GetType().GetMethod("PlayerHit", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rt, new object[] { });
                                        }
                                        else
                                        {
                                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Count > i)
                                            {
                                                var p = InputDataManager.inst.players[i].player;

                                                p.GetType().GetMethod("PlayerHit", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, new object[] { });
                                            }
                                        }

                                        if (hit > 1)
                                        {
                                            InputDataManager.inst.players[i].health -= hit + 1;
                                        }
                                    }
                                }

                            break;
                        }
                    case "playerHitAll":
                        {
                            if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !constant)
                                for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                                {
                                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && int.TryParse(value, out int hit))
                                    {
                                        var rt = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");

                                        if (rt != null)
                                            rt.GetType().GetMethod("PlayerHit", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rt, new object[] { });
                                        else
                                        {
                                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Count > i)
                                            {
                                                var p = InputDataManager.inst.players[i].player;

                                                p.GetType().GetMethod("PlayerHit", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, new object[] { });
                                            }
                                        }

                                        if (hit > 1)
                                        {
                                            InputDataManager.inst.players[i].health -= hit + 1;
                                        }
                                    }
                                }
                            break;
                        }
                    case "playerHeal":
                        {
                            if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !constant)
                                if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                                {
                                    var i = ObjectExtensions.ClosestPlayer(Objects.beatmapObjects[modifierObject.id].gameObject);

                                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && int.TryParse(value, out int hit))
                                    {
                                        InputDataManager.inst.players[i].health += hit;

                                        var rt = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");

                                        if (rt != null)
                                            rt.GetType().GetMethod("UpdateTail", BindingFlags.Public | BindingFlags.Instance).Invoke(rt, new object[] { InputDataManager.inst.players[i].health, Vector3.zero });
                                        else
                                        {
                                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Count > i)
                                            {
                                                var p = InputDataManager.inst.players[i].player;

                                                p.trail.UpdateTail(InputDataManager.inst.players[i].health, Vector3.zero);
                                            }
                                        }
                                    }
                                }
                            break;
                        }
                    case "playerHealAll":
                        {
                            if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !constant)
                                for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                                {
                                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && int.TryParse(value, out int hit))
                                    {
                                        InputDataManager.inst.players[i].health += hit;

                                        var rt = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");

                                        if (rt != null)
                                            rt.GetType().GetMethod("UpdateTail", BindingFlags.Public | BindingFlags.Instance).Invoke(rt, new object[] { InputDataManager.inst.players[i].health, Vector3.zero });
                                        else
                                        {
                                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Count > i)
                                            {
                                                var p = InputDataManager.inst.players[i].player;

                                                p.trail.UpdateTail(InputDataManager.inst.players[i].health, Vector3.zero);
                                            }
                                        }
                                    }
                                }
                            break;
                        }
                    case "playerKill":
                        {
                            if ((EditorManager.inst == null || !EditorManager.inst.isEditing) && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 && !constant)
                                if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                                {
                                    var i = ObjectExtensions.ClosestPlayer(Objects.beatmapObjects[modifierObject.id].gameObject);

                                    InputDataManager.inst.players[i].health = 0;
                                }

                            break;
                        }
                    case "playerKillAll":
                        {
                            if ((EditorManager.inst == null || !EditorManager.inst.isEditing) && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 && !constant)
                            {
                                for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                                {
                                    InputDataManager.inst.players[i].health = 0;
                                }
                            }
                            break;
                        }
                    case "playerMove":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                            {
                                var i = ObjectExtensions.ClosestPlayer(Objects.beatmapObjects[modifierObject.id].gameObject);

                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                    var vector = value.Split(new char[] { '.' });

                                    if (constant)
                                        player.transform.localPosition = new Vector3(float.Parse(vector[0]), float.Parse(vector[1]), 0f);
                                    else
                                    {
                                        player.transform.DOLocalMove(new Vector3(float.Parse(vector[0]), float.Parse(vector[1]), 0f), float.Parse(command[1])).SetEase(DataManager.inst.AnimationList[int.Parse(command[2])].Animation);
                                    }
                                }
                            }

                            break;
                        }
                    case "playerMoveAll":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                    var vector = value.Split(new char[] { '.' });

                                    if (constant)
                                        player.transform.localPosition = new Vector3(float.Parse(vector[0]), float.Parse(vector[1]), 0f);
                                    else
                                    {
                                        player.transform.DOLocalMove(new Vector3(float.Parse(vector[0]), float.Parse(vector[1]), 0f), float.Parse(command[1])).SetEase(DataManager.inst.AnimationList[int.Parse(command[2])].Animation);
                                    }
                                }
                            }

                            break;
                        }
                    case "playerMoveX":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                            {
                                var i = ObjectExtensions.ClosestPlayer(Objects.beatmapObjects[modifierObject.id].gameObject);

                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                    if (constant)
                                    {
                                        var v = player.transform.localPosition;
                                        v.x += float.Parse(value);
                                        player.transform.localPosition = v;
                                    }
                                    else
                                    {
                                        player.transform.DOLocalMoveX(float.Parse(value), float.Parse(command[1])).SetEase(DataManager.inst.AnimationList[int.Parse(command[2])].Animation);
                                    }
                                }
                            }

                            break;
                        }
                    case "playerMoveXAll":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                    if (constant)
                                    {
                                        var v = player.transform.localPosition;
                                        v.x += float.Parse(value);
                                        player.transform.localPosition = v;
                                    }
                                    else
                                    {
                                        player.transform.DOLocalMoveX(float.Parse(value), float.Parse(command[1])).SetEase(DataManager.inst.AnimationList[int.Parse(command[2])].Animation);
                                    }
                                }
                            }

                            break;
                        }
                    case "playerMoveY":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                            {
                                var i = ObjectExtensions.ClosestPlayer(Objects.beatmapObjects[modifierObject.id].gameObject);

                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                    if (constant)
                                    {
                                        var v = player.transform.localPosition;
                                        v.y += float.Parse(value);
                                        player.transform.localPosition = v;
                                    }
                                    else
                                    {
                                        player.transform.DOLocalMoveY(float.Parse(value), float.Parse(command[1])).SetEase(DataManager.inst.AnimationList[int.Parse(command[2])].Animation);
                                    }
                                }
                            }

                            break;
                        }
                    case "playerMoveYAll":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                    if (constant)
                                    {
                                        var v = player.transform.localPosition;
                                        v.y += float.Parse(value);
                                        player.transform.localPosition = v;
                                    }
                                    else
                                    {
                                        player.transform.DOLocalMoveY(float.Parse(value), float.Parse(command[1])).SetEase(DataManager.inst.AnimationList[int.Parse(command[2])].Animation);
                                    }
                                }
                            }

                            break;
                        }
                    case "playerRotate":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                            {
                                var i = ObjectExtensions.ClosestPlayer(Objects.beatmapObjects[modifierObject.id].gameObject);

                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                    if (constant)
                                    {

                                    }
                                    else
                                    {
                                        player.transform.DORotate(new Vector3(0f, 0f, float.Parse(value)), float.Parse(command[1])).SetEase(DataManager.inst.AnimationList[int.Parse(command[2])].Animation);
                                    }
                                }
                            }

                            break;
                        }
                    case "playerRotateAll":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                    if (constant)
                                    {

                                    }
                                    else
                                    {
                                        player.transform.DORotate(new Vector3(0f, 0f, float.Parse(value)), float.Parse(command[1])).SetEase(DataManager.inst.AnimationList[int.Parse(command[2])].Animation);
                                    }
                                }
                            }

                            break;
                        }
                    case "playerBoost":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null && !constant)
                            {
                                var i = ObjectExtensions.ClosestPlayer(Objects.beatmapObjects[modifierObject.id].gameObject);

                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    var rt = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");

                                    if (rt != null)
                                    {
                                        rt.GetType().GetMethod("StartBoost", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rt, new object[] { });
                                    }
                                    else
                                    {
                                        if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Count > i)
                                        {
                                            var p = InputDataManager.inst.players[i].player;

                                            p.GetType().GetMethod("StartBoost", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, new object[] { });
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    case "playerBoostAll":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && !constant)
                                {
                                    var rt = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");

                                    if (rt != null)
                                    {
                                        rt.GetType().GetMethod("StartBoost", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(rt, new object[] { });
                                    }
                                    else
                                    {
                                        if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Count > i)
                                        {
                                            var p = InputDataManager.inst.players[i].player;

                                            p.GetType().GetMethod("StartBoost", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, new object[] { });
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    case "playerDisableBoost":
                        {
                            for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                            {
                                if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                {
                                    hasChanged = false;
                                    var rt = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");

                                    if (rt != null)
                                    {
                                        rt.GetType().GetField("canBoost", BindingFlags.Public | BindingFlags.Instance).SetValue(rt, false);
                                    }
                                    else
                                    {
                                        if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Count > i)
                                        {
                                            var p = InputDataManager.inst.players[i].player;

                                            p.GetType().GetField("canBoost", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(p, false);
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    case "showMouse":
                        {
                            LSFunctions.LSHelpers.ShowCursor();
                            break;
                        }
                    case "hideMouse":
                        {
                            if (EditorManager.inst == null || !EditorManager.inst.isEditing)
                            {
                                LSFunctions.LSHelpers.HideCursor();
                            }
                            break;
                        }
                    case "addVariable":
                        {
                            if (refModifier != null && !string.IsNullOrEmpty(command[1]) && DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]) != null)
                            {
                                foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]))
                                {
                                    beatmapObject.GetModifierObject().variable += int.Parse(value);
                                }
                            }
                            break;
                        }
                    case "subVariable":
                        {
                            if (refModifier != null && !string.IsNullOrEmpty(command[1]) && DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]) != null)
                            {
                                foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]))
                                {
                                    beatmapObject.GetModifierObject().variable -= int.Parse(value);
                                }
                            }
                            break;
                        }
                    case "setVariable":
                        {
                            if (refModifier != null && !string.IsNullOrEmpty(command[1]) && DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]) != null)
                            {
                                foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]))
                                {
                                    beatmapObject.GetModifierObject().variable = int.Parse(value);
                                }
                            }
                            break;
                        }
                    case "disableObject":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].transformChain != null && Objects.beatmapObjects[modifierObject.id].transformChain.Count > 0)
                            {
                                Objects.beatmapObjects[modifierObject.id].transformChain[0].gameObject.SetActive(false);
                            }
                            break;
                        }
                    case "disableObjectTree":
                        {
                            var parentChain = modifierObject.GetParentChain();

                            foreach (var cc in parentChain[parentChain.Count - 1].GetChildChain())
                            {
                                for (int o = 0; o < cc.Count; o++)
                                {
                                    if (cc[o] != null && Objects.beatmapObjects.ContainsKey(cc[o].id))
                                    {
                                        var mod = Objects.beatmapObjects[cc[o].id];

                                        if (mod.transformChain != null && mod.transformChain.Count > 0)
                                        {
                                            mod.transformChain[0].gameObject.SetActive(false);
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "save":
                        {
                            if ((EditorManager.inst == null || !EditorManager.inst.isEditing) && float.TryParse(value, out float num))
                            {
                                ObjectModifiersPlugin.SaveProgress(command[1], command[2], command[3], num);
                            }
                            break;
                        }
                    case "saveVariable":
                        {
                            if (EditorManager.inst == null || !EditorManager.inst.isEditing)
                            {
                                ObjectModifiersPlugin.SaveProgress(command[1], command[2], command[3], refModifier.variable);
                            }
                            break;
                        }
                    case "reactivePos":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && int.TryParse(command[1], out int sampleX) && float.TryParse(command[3], out float intensityX) && int.TryParse(command[2], out int sampleY) && float.TryParse(command[4], out float intensityY) && float.TryParse(value, out float val))
                            {
                                float[] samples = new float[256];

                                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                                sampleX = Mathf.Clamp(sampleX, 0, 255);
                                sampleY = Mathf.Clamp(sampleY, 0, 255);

                                float reactivePositionX = samples[sampleX] * intensityX * val;
                                float reactivePositionY = samples[sampleY] * intensityY * val;

                                var functionObject = Objects.beatmapObjects[modifierObject.id];

                                if (functionObject.gameObject != null)
                                {
                                    var x = modifierObject.origin.x;
                                    var y = modifierObject.origin.y;

                                    functionObject.gameObject.transform.localPosition = new Vector3(x + reactivePositionX, y + reactivePositionY, 1f);
                                }

                                samples = null;
                            }
                            break;
                        }
                    case "reactiveSca":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && int.TryParse(command[1], out int sampleX) && float.TryParse(command[3], out float intensityX) && int.TryParse(command[2], out int sampleY) && float.TryParse(command[4], out float intensityY) && float.TryParse(value, out float val))
                            {
                                float[] samples = new float[256];

                                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                                sampleX = Mathf.Clamp(sampleX, 0, 255);
                                sampleY = Mathf.Clamp(sampleY, 0, 255);

                                float reactiveScaleX = samples[sampleX] * intensityX * val;
                                float reactiveScaleY = samples[sampleY] * intensityY * val;

                                var functionObject = Objects.beatmapObjects[modifierObject.id];

                                if (functionObject.gameObject != null)
                                    functionObject.gameObject.transform.localScale = new Vector3(1f + reactiveScaleX, 1f + reactiveScaleY, 1f);

                                samples = null;
                            }
                            break;
                        }
                    case "reactiveRot":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && int.TryParse(command[1], out int sample) && float.TryParse(value, out float val))
                            {
                                float[] samples = new float[256];

                                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                                sample = Mathf.Clamp(sample, 0, 255);

                                float reactiveRotation = samples[sample] * val;

                                var functionObject = Objects.beatmapObjects[modifierObject.id];

                                if (functionObject.gameObject != null)
                                {
                                    functionObject.gameObject.transform.localRotation = Quaternion.Euler(0f, 0f, reactiveRotation);
                                    //var e = functionObject.gameObject.transform.parent.localRotation;
                                    //e.eulerAngles += new Vector3(0f, 0f, reactiveRotation);
                                    //functionObject.gameObject.transform.parent.localRotation = e;
                                }

                                samples = null;
                            }
                            break;
                        }
                    case "reactiveCol":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && int.TryParse(command[1], out int sample) && float.TryParse(value, out float val))
                            {
                                float[] samples = new float[256];

                                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                                sample = Mathf.Clamp(sample, 0, 255);

                                float reactiveColor = samples[sample] * val;

                                var functionObject = Objects.beatmapObjects[modifierObject.id];

                                if (functionObject.renderer != null && int.TryParse(command[2], out int col))
                                    functionObject.renderer.material.color += GameManager.inst.LiveTheme.objectColors[col] * reactiveColor;

                                samples = null;
                            }
                            break;
                        }
                    case "reactivePosChain":
                        {
                            if (refModifier != null)
                            {
                                var ch = refModifier.beatmapObject.GetChildChain();

                                float[] samples = new float[256];

                                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                                float reactivePositionX = samples[int.Parse(command[1])] * float.Parse(command[3]) * float.Parse(value);
                                float reactivePositionY = samples[int.Parse(command[2])] * float.Parse(command[4]) * float.Parse(value);

                                foreach (var cc in ch)
                                {
                                    for (int i = 0; i < cc.Count; i++)
                                    {
                                        if (Objects.beatmapObjects.ContainsKey(cc[i].id))
                                        {
                                            var modifier = Objects.beatmapObjects[cc[i].id];

                                            var tf = modifier.transformChain;

                                            if (tf != null && tf.Count > 2)
                                            {
                                                var index = cc[i].GetParentChain().FindIndex(x => x.id == refModifier.beatmapObject.id);

                                                if (tf[tf.Count - 2 - index].name != "top")
                                                {
                                                    if (ModCompatibility.catalyst != null && ModCompatibility.catalystType == ModCompatibility.CatalystType.Editor)
                                                    {
                                                        tf[tf.Count - 2 - index].localPosition += new Vector3(reactivePositionX, reactivePositionY, 0f);
                                                    }
                                                    else
                                                    {
                                                        var nextKF = cc[i].NextEventKeyframe(0);
                                                        var prevKF = cc[i].PrevEventKeyframe(0);

                                                        if (nextKF != null)
                                                        {
                                                            var t = (AudioManager.inst.CurrentAudioSource.time - cc[i].StartTime) / nextKF.eventTime;

                                                            var x = RTMath.InterpolateOverCurve(nextKF.curveType.Animation, prevKF.eventValues[0], nextKF.eventValues[0], t);
                                                            var y = RTMath.InterpolateOverCurve(nextKF.curveType.Animation, prevKF.eventValues[1], nextKF.eventValues[1], t);

                                                            var tf2 = tf[tf.Count - 2 - index];
                                                            tf2.localPosition = new Vector3(x + reactivePositionX, y + reactivePositionY, tf2.localPosition.z);
                                                        }
                                                        else
                                                        {
                                                            var x = prevKF.eventValues[0];
                                                            var y = prevKF.eventValues[1];

                                                            var tf2 = tf[tf.Count - 2 - index];
                                                            tf2.localPosition = new Vector3(x + reactivePositionX, y + reactivePositionY, tf2.localPosition.z);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "reactiveScaChain":
                        {
                            if (refModifier != null)
                            {
                                var ch = refModifier.beatmapObject.GetChildChain();

                                float[] samples = new float[256];

                                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                                float reactiveScaleX = samples[int.Parse(command[1])] * float.Parse(command[3]) * float.Parse(value);
                                float reactiveScaleY = samples[int.Parse(command[2])] * float.Parse(command[4]) * float.Parse(value);

                                foreach (var cc in ch)
                                {
                                    for (int i = 0; i < cc.Count; i++)
                                    {
                                        if (Objects.beatmapObjects.ContainsKey(cc[i].id))
                                        {
                                            var modifier = Objects.beatmapObjects[cc[i].id];

                                            var tf = modifier.transformChain;

                                            if (tf != null && tf.Count > 2)
                                            {
                                                var index = cc[i].GetParentChain().FindIndex(x => x.id == refModifier.beatmapObject.id);

                                                if (tf[tf.Count - 2 - index].name != "top")
                                                {
                                                    if (ModCompatibility.catalyst != null && ModCompatibility.catalystType == ModCompatibility.CatalystType.Editor)
                                                    {
                                                        tf[tf.Count - 2 - index].localScale += new Vector3(reactiveScaleX, reactiveScaleY, 0f);
                                                    }
                                                    else
                                                    {
                                                        var nextKF = cc[i].NextEventKeyframe(1);
                                                        var prevKF = cc[i].PrevEventKeyframe(1);

                                                        if (nextKF != null)
                                                        {
                                                            var t = (AudioManager.inst.CurrentAudioSource.time - cc[i].StartTime) / nextKF.eventTime;

                                                            var x = RTMath.InterpolateOverCurve(nextKF.curveType.Animation, prevKF.eventValues[0], nextKF.eventValues[0], t);
                                                            var y = RTMath.InterpolateOverCurve(nextKF.curveType.Animation, prevKF.eventValues[1], nextKF.eventValues[1], t);

                                                            var tf2 = tf[tf.Count - 2 - index];
                                                            tf2.localScale = new Vector3(x + reactiveScaleX, y + reactiveScaleY, tf2.localScale.z);
                                                        }
                                                        else
                                                        {
                                                            var x = prevKF.eventValues[0];
                                                            var y = prevKF.eventValues[1];

                                                            var tf2 = tf[tf.Count - 2 - index];
                                                            tf2.localScale = new Vector3(x + reactiveScaleX, y + reactiveScaleY, tf2.localScale.z);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "reactiveRotChain":
                        {
                            if (refModifier != null)
                            {
                                var ch = refModifier.beatmapObject.GetChildChain();

                                float[] samples = new float[256];

                                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                                float reactiveRotation = samples[int.Parse(command[1])] * float.Parse(value);

                                foreach (var cc in ch)
                                {
                                    for (int i = 0; i < cc.Count; i++)
                                    {
                                        if (Objects.beatmapObjects.ContainsKey(cc[i].id))
                                        {
                                            var modifier = Objects.beatmapObjects[cc[i].id];

                                            var tf = modifier.transformChain;

                                            if (tf != null && tf.Count > 2)
                                            {
                                                var index = cc[i].GetParentChain().FindIndex(x => x.id == refModifier.beatmapObject.id);

                                                if (tf[tf.Count - 2 - index].name != "top")
                                                {
                                                    var parent = tf[tf.Count - 2 - index];


                                                    if (ModCompatibility.catalyst != null && ModCompatibility.catalystType == ModCompatibility.CatalystType.Editor)
                                                    {
                                                        var e = parent.localRotation;
                                                        e.eulerAngles += new Vector3(0f, 0f, reactiveRotation);
                                                        parent.localRotation = e;
                                                    }
                                                    else
                                                    {
                                                        var nextKF = cc[i].NextEventKeyframe(2);
                                                        var prevKF = cc[i].PrevEventKeyframe(2);

                                                        if (nextKF != null)
                                                        {
                                                            var t = (AudioManager.inst.CurrentAudioSource.time - cc[i].StartTime) / nextKF.eventTime;

                                                            var x = RTMath.InterpolateOverCurve(nextKF.curveType.Animation, prevKF.eventValues[0], nextKF.eventValues[0], t);
                                                            var y = RTMath.InterpolateOverCurve(nextKF.curveType.Animation, prevKF.eventValues[1], nextKF.eventValues[1], t);

                                                            var e = parent.localRotation;
                                                            e.eulerAngles += new Vector3(0f, 0f, x + reactiveRotation);
                                                            parent.localRotation = e;
                                                        }
                                                        else
                                                        {
                                                            var x = prevKF.eventValues[0];
                                                            var y = prevKF.eventValues[1];

                                                            var e = parent.localRotation;
                                                            e.eulerAngles += new Vector3(0f, 0f, x + reactiveRotation);
                                                            parent.localRotation = e;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "reactiveColChain":
                        {
                            if (refModifier != null)
                            {
                                var ch = refModifier.beatmapObject.GetChildChain();

                                float[] samples = new float[256];

                                AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                                float reactiveColor = samples[int.Parse(command[1])] * float.Parse(value);

                                foreach (var cc in ch)
                                {
                                    for (int i = 0; i < cc.Count; i++)
                                    {
                                        var modifier = Objects.beatmapObjects[cc[i].id];

                                        if (modifier.renderer != null)
                                        {
                                            modifier.renderer.material.color += GameManager.inst.LiveTheme.objectColors[int.Parse(command[2])] * reactiveColor;
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    case "setPlayerModel":
                        {
                            if (!constant && ModCompatibility.playerPlugin != null)
                            {
                                ModCompatibility.SetPlayerModel(int.Parse(command[1]), value);
                            }
                            break;
                        }
                    case "eventOffset":
                        {
                            if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
                            {
                                var list = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];

                                var indexArray = int.Parse(command[1]);
                                var indexValue = int.Parse(command[2]);

                                if (indexArray < list.Count && indexValue < list[indexArray].Count)
                                    list[indexArray][indexValue] = float.Parse(value);

                                ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                            }
                            break;
                        }
                    case "legacyTail":
                        {
                            if (!tailDone)
                            {
                                var parent = new GameObject(modifierObject.id);
                                parent.transform.SetParent(ObjectManager.inst.objectParent.transform);
                                parent.transform.localScale = Vector3.one;
                                var legacyTracker = parent.AddComponent<LegacyTracker>();

                                var ch = modifierObject.GetChildChain();

                                foreach (var cc in ch)
                                {
                                    for (int i = 0; i < cc.Count; i++)
                                    {
                                        var obj = cc[i];

                                        if (Objects.beatmapObjects.ContainsKey(obj.id))
                                        {
                                            var modifier = Objects.beatmapObjects[cc[i].id];

                                            var tf = modifier.transformChain;

                                            if (tf != null && tf.Count > 0)
                                            {
                                                var top = tf[0];

                                                var id = LSFunctions.LSText.randomNumString(16);

                                                var rt = new LegacyTracker.RTObject(top.gameObject);
                                                rt.values.Add("Renderer", top.GetComponentInChildren<Renderer>());
                                                legacyTracker.originals.Add(id, rt);
                                            }
                                        }
                                    }
                                }

                                legacyTracker.distance = float.Parse(value);
                                if (command.Count > 1)
                                    legacyTracker.tailCount = int.Parse(command[1]);
                                if (command.Count > 2)
                                    legacyTracker.transformSpeed = float.Parse(command[2]);
                                if (command.Count > 3)
                                    legacyTracker.distanceSpeed = float.Parse(command[3]);

                                legacyTracker.Setup();

                                tailDone = true;
                            }
                            break;
                        }
                    case "blackHole":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null)
                            {
                                var gm = Objects.beatmapObjects[modifierObject.id].gameObject;

                                for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                                {
                                    if (GameManager.inst.players.transform.Find("Player " + (i + 1).ToString()))
                                    {
                                        var pl = GameManager.inst.players.transform.Find("Player " + (i + 1).ToString() + "/Player");

                                        float pitch = AudioManager.inst.CurrentAudioSource.pitch;

                                        if (pitch < 0f)
                                            pitch = -pitch;
                                        if (pitch == 0f)
                                            pitch = 0.001f;

                                        float p = Time.deltaTime * 60f * pitch;

                                        float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(float.Parse(value), 0.001f, 1f), p);

                                        var vector = new Vector3(pl.position.x, pl.position.y, 0f);
                                        var target = new Vector3(gm.transform.position.x, gm.transform.position.y, 0f);

                                        if (AudioManager.inst.CurrentAudioSource.isPlaying)
                                            pl.position += (target - vector) * moveDelay;
                                    }
                                }
                            }
                            break;
                        }
                    case "addColor":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && int.TryParse(command[1], out int index) && float.TryParse(value, out float num))
                            {
                                var functionObject = Objects.beatmapObjects[modifierObject.id];

                                index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                                if (functionObject.renderer != null)
                                    functionObject.renderer.material.color += GameManager.inst.LiveTheme.objectColors[index] * num;
                            }

                            break;
                        }
                    case "addColorOther":
                        {
                            foreach (var bm in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]))
                            {
                                if (bm != null && Objects.beatmapObjects.ContainsKey(bm.id) && int.TryParse(command[2], out int index) && float.TryParse(value, out float num))
                                {
                                    var functionObject = Objects.beatmapObjects[bm.id];

                                    index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                                    if (functionObject.renderer != null)
                                        functionObject.renderer.material.color += GameManager.inst.LiveTheme.objectColors[index] * num;
                                }
                            }


                            break;
                        }
                    case "addColorPlayerDistance":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && Objects.beatmapObjects[modifierObject.id].gameObject != null && int.TryParse(command[1], out int index) && float.TryParse(value, out float num))
                            {
                                var i = ObjectExtensions.ClosestPlayer(Objects.beatmapObjects[modifierObject.id].gameObject);

                                var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                var distance = Vector2.Distance(player.transform.position, Objects.beatmapObjects[modifierObject.id].gameObject.transform.position);

                                var functionObject = Objects.beatmapObjects[modifierObject.id];

                                index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                                if (functionObject.renderer != null)
                                    functionObject.renderer.material.color += GameManager.inst.LiveTheme.objectColors[index] * distance * num;
                            }

                            break;
                        }
                    case "setAlpha":
                        {
                            if (modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id) && float.TryParse(value, out float num))
                            {
                                var functionObject = Objects.beatmapObjects[modifierObject.id];

                                if (functionObject.renderer != null)
                                    functionObject.renderer.material.color = LSFunctions.LSColors.fadeColor(functionObject.renderer.material.color, num);
                            }

                            break;
                        }
                    case "setAlphaOther":
                        {
                            foreach (var bm in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == command[1]))
                            {
                                if (bm != null && Objects.beatmapObjects.ContainsKey(bm.id) && float.TryParse(value, out float num))
                                {
                                    var functionObject = Objects.beatmapObjects[bm.id];

                                    if (functionObject.renderer != null)
                                        functionObject.renderer.material.color = LSFunctions.LSColors.fadeColor(functionObject.renderer.material.color, num);
                                }
                            }

                            break;
                        }
                    case "updateObjects":
                        {
                            if (!constant)
                                ObjectManager.inst.updateObjects();
                            break;
                        }
                    case "updateObject":
                        {
                            if (!constant && DataManager.inst.gameData.beatmapObjects.Find(x => x.name == value) != null)
                            {
                                foreach (var bm in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == value))
                                {
                                    var objectSelection = new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, DataManager.inst.gameData.beatmapObjects.IndexOf(bm));

                                    ObjectManager.inst.updateObjects(objectSelection);
                                }
                            }
                            break;
                        }
                    case "animationObject":
                        {

                            break;
                        }
                    case "code":
                        {
                            string id = "a";
                            if (refModifier != null && refModifier.beatmapObject != null)
                                id = refModifier.beatmapObject.id;

                            int index;
                            if (ObjectModifiersPlugin.modifierObjects.ContainsKey(id))
                                index = ObjectModifiersPlugin.modifierObjects[id].modifiers.IndexOf(this);
                            else index = -1;

                            string codeToInclude = $"var refID = \"{id}\"; var refModifierIndex = {index};";

                            string code = "";
                            if (!code.Contains("System.IO.File.") && !code.Contains("File."))
                                code = value;

                            RTCode.Evaluate($"{codeToInclude}{code}");
                            break;
                        }
                }
            }

            private bool tailDone = false;

            private bool hasChanged = false;
            
            public void Inactive()
            {
                switch (command[0])
                {
                    case "spawnPrefab":
                        {
                            if (!constant && result != null && result.GetType() == typeof(PrefabObject) && type == Type.Action)
                            {
                                var id = ((PrefabObject)result).ID;

                                DataManager.inst.gameData.prefabObjects.Remove((PrefabObject)result);

                                if (EditorManager.inst != null)
                                {
                                    ObjEditor.inst.DestroyTimelineObject(id);
                                }

                                if (EditorManager.inst != null && DataManager.inst.gameData.beatmapObjects.Count > 0 && ObjEditor.inst.selectedObjects.Find(x => x.ID == id) != null)
                                {
                                    ObjEditor.inst.SetCurrentObj(new ObjEditor.ObjectSelection(ObjEditor.ObjectSelection.SelectionType.Object, 0));
                                }

                                ObjectManager.inst.updateObjects();

                                //foreach (var beatmapObject in DataManager.inst.gameData.beatmapObjects)
                                //{
                                //    if (beatmapObject.prefabInstanceID == id && ObjectManager.inst.beatmapGameObjects.ContainsKey(beatmapObject.id))
                                //    {
                                //        Object.Destroy(ObjectManager.inst.beatmapGameObjects[beatmapObject.id].obj);
                                //        ObjectManager.inst.beatmapGameObjects[beatmapObject.id].sequence.all.Kill(false);
                                //        ObjectManager.inst.beatmapGameObjects[beatmapObject.id].sequence.col.Kill(false);
                                //        ObjectManager.inst.beatmapGameObjects.Remove(beatmapObject.id);
                                //        DataManager.inst.gameData.beatmapObjects.Remove(beatmapObject);
                                //        Debug.LogFormat("{0}Removed prefab object {1}", ObjectModifiersPlugin.className, id);
                                //    }
                                //}

                                result = null;
                            }
                            break;
                        }
                    case "playerDisableBoost":
                        {
                            if (!hasChanged)
                            {
                                hasChanged = true;
                                for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                                {
                                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                                    {
                                        var rt = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)).gameObject.GetComponentByName("RTPlayer");

                                        if (rt != null)
                                        {
                                            rt.GetType().GetField("canBoost", BindingFlags.Public | BindingFlags.Instance).SetValue(rt, true);
                                        }
                                        else
                                        {
                                            if (InputDataManager.inst.players.Count > 0 && InputDataManager.inst.players.Count > i)
                                            {
                                                var p = InputDataManager.inst.players[i].player;

                                                p.GetType().GetField("canBoost", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(p, true);
                                            }
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    case "disableObject":
                        {
                            if (!hasChanged && modifierObject != null && Objects.beatmapObjects.ContainsKey(modifierObject.id))
                            {
                                var tf = Objects.beatmapObjects[modifierObject.id].transformChain;
                                if (tf != null && tf.Count > 0 && tf[0] != null)
                                    tf[0].gameObject.SetActive(true);
                                hasChanged = true;
                            }

                            break;
                        }
                    case "disableObjectTree":
                        {
                            if (!hasChanged)
                            {
                                var parentChain = modifierObject.GetParentChain();

                                foreach (var cc in parentChain[parentChain.Count - 1].GetChildChain())
                                {
                                    for (int o = 0; o < cc.Count; o++)
                                    {
                                        if (cc[o] != null && Objects.beatmapObjects.ContainsKey(cc[o].id))
                                        {
                                            var tf = Objects.beatmapObjects[cc[o].id].transformChain;
                                            if (tf != null && tf.Count > 0 && tf[0] != null)
                                                tf[0].gameObject.SetActive(true);
                                        }
                                    }
                                }
                                hasChanged = true;
                            }

                            break;
                        }
                }
            }


            float Interpolate()
            {
                var time = AudioManager.inst.CurrentAudioSource.time - modifierObject.StartTime;

                var i = 3;

                var nextKFIndex = modifierObject.events[i].FindIndex(x => x.eventTime > time);

                if (nextKFIndex >= 0)
                {
                    var prevKFIndex = nextKFIndex - 1;
                    if (prevKFIndex < 0)
                        prevKFIndex = 0;

                    var nextKF = modifierObject.events[i][nextKFIndex];
                    var prevKF = modifierObject.events[i][prevKFIndex];

                    var j = 1;
                    var next = nextKF.eventValues[j];
                    var prev = prevKF.eventValues[j];

                    if (float.IsNaN(prev))
                        prev = 0f;

                    if (float.IsNaN(next))
                        next = 0f;

                    var x = RTMath.Lerp(prev, next, Ease.GetEaseFunction(nextKF.curveType.Name)(RTMath.InverseLerp(prevKF.eventTime, nextKF.eventTime, time)));

                    if (prevKFIndex == nextKFIndex)
                        x = next;

                    if (float.IsNaN(x) || float.IsInfinity(x))
                        x = next;

                    return x;
                }
                else
                {
                    var j = 1;
                    var x = modifierObject.events[i][modifierObject.events[i].Count - 1].eventValues[j];

                    if (float.IsNaN(x))
                        x = 0f;

                    if (float.IsNaN(x) || float.IsInfinity(x))
                        x = modifierObject.events[i][modifierObject.events[i].Count - 1].eventValues[j];

                    return x;
                }
            }
        }
    }
}
