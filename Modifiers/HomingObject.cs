using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using DG.Tweening;

using ObjectModifiers.Functions;

namespace ObjectModifiers.Modifiers
{
    public class HomingObject
    {
        //Homing Objects will act similarly to regular PA objects, however they will have a lot of different ways of functioning. Each keyframe has a homing toggle, homing type, follow type and player type.
        public HomingObject()
        {
        }

        public bool active;

        public string id;
        public string homingName;
        public int shape;
        public int shapeOption;

        public float startTime;
        public float totalDuration;

        public int depth;

        public int playerTarget;

        public bool deco;
        public bool collide;
        public bool multiple;

        public EditorData editorData = new EditorData();
        public List<List<HomingKeyframe>> events = new List<List<HomingKeyframe>>();
        public List<bool> followPlayer = new List<bool>();

        public HomingLogic homingLogic;
        public GameObject gameObject;

        //Move this outside of this class to the main ObjectModifiersPlugin class so I can better do the homing tracking.
        public void ICreateHomingObject()
        {
            active = true;
            GameObject bullet = Object.Instantiate(ObjectManager.inst.objectPrefabs[shape].options[shapeOption]);
            bullet.transform.SetParent(GameObject.Find("GameObjects").transform);
            bullet.transform.localScale = Vector3.one;
            var logic = bullet.AddComponent<HomingLogic>();
            logic.angleChangingSpeed = 200f;
            logic.movementSpeed = 20f;
            logic.target = InputDataManager.inst.players[0].player.transform.Find("Player");

            Debug.LogFormat("{0}Bullet is null: {1}", ObjectModifiersPlugin.className, bullet == null);

            float z = 0.1f * depth;
            float calc = events[0][0].eventValues[2] / 10f;
            z = z + calc;

            bullet.transform.GetChild(0).position = new Vector3(events[0][0].eventValues[0], events[0][0].eventValues[1], z);
            bullet.transform.GetChild(0).localScale = new Vector3(events[1][0].eventValues[0], events[1][0].eventValues[1], 1f);
            bullet.transform.GetChild(0).eulerAngles = new Vector3(0f, 0f, events[2][0].eventValues[0]);

            if (bullet.transform.GetChild(0).GetComponent<SelectObjectInEditor>())
            {
                Object.Destroy(bullet.transform.GetChild(0).GetComponent<SelectObjectInEditor>());
            }
            if (bullet.transform.GetChild(0).gameObject.GetComponentByName("RTObject"))
            {
                Object.Destroy(bullet.transform.GetChild(0).gameObject.GetComponentByName("RTObject"));
            }

            if (bullet.transform.GetChild(0).GetComponent<Collider2D>())
            {
                var collider = bullet.transform.GetChild(0).GetComponent<Collider2D>();
                collider.enabled = true;
                collider.isTrigger = !collide;
                if (deco)
                {
                    collider.tag = "Helper";
                }
            }

            float time = Time.time;
            var listTime = new List<float>();

            if (bullet.transform.GetChild(0).GetComponent<Renderer>())
            {
                Renderer bulletRenderer = bullet.transform.GetChild(0).GetComponent<Renderer>();
                bulletRenderer.enabled = true;
                bulletRenderer.material.color = new Color(events[3][0].eventValues[0], events[3][0].eventValues[1], events[3][0].eventValues[2], events[3][0].eventValues[3]);

                if (events[3].Count > 1)
                {
                    Color[] colorArray = new Color[events[3].Count];
                    float[] colorDuration = new float[events[3].Count];
                    int[] colorCurveType = new int[events[3].Count];

                    for (int i = 0; i < events[3].Count; i++)
                    {
                        colorArray[i] = new Color(events[3][i].eventValues[0], events[3][i].eventValues[1], events[3][i].eventValues[2], events[3][i].eventValues[3]);
                        colorDuration[i] = events[3][i].eventTime;
                        colorCurveType[i] = DataManager.inst.AnimationList.IndexOf(events[3][i].curveType);
                        listTime.Add(events[3][i].eventTime);
                    }
                    HomingObjectColor(bulletRenderer, colorArray, colorDuration, colorCurveType);
                }
            }

            if (events[0].Count > 1)
            {
                //Vector3[] posArray = new Vector3[events[0].Count];
                //float[] posDuration = new float[events[0].Count];
                //int[] posCurveType = new int[events[0].Count];
                //bool[] followArray = new bool[events[0].Count];

                //for (int i = 0; i < events[0].Count; i++)
                //{
                //    posArray[i] = new Vector3(events[0][i].eventValues[0], events[0][i].eventValues[1], events[0][i].eventValues[2]);
                //    posDuration[i] = events[0][i].eventTime;
                //    posCurveType[i] = DataManager.inst.AnimationList.IndexOf(events[0][i].curveType);
                //    listTime.Add(events[0][i].eventTime);
                //    followArray[i] = followPlayer[i];
                //}
                //HomingObjectPosition(bullet, playerTarget, followArray, posArray, posDuration, posCurveType);

                HomingObjectPosition(bullet, events[0]);
            }

            if (events[1].Count > 1)
            {
                Vector3[] scaArray = new Vector3[events[1].Count];
                float[] scaDuration = new float[events[1].Count];
                int[] scaCurveType = new int[events[1].Count];

                for (int i = 0; i < events[1].Count; i++)
                {
                    scaArray[i] = new Vector2(events[1][i].eventValues[0], events[1][i].eventValues[1]);
                    scaDuration[i] = events[1][i].eventTime;
                    scaCurveType[i] = DataManager.inst.AnimationList.IndexOf(events[1][i].curveType);
                    listTime.Add(events[1][i].eventTime);
                }
                HomingObjectScale(bullet, scaArray, scaDuration, scaCurveType);
            }

            if (events[2].Count > 1)
            {
                float[] rotArray = new float[events[2].Count];
                float[] rotDuration = new float[events[2].Count];
                int[] rotCurveType = new int[events[2].Count];

                for (int i = 0; i < events[2].Count; i++)
                {
                    rotArray[i] = events[2][i].eventValues[0];
                    rotDuration[i] = events[2][i].eventTime;
                    rotCurveType[i] = DataManager.inst.AnimationList.IndexOf(events[2][i].curveType);
                    listTime.Add(events[2][i].eventTime);
                }
                HomingObjectRotation(bullet, rotArray, rotDuration, rotCurveType);
            }

            listTime = (from x in listTime
                        orderby x
                        select x).ToList();

            totalDuration = listTime[listTime.Count - 1];

            Tweener tw = GameObject.Find("ObjectModifiers PluginThing").transform.DOScale(1f, totalDuration);

            tw.OnComplete(delegate ()
            {
                bullet.transform.DOKill();
                if (bullet.transform.GetChild(0).GetComponent<Renderer>())
                {
                    bullet.transform.GetChild(0).GetComponent<Renderer>().material.DOKill();
                }
                Object.Destroy(bullet);
            });
        }

        public void HomingObjectPosition(GameObject _bullet, int _pl, bool[] _tpl, Vector3[] _pos, float[] _posDT, int[] _posCT)
        {
            for (int i = 0; i < _pos.Length; i++)
            {
                if (!_tpl[i])
                {
                    var position = _bullet.transform.GetChild(0).DOLocalMove(_pos[i], _posDT[i]).SetEase(DataManager.inst.AnimationList[_posCT[i]].Animation);
                    position.Play();
                }
                else
                {
                    var position = _bullet.transform.GetChild(0).DOLocalMove(InputDataManager.inst.players[_pl].player.transform.Find("Player/Player").position, _posDT[i]).SetEase(DataManager.inst.AnimationList[_posCT[i]].Animation);
                    position.Play();
                }
            }
        }

        public void HomingObjectPosition(GameObject _bullet, List<HomingKeyframe> homingKeyframes)
        {
            for (int i = 0; i < homingKeyframes.Count; i++)
            {
                switch (homingKeyframes[i].homingType)
                {
                    case HomingKeyframe.HomingType.Static:
                        {
                            if (!homingKeyframes[i].targetPlayer)
                            {
                                var position = _bullet.transform.GetChild(0).DOLocalMove(new Vector3(homingKeyframes[i].eventValues[0], homingKeyframes[i].eventValues[1]), homingKeyframes[i].eventTime).SetEase(homingKeyframes[i].curveType.Animation);
                                position.Play();
                            }
                            else
                            {
                                if (!multiple)
                                {
                                    var position = _bullet.transform.GetChild(0).DOLocalMove(InputDataManager.inst.players[(int)homingKeyframes[i].eventValues[0]].player.transform.Find("Player").position, homingKeyframes[i].eventTime).SetEase(homingKeyframes[i].curveType.Animation);
                                    position.Play();
                                }
                            }
                            _bullet.GetComponent<HomingLogic>().followPos = false;
                            break;
                        }
                    case HomingKeyframe.HomingType.Dynamic:
                        {
                            _bullet.GetComponent<HomingLogic>().followPos = true;
                            break;
                        }
                }
            }
        }

        public void HomingObjectScale(GameObject _bullet, Vector3[] _sca, float[] _scaDT, int[] _scaCT)
        {
            for (int i = 0; i < _sca.Length; i++)
            {
                var scale = _bullet.transform.GetChild(0).DOScale(_sca[i], _scaDT[i]).SetEase(DataManager.inst.AnimationList[_scaCT[i]].Animation);
                scale.Play();
            }
        }

        public void HomingObjectRotation(GameObject _bullet, float[] _rot, float[] _rotDT, int[] _rotCT)
        {
            for (int i = 0; i < _rot.Length; i++)
            {
                var rotation = _bullet.transform.GetChild(0).DORotate(new Vector3(0f, 0f, _rot[i]), _rotDT[i], RotateMode.LocalAxisAdd).SetEase(DataManager.inst.AnimationList[_rotCT[i]].Animation);
                rotation.Play();
            }
        }

        public void HomingObjectColor(Renderer _bullet, Color[] _col, float[] _colDT, int[] _colCT)
        {
            for (int i = 0; i < _col.Length; i++)
            {
                var color = _bullet.material.DOColor(_col[i], _colDT[i]).SetEase(DataManager.inst.AnimationList[_colCT[i]].Animation);
                color.Play();
            }
        }

        public class EditorData
        {
            public int Bin
            {
                get
                {
                    return bin;
                }
                set
                {
                    bin = Mathf.Clamp(value, 0, 14);
                }
            }

            public int Layer
            {
                get
                {
                    return layer;
                }
                set
                {
                    layer = value;
                }
            }

            private int bin;

            private int layer;

            public bool locked;

            public bool collapse;
        }

        public class HomingKeyframe
        {
            public HomingKeyframe()
            {
            }

            public HomingType homingType;
            public MovementType movementType;
            public FollowType followType;
            public enum HomingType
            {
                Static,
                Dynamic
            }

            public enum MovementType
            {
                Follow,
                Flee
            }

            public enum FollowType
            {
                Nearest,
                Specific,
                All
            }

            public float eventTime;
            public float range;

            public bool targetPlayer;
            public float[] eventValues;
            public DataManager.LSAnimation curveType;
        }
    }
}
