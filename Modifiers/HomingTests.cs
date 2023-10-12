using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using LSFunctions;

namespace ObjectModifiers.Modifiers
{
    public class HomingTests
    {
        public static void PlayTest()
        {
            if (ObjectModifiersPlugin.homingObjects.Count < 1)
            {
                CreateNewHomingObject();
            }
            Play(ObjectModifiersPlugin.homingObjects[0]);
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
            Debug.LogFormat("{0}{1}", ObjectModifiersPlugin.className, homingObject.homingName + " | " + homingObject.id);
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

            Debug.LogFormat("{0}Homing State: {1}", ObjectModifiersPlugin.className, homingObject == null);

            ObjectModifiersPlugin.homingObjects.Add(homingObject);
            if (select)
            {
                ObjectModifiersPlugin.homingSelection = homingObject;
                ObjectModifiersPlugin.selectedHomingObjects.Add(homingObject);
            }

            return homingObject;
        }
    }
}
