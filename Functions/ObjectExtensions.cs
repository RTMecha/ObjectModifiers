﻿using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using RTFunctions.Functions.Data;
using RTFunctions.Functions.Optimization;
using RTFunctions.Functions.Optimization.Objects;

using BaseBeatmapObject = DataManager.GameData.BeatmapObject;

namespace ObjectModifiers.Functions
{
    public static class ObjectExtensions
    {
        // Optimize these when RTPlayer has replaced the original component.

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

        public static bool IsTouchingPlayer(this BaseBeatmapObject beatmapObject)
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

        public static void SetTransform(this BeatmapObject beatmapObject, int toType, int toAxis, float value)
        {
            switch (toType)
            {
                case 0:
                    {
                        if (toAxis == 0)
                            beatmapObject.positionOffset.x = value;
                        if (toAxis == 1)
                            beatmapObject.positionOffset.y = value;
                        if (toAxis == 2)
                            beatmapObject.positionOffset.z = value;

                        break;
                    }
                case 1:
                    {
                        if (toAxis == 0)
                            beatmapObject.scaleOffset.x = value;
                        if (toAxis == 1)
                            beatmapObject.scaleOffset.y = value;
                        if (toAxis == 2)
                            beatmapObject.scaleOffset.z = value;

                        break;
                    }
                case 2:
                    {
                        if (toAxis == 0)
                            beatmapObject.rotationOffset.x = value;
                        if (toAxis == 1)
                            beatmapObject.rotationOffset.y = value;
                        if (toAxis == 2)
                            beatmapObject.rotationOffset.z = value;

                        break;
                    }
            }
        }
    }
}
