using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace ObjectModifiers.Functions.Components
{
    /// <summary>
    /// Figure out a more optimized way of doing this. (or remove it, idk)
    /// </summary>
    public class LegacyTracker : MonoBehaviour
    {
        public TailUpdateMode updateMode = TailUpdateMode.FixedUpdate;
        public enum TailUpdateMode
        {
            Update,
            FixedUpdate,
            LateUpdate
        }

        public Dictionary<string, RTObject> originals = new Dictionary<string, RTObject>();
        public List<Dictionary<string, RTObject>> duplicates = new List<Dictionary<string, RTObject>>();

        public float distance = 2f;
        public int tailCount = 3;
        public float transformSpeed = 200f;
        public float distanceSpeed = 12f;

        void Awake()
        {

        }

        public void Setup()
        {
            var leader = originals.ElementAt(0).Value.gameObject.GetComponentInChildren<Renderer>().transform;
            path.Add(new MovementPath(Vector3.zero, Quaternion.identity, leader));

            while (duplicates.Count < tailCount)
            {
                var parent = new GameObject("legacy tail");
                parent.transform.SetParent(ObjectManager.inst.objectParent.transform);
                parent.transform.localScale = Vector3.one;

                var duplicate = new Dictionary<string, RTObject>();

                foreach (var og in originals)
                {
                    if (!duplicate.ContainsKey(og.Key) && og.Value.gameObject != null)
                    {
                        var gm = Instantiate(og.Value.gameObject);
                        gm.transform.SetParent(parent.transform);
                        gm.transform.localScale = og.Value.gameObject.transform.localScale;
                        Destroy(gm.GetComponentInChildren<RTFunctions.Functions.Components.RTObject>());

                        var rt = new RTObject(gm);
                        rt.values.Add("Renderer", gm.GetComponentInChildren<Renderer>());

                        duplicate.Add(og.Key, rt);
                    }
                }

                duplicates.Add(duplicate);

                path.Add(new MovementPath(leader.position, leader.rotation, parent.transform));
            }
        }

        void Update()
        {
            if (updateMode == TailUpdateMode.Update)
                UpdateTailDistance();

            foreach (var og in originals)
            {
                for (int i = 0; i < duplicates.Count; i++)
                {
                    if (duplicates[i].ContainsKey(og.Key))
                    {
                        if (duplicates[i][og.Key].values.ContainsKey("Renderer") && duplicates[i][og.Key].values["Renderer"] != null && og.Value.values.ContainsKey("Renderer") && og.Value.values["Renderer"] != null)
                        {
                            ((Renderer)duplicates[i][og.Key].values["Renderer"]).material.color = ((Renderer)og.Value.values["Renderer"]).material.color;
                        }
                    }
                }
            }
        }

        void FixedUpdate()
        {
            if (updateMode == TailUpdateMode.FixedUpdate)
                UpdateTailDistance();
        }

        void LateUpdate()
        {
            if (updateMode == TailUpdateMode.LateUpdate)
                UpdateTailDistance();
            UpdateTailTransform();
        }

        void UpdateTailDistance()
        {
            if (originals == null || originals.Count < 1)
                return;

            var leader = path[0].transform;
            path[0].pos = leader.position;
            path[0].rot = leader.rotation;

            for (int i = 1; i < path.Count; i++)
            {
                int num = i - 1;

                if (Vector3.Distance(path[i].pos, path[num].pos) > distance)
                {
                    Vector3 pos = Vector3.Lerp(path[i].pos, path[num].pos, Time.deltaTime * distanceSpeed);
                    Quaternion rot = Quaternion.Lerp(path[i].rot, path[num].rot, Time.deltaTime * distanceSpeed);

                    path[i].pos = pos;
                    path[i].rot = rot;
                }
            }
        }

        void UpdateTailTransform()
        {
            if (GameManager.inst == null || GameManager.inst.gameState != GameManager.State.Paused)
            {
                float num = Time.deltaTime * transformSpeed;
                for (int i = 1; i < path.Count; i++)
                {
                    if (path.Count >= i && path[i].transform != null && path[i].transform.gameObject.activeSelf)
                    {
                        num *= Vector3.Distance(path[i].lastPos, path[i].pos);
                        path[i].transform.position = Vector3.MoveTowards(path[i].lastPos, path[i].pos, num);
                        path[i].lastPos = path[i].transform.position;
                        path[i].transform.rotation = path[i].rot;
                    }
                }
            }
        }

        public class RTObject
        {
            public RTObject(GameObject gm)
            {
                gameObject = gm;
                values = new Dictionary<string, object>();
            }

            public RTObject(string _name, GameObject _gm)
            {
                name = _name;
                gameObject = _gm;
                values = new Dictionary<string, object>();

                values.Add("Position", Vector3.zero);
                values.Add("Scale", Vector3.one);
                values.Add("Rotation", 0f);
                values.Add("Color", 0);
            }

            public RTObject(string _name, Dictionary<string, object> _values, GameObject _gm)
            {
                name = _name;
                values = _values;
                gameObject = _gm;
            }

            public string name;
            public GameObject gameObject;

            public Dictionary<string, object> values;
        }

        public List<MovementPath> path = new List<MovementPath>();

        public class MovementPath
        {
            public MovementPath(Vector3 _pos, Quaternion _rot, Transform _tf)
            {
                pos = _pos;
                rot = _rot;
                transform = _tf;
            }

            public MovementPath(Vector3 _pos, Quaternion _rot, Transform _tf, bool active)
            {
                pos = _pos;
                rot = _rot;
                transform = _tf;
                this.active = active;
            }

            public bool active = true;

            public Vector3 lastPos;
            public Vector3 pos;

            public Quaternion rot;

            public Transform transform;
        }
    }
}
