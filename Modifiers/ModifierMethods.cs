using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using SimpleJSON;

using RTFunctions.Functions;
using RTFunctions.Functions.Animation;
using RTFunctions.Functions.Animation.Keyframe;
using RTFunctions.Functions.Components;
using RTFunctions.Functions.Data;
using RTFunctions.Functions.IO;
using RTFunctions.Functions.Managers;
using RTFunctions.Functions.Optimization;
using RTFunctions.Functions.Optimization.Objects;
using RTFunctions.Functions.Optimization.Objects.Visual;

using ObjectModifiers.Functions;

using DG.Tweening;
using Ease = RTFunctions.Functions.Animation.Ease;

namespace ObjectModifiers.Modifiers
{
    public static class ModifierMethods
    {
        public static bool Trigger(BeatmapObject.Modifier modifier)
        {
            switch (modifier.commands[0])
            {
                case "playerCollide":
                    {
                        return modifier.modifierObject.IsTouchingPlayer();
                    }
                case "playerHealthEquals":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.All(x => x.health == num);
                    }
                case "playerHealthLesserEquals":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.All(x => x.health <= num);
                    }
                case "playerHealthGreaterEquals":
                    {
                            return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.All(x => x.health >= num);
                    }
                case "playerHealthLesser":
                    {
                          return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.All(x => x.health < num);
                    }
                case "playerHealthGreater":
                    {
                            return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.All(x => x.health > num);
                    }
                case "playerMoving":
                    {
                        for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                        {
                            if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                            {
                                var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                                if (modifier.Result == null)
                                    modifier.Result = player.position;

                                if (player.position != (Vector3)modifier.Result)
                                {
                                    modifier.Result = player.position;
                                    return true;
                                }
                            }
                        }
                        break;
                    }
                case "playerBoosting":
                    {
                        if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                return closest.Player.isBoosting;
                            }
                        }

                        break;
                    }
                case "playerAlive":
                    {
                        if (int.TryParse(modifier.value, out int hit) && modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            if (PlayerManager.Players.Count > hit)
                            {
                                var closest = PlayerManager.Players[hit];

                                return closest.Player && closest.Player.PlayerAlive;
                            }
                        }

                        break;
                    }
                case "playerDeathsEquals":
                    {
                            return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count == num);
                    }
                case "playerDeathsLesserEquals":
                    {
                            return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count <= num);
                    }
                case "playerDeathsGreaterEquals":
                    {
                        return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count >= num);
                    }
                case "playerDeathsLesser":
                    {
                            return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count < num);
                    }
                case "playerDeathsGreater":
                    {
                            return InputDataManager.inst.players.Count > 0 && int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Any(x => x.PlayerDeaths.Count > num);
                    }
                case "playerDistanceGreater":
                    {
                        for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                        {
                            if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject && float.TryParse(modifier.value, out float num))
                            {
                                var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                                return Vector2.Distance(player.transform.position, levelObject.visualObject.GameObject.transform.position) > num;
                            }
                        }

                        break;
                    }
                case "playerDistanceLesser":
                    {
                        for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                        {
                            if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)) && modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject && float.TryParse(modifier.value, out float num))
                            {
                                var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                                return Vector2.Distance(player.transform.position, levelObject.visualObject.GameObject.transform.position) < num;
                            }
                        }

                        break;
                    }
                case "playerCountEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count == num;
                    }
                case "playerCountLesserEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count <= num;
                    }
                case "playerCountGreaterEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count >= num;
                    }
                case "playerCountLesser":
                    {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count < num;
                    }
                case "playerCountGreater":
                    {
                        return int.TryParse(modifier.value, out int num) && InputDataManager.inst.players.Count > num;
                    }
                case "keyPressDown":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetKeyDown((KeyCode)num);
                    }
                case "keyPress":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetKey((KeyCode)num);
                    }
                case "keyPressUp":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetKeyUp((KeyCode)num);
                    }
                case "mouseButtonDown":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetMouseButtonDown(num);
                    }
                case "mouseButton":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetMouseButton(num);
                    }
                case "mouseButtonUp":
                    {
                        return int.TryParse(modifier.value, out int num) && Input.GetMouseButtonUp(num);
                    }
                case "mouseOver":
                    {
                        if (modifier.modifierObject != null && modifier.modifierObject.levelObject && modifier.modifierObject.levelObject.visualObject != null && modifier.modifierObject.levelObject.visualObject.GameObject)
                        {
                            if (!modifier.modifierObject.detector)
                            {
                                var gameObject = modifier.modifierObject.levelObject.visualObject.GameObject;
                                var op = gameObject.GetComponent<Detector>() ?? gameObject.AddComponent<Detector>();
                                op.beatmapObject = modifier.modifierObject;
                                modifier.modifierObject.detector = op;
                            }

                            if (modifier.modifierObject.detector)
                                return modifier.modifierObject.detector.hovered;
                        }
                        break;
                    }
                case "mouseOverSignalModifier":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));
                        if (modifier.modifierObject != null && modifier.modifierObject.levelObject && modifier.modifierObject.levelObject.visualObject != null && modifier.modifierObject.levelObject.visualObject.GameObject)
                        {
                            if (!modifier.modifierObject.detector)
                            {
                                var gameObject = modifier.modifierObject.levelObject.visualObject.GameObject;
                                var op = gameObject.GetComponent<Detector>() ?? gameObject.AddComponent<Detector>();
                                op.beatmapObject = modifier.modifierObject;
                                modifier.modifierObject.detector = op;
                            }

                            if (modifier.modifierObject.detector)
                            {
                                if (modifier.modifierObject.detector.hovered && list.Count() > 0)
                                {
                                    foreach (var bm in list)
                                    {
                                        if (bm != null)
                                        {
                                            ObjectModifiersPlugin.inst.StartCoroutine(ObjectModifiersPlugin.ActivateModifier((BeatmapObject)bm, Parser.TryParse(modifier.value, 0f)));
                                        }
                                    }
                                }

                                if (modifier.modifierObject.detector.hovered)
                                    return true;
                            }
                        }
                        break;
                    }
                case "bulletCollide":
                    {
                        if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject)
                        {
                            if (!modifier.modifierObject.detector)
                            {
                                var op = levelObject.visualObject.GameObject.GetComponent<Detector>() ?? levelObject.visualObject.GameObject.AddComponent<Detector>();
                                op.beatmapObject = modifier.modifierObject;
                                modifier.modifierObject.detector = op;
                            }

                            if (modifier.modifierObject.detector)
                                return modifier.modifierObject.detector.bulletOver;
                        }
                        break;
                    }
                case "objectCollide":
                    {
                        if (Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.Collider)
                        {
                            var list = DataManager.inst.gameData.beatmapObjects.Where(x => ((BeatmapObject)x).tags.Contains(modifier.value) && ((BeatmapObject)x).levelObject && ((BeatmapObject)x).levelObject.visualObject != null && ((BeatmapObject)x).levelObject.visualObject.Collider);
                            return list.Count() > 0 && list.Any(x => ((BeatmapObject)x).levelObject.visualObject.Collider.IsTouching(levelObject.visualObject.Collider));
                        }

                        break;
                    }
                case "loadEquals":
                    {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return
                                    !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]], out float eq) &&
                                    float.TryParse(modifier.value, out float num) && eq == num;
                            }
                        }
                        break;
                    }
                case "loadLesserEquals":
                    {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return
                                    !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]], out float eq) &&
                                    float.TryParse(modifier.value, out float num) && eq <= num;
                            }
                        }
                        break;
                    }
                case "loadGreaterEquals":
                    {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return
                                    !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]], out float eq) &&
                                    float.TryParse(modifier.value, out float num) && eq >= num;
                            }
                        }
                        break;
                    }
                case "loadLesser":
                    {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return
                                    !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]], out float eq) &&
                                    float.TryParse(modifier.value, out float num) && eq < num;
                            }
                        }
                        break;
                    }
                case "loadGreater":
                    {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return
                                    !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]) &&
                                    float.TryParse(jn[modifier.commands[2]][modifier.commands[3]], out float eq) &&
                                    float.TryParse(modifier.value, out float num) && eq > num;
                            }
                        }
                        break;
                    }
                case "loadExists":
                    {
                        if (RTFile.FileExists(RTFile.ApplicationDirectory + "profile/" + modifier.commands[1] + ".ses"))
                        {
                            string json = FileManager.inst.LoadJSONFile("profile/" + modifier.commands[1] + ".ses");

                            if (!string.IsNullOrEmpty(json))
                            {
                                var jn = JSON.Parse(json);

                                return !string.IsNullOrEmpty(jn[modifier.commands[2]][modifier.commands[3]]);
                            }
                        }
                        break;
                    }
                case "variableEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && modifier.modifierObject && modifier.modifierObject.integerVariable == num;
                    }
                case "variableLesserEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && modifier.modifierObject && modifier.modifierObject.integerVariable <= num;
                    }
                case "variableGreaterEquals":
                    {
                        return int.TryParse(modifier.value, out int num) && modifier.modifierObject && modifier.modifierObject.integerVariable >= num;
                    }
                case "variableLesser":
                    {
                        return int.TryParse(modifier.value, out int num) && modifier.modifierObject && modifier.modifierObject.integerVariable < num;
                    }
                case "variableGreater":
                    {
                        return int.TryParse(modifier.value, out int num) && modifier.modifierObject && modifier.modifierObject.integerVariable > num;
                    }
                case "variableOtherEquals":
                    {
                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.modifierObject &&
                            !string.IsNullOrEmpty(modifier.commands[1]) &&
                            DataManager.inst.gameData.beatmapObjects.Any(x => ((BeatmapObject)x).tags.Contains(modifier.commands[1]) && ((BeatmapObject)x).integerVariable == num);
                    }
                case "variableOtherLesserEquals":
                    {
                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.modifierObject &&
                            !string.IsNullOrEmpty(modifier.commands[1]) &&
                            DataManager.inst.gameData.beatmapObjects.Any(x => ((BeatmapObject)x).tags.Contains(modifier.commands[1]) && ((BeatmapObject)x).integerVariable <= num);
                    }
                case "variableOtherGreaterEquals":
                    {
                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.modifierObject &&
                            !string.IsNullOrEmpty(modifier.commands[1]) &&
                            DataManager.inst.gameData.beatmapObjects.Any(x => ((BeatmapObject)x).tags.Contains(modifier.commands[1]) && ((BeatmapObject)x).integerVariable >= num);
                    }
                case "variableOtherLesser":
                    {
                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.modifierObject &&
                            !string.IsNullOrEmpty(modifier.commands[1]) &&
                            DataManager.inst.gameData.beatmapObjects.Any(x => ((BeatmapObject)x).tags.Contains(modifier.commands[1]) && ((BeatmapObject)x).integerVariable < num);
                    }
                case "variableOtherGreater":
                    {
                        return
                            int.TryParse(modifier.value, out int num) &&
                            modifier.modifierObject &&
                            !string.IsNullOrEmpty(modifier.commands[1]) &&
                            DataManager.inst.gameData.beatmapObjects.Any(x => ((BeatmapObject)x).tags.Contains(modifier.commands[1]) && ((BeatmapObject)x).integerVariable > num);
                    }
                case "pitchEquals":
                    {
                            return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch == num;
                    }
                case "pitchLesserEquals":
                    {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch <= num;
                    }
                case "pitchGreaterEquals":
                    {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch >= num;
                    }
                case "pitchLesser":
                    {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch < num;
                    }
                case "pitchGreater":
                    {
                        return float.TryParse(modifier.value, out float num) && AudioManager.inst.pitch > num;
                    }
                case "onPlayerHit":
                    {
                        if (modifier.Result == null)
                        {
                            modifier.Result = PlayerManager.Players.Select(x => x.Health).ToList();
                        }

                        if (modifier.Result is List<int>)
                            if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                            {
                                var result = modifier.Result as List<int>;

                                var orderedList = PlayerManager.Players
                                    .Where(x => x.Player)
                                    .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                                if (orderedList.Count > 0)
                                {
                                    var closest = orderedList[0];

                                    var a = result.Count > closest.index && result[closest.index] > closest.Health;

                                    if (a)
                                    {
                                        result[closest.index] = closest.Health;
                                        modifier.Result = result;
                                    }

                                    return a;
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
                        if (modifier.Result == null)
                            modifier.Result = int.TryParse(modifier.commands[1], out int x) && int.TryParse(modifier.commands[2], out int y) && int.TryParse(modifier.value, out int z) && UnityEngine.Random.Range(x, y) > z;

                        return modifier.Result != null && (bool)modifier.Result;
                    }
                case "randomLesser":
                    {
                        if (modifier.Result == null)
                            modifier.Result = int.TryParse(modifier.commands[1], out int x) && int.TryParse(modifier.commands[2], out int y) && int.TryParse(modifier.value, out int z) && UnityEngine.Random.Range(x, y) < z;

                        return modifier.Result != null && (bool)modifier.Result;
                    }
                case "randomEquals":
                    {
                        if (modifier.Result == null)
                            modifier.Result = int.TryParse(modifier.commands[1], out int x) && int.TryParse(modifier.commands[2], out int y) && int.TryParse(modifier.value, out int z) && UnityEngine.Random.Range(x, y) == z;

                        return modifier.Result != null && (bool)modifier.Result;
                    }
                case "requireSignal":
                    {
                        return modifier.Result != null;
                    }
                case "musicTimeGreater":
                    {
                        return float.TryParse(modifier.value, out float x) && AudioManager.inst.CurrentAudioSource.time > x;
                    }
                case "musicTimeLesser":
                    {
                        return float.TryParse(modifier.value, out float x) && AudioManager.inst.CurrentAudioSource.time < x;
                    }
                case "musicPlaying":
                    {
                        return AudioManager.inst.CurrentAudioSource.isPlaying;
                    }
            }

            modifier.Inactive?.Invoke(modifier);
            return false;
        }

        public static void Action(BeatmapObject.Modifier modifier)
        {
            modifier.hasChanged = false;
            switch (modifier.commands[0])
            {
                case "setPitch":
                    {
                        if (float.TryParse(modifier.value, out float num))
                        {
                            if (GameObject.Find("Game Systems/EventManager").GetComponentByName("RTEventManager"))
                            {
                                var rt = GameObject.Find("Game Systems/EventManager").GetComponentByName("RTEventManager");

                                rt.GetType().GetField("pitchOffset", BindingFlags.Public | BindingFlags.Instance).SetValue(rt, num);
                            }
                            else
                                AudioManager.inst.pitch = num * GameManager.inst.getPitch();
                        }

                        break;
                    }
                case "addPitch":
                    {
                        if (float.TryParse(modifier.value, out float num))
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
                        if (float.TryParse(modifier.value, out float num))
                            AudioManager.inst.SetMusicTime(num);
                        break;
                    }
                case "playSound":
                    {
                        if (modifier.commands.Count > 4 && bool.TryParse(modifier.commands[1], out bool global) && float.TryParse(modifier.commands[2], out float pitch) && float.TryParse(modifier.commands[3], out float vol) && bool.TryParse(modifier.commands[4], out bool loop))
                        {
                            ObjectModifiersPlugin.GetSoundPath(modifier.modifierObject.id, modifier.value, global, pitch, vol, loop);
                        }
                        else
                        {
                            ObjectModifiersPlugin.GetSoundPath(modifier.modifierObject.id, modifier.value);
                        }
                        break;
                    }
                case "playSoundOnline":
                    {
                        if (modifier.commands.Count > 1 && bool.TryParse(modifier.commands[1], out bool global) && float.TryParse(modifier.commands[2], out float pitch) && float.TryParse(modifier.commands[3], out float vol) && bool.TryParse(modifier.commands[4], out bool loop))
                        {
                            if (!string.IsNullOrEmpty(modifier.value))
                            {
                                ObjectModifiersPlugin.DownloadSoundAndPlay(modifier.modifierObject.id, modifier.value, pitch, vol, loop);
                            }
                        }
                        else if (!string.IsNullOrEmpty(modifier.value))
                        {
                            ObjectModifiersPlugin.DownloadSoundAndPlay(modifier.modifierObject.id, modifier.value);
                        }
                        break;
                    }
                case "loadLevel":
                    {
                        if (EditorManager.inst && EditorManager.inst.isEditing)
                        {
                            if (ObjectModifiersPlugin.EditorLoadLevel.Value)
                            {
                                if (ObjectModifiersPlugin.EditorSavesBeforeLoad.Value)
                                {
                                    EditorManager.inst.SaveBeatmap();
                                }

                                string str = RTFile.BasePath;
                                string modBackup = str + "level-modifier-backup.lsb";
                                if (RTFile.FileExists(modBackup))
                                {
                                    System.IO.File.Delete(modBackup);
                                }

                                string lvl = RTFile.ApplicationDirectory + str + "level.lsb";
                                if (RTFile.FileExists(lvl))
                                    System.IO.File.Copy(lvl, modBackup);

                                EditorManager.inst.StartCoroutine(EditorManager.inst.LoadLevel(modifier.value));
                            }
                        }
                        else if (!EditorManager.inst)
                        {
                            LevelManager.Load($"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{modifier.value}/level.lsb");
                        }
                        break;
                    }
                case "loadLevelInternal":
                    {
                        if (EditorManager.inst && EditorManager.inst.isEditing)
                        {
                            if (ObjectModifiersPlugin.EditorLoadLevel.Value)
                            {
                                if (ObjectModifiersPlugin.EditorSavesBeforeLoad.Value)
                                {
                                    EditorManager.inst.SaveBeatmap();
                                }

                                string str = RTFile.BasePath;
                                string modBackup = str + "level-modifier-backup.lsb";
                                if (RTFile.FileExists(modBackup))
                                {
                                    System.IO.File.Delete(modBackup);
                                }

                                string lvl = RTFile.ApplicationDirectory + str + "level.lsb";
                                if (RTFile.FileExists(lvl))
                                    System.IO.File.Copy(lvl, modBackup);

                                EditorManager.inst.StartCoroutine(EditorManager.inst.LoadLevel($"{EditorManager.inst.currentLoadedLevel}/{modifier.value}"));
                            }
                        }
                        else if (!EditorManager.inst)
                        {
                            LevelManager.Load($"{RTFile.ApplicationDirectory}{LevelManager.ListSlash}{System.IO.Path.GetFileName(GameManager.inst.basePath.Substring(0, GameManager.inst.basePath.Length - 1))}/{modifier.value}/level.lsb");
                        }
                        break;
                    }
                case "quitToMenu":
                    {
                        if (EditorManager.inst != null && !EditorManager.inst.isEditing)
                        {
                            if (ObjectModifiersPlugin.EditorLoadLevel.Value)
                            {
                                if (ObjectModifiersPlugin.EditorSavesBeforeLoad.Value)
                                {
                                    EditorManager.inst.SaveBeatmap();
                                }

                                string str = RTFile.BasePath;
                                if (RTFile.FileExists(str + "/level-modifier-backup.lsb"))
                                {
                                    System.IO.File.Delete(str + "/level-modifier-backup.lsb");
                                }

                                if (RTFile.FileExists(str + "/level.lsb"))
                                    System.IO.File.Copy(str + "/level.lsb", str + "/level-modifier-backup.lsb");

                                EditorManager.inst.QuitToMenu();
                            }
                        }
                        else if (EditorManager.inst == null)
                        {
                            DG.Tweening.DOTween.KillAll();
                            DG.Tweening.DOTween.Clear(true);
                            DataManager.inst.gameData = null;
                            DataManager.inst.gameData = new GameData();
                            DiscordController.inst.OnIconChange("");
                            DiscordController.inst.OnStateChange("");
                            Debug.Log($"{ObjectModifiersPlugin.className}Quit to Main Menu");
                            InputDataManager.inst.players.Clear();
                            SceneManager.inst.LoadScene("Main Menu");
                        }
                        break;
                    }
                case "quitToArcade":
                    {
                        if (EditorManager.inst != null && !EditorManager.inst.isEditing)
                        {
                            if (ObjectModifiersPlugin.EditorLoadLevel.Value)
                            {
                                if (ObjectModifiersPlugin.EditorSavesBeforeLoad.Value)
                                {
                                    EditorManager.inst.SaveBeatmap();
                                }

                                string str = RTFile.BasePath;
                                if (RTFile.FileExists(str + "/level-modifier-backup.lsb"))
                                    System.IO.File.Delete(str + "/level-modifier-backup.lsb");

                                if (RTFile.FileExists(str + "/level.lsb"))
                                    System.IO.File.Copy(str + "/level.lsb", str + "/level-modifier-backup.lsb");

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
                        if (modifier.modifierObject &&
                            modifier.modifierObject.objectType != DataManager.GameData.BeatmapObject.ObjectType.Empty &&
                            Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) &&
                            levelObject.visualObject.Renderer &&
                            float.TryParse(modifier.value, out float num))
                        {
                            var rend = levelObject.visualObject.Renderer;
                            rend.material = ObjectModifiersPlugin.blur;
                            if (modifier.commands.Count > 1 && bool.TryParse(modifier.commands[1], out bool r) && r)
                            {
                                rend.material.SetFloat("_blurSizeXY", -(modifier.modifierObject.Interpolate(3, 1) - 1f) * num);
                            }
                            else
                                rend.material.SetFloat("_blurSizeXY", num);
                        }
                        break;
                    }
                case "particleSystem":
                    {
                        if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var mod = levelObject.visualObject.GameObject;

                            if (!modifier.modifierObject.particleSystem && !mod.GetComponent<ParticleSystem>())
                            {
                                modifier.modifierObject.particleSystem = mod.AddComponent<ParticleSystem>();
                                var ps = modifier.modifierObject.particleSystem;

                                var mat = mod.GetComponent<ParticleSystemRenderer>();
                                mat.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                                mat.material.color = Color.white;
                                mat.trailMaterial = mat.material;
                                mat.renderMode = ParticleSystemRenderMode.Mesh;

                                var s = int.Parse(modifier.commands[1]);
                                var so = int.Parse(modifier.commands[2]);

                                s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                                so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                                mat.mesh = ObjectManager.inst.objectPrefabs[s == 4 ? 0 : s == 6 ? 0 : s].options[so].GetComponentInChildren<MeshFilter>().mesh;

                                var psMain = ps.main;
                                var psEmission = ps.emission;

                                psMain.simulationSpace = ParticleSystemSimulationSpace.World;

                                psMain.startSpeed = float.Parse(modifier.commands[9]);

                                if (modifier.constant)
                                    ps.emissionRate = float.Parse(modifier.commands[10]);
                                else
                                {
                                    ps.emissionRate = 0f;
                                    psMain.loop = false;
                                    psEmission.burstCount = (int)float.Parse(modifier.commands[10]);
                                    psMain.duration = float.Parse(modifier.commands[11]);
                                }

                                var rotationOverLifetime = ps.rotationOverLifetime;
                                rotationOverLifetime.enabled = true;
                                rotationOverLifetime.separateAxes = true;
                                rotationOverLifetime.xMultiplier = 0f;
                                rotationOverLifetime.yMultiplier = 0f;
                                rotationOverLifetime.zMultiplier = float.Parse(modifier.commands[8]);

                                var forceOverLifetime = ps.forceOverLifetime;
                                forceOverLifetime.enabled = true;
                                forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                                forceOverLifetime.xMultiplier = float.Parse(modifier.commands[12]);
                                forceOverLifetime.yMultiplier = float.Parse(modifier.commands[13]);

                                var particlesTrail = ps.trails;
                                particlesTrail.enabled = bool.Parse(modifier.commands[14]);

                                var colorOverLifetime = ps.colorOverLifetime;
                                colorOverLifetime.enabled = true;
                                var psCol = colorOverLifetime.color;

                                float alphaStart = float.Parse(modifier.commands[4]);
                                float alphaEnd = float.Parse(modifier.commands[5]);

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

                                var sizeOverLifetime = ps.sizeOverLifetime;
                                sizeOverLifetime.enabled = true;

                                var ssss = sizeOverLifetime.size;

                                var sizeStart = float.Parse(modifier.commands[6]);
                                var sizeEnd = float.Parse(modifier.commands[7]);

                                var curve = new AnimationCurve(new Keyframe[2]
                                {
                                            new Keyframe(0f, sizeStart),
                                            new Keyframe(1f, sizeEnd)
                                });

                                ssss.curve = curve;

                                sizeOverLifetime.size = ssss;
                            }
                            else if (!modifier.modifierObject.particleSystem)
                            {
                                modifier.modifierObject.particleSystem = mod.GetComponent<ParticleSystem>();
                                var ps = modifier.modifierObject.particleSystem;

                                var mat = mod.GetComponent<ParticleSystemRenderer>();
                                mat.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                                mat.material.color = Color.white;
                                mat.trailMaterial = mat.material;
                                mat.renderMode = ParticleSystemRenderMode.Mesh;

                                var s = int.Parse(modifier.commands[1]);
                                var so = int.Parse(modifier.commands[2]);

                                s = Mathf.Clamp(s, 0, ObjectManager.inst.objectPrefabs.Count - 1);
                                so = Mathf.Clamp(so, 0, ObjectManager.inst.objectPrefabs[s].options.Count - 1);

                                mat.mesh = ObjectManager.inst.objectPrefabs[s == 4 ? 0 : s == 6 ? 0 : s].options[so].GetComponentInChildren<MeshFilter>().mesh;

                                var psMain = ps.main;
                                var psEmission = ps.emission;

                                psMain.simulationSpace = ParticleSystemSimulationSpace.World;

                                psMain.startSpeed = float.Parse(modifier.commands[9]);

                                if (modifier.constant)
                                    ps.emissionRate = float.Parse(modifier.commands[10]);
                                else
                                {
                                    ps.emissionRate = 0f;
                                    psMain.loop = false;
                                    psEmission.burstCount = int.Parse(modifier.commands[10]);
                                    psMain.duration = float.Parse(modifier.commands[11]);
                                }

                                var rotationOverLifetime = ps.rotationOverLifetime;
                                rotationOverLifetime.enabled = true;
                                rotationOverLifetime.separateAxes = true;
                                rotationOverLifetime.xMultiplier = 0f;
                                rotationOverLifetime.yMultiplier = 0f;
                                rotationOverLifetime.zMultiplier = float.Parse(modifier.commands[8]);

                                var forceOverLifetime = ps.forceOverLifetime;
                                forceOverLifetime.enabled = true;
                                forceOverLifetime.space = ParticleSystemSimulationSpace.World;
                                forceOverLifetime.xMultiplier = float.Parse(modifier.commands[12]);
                                forceOverLifetime.yMultiplier = float.Parse(modifier.commands[13]);

                                var particlesTrail = ps.trails;
                                particlesTrail.enabled = bool.Parse(modifier.commands[14]);

                                var colorOverLifetime = ps.colorOverLifetime;
                                colorOverLifetime.enabled = true;
                                var psCol = colorOverLifetime.color;

                                float alphaStart = float.Parse(modifier.commands[4]);
                                float alphaEnd = float.Parse(modifier.commands[5]);

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

                                var sizeOverLifetime = ps.sizeOverLifetime;
                                sizeOverLifetime.enabled = true;

                                var ssss = sizeOverLifetime.size;

                                var sizeStart = float.Parse(modifier.commands[6]);
                                var sizeEnd = float.Parse(modifier.commands[7]);

                                var curve = new AnimationCurve(new Keyframe[2]
                                {
                                            new Keyframe(0f, sizeStart),
                                            new Keyframe(1f, sizeEnd)
                                });

                                ssss.curve = curve;

                                sizeOverLifetime.size = ssss;
                            }

                            if (modifier.modifierObject.particleSystem)
                            {
                                var ps = modifier.modifierObject.particleSystem;
                                var psMain = ps.main;
                                var psEmission = ps.emission;

                                psMain.startLifetime = float.Parse(modifier.value);
                                psEmission.enabled = !(mod.transform.lossyScale.x < 0.001f && mod.transform.lossyScale.x > -0.001f || mod.transform.lossyScale.y < 0.001f && mod.transform.lossyScale.y > -0.001f) && mod.activeSelf && mod.activeInHierarchy;

                                var beatmapTheme = RTHelpers.BeatmapTheme;

                                psMain.startColor = beatmapTheme.GetObjColor(int.Parse(modifier.commands[3]));

                                if (!modifier.constant && !psMain.loop)
                                {
                                    ps.Play();
                                }
                            }
                        }

                        break;
                    }
                case "trailRenderer":
                    {
                        if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var mod = levelObject.visualObject.GameObject;

                            if (!modifier.modifierObject.trailRenderer && !mod.GetComponent<TrailRenderer>())
                            {
                                modifier.modifierObject.trailRenderer = mod.AddComponent<TrailRenderer>();

                                modifier.modifierObject.trailRenderer.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                                modifier.modifierObject.trailRenderer.material.color = Color.white;
                            }
                            else if (!modifier.modifierObject.trailRenderer)
                            {
                                modifier.modifierObject.trailRenderer = mod.GetComponent<TrailRenderer>();

                                modifier.modifierObject.trailRenderer.material = GameManager.inst.PlayerPrefabs[0].transform.GetChild(0).GetChild(0).GetComponent<TrailRenderer>().material;
                                modifier.modifierObject.trailRenderer.material.color = Color.white;
                            }
                            else
                            {
                                var tr = modifier.modifierObject.trailRenderer;

                                if (float.TryParse(modifier.value, out float time))
                                {
                                    tr.time = time;
                                }

                                tr.emitting = !(mod.transform.lossyScale.x < 0.001f && mod.transform.lossyScale.x > -0.001f || mod.transform.lossyScale.y < 0.001f && mod.transform.lossyScale.y > -0.001f) && mod.activeSelf && mod.activeInHierarchy;

                                if (float.TryParse(modifier.commands[1], out float startWidth) && float.TryParse(modifier.commands[2], out float endWidth))
                                {
                                    var t = mod.transform.lossyScale.magnitude * 0.576635f;
                                    tr.startWidth = startWidth * t;
                                    tr.endWidth = endWidth * t;
                                }

                                var beatmapTheme = RTHelpers.BeatmapTheme;

                                if (int.TryParse(modifier.commands[3], out int startColor) && float.TryParse(modifier.commands[4], out float startOpacity))
                                    tr.startColor = LSFunctions.LSColors.fadeColor(beatmapTheme.GetObjColor(startColor), startOpacity);
                                if (int.TryParse(modifier.commands[5], out int endColor) && float.TryParse(modifier.commands[6], out float endOpacity))
                                    tr.endColor = LSFunctions.LSColors.fadeColor(beatmapTheme.GetObjColor(endColor), endOpacity);
                            }
                        }
                        break;
                    }
                case "spawnPrefab":
                    {
                        if (modifier.commands.Count < 8)
                        {
                            modifier.commands.Add("0");
                            modifier.commands.Add("0");
                            modifier.commands.Add("1");
                        }

                        if (!modifier.constant && int.TryParse(modifier.value, out int num) && DataManager.inst.gameData.prefabs.Count > num
                            && float.TryParse(modifier.commands[1], out float posX) && float.TryParse(modifier.commands[2], out float posY)
                            && float.TryParse(modifier.commands[3], out float scaX) && float.TryParse(modifier.commands[4], out float scaY) && float.TryParse(modifier.commands[5], out float rot)
                            && int.TryParse(modifier.commands[6], out int repeatCount) && float.TryParse(modifier.commands[7], out float repeatOffsetTime) && float.TryParse(modifier.commands[8], out float speed))
                        {
                            modifier.Result = ObjectModifiersPlugin.AddPrefabObjectToLevel(DataManager.inst.gameData.prefabs[num],
                                AudioManager.inst.CurrentAudioSource.time,
                                new Vector2(posX, posY),
                                new Vector2(scaX, scaY),
                                rot, repeatCount, repeatOffsetTime, speed);

                            DataManager.inst.gameData.prefabObjects.Add((PrefabObject)modifier.Result);

                            Updater.AddPrefabToLevel((PrefabObject)modifier.Result);
                        }

                        break;
                    }
                case "playerHit":
                    {
                        if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !modifier.constant)
                        {
                            if (Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject != null && levelObject.visualObject.GameObject && int.TryParse(modifier.value, out int hit))
                            {
                                var orderedList = PlayerManager.Players
                                    .Where(x => x.Player)
                                    .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                                if (orderedList.Count > 0)
                                {
                                    var closest = orderedList[0];

                                    closest?.Player?.PlayerHit();

                                    if (hit > 1 && closest)
                                        closest.Health -= hit;
                                }
                            }
                        }

                        break;
                    }
                case "playerHitAll":
                    {
                        if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !modifier.constant && int.TryParse(modifier.value, out int hit))
                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                            {
                                player.Player.PlayerHit();

                                if (hit > 1)
                                    player.Health -= hit;
                            }

                        break;
                    }
                case "playerHeal":
                    {
                        if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !modifier.constant)
                            if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject && int.TryParse(modifier.value, out int hit))
                            {
                                var orderedList = PlayerManager.Players
                                    .Where(x => x.Player)
                                    .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                                if (orderedList.Count > 0)
                                {
                                    var closest = orderedList[0];

                                    if (closest)
                                        closest.Health += hit;
                                }
                            }
                        break;
                    }
                case "playerHealAll":
                    {
                        if ((EditorManager.inst == null && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 || !EditorManager.inst.isEditing) && !modifier.constant && int.TryParse(modifier.value, out int hit))
                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                            {
                                player.Health += hit;
                            }
                        break;
                    }
                case "playerKill":
                    {
                        if ((EditorManager.inst == null || !EditorManager.inst.isEditing) && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 && !modifier.constant)
                            if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                            {
                                var orderedList = PlayerManager.Players
                                    .Where(x => x.Player)
                                    .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                                if (orderedList.Count > 0)
                                {
                                    var closest = orderedList[0];

                                    if (closest)
                                        closest.Health = 0;
                                }
                            }

                        break;
                    }
                case "playerKillAll":
                    {
                        if ((EditorManager.inst == null || !EditorManager.inst.isEditing) && DataManager.inst.GetSettingEnum("ArcadeDifficulty", 1) != 0 && !modifier.constant)
                        {
                            foreach (var player in PlayerManager.Players.Where(x => x.Player))
                            {
                                player.Health = 0;
                            }
                        }
                        break;
                    }
                case "playerMove":
                    {
                        if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var vector = modifier.value.Split(new char[] { ',' });

                            if (modifier.commands.Count < 4)
                                modifier.commands.Add("False");

                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                bool relative = Parser.TryParse(modifier.commands[3], false);
                                if (closest)
                                {
                                    var tf = closest.Player.playerObjects["RB Parent"].gameObject.transform;
                                    if (modifier.constant)
                                        tf.localPosition = new Vector3(Parser.TryParse(vector[0], 0f), Parser.TryParse(vector[1], 0f), 0f);
                                    else
                                        tf
                                            .DOLocalMove(new Vector3(Parser.TryParse(vector[0], 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(vector[1], 0f) + (relative ? tf.position.y : 0f), 0f), Parser.TryParse(modifier.commands[1], 1f))
                                            .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                                }
                            }
                        }

                        break;
                    }
                case "playerMoveAll":
                    {
                        var vector = modifier.value.Split(new char[] { ',' });

                        if (modifier.commands.Count < 4)
                            modifier.commands.Add("False");

                        bool relative = Parser.TryParse(modifier.commands[3], false);
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            var tf = player.Player.playerObjects["RB Parent"].gameObject.transform;
                            if (modifier.constant)
                                tf.localPosition = new Vector3(Parser.TryParse(vector[0], 0f), Parser.TryParse(vector[1], 0f), 0f);
                            else
                                tf
                                    .DOLocalMove(new Vector3(Parser.TryParse(vector[0], 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(vector[1], 0f) + (relative ? tf.position.y : 0f), 0f), Parser.TryParse(modifier.commands[1], 1f))
                                    .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                        }

                        break;
                    }
                case "playerMoveX":
                    {
                        if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            if (modifier.commands.Count < 4)
                                modifier.commands.Add("False");

                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                bool relative = Parser.TryParse(modifier.commands[3], false);
                                if (closest)
                                {
                                    var tf = closest.Player.playerObjects["RB Parent"].gameObject.transform;
                                    if (modifier.constant)
                                    {
                                        var v = tf.localPosition;
                                        v.x += Parser.TryParse(modifier.value, 1f);
                                        tf.localPosition = v;
                                    }
                                    else
                                        tf
                                            .DOLocalMoveX(Parser.TryParse(modifier.value, 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(modifier.commands[1], 1f))
                                            .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                                }
                            }
                        }

                        break;
                    }
                case "playerMoveXAll":
                    {
                        if (modifier.commands.Count < 4)
                            modifier.commands.Add("False");

                        bool relative = Parser.TryParse(modifier.commands[3], false);
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            var tf = player.Player.playerObjects["RB Parent"].gameObject.transform;
                            if (modifier.constant)
                            {
                                var v = tf.localPosition;
                                v.x += Parser.TryParse(modifier.value, 1f);
                                tf.localPosition = v;
                            }
                            else
                                tf
                                    .DOLocalMoveX(Parser.TryParse(modifier.value, 0f) + (relative ? tf.position.x : 0f), Parser.TryParse(modifier.commands[1], 1f))
                                    .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                        }

                        break;
                    }
                case "playerMoveY":
                    {
                        if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            if (modifier.commands.Count < 4)
                            {
                                modifier.commands.Add("False");
                            }

                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                bool relative = Parser.TryParse(modifier.commands[3], false);
                                if (closest)
                                {
                                    var tf = closest.Player.playerObjects["RB Parent"].gameObject.transform;
                                    if (modifier.constant)
                                    {
                                        var v = tf.localPosition;
                                        v.y += Parser.TryParse(modifier.value, 1f);
                                        tf.localPosition = v;
                                    }
                                    else
                                        tf
                                            .DOLocalMoveY(Parser.TryParse(modifier.value, 0f) + (relative ? tf.position.y : 0f), Parser.TryParse(modifier.commands[1], 1f))
                                            .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                                }
                            }
                        }

                        break;
                    }
                case "playerMoveYAll":
                    {
                        if (modifier.commands.Count < 4)
                            modifier.commands.Add("False");

                        bool relative = Parser.TryParse(modifier.commands[3], false);
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            var tf = player.Player.playerObjects["RB Parent"].gameObject.transform;
                            if (modifier.constant)
                            {
                                var v = tf.localPosition;
                                v.y += Parser.TryParse(modifier.value, 1f);
                                tf.localPosition = v;
                            }
                            else
                                tf
                                    .DOLocalMoveY(Parser.TryParse(modifier.value, 0f) + (relative ? tf.position.y : 0f), Parser.TryParse(modifier.commands[1], 1f))
                                    .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                        }

                        break;
                    }
                case "playerRotate":
                    {
                        if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            if (modifier.commands.Count < 4)
                                modifier.commands.Add("False");

                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                bool relative = Parser.TryParse(modifier.commands[3], false);
                                if (closest)
                                {
                                    if (modifier.constant)
                                    {
                                        var v = closest.Player.playerObjects["RB Parent"].gameObject.transform.localRotation.eulerAngles;
                                        v.z += Parser.TryParse(modifier.value, 1f);
                                        closest.Player.playerObjects["RB Parent"].gameObject.transform.localRotation = Quaternion.Euler(v);
                                    }
                                    else
                                        closest.Player.playerObjects["RB Parent"].gameObject.transform
                                            .DORotate(new Vector3(0f, 0f, Parser.TryParse(modifier.value, 0f)), Parser.TryParse(modifier.commands[1], 1f))
                                            .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                                }
                            }
                        }

                        break;
                    }
                case "playerRotateAll":
                    {
                        if (modifier.commands.Count < 4)
                            modifier.commands.Add("False");

                        bool relative = Parser.TryParse(modifier.commands[3], false);
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            if (modifier.constant)
                            {
                                var v = player.Player.playerObjects["RB Parent"].gameObject.transform.localRotation.eulerAngles;
                                v.z += Parser.TryParse(modifier.value, 1f);
                                player.Player.playerObjects["RB Parent"].gameObject.transform.localRotation = Quaternion.Euler(v);
                            }
                            else
                                player.Player.playerObjects["RB Parent"].gameObject.transform
                                    .DORotate(new Vector3(0f, 0f, Parser.TryParse(modifier.value, 0f)), Parser.TryParse(modifier.commands[1], 1f))
                                    .SetEase(DataManager.inst.AnimationList[Parser.TryParse(modifier.commands[2], 0)].Animation);
                        }

                        break;
                    }
                case "playerBoost":
                    {
                        if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject && !modifier.constant)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                closest?.Player?.StartBoost();
                            }
                        }

                        break;
                    }
                case "playerBoostAll":
                    {
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            player.Player.StartBoost();
                        }

                        break;
                    }
                case "playerDisableBoost":
                    {
                        if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject && !modifier.constant)
                        {
                            var orderedList = PlayerManager.Players
                                .Where(x => x.Player)
                                .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList();

                            if (orderedList.Count > 0)
                            {
                                var closest = orderedList[0];

                                if (closest)
                                    closest.Player.canBoost = false;
                            }
                        }

                        break;
                    }
                case "playerDisableBoostAll":
                    {
                        foreach (var player in PlayerManager.Players.Where(x => x.Player))
                        {
                            player.Player.canBoost = false;
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
                            LSFunctions.LSHelpers.HideCursor();
                        break;
                    }
                case "addVariable":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && int.TryParse(modifier.value, out int num))
                        {
                            foreach (var bm in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == modifier.commands[1]))
                            {
                                var beatmapObject = (BeatmapObject)bm;
                                beatmapObject.integerVariable += num;
                            }
                        }
                        break;
                    }
                case "subVariable":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && int.TryParse(modifier.value, out int num))
                        {
                            foreach (var bm in DataManager.inst.gameData.beatmapObjects.FindAll(x => x.name == modifier.commands[1]))
                            {
                                var beatmapObject = (BeatmapObject)bm;
                                beatmapObject.integerVariable -= num;
                            }
                        }
                        break;
                    }
                case "setVariable":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && int.TryParse(modifier.value, out int num))
                        {
                            foreach (var bm in list)
                            {
                                var beatmapObject = (BeatmapObject)bm;
                                beatmapObject.integerVariable = num;
                            }
                        }
                        break;
                    }
                case "setVariableRandom":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (list.Count() > 0 && int.TryParse(modifier.commands[1], out int min) && int.TryParse(modifier.commands[2], out int max))
                        {
                            foreach (var bm in list)
                            {
                                var beatmapObject = (BeatmapObject)bm;
                                beatmapObject.integerVariable = UnityEngine.Random.Range(min, max < 0 ? max - 1 : max + 1);
                            }
                        }
                        break;
                    }
                case "disableObject":
                    {
                        if (Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                        {
                            levelObject.transformChain[0].gameObject.SetActive(false);
                        }
                        break;
                    }
                case "disableObjectTree":
                    {
                        var parentChain = modifier.modifierObject.GetParentChain();

                        foreach (var cc in parentChain[parentChain.Count - 1].GetChildChain())
                        {
                            for (int o = 0; o < cc.Count; o++)
                            {
                                if (cc[o] != null && Updater.TryGetObject(cc[o], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                {
                                    levelObject.transformChain[0].gameObject.SetActive(false);
                                }
                            }
                        }
                        break;
                    }
                case "save":
                    {
                        if ((EditorManager.inst == null || !EditorManager.inst.isEditing) && float.TryParse(modifier.value, out float num))
                        {
                            ObjectModifiersPlugin.SaveProgress(modifier.commands[1], modifier.commands[2], modifier.commands[3], num);
                        }
                        break;
                    }
                case "saveVariable":
                    {
                        if (EditorManager.inst == null || !EditorManager.inst.isEditing)
                        {
                            ObjectModifiersPlugin.SaveProgress(modifier.commands[1], modifier.commands[2], modifier.commands[3], modifier.modifierObject.integerVariable);
                        }
                        break;
                    }
                case "reactivePos":
                    {
                        if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject
                            && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                            && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                            && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sampleX = Mathf.Clamp(sampleX, 0, 255);
                            sampleY = Mathf.Clamp(sampleY, 0, 255);

                            float reactivePositionX = samples[sampleX] * intensityX * val;
                            float reactivePositionY = samples[sampleY] * intensityY * val;

                            var x = modifier.modifierObject.origin.x;
                            var y = modifier.modifierObject.origin.y;

                            levelObject.visualObject.GameObject.transform.localPosition = new Vector3(x + reactivePositionX, y + reactivePositionY, modifier.modifierObject.depth * 0.1f);

                            samples = null;
                        }
                        break;
                    }
                case "reactiveSca":
                    {
                        if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject
                            && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                            && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                            && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sampleX = Mathf.Clamp(sampleX, 0, 255);
                            sampleY = Mathf.Clamp(sampleY, 0, 255);

                            float reactiveScaleX = samples[sampleX] * intensityX * val;
                            float reactiveScaleY = samples[sampleY] * intensityY * val;

                            levelObject.visualObject.GameObject.transform.localScale = new Vector3(1f + reactiveScaleX, 1f + reactiveScaleY, 1f);

                            samples = null;
                        }
                        break;
                    }
                case "reactiveRot":
                    {
                        if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject
                            && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sample = Mathf.Clamp(sample, 0, 255);

                            float reactiveRotation = samples[sample] * val;

                            levelObject.visualObject.GameObject.transform.localRotation = Quaternion.Euler(0f, 0f, reactiveRotation);

                            samples = null;
                        }
                        break;
                    }
                case "reactiveCol":
                    {
                        if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.Renderer
                            && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sample = Mathf.Clamp(sample, 0, 255);

                            float reactiveColor = samples[sample] * val;

                            if (levelObject.visualObject.Renderer != null && int.TryParse(modifier.commands[2], out int col))
                                levelObject.visualObject.Renderer.material.color += GameManager.inst.LiveTheme.objectColors[col] * reactiveColor;

                            samples = null;
                        }
                        break;
                    }
                case "reactiveColLerp":
                    {
                        if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.Renderer
                            && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sample = Mathf.Clamp(sample, 0, 255);

                            float reactiveColor = samples[sample] * val;
                            
                            if (levelObject.visualObject.Renderer != null && int.TryParse(modifier.commands[2], out int col))
                                levelObject.visualObject.Renderer.material.color = RTMath.Lerp(levelObject.visualObject.Renderer.material.color, GameManager.inst.LiveTheme.objectColors[col], reactiveColor);

                            samples = null;
                        }
                        break;
                    }
                case "reactivePosChain":
                    {
                        if (modifier.modifierObject
                            && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                            && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                            && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sampleX = Mathf.Clamp(sampleX, 0, 255);
                            sampleY = Mathf.Clamp(sampleY, 0, 255);

                            float reactivePositionX = samples[sampleX] * intensityX * val;
                            float reactivePositionY = samples[sampleY] * intensityY * val;

                            modifier.modifierObject.reactivePositionOffset = new Vector3(reactivePositionX, reactivePositionY);

                            samples = null;
                        }

                        break;
                    }
                case "reactiveScaChain":
                    {
                        if (modifier.modifierObject
                            && int.TryParse(modifier.commands[1], out int sampleX) && float.TryParse(modifier.commands[3], out float intensityX)
                            && int.TryParse(modifier.commands[2], out int sampleY) && float.TryParse(modifier.commands[4], out float intensityY)
                            && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sampleX = Mathf.Clamp(sampleX, 0, 255);
                            sampleY = Mathf.Clamp(sampleY, 0, 255);

                            float reactiveScaleX = samples[sampleX] * intensityX * val;
                            float reactiveScaleY = samples[sampleY] * intensityY * val;

                            modifier.modifierObject.reactiveScaleOffset = new Vector3(reactiveScaleX, reactiveScaleY, 1f);

                            samples = null;
                        }

                        break;
                    }
                case "reactiveRotChain":
                    {
                        if (modifier.modifierObject && int.TryParse(modifier.commands[1], out int sample) && float.TryParse(modifier.value, out float val))
                        {
                            float[] samples = new float[256];

                            AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                            sample = Mathf.Clamp(sample, 0, 255);

                            float reactiveRotation = samples[sample] * val;

                            modifier.modifierObject.reactiveRotationOffset = reactiveRotation;

                            samples = null;
                        }
                        break;
                    }
                case "reactiveColChain":
                    {
                        //if (refModifier != null)
                        //{
                        //    var ch = refModifier.beatmapObject.GetChildChain();

                        //    float[] samples = new float[256];

                        //    AudioManager.inst.CurrentAudioSource.GetSpectrumData(samples, 0, FFTWindow.Rectangular);

                        //    float reactiveColor = samples[int.Parse(modifier.commands[1])] * float.Parse(value);

                        //    foreach (var cc in ch)
                        //    {
                        //        for (int i = 0; i < cc.Count; i++)
                        //        {
                        //            var modifier = Objects.beatmapObjects[cc[i].id];

                        //            if (modifier.renderer != null)
                        //                modifier.renderer.material.color += GameManager.inst.LiveTheme.objectColors[int.Parse(modifier.commands[2])] * reactiveColor;
                        //        }
                        //    }
                        //}
                        break;
                    }
                case "setPlayerModel":
                    {
                        if (!modifier.constant && ModCompatibility.mods.ContainsKey("CreativePlayers") &&
                            int.TryParse(modifier.commands[1], out int result) && PlayerManager.PlayerModels.ContainsKey(modifier.value))
                        {
                            PlayerManager.SetPlayerModel?.Invoke(result, modifier.value);
                            PlayerManager.AssignPlayerModels();

                            if (PlayerManager.Players.Count > result && PlayerManager.Players[result].Player)
                            {
                                PlayerManager.Players[result].Player.playerNeedsUpdating = true;
                                PlayerManager.Players[result].Player.UpdatePlayer();
                            }
                        }
                        break;
                    }
                case "eventOffset":
                    {
                        if (ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
                        {
                            var list = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];

                            var indexArray = Parser.TryParse(modifier.commands[1], 0);
                            var indexValue = Parser.TryParse(modifier.commands[2], 0);

                            if (indexArray < list.Count && indexValue < list[indexArray].Count)
                                list[indexArray][indexValue] = Parser.TryParse(modifier.value, 0f);

                            ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                        }
                        break;
                    }
                case "eventOffsetAnimate":
                    {
                        if (!modifier.constant && ModCompatibility.sharedFunctions.ContainsKey("EventsCoreEventOffsets"))
                        {
                            var list = (List<List<float>>)ModCompatibility.sharedFunctions["EventsCoreEventOffsets"];

                            var indexArray = Parser.TryParse(modifier.commands[1], 0);
                            var indexValue = Parser.TryParse(modifier.commands[2], 0);

                            if (modifier.commands.Count < 6)
                                modifier.commands.Add("False");

                            if (indexArray < list.Count && indexValue < list[indexArray].Count)
                            {
                                var value = Parser.TryParse(modifier.commands[5], false) ? list[indexArray][indexValue] + Parser.TryParse(modifier.value, 0f) : Parser.TryParse(modifier.value, 0f);

                                var animation = new AnimationManager.Animation("Event Offset Animation");
                                animation.floatAnimations = new List<AnimationManager.Animation.AnimationObject<float>>
                                {
                                    new AnimationManager.Animation.AnimationObject<float>(new List<IKeyframe<float>>
                                    {
                                        new FloatKeyframe(0f, list[indexArray][indexValue], Ease.Linear),
                                        new FloatKeyframe(Parser.TryParse(modifier.commands[3], 1f), value, Ease.HasEaseFunction(modifier.commands[4]) ? Ease.GetEaseFunction(modifier.commands[4]) : Ease.Linear),
                                        new FloatKeyframe(Parser.TryParse(modifier.commands[3], 1f) + 0.1f, value, Ease.Linear),
                                    }, delegate (float x)
                                    {
                                        list[indexArray][indexValue] = x;
                                        ModCompatibility.sharedFunctions["EventsCoreEventOffsets"] = list;
                                    })
                                };
                                animation.onComplete = delegate ()
                                {
                                    AnimationManager.inst.RemoveID(animation.id);
                                };

                                AnimationManager.inst.Play(animation);
                            }
                        }
                        break;
                    }
                case "legacyTail":
                    {
                        //if (!tailDone)
                        //{
                        //    var parent = new GameObject(modifierObject.id);
                        //    parent.transform.SetParent(ObjectManager.inst.objectParent.transform);
                        //    parent.transform.localScale = Vector3.one;
                        //    var legacyTracker = parent.AddComponent<LegacyTracker>();

                        //    var ch = modifierObject.GetChildChain();

                        //    foreach (var cc in ch)
                        //    {
                        //        for (int i = 0; i < cc.Count; i++)
                        //        {
                        //            var obj = cc[i];

                        //            if (Objects.beatmapObjects.ContainsKey(obj.id))
                        //            {
                        //                var modifier = Objects.beatmapObjects[cc[i].id];

                        //                var tf = modifier.transformChain;

                        //                if (tf != null && tf.Count > 0)
                        //                {
                        //                    var top = tf[0];

                        //                    var id = LSFunctions.LSText.randomNumString(16);

                        //                    var rt = new LegacyTracker.RTObject(top.gameObject);
                        //                    rt.values.Add("Renderer", top.GetComponentInChildren<Renderer>());
                        //                    legacyTracker.originals.Add(id, rt);
                        //                }
                        //            }
                        //        }
                        //    }

                        //    legacyTracker.distance = float.Parse(value);
                        //    if (command.Count > 1)
                        //        legacyTracker.tailCount = int.Parse(modifier.commands[1]);
                        //    if (command.Count > 2)
                        //        legacyTracker.transformSpeed = float.Parse(modifier.commands[2]);
                        //    if (command.Count > 3)
                        //        legacyTracker.distanceSpeed = float.Parse(modifier.commands[3]);

                        //    legacyTracker.Setup();

                        //    tailDone = true;
                        //}
                        break;
                    }
                case "blackHole":
                    {
                        if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject)
                        {
                            var gm = levelObject.visualObject.GameObject;

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

                                    float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(float.Parse(modifier.value), 0.001f, 1f), p);

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
                        if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.Renderer && int.TryParse(modifier.commands[1], out int index) && float.TryParse(modifier.value, out float num))
                        {
                            index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                            if (levelObject.visualObject.Renderer != null)
                                levelObject.visualObject.Renderer.material.color += GameManager.inst.LiveTheme.objectColors[index] * num;
                        }

                        break;
                    }
                case "addColorOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                            foreach (var bm in list)
                            {
                                if (Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject.Renderer && int.TryParse(modifier.commands[2], out int index) && float.TryParse(modifier.value, out float num))
                                {
                                    index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                                    levelObject.visualObject.Renderer.material.color += GameManager.inst.LiveTheme.objectColors[index] * num;
                                }
                            }

                        break;
                    }
                case "lerpColor":
                    {
                        if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.Renderer && int.TryParse(modifier.commands[1], out int index) && float.TryParse(modifier.value, out float num))
                        {
                            index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                            if (levelObject.visualObject != null && levelObject.visualObject.Renderer)
                                levelObject.visualObject.Renderer.material.color = RTMath.Lerp(levelObject.visualObject.Renderer.material.color, GameManager.inst.LiveTheme.objectColors[index], num);
                        }

                        break;
                    }
                case "lerpColorOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                            foreach (var bm in list)
                            {
                                if (bm != null && Updater.TryGetObject(bm, out LevelObject levelObject) && int.TryParse(modifier.commands[2], out int index) && float.TryParse(modifier.value, out float num))
                                {
                                    index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                                    if (levelObject.visualObject != null && levelObject.visualObject.Renderer)
                                        levelObject.visualObject.Renderer.material.color = RTMath.Lerp(levelObject.visualObject.Renderer.material.color, GameManager.inst.LiveTheme.objectColors[index], num);
                                }
                            }


                        break;
                    }
                case "addColorPlayerDistance":
                    {
                        if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject && levelObject.visualObject.Renderer && int.TryParse(modifier.commands[1], out int index) && float.TryParse(modifier.value, out float num))
                        {
                            var i = ObjectExtensions.ClosestPlayer(levelObject.visualObject.GameObject);

                            var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));

                            var distance = Vector2.Distance(player.transform.position, levelObject.visualObject.GameObject.transform.position);

                            index = Mathf.Clamp(index, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

                            levelObject.visualObject.Renderer.material.color += GameManager.inst.LiveTheme.objectColors[index] * distance * num;
                        }

                        break;
                    }
                case "setAlpha":
                    {
                        if (modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.Renderer && float.TryParse(modifier.value, out float num))
                        {
                            if (levelObject.visualObject is not TextObject)
                                levelObject.visualObject.Renderer.material.color = LSFunctions.LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                            else
                                ((TextObject)levelObject.visualObject).TextMeshPro.color = LSFunctions.LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                        }

                        break;
                    }
                case "setAlphaOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                            foreach (var bm in list)
                            {
                                if (bm != null && Updater.TryGetObject(bm, out LevelObject levelObject) && levelObject.visualObject.Renderer && float.TryParse(modifier.value, out float num))
                                {
                                    if (levelObject.visualObject is not TextObject)
                                        levelObject.visualObject.Renderer.material.color = LSFunctions.LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                                    else
                                        ((TextObject)levelObject.visualObject).TextMeshPro.color = LSFunctions.LSColors.fadeColor(levelObject.visualObject.Renderer.material.color, num);
                                }
                            }

                        break;
                    }
                case "copyColor":
                    {
                        if (DataManager.inst.gameData.beatmapObjects.TryFind(x => (x as BeatmapObject).tags.Contains(modifier.value), out DataManager.GameData.BeatmapObject beatmapObject) &&
                            Updater.TryGetObject(beatmapObject, out LevelObject otherLevelObject) &&
                            otherLevelObject.visualObject.Renderer &&
                            Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) &&
                            levelObject.visualObject.Renderer)
                        {
                            levelObject.visualObject.Renderer.material.color = otherLevelObject.visualObject.Renderer.material.color;
                        }

                        break;
                    }
                case "copyColorOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (list.Count() > 0)
                            foreach (var bm in list)
                            {
                                if (Updater.TryGetObject(bm, out LevelObject otherLevelObject) &&
                                    otherLevelObject.visualObject.Renderer &&
                                    Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) &&
                                    levelObject.visualObject.Renderer)
                                {
                                    otherLevelObject.visualObject.Renderer.material.color = levelObject.visualObject.Renderer.material.color;
                                }
                            }

                        break;
                    }
                case "updateObjects":
                    {
                        if (!modifier.constant)
                            ObjectManager.inst.updateObjects();
                        break;
                    }
                case "updateObject":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.value));

                        if (!modifier.constant && list.Count() > 0)
                        {
                            foreach (var bm in list)
                            {
                                Updater.UpdateProcessor(bm);
                            }
                        }
                        break;
                    }
                case "animationObject":
                    {

                        break;
                    }
                case "signalModifier":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        foreach (var bm in list)
                        {
                            ObjectModifiersPlugin.inst.StartCoroutine(ObjectModifiersPlugin.ActivateModifier((BeatmapObject)bm, Parser.TryParse(modifier.value, 0f)));
                        }

                        break;
                    }
                case "editorNotify":
                    {
                        EditorManager.inst?.DisplayNotification(modifier.value, Parser.TryParse(modifier.commands[1], 0.5f), (EditorManager.NotificationType)Parser.TryParse(modifier.commands[2], 0));
                        break;
                    }
                case "setText":
                    {
                        if (modifier.modifierObject.shape == 4 && modifier.modifierObject.levelObject && modifier.modifierObject.levelObject.visualObject != null &&
                            modifier.modifierObject.levelObject.visualObject is TextObject)
                        {
                            if (modifier.constant)
                                ((TextObject)modifier.modifierObject.levelObject.visualObject).SetText(modifier.value);
                            else
                                ((TextObject)modifier.modifierObject.levelObject.visualObject).Text = modifier.value;
                        }
                        break;
                    }
                case "setTextOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    if (modifier.constant)
                                        textObject.SetText(modifier.value);
                                    else
                                        textObject.Text = modifier.value;
                                }
                            }
                        }

                        break;
                    }
                case "addText":
                    {
                        if (modifier.modifierObject.shape == 4 && modifier.modifierObject.levelObject && modifier.modifierObject.levelObject.visualObject != null &&
                            modifier.modifierObject.levelObject.visualObject is TextObject)
                        {
                            ((TextObject)modifier.modifierObject.levelObject.visualObject).Text += modifier.value;
                        }
                        break;
                    }
                case "addTextOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    textObject.Text += modifier.value;
                                }
                            }
                        }

                        break;
                    }
                case "removeText":
                    {
                        if (modifier.modifierObject.shape == 4 && modifier.modifierObject.levelObject && modifier.modifierObject.levelObject.visualObject != null &&
                            modifier.modifierObject.levelObject.visualObject is TextObject && int.TryParse(modifier.value, out int remove))
                        {
                            var visualObject = (TextObject)modifier.modifierObject.levelObject.visualObject;
                            string text = string.IsNullOrEmpty(visualObject.TextMeshPro.text) ? "" :
                                visualObject.TextMeshPro.text.Substring(0, visualObject.TextMeshPro.text.Length - Mathf.Clamp(remove, 0, visualObject.TextMeshPro.text.Length - 1));

                            if (modifier.constant)
                                visualObject.SetText(text);
                            else
                                visualObject.Text = text;
                        }
                        break;
                    }
                case "removeTextOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && int.TryParse(modifier.value, out int remove))
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    string text = string.IsNullOrEmpty(textObject.TextMeshPro.text) ? "" :
                                        textObject.TextMeshPro.text.Substring(0, textObject.TextMeshPro.text.Length - Mathf.Clamp(remove, 0, textObject.TextMeshPro.text.Length - 1));

                                    if (modifier.constant)
                                        textObject.SetText(text);
                                    else
                                        textObject.Text = text;
                                }
                            }
                        }
                        break;
                    }
                case "removeTextAt":
                    {
                        if (modifier.modifierObject.shape == 4 && modifier.modifierObject.levelObject && modifier.modifierObject.levelObject.visualObject != null &&
                            modifier.modifierObject.levelObject.visualObject is TextObject && int.TryParse(modifier.value, out int remove))
                        {
                            var visualObject = (TextObject)modifier.modifierObject.levelObject.visualObject;
                            string text = string.IsNullOrEmpty(visualObject.TextMeshPro.text) ? "" : visualObject.TextMeshPro.text.Length > remove ?
                                visualObject.TextMeshPro.text.Remove(remove, 1) : "";

                            if (modifier.constant)
                                visualObject.SetText(text);
                            else
                                visualObject.Text = text;
                        }
                        break;
                    }
                case "removeTextOtherAt":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && int.TryParse(modifier.value, out int remove))
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                {
                                    string text = string.IsNullOrEmpty(textObject.TextMeshPro.text) ? "" : textObject.TextMeshPro.text.Length > remove ?
                                        textObject.TextMeshPro.text.Remove(remove, 1) : "";

                                    if (modifier.constant)
                                        textObject.SetText(text);
                                    else
                                        textObject.Text = text;
                                }
                            }
                        }
                        break;
                    }
                case "clampVariable":
                    {
                        modifier.modifierObject.integerVariable = Mathf.Clamp(modifier.modifierObject.integerVariable, Parser.TryParse(modifier.commands.Count > 1 ? modifier.commands[1] : "1", 0), Parser.TryParse(modifier.commands.Count > 2 ? modifier.commands[2] : "1", 1));
                        break;
                    }
                case "clampVariableOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0)
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                bm.integerVariable = Mathf.Clamp(bm.integerVariable, Parser.TryParse(modifier.commands.Count > 1 ? modifier.commands[1] : "1", 0), Parser.TryParse(modifier.commands.Count > 2 ? modifier.commands[2] : "1", 1));
                            }
                        }

                        break;
                    }
                case "animateObject":
                    {
                        if (int.TryParse(modifier.commands[1], out int type)
                            && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                            && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                        {
                            Vector3 vector;
                            if (type == 0)
                                vector = modifier.modifierObject.positionOffset;
                            else if (type == 1)
                                vector = modifier.modifierObject.scaleOffset;
                            else
                                vector = modifier.modifierObject.rotationOffset;

                            var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

                            if (!modifier.constant)
                            {
                                var animation = new AnimationManager.Animation("Animate Object Offset");

                                animation.vector3Animations = new List<AnimationManager.Animation.AnimationObject<Vector3>>
                                {
                                    new AnimationManager.Animation.AnimationObject<Vector3>(new List<IKeyframe<Vector3>>
                                    {
                                        new Vector3Keyframe(0f, vector, Ease.Linear),
                                        new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector,
                                        Ease.HasEaseFunction(modifier.commands[6]) ? Ease.GetEaseFunction(modifier.commands[6]) : Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f) + 0.1f, setVector, Ease.Linear),
                                    }, delegate (Vector3 vector3)
                                    {
                                        if (type == 0)
                                            modifier.modifierObject.positionOffset = vector3;
                                        else if (type == 1)
                                            modifier.modifierObject.scaleOffset = vector3;
                                        else
                                            modifier.modifierObject.rotationOffset = vector3;
                                    }),
                                };
                                animation.onComplete = delegate ()
                                {
                                    AnimationManager.inst.RemoveID(animation.id);
                                };
                                AnimationManager.inst.Play(animation);
                            }
                            else
                            {
                                if (type == 0)
                                    modifier.modifierObject.positionOffset = setVector;
                                else if (type == 1)
                                    modifier.modifierObject.scaleOffset = setVector;
                                else
                                    modifier.modifierObject.rotationOffset = setVector;
                            }
                        }

                        break;
                    }
                case "animateObjectOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[7]));

                        if (list.Count() > 0 && int.TryParse(modifier.commands[1], out int type)
                            && float.TryParse(modifier.commands[2], out float x) && float.TryParse(modifier.commands[3], out float y) && float.TryParse(modifier.commands[4], out float z)
                            && bool.TryParse(modifier.commands[5], out bool relative) && float.TryParse(modifier.value, out float time))
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                Vector3 vector;
                                if (type == 0)
                                    vector = bm.positionOffset;
                                else if (type == 1)
                                    vector = bm.scaleOffset;
                                else
                                    vector = bm.rotationOffset;

                                var setVector = new Vector3(x, y, z) + (relative ? vector : Vector3.zero);

                                if (!modifier.constant)
                                {
                                    var animation = new AnimationManager.Animation("Animate Other Object Offset");

                                    animation.vector3Animations = new List<AnimationManager.Animation.AnimationObject<Vector3>>
                                    {
                                        new AnimationManager.Animation.AnimationObject<Vector3>(new List<IKeyframe<Vector3>>
                                        {
                                            new Vector3Keyframe(0f, vector, Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f), setVector,
                                            Ease.HasEaseFunction(modifier.commands[6]) ? Ease.GetEaseFunction(modifier.commands[6]) : Ease.Linear),
                                            new Vector3Keyframe(Mathf.Clamp(time, 0f, 9999f) + 0.1f, setVector, Ease.Linear),
                                        }, delegate (Vector3 vector3)
                                        {
                                            if (type == 0)
                                                bm.positionOffset = vector3;
                                            else if (type == 1)
                                                bm.scaleOffset = vector3;
                                            else
                                                bm.rotationOffset = vector3;
                                        }),
                                    };
                                    animation.onComplete = delegate ()
                                    {
                                        AnimationManager.inst.RemoveID(animation.id);
                                    };
                                    AnimationManager.inst.Play(animation);
                                }
                                else
                                {
                                    if (type == 0)
                                        bm.positionOffset = setVector;
                                    else if (type == 1)
                                        bm.scaleOffset = setVector;
                                    else
                                        bm.rotationOffset = setVector;
                                }
                            }
                        }

                        break;
                    }
                case "rigidBody":
                    {


                        break;
                    }
                case "copyAxis":
                    {
                        /*
                        From Type: (Pos / Sca / Rot)
                        From Axis: (X / Y / Z)
                        Object Group
                        To Type: (Pos / Sca / Rot)
                        To Axis: (X / Y / Z)
                        */

                        if (int.TryParse(modifier.commands[1], out int fromType) && int.TryParse(modifier.commands[2], out int fromAxis)
                            && int.TryParse(modifier.commands[3], out int toType) && int.TryParse(modifier.commands[4], out int toAxis)
                            && DataManager.inst.gameData.beatmapObjects.TryFind(x => (x as BeatmapObject).tags.Contains(modifier.value), out DataManager.GameData.BeatmapObject beatmapObject)
                            && beatmapObject != null)
                        {
                            var bm = beatmapObject as BeatmapObject;

                            fromType = Mathf.Clamp(fromType, 0, bm.events.Count);
                            fromAxis = Mathf.Clamp(fromAxis, 0, bm.events[fromType][0].eventValues.Length);

                            if (toType == 0 && toAxis == 0)
                                modifier.modifierObject.positionOffset.x = bm.Interpolate(fromType, fromAxis);
                            
                            if (toType == 0 && toAxis == 1)
                                modifier.modifierObject.positionOffset.y = bm.Interpolate(fromType, fromAxis);
                            
                            if (toType == 0 && toAxis == 2)
                                modifier.modifierObject.positionOffset.z = bm.Interpolate(fromType, fromAxis);
                            
                            if (toType == 1 && toAxis == 0)
                                modifier.modifierObject.scaleOffset.x = bm.Interpolate(fromType, fromAxis);
                            
                            if (toType == 1 && toAxis == 1)
                                modifier.modifierObject.scaleOffset.y = bm.Interpolate(fromType, fromAxis);
                            
                            if (toType == 1 && toAxis == 2)
                                modifier.modifierObject.scaleOffset.z = bm.Interpolate(fromType, fromAxis);
                            
                            if (toType == 2 && toAxis == 0)
                                modifier.modifierObject.rotationOffset.x = bm.Interpolate(fromType, fromAxis);
                            
                            if (toType == 2 && toAxis == 1)
                                modifier.modifierObject.rotationOffset.y = bm.Interpolate(fromType, fromAxis);
                            
                            if (toType == 2 && toAxis == 2)
                                modifier.modifierObject.rotationOffset.z = bm.Interpolate(fromType, fromAxis);

                        }

                        break;
                    }
            }
        }

        public static void Inactive(BeatmapObject.Modifier modifier)
        {
            switch (modifier.commands[0])
            {
                case "spawnPrefab":
                    {
                        if (!modifier.constant && modifier.Result != null && modifier.Result is PrefabObject)
                        {
                            Updater.UpdatePrefab((PrefabObject)modifier.Result, false);

                            DataManager.inst.gameData.prefabObjects.RemoveAll(x => ((PrefabObject)x).fromModifier && x.ID == ((PrefabObject)modifier.Result).ID);

                            modifier.Result = null;
                        }
                        break;
                    }
                case "playerDisableBoost":
                    {
                        if (!modifier.hasChanged)
                        {
                            modifier.hasChanged = true;

                            if (modifier.modifierObject && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.visualObject.GameObject && !modifier.constant)
                            {
                                var closest = PlayerManager.Players
                                    .Where(x => x.Player)
                                    .OrderBy(x => Vector2.Distance(x.Player.playerObjects["RB Parent"].gameObject.transform.position, levelObject.visualObject.GameObject.transform.position)).ToList()[0];

                                if (closest)
                                    closest.Player.canBoost = true;
                            }
                        }

                        break;
                    }
                case "disableObject":
                    {
                        if (!modifier.hasChanged && modifier.modifierObject != null && Updater.TryGetObject(modifier.modifierObject, out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                        {
                            levelObject.transformChain[0].gameObject.SetActive(true);
                            modifier.hasChanged = true;
                        }

                        break;
                    }
                case "disableObjectTree":
                    {
                        if (!modifier.hasChanged)
                        {
                            var parentChain = modifier.modifierObject.GetParentChain();

                            foreach (var cc in parentChain[parentChain.Count - 1].GetChildChain())
                            {
                                for (int o = 0; o < cc.Count; o++)
                                {
                                    if (cc[o] != null && Updater.TryGetObject(cc[o], out LevelObject levelObject) && levelObject.transformChain != null && levelObject.transformChain.Count > 0 && levelObject.transformChain[0] != null)
                                        levelObject.transformChain[0].gameObject.SetActive(true);
                                }
                            }
                            modifier.hasChanged = true;
                        }

                        break;
                    }
                case "reactivePosChain":
                    {
                        modifier.modifierObject.reactivePositionOffset = Vector3.zero;

                        break;
                    }
                case "reactiveScaChain":
                    {
                        modifier.modifierObject.reactiveScaleOffset = Vector3.zero;

                        break;
                    }
                case "reactiveRotChain":
                    {
                        modifier.modifierObject.reactiveRotationOffset = 0f;

                        break;
                    }
                case "signalModifier":
                case "mouseOverSignalModifier":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (list.Count() > 0 && !modifier.constant)
                            foreach (var bm in list)
                            {
                                if ((bm as BeatmapObject).modifiers.Count > 0 && (bm as BeatmapObject).modifiers.Where(x => x.commands[0] == "requireSignal" && x.type == BeatmapObject.Modifier.Type.Trigger).Count() > 0 &&
                                    (bm as BeatmapObject).modifiers.TryFind(x => x.commands[0] == "requireSignal" && x.type == BeatmapObject.Modifier.Type.Trigger, out BeatmapObject.Modifier m))
                                {
                                    m.Result = null;
                                }
                            }

                        break;
                    }
                case "randomGreater":
                case "randomLesser":
                case "randomEquals":
                    {
                        modifier.Result = null;
                        break;
                    }
                case "setText":
                    {
                        if (modifier.constant && modifier.modifierObject.shape == 4 && modifier.modifierObject.levelObject && modifier.modifierObject.levelObject.visualObject != null &&
                            modifier.modifierObject.levelObject.visualObject is TextObject textObject)
                            textObject.Text = modifier.modifierObject.text;
                        break;
                    }
                case "setTextOther":
                    {
                        var list = DataManager.inst.gameData.beatmapObjects.Where(x => (x as BeatmapObject).tags.Contains(modifier.commands[1]));

                        if (modifier.constant && list.Count() > 0)
                        {
                            foreach (var bm in list.Select(x => x as BeatmapObject))
                            {
                                if (bm.shape == 4 && bm.levelObject && bm.levelObject.visualObject != null &&
                                    bm.levelObject.visualObject is TextObject textObject)
                                    textObject.Text = bm.text;
                            }
                        }
                        break;
                    }
            }
        }
    }
}
