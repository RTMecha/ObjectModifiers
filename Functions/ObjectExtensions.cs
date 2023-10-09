using System;
using System.Collections.Generic;
using System.Linq;

using ObjectModifiers.Modifiers;

using UnityEngine;

using RTFunctions.Functions.Optimization;
using RTFunctions.Functions.Optimization.Objects;

using Object = UnityEngine.Object;

using BeatmapObject = DataManager.GameData.BeatmapObject;
using AKT = DataManager.GameData.BeatmapObject.AutoKillType;
using Prefab = DataManager.GameData.Prefab;

namespace ObjectModifiers.Functions
{
    public static class ObjectExtensions
    {

        public static AnimationObject ToAnimationObject(this Prefab prefab)
        {
            var list = new List<AnimationObject.BeatmapObject>();
            foreach (var beatmapObject in prefab.objects)
            {
                list.Add(AnimationObject.BeatmapObject.DeepCopy(beatmapObject));
            }
            return new AnimationObject(list);
        }

        public static void DestroyTimelineObject(this ObjEditor _inst, string id)
        {
            if (_inst != null)
            {
                if (_inst.beatmapObjects.ContainsKey(id))
                    Object.Destroy(_inst.beatmapObjects[id]);
                if (_inst.prefabObjects.ContainsKey(id))
                    Object.Destroy(_inst.prefabObjects[id]);
            }
        }

        public static void Try<T>(this List<T> list, int index, Action action)
        {
            if (index > 0 && index < list.Count)
            {
                action();
            }
        }

        public static int ClosestPlayer(GameObject _gm)
        {
            if (InputDataManager.inst.players.Count > 0)
            {
                float distance = float.MaxValue;
                for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                {
                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                    {
                        var player = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1));

                        if (Vector2.Distance(player.Find("Player").position, _gm.transform.position) < distance)
                        {
                            distance = Vector2.Distance(player.Find("Player").position, _gm.transform.position);
                        }
                    }
                }
                for (int i = 0; i < InputDataManager.inst.players.Count; i++)
                {
                    if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                    {
                        var player = GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1));

                        if (Vector2.Distance(player.Find("Player").position, _gm.transform.position) < distance)
                        {
                            return i;
                        }
                    }
                }
            }

            return 0;
        }

        public static bool IsTouchingPlayer(this BeatmapObject beatmapObject)
        {
            var list = new List<bool>();

            if (Updater.TryGetObject(beatmapObject, out LevelObject levelObject) && levelObject.visualObject.Collider)
            {
                if (levelObject.visualObject.Collider)
                {
                    var collider = levelObject.visualObject.Collider;

                    for (int i = 0; i < GameManager.inst.players.transform.childCount; i++)
                    {
                        if (GameManager.inst.players.transform.Find(string.Format("Player {0}", i + 1)))
                        {
                            var player = GameManager.inst.players.transform.Find(string.Format("Player {0}/Player", i + 1));
                            list.Add(player.GetComponent<Collider2D>().IsTouching(collider));
                        }
                    }
                }
            }

            return list.Any(x => x == true);
        }

        public static void AddTrigger(this BeatmapObject _beatmapObject, int index)
        {
            if (_beatmapObject.GetModifierObject() != null)
            {
                var copy = ModifierObject.Modifier.DeepCopy(ObjectModifiersPlugin.modifierTypes[index]);
                copy.modifierObject = _beatmapObject;
                _beatmapObject.GetModifierObject().modifiers.Add(copy);

                Debug.LogFormat("{0}Added trigger: {1} {2} {3} {4}", ObjectModifiersPlugin.className, _beatmapObject.id, copy.type, copy.command[0], copy.value);
            }
        }

        //ObjEditor.inst.currentObjectSelection.GetObjectData().SetTriggerValue(0, "0.5");
        public static void SetTriggerValue(this BeatmapObject _beatmapObject, int modifier, string _text)
        {
            if (_beatmapObject.GetModifierObject() != null)
            {
                _beatmapObject.GetModifierObject().modifiers[modifier].value = _text;
                Debug.LogFormat("{0}Set trigger: {1} {2}", ObjectModifiersPlugin.className, _beatmapObject.id, _text);
            }
        }

        //ObjEditor.inst.currentObjectSelection.GetObjectData().SetTrigger(0, 0);
        public static void SetTrigger(this BeatmapObject _beatmapObject, int modifier, int index)
        {
            if (_beatmapObject.GetModifierObject() != null)
            {
                var copy = ModifierObject.Modifier.DeepCopy(ObjectModifiersPlugin.modifierTypes[index]);
                copy.modifierObject = _beatmapObject;

                _beatmapObject.GetModifierObject().modifiers[modifier] = copy;
                Debug.LogFormat("{0}Set trigger data to {1} : {2} {3} {4} {5}", ObjectModifiersPlugin.className, modifier, _beatmapObject.id, copy.type, copy.command[0], copy.value);
            }
        }

        public static void RemoveModifier(this BeatmapObject _beatmapObject, int modifier)
        {
            if (_beatmapObject.GetModifierObject() != null)
            {
                _beatmapObject.GetModifierObject().modifiers.RemoveAt(modifier);
                Debug.LogFormat("{0}Removed modifier at {1}", ObjectModifiersPlugin.className, modifier);
            }
        }

        public static ModifierObject AddModifierObject(this BeatmapObject _beatmapObject)
        {
            var p = new ModifierObject(_beatmapObject, new List<ModifierObject.Modifier> { });

            if (!ObjectModifiersPlugin.modifierObjects.ContainsKey(_beatmapObject.id))
                ObjectModifiersPlugin.modifierObjects.Add(_beatmapObject.id, p);

            return p;
        }

        public static ModifierObject AddModifierPrefab(this Prefab prefab, BeatmapObject beatmapObject)
        {
            var p = new ModifierObject(beatmapObject, new List<ModifierObject.Modifier> { });

            if (!ObjectModifiersPlugin.prefabModifiers.ContainsKey(prefab.ID))
                ObjectModifiersPlugin.prefabModifiers.Add(prefab.ID, new Dictionary<string, ModifierObject>());

            if (!ObjectModifiersPlugin.prefabModifiers[prefab.ID].ContainsKey(beatmapObject.id))
                ObjectModifiersPlugin.prefabModifiers[prefab.ID].Add(beatmapObject.id, p);

            return p;
        }

        public static ModifierObject GetModifierObject(this BeatmapObject _beatmapObject, bool fromPrefab = false)
        {
            if (ObjectModifiersPlugin.modifierObjects.ContainsKey(_beatmapObject.id) && !fromPrefab)
            {
                return ObjectModifiersPlugin.modifierObjects[_beatmapObject.id];
            }

            if (ObjectModifiersPlugin.prefabModifiers.ContainsKey(_beatmapObject.prefabID) && ObjectModifiersPlugin.prefabModifiers[_beatmapObject.prefabID].ContainsKey(_beatmapObject.id))
                return ObjectModifiersPlugin.prefabModifiers[_beatmapObject.prefabID][_beatmapObject.id];

            return null;
        }

        public static void RemoveModifierObject(this BeatmapObject _beatmapObject, bool fromPrefab = false)
        {
            if (ObjectModifiersPlugin.modifierObjects.ContainsKey(_beatmapObject.id) && !fromPrefab)
            {
                ObjectModifiersPlugin.modifierObjects.Remove(_beatmapObject.id);
                Debug.LogFormat("{0}Removed ModifierObject at {1}", ObjectModifiersPlugin.className, _beatmapObject.id);
            }

            if (ObjectModifiersPlugin.prefabModifiers.ContainsKey(_beatmapObject.prefabID) && ObjectModifiersPlugin.prefabModifiers[_beatmapObject.prefabID].ContainsKey(_beatmapObject.id))
            {
                ObjectModifiersPlugin.prefabModifiers[_beatmapObject.prefabID].Remove(_beatmapObject.id);
                Debug.LogFormat("{0}Removed ModifierObject at Prefab: {1} - BeatmapObject: {2}", ObjectModifiersPlugin.className, _beatmapObject.prefabID, _beatmapObject.id);
            }
        }

        public static void RemoveModifierPrefab(this Prefab prefab)
        {
            if (ObjectModifiersPlugin.prefabModifiers.ContainsKey(prefab.ID))
            {
                ObjectModifiersPlugin.prefabModifiers.Remove(prefab.ID);
                Debug.LogFormat("{0}Removed ModifierObject at Prefab: {1}", ObjectModifiersPlugin.className, prefab.ID);
            }
        }

        public static float[] ToArray(this Vector2 _vector2)
        {
            float[] array = new float[2]
            {
                _vector2.x,
                _vector2.y
            };
            return array;
        }
        public static float[] ToArray(this Vector3 _vector3)
        {
            float[] array = new float[3]
            {
                _vector3.x,
                _vector3.y,
                _vector3.z
            };
            return array;
        }
        public static float[] ToArray(this Quaternion _quaternion)
        {
            float[] array = new float[4]
            {
                _quaternion.x,
                _quaternion.y,
                _quaternion.z,
                _quaternion.w
            };
            return array;
        }

        public static bool CheckList(this List<List<BeatmapObject>> beatmapObjects, BeatmapObject _beatmapObject)
        {
            foreach (var list in beatmapObjects)
            {
                if (list.Contains(_beatmapObject))
                {
                    return true;
                }
            }
            return false;
        }

        public static List<BeatmapObject> GetChildren(this BeatmapObject _beatmapObject)
        {
            var dictionary = new Dictionary<string, BeatmapObject>();

            var select = _beatmapObject;

            var findAll = DataManager.inst.gameData.beatmapObjects.FindAll(x => x.parent == select.id);

            while (findAll.Count > 0)
            {
                foreach (var bm in findAll)
                {
                    if (!dictionary.ContainsKey(bm.id))
                        dictionary.Add(bm.id, bm);

                    findAll.AddRange(DataManager.inst.gameData.beatmapObjects.FindAll(x => x.parent == bm.id));
                }
            }

            return dictionary.ValuesToList();
        }

        public static List<TValue> ValuesToList<TKey, TValue>(this Dictionary<TKey, TValue> keyValuePairs)
        {
            var list = new List<TValue>();
            foreach (var value in keyValuePairs)
                list.Add(value.Value);

            return list;
        }
        
        public static List<TKey> KeysToList<TKey, TValue>(this Dictionary<TKey, TValue> keyValuePairs)
        {
            var list = new List<TKey>();
            foreach (var value in keyValuePairs)
                list.Add(value.Key);

            return list;
        }

        public static float EventValuesZ1(DataManager.GameData.EventKeyframe _posEvent, int _beatmapObject)
        {
            BeatmapObject bo = null;
            if (DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent)) != null)
            {
                bo = DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent));
            }
            float z = 0.0005f * bo.Depth;
            if (_posEvent.eventValues.Length > 2 && bo != null)
            {
                float calc = _posEvent.eventValues[2] / 10f;
                z = z + calc;
            }
            return z;
        }

        public static float EventValuesZ2(DataManager.GameData.EventKeyframe _posEvent, int _beatmapObject)
        {
            BeatmapObject bo = null;
            if (DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent)) != null)
            {
                bo = DataManager.inst.gameData.beatmapObjects.Find((BeatmapObject x) => x.events[0].Contains(_posEvent));
            }
            float z = 0.1f * bo.Depth;
            if (_posEvent.eventValues.Length > 2 && bo != null)
            {
                float calc = _posEvent.eventValues[2] / 10f;
                z = z + calc;
            }
            return z;
        }
    }
}
