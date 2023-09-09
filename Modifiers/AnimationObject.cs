using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using LSFunctions;

using RTFunctions.Functions.Animation;
using RTFunctions.Functions.Animation.Keyframe;

using EventKeyframe = DataManager.GameData.EventKeyframe;
using OGObject = DataManager.GameData.BeatmapObject;
using SequenceManager = RTFunctions.Functions.Animation.SequenceManager;

namespace ObjectModifiers.Modifiers
{
    public class AnimationObject
    {
		public AnimationObject(List<BeatmapObject> beatmapObjects)
        {
			this.beatmapObjects = beatmapObjects;

			foreach (var beatmapObject in this.beatmapObjects)
            {
				beatmapObject.instance = this;
				CachedSequences collection = new CachedSequences();
				if (beatmapObject.events[0][0].eventValues.Length > 2)
				{
					collection = new CachedSequences()
					{
						Position3DSequence = beatmapObject.GetVector3Sequence(beatmapObject.events[0], new Vector3Keyframe(0.0f, Vector3.zero, Ease.Linear)),
						ScaleSequence = beatmapObject.GetVector2Sequence(beatmapObject.events[1], new Vector2Keyframe(0.0f, Vector2.one, Ease.Linear)),
						RotationSequence = beatmapObject.GetFloatSequence(beatmapObject.events[2], new FloatKeyframe(0.0f, 0.0f, Ease.Linear), true)
					};
				}
				else
				{
					collection = new CachedSequences()
					{
						PositionSequence = beatmapObject.GetVector2Sequence(beatmapObject.events[0], new Vector2Keyframe(0.0f, Vector2.zero, Ease.Linear)),
						ScaleSequence = beatmapObject.GetVector2Sequence(beatmapObject.events[1], new Vector2Keyframe(0.0f, Vector2.one, Ease.Linear)),
						RotationSequence = beatmapObject.GetFloatSequence(beatmapObject.events[2], new FloatKeyframe(0.0f, 0.0f, Ease.Linear), true)
					};
				}

				// Empty objects don't need a color sequence, so it is not cached
				if (beatmapObject.objectType != OGObject.ObjectType.Empty)
				{
					if (beatmapObject.events[3][0].eventValues.Length > 2)
					{
						collection.ColorSequence = beatmapObject.GetColorSequence(beatmapObject.events[3], new ThemeKeyframe(0.0f, 0, Ease.Linear));
						collection.OpacitySequence = beatmapObject.GetOpacitySequence(beatmapObject.events[3], 1, new FloatKeyframe(0.0f, 0, Ease.Linear));
						collection.HueSequence = beatmapObject.GetOpacitySequence(beatmapObject.events[3], 2, new FloatKeyframe(0.0f, 0, Ease.Linear));
						collection.SaturationSequence = beatmapObject.GetOpacitySequence(beatmapObject.events[3], 3, new FloatKeyframe(0.0f, 0, Ease.Linear));
						collection.ValueSequence = beatmapObject.GetOpacitySequence(beatmapObject.events[3], 4, new FloatKeyframe(0.0f, 0, Ease.Linear));
					}
					else if (beatmapObject.events[3][0].eventValues.Length > 1)
					{
						collection.ColorSequence = beatmapObject.GetColorSequence(beatmapObject.events[3], new ThemeKeyframe(0.0f, 0, Ease.Linear));
						collection.OpacitySequence = beatmapObject.GetOpacitySequence(beatmapObject.events[3], 1, new FloatKeyframe(0.0f, 0, Ease.Linear));
					}
					else
					{
						collection.ColorSequence = beatmapObject.GetColorSequence(beatmapObject.events[3], new ThemeKeyframe(0.0f, 0, Ease.Linear));
					}
				}

				cachedSequences.Add(beatmapObject.id, collection);

				if (!beatmapObjectsDictionary.ContainsKey(beatmapObject.id))
					beatmapObjectsDictionary.Add(beatmapObject.id, beatmapObject);
			}

			ObjectModifiersPlugin.animationObjects.Add(this);
        }

		public static AnimationObject Init(List<BeatmapObject> beatmapObjects)
        {
			return new AnimationObject(beatmapObjects);
        }

		public void Update()
		{
			time += add * speed;

			for (int i = 0; i < beatmapObjects.Count; i++)
			{
				var beatmapObject = beatmapObjects[i];
				if (beatmapObject.StartTime > time && !beatmapObject.active && beatmapObject.objectType != OGObject.ObjectType.Empty)
					beatmapObject.Spawn();

				var depth = beatmapObject.Depth;
				{
					// Update parents
					float positionOffset = 0.0f;
					float scaleOffset = 0.0f;
					float rotationOffset = 0.0f;

					bool animatePosition = true;
					bool animateScale = true;
					bool animateRotation = true;

					foreach (var parentObject in beatmapObject.parentObjects)
					{
						// If last parent is position parented, animate position
						if (animatePosition)
						{
							if (parentObject.Position3DSequence != null)
							{
								Vector3 value = parentObject.Position3DSequence.Interpolate(time - parentObject.TimeOffset - positionOffset);
								float z = depth * 0.0005f;
								float calc = value.z / 10f;
								z = z + calc;
								parentObject.Transform.localPosition = new Vector3(value.x, value.y, z);
							}
							else
							{
								Vector2 value = parentObject.PositionSequence.Interpolate(time - parentObject.TimeOffset - positionOffset);
								parentObject.Transform.localPosition = new Vector3(value.x, value.y, depth * 0.0005f);
							}
						}

						// If last parent is scale parented, animate scale
						if (animateScale)
						{
							Vector2 value = parentObject.ScaleSequence.Interpolate(time - parentObject.TimeOffset - scaleOffset);
							parentObject.Transform.localScale = new Vector3(value.x, value.y, 1.0f);
						}

						// If last parent is rotation parented, animate rotation
						if (animateRotation)
						{
							parentObject.Transform.localRotation = Quaternion.AngleAxis(
								parentObject.RotationSequence.Interpolate(time - parentObject.TimeOffset - rotationOffset),
								Vector3.forward);
						}

						// Cache parent values to use for next parent
						positionOffset = parentObject.ParentOffsetPosition;
						scaleOffset = parentObject.ParentOffsetScale;
						rotationOffset = parentObject.ParentOffsetRotation;

						animatePosition = parentObject.ParentAnimatePosition;
						animateScale = parentObject.ParentAnimateScale;
						animateRotation = parentObject.ParentAnimateRotation;
					}
				}

				if (beatmapObject.autoKillType != OGObject.AutoKillType.OldStyleNoAutokill && (beatmapObject.autoKillType == OGObject.AutoKillType.SongTime && beatmapObject.autoKillOffset > time || beatmapObject.autoKillType == OGObject.AutoKillType.LastKeyframe && beatmapObject.StartTime + beatmapObject.GetLongestSequence() > time || beatmapObject.autoKillType == OGObject.AutoKillType.LastKeyframeOffset && beatmapObject.StartTime + beatmapObject.GetLongestSequence() + beatmapObject.autoKillOffset > time || beatmapObject.autoKillType == OGObject.AutoKillType.FixedTime && beatmapObject.StartTime + beatmapObject.autoKillOffset > time))
                {
					UnityEngine.Object.Destroy(beatmapObject.unityData.gameObject);
					beatmapObjects.RemoveAt(i);
                }
			}

			for (int i = 0; i < localPositionSequence.Count; i++)
			{
				var sequence = localPositionSequence[i];
				if (sequence.length < time)
				{
					if (sequence.onComplete != null)
						sequence.onComplete();

					localPositionSequence.RemoveAt(i);

					sequence = null;
				}
				else
				{
					sequence.currentTime = time;
					if (sequence.instance != null)
						sequence.instance.localPosition = sequence.sequence.Interpolate(time);
					else if (sequence.action != null)
						sequence.action(time);
				}
			}

			for (int i = 0; i < localScaleSequence.Count; i++)
			{
				var sequence = localScaleSequence[i];
				if (sequence.length < time)
				{
					if (sequence.onComplete != null)
						sequence.onComplete();

					localScaleSequence.RemoveAt(i);

					sequence = null;
				}
				else
				{
					sequence.currentTime = time;
					if (sequence.instance != null)
						sequence.instance.localScale = sequence.sequence.Interpolate(time);
					else if (sequence.action != null)
						sequence.action(time);
				}
			}

			for (int i = 0; i < localRotationSequence.Count; i++)
			{
				var sequence = localRotationSequence[i];
				if (sequence.length < time)
				{
					if (sequence.onComplete != null)
						sequence.onComplete();

					localRotationSequence.RemoveAt(i);

					sequence = null;
				}
				else
				{
					sequence.currentTime = time;
					if (sequence.instance != null)
						sequence.instance.localRotation = Quaternion.Euler(sequence.sequence.Interpolate(time));
					else if (sequence.action != null)
						sequence.action(time);
				}
			}

			for (int i = 0; i < materialColorSequence.Count; i++)
			{
				var sequence = materialColorSequence[i];
				if (sequence.length < time)
				{
					if (sequence.onComplete != null)
						sequence.onComplete();

					materialColorSequence.RemoveAt(i);

					sequence = null;
				}
				else
				{
					sequence.currentTime = time;
					if (sequence.instance != null)
						sequence.instance.color = sequence.sequence.Interpolate(time);
					else if (sequence.action != null)
						sequence.action(time);
				}
			}
		}

		public float speed = 10f;
		public float add = 0.001f;
		public float time = 0f;

		public Dictionary<string, CachedSequences> cachedSequences = new Dictionary<string, CachedSequences>();
		public List<BeatmapObject> beatmapObjects = new List<BeatmapObject>();
		public Dictionary<string, BeatmapObject> beatmapObjectsDictionary = new Dictionary<string, BeatmapObject>();

		public void ResetTime() => time = 0f;

		public void SetSpeeds(float add = 0.001f, float speed = 10f)
		{
			this.add = add;
			this.speed = speed;
		}

		public void AnimateLocalPosition(Transform tf, Sequence<Vector3> sequence, float length = 0f, Action<float> action = null, Action onComplete = null)
		{
			var t = length;
			if (t == 0f)
			{
				foreach (var kf in sequence.keyframes)
					t += kf.Time;
			}

			var seq = new SequenceManager.SequenceObject<Vector3, Transform>(sequence, tf, t, action, onComplete);
			localPositionSequence.Add(seq);
		}

		public void AnimateLocalScale(Transform tf, Sequence<Vector3> sequence, float length = 0f, Action<float> action = null, Action onComplete = null)
		{
			var t = length;
			if (t == 0f)
			{
				foreach (var kf in sequence.keyframes)
					t += kf.Time;
			}

			var seq = new SequenceManager.SequenceObject<Vector3, Transform>(sequence, tf, t, action, onComplete);
			localScaleSequence.Add(seq);
		}

		public void AnimateLocalRotation(Transform tf, Sequence<Vector3> sequence, float length = 0f, Action<float> action = null, Action onComplete = null)
		{
			var t = length;
			if (t == 0f)
			{
				foreach (var kf in sequence.keyframes)
					t += kf.Time;
			}

			var seq = new SequenceManager.SequenceObject<Vector3, Transform>(sequence, tf, t, action, onComplete);
			localRotationSequence.Add(seq);
		}

		public void AnimateColor(Material mat, Sequence<Color> sequence, float length = 0f, Action<float> action = null, Action onComplete = null)
		{
			var t = length;
			if (t == 0f)
			{
				foreach (var kf in sequence.keyframes)
					t += kf.Time;
			}

			var seq = new SequenceManager.SequenceObject<Color, Material>(sequence, mat, t, action, onComplete);
			materialColorSequence.Add(seq);
		}

		public List<SequenceManager.SequenceObject<Vector3, Transform>> localPositionSequence = new List<SequenceManager.SequenceObject<Vector3, Transform>>();
		public List<SequenceManager.SequenceObject<Vector3, Transform>> localScaleSequence = new List<SequenceManager.SequenceObject<Vector3, Transform>>();
		public List<SequenceManager.SequenceObject<Vector3, Transform>> localRotationSequence = new List<SequenceManager.SequenceObject<Vector3, Transform>>();

		public List<SequenceManager.SequenceObject<Color, Material>> materialColorSequence = new List<SequenceManager.SequenceObject<Color, Material>>();

		public class CachedSequences
		{
			public Sequence<Vector2> PositionSequence { get; set; }
			public Sequence<Vector3> Position3DSequence { get; set; }
			public Sequence<Vector2> ScaleSequence { get; set; }
			public Sequence<float> RotationSequence { get; set; }
			public Sequence<Color> ColorSequence { get; set; }

			public Sequence<float> OpacitySequence { get; set; }
			public Sequence<float> HueSequence { get; set; }
			public Sequence<float> SaturationSequence { get; set; }
			public Sequence<float> ValueSequence { get; set; }
		}

		public class BeatmapObject
        {
			public AnimationObject instance;

			public void Spawn()
			{
				active = true;

				GameObject parent = null;

				if (!string.IsNullOrEmpty(this.parent) && instance != null && instance.beatmapObjectsDictionary.ContainsKey(this.parent))
				{
					parent = InitParentChain(instance.beatmapObjectsDictionary[this.parent], parentObjects);
				}

				var shape = Mathf.Clamp(this.shape, 0, ObjectManager.inst.objectPrefabs.Count - 1);
				var shapeOption = Mathf.Clamp(this.shapeOption, 0, ObjectManager.inst.objectPrefabs[shape].options.Count - 1);

				var baseObject = UnityEngine.Object.Instantiate(ObjectManager.inst.objectPrefabs[shape].options[shapeOption], parent == null ? null : parent.transform);

				var p = InitLevelParentObject(this, baseObject);
				if (parentObjects.Count > 0)
					parentObjects.Insert(0, p);
				else
					parentObjects.Add(p);

				var top = new GameObject("top");
				top.transform.SetParent(ObjectManager.inst.objectParent.transform);

				unityData.gameObject = top;

				//if (parentObjects.Count > 0)
				//	baseObject.transform.SetParent(parentObjects[0].Transform);

				if (parentObjects.Count > 0)
					parentObjects[parentObjects.Count - 1].Transform.SetParent(top.transform);
				else
					baseObject.transform.SetParent(top.transform);

				//baseObject.transform.SetParent(top.transform);

				GameObject visualObject = baseObject.transform.GetChild(0).gameObject;
				visualObject.transform.localPosition = new Vector3(origin.x, origin.y, Depth * 0.1f);
                visualObject.name = "Visual [ " + id + " ]";

				baseObject.SetActive(true);
				visualObject.SetActive(true);

				visualObject.GetComponent<Renderer>().enabled = true;

				visualObject.GetComponent<Renderer>().material.color = new Color(1f, 1f, 1f, 1f);

				//foreach (var levelParent in parentObjects)
				//{
				//	instance.AnimateLocalPosition(levelParent.Transform, levelParent.Position3DSequence, onComplete: delegate ()
				//	{
				//		Debug.LogFormat("{0}Finished animating position for {1}", ObjectModifiersPlugin.className, id);
				//	});

				//	instance.AnimateLocalScale(levelParent.Transform, levelParent.ScaleSequence.ToVector3Sequence(), onComplete: delegate ()
				//	{
				//		Debug.LogFormat("{0}Finished animating scale for {1}", ObjectModifiersPlugin.className, id);
				//	});

				//	instance.AnimateLocalRotation(levelParent.Transform, levelParent.RotationSequence.ToVector3Sequence(SequenceExtensions.Axis.Z), onComplete: delegate ()
				//	{
				//		Debug.LogFormat("{0}Finished animating rotation for {1}", ObjectModifiersPlugin.className, id);
				//	});
    //            }

				instance.AnimateColor(visualObject.GetComponent<Renderer>().material, events[3].ToColorSequence(), onComplete: delegate ()
				{
					Debug.LogFormat("{0}Finished animating color for {1}", ObjectModifiersPlugin.className, id);
				});
			}

			public string GetParentType()
			{
				return parentType;
			}

			public bool GetParentType(int _index)
			{
				return parentType[_index] == '1';
			}

			public void SetParentType(int _index, bool _new)
			{
				StringBuilder stringBuilder = new StringBuilder(parentType);
				stringBuilder[_index] = (_new ? '1' : '0');
				parentType = stringBuilder.ToString();
			}

			public List<float> getParentOffsets()
			{
				return parentOffsets;
			}

			public float getParentOffset(int _index)
			{
				if (_index >= 0 && _index < parentOffsets.Count())
				{
					return parentOffsets[_index];
				}
				return 0f;
			}

			public void SetParentOffset(int _index, float _new)
			{
				if (_index >= 0 && _index <= 2)
				{
					parentOffsets[_index] = _new;
				}
			}

			public int Depth
			{
				get
				{
					return depth;
				}
				set
				{
					depth = value;
				}
			}

			public float StartTime
			{
				get
				{
					return startTime;
				}
				set
				{
					startTime = value;
				}
			}

			public static BeatmapObject DeepCopy(BeatmapObject orig, bool _newID = true)
			{
				if (orig != null)
				{
					if (orig.editorData == null)
					{
						orig.editorData = new EditorData();
					}
					if (orig.events == null)
					{
						orig.events = new List<List<EventKeyframe>>
						{
							new List<EventKeyframe>(),
							new List<EventKeyframe>(),
							new List<EventKeyframe>(),
							new List<EventKeyframe>()
						};
					}
					var beatmapObject = new BeatmapObject
					{
						id = (_newID ? LSText.randomString(16) : orig.id),
						prefabID = orig.prefabID,
						prefabInstanceID = orig.prefabInstanceID,
						active = orig.active,
						parent = orig.parent,
						parentType = orig.parentType,
						parentOffsets = new List<float>(orig.parentOffsets),
						depth = orig.depth,
						objectType = orig.objectType,
						StartTime = orig.StartTime,
						name = orig.name,
						origin = orig.origin,
						autoKillType = orig.autoKillType,
						autoKillOffset = orig.autoKillOffset,
						shape = orig.shape,
						shapeOption = orig.shapeOption,
						text = orig.text,
						editorData = new EditorData
						{
							collapse = orig.editorData.collapse,
							locked = orig.editorData.locked,
							Bin = orig.editorData.Bin,
							Layer = orig.editorData.Layer
						}
					};
					int num = 0;
					foreach (List<EventKeyframe> list in orig.events)
					{
						foreach (EventKeyframe eventKeyframe in list)
						{
                            EventKeyframe item = new EventKeyframe
                            {
								active = eventKeyframe.active,
								eventTime = eventKeyframe.eventTime,
								random = eventKeyframe.random,
								curveType = eventKeyframe.curveType,
								eventValues = (float[])eventKeyframe.eventValues.Clone(),
								eventRandomValues = (float[])eventKeyframe.eventRandomValues.Clone()
							};
							beatmapObject.events[num].Add(item);
						}
						num++;
					}
					return beatmapObject;
				}
				return null;
			}
			
			public static BeatmapObject DeepCopy(OGObject orig, bool _newID = true)
			{
				if (orig != null)
				{
					if (orig.editorData == null)
					{
						orig.editorData = new OGObject.EditorData();
					}
					if (orig.events == null)
					{
						orig.events = new List<List<EventKeyframe>>
						{
							new List<EventKeyframe>(),
							new List<EventKeyframe>(),
							new List<EventKeyframe>(),
							new List<EventKeyframe>()
						};
					}
					var beatmapObject = new BeatmapObject
					{
						id = (_newID ? LSText.randomString(16) : orig.id),
						prefabID = orig.prefabID,
						prefabInstanceID = orig.prefabInstanceID,
						active = orig.active,
						parent = orig.parent,
						parentType = orig.GetParentType(),
						parentOffsets = new List<float>(3),
						depth = orig.Depth,
						objectType = orig.objectType,
						StartTime = orig.StartTime,
						name = orig.name,
						origin = orig.origin,
						autoKillType = orig.autoKillType,
						autoKillOffset = orig.autoKillOffset,
						shape = orig.shape,
						shapeOption = orig.shapeOption,
						text = orig.text,
						editorData = new EditorData
						{
							collapse = orig.editorData.collapse,
							locked = orig.editorData.locked,
							Bin = orig.editorData.Bin,
							Layer = orig.editorData.Layer
						}
					};
					int num = 0;
					foreach (List<EventKeyframe> list in orig.events)
					{
						foreach (EventKeyframe eventKeyframe in list)
						{
                            EventKeyframe item = new EventKeyframe
                            {
								active = eventKeyframe.active,
								eventTime = eventKeyframe.eventTime,
								random = eventKeyframe.random,
								curveType = eventKeyframe.curveType,
								eventValues = (float[])eventKeyframe.eventValues.Clone(),
								eventRandomValues = (float[])eventKeyframe.eventRandomValues.Clone()
							};
							beatmapObject.events[num].Add(item);
						}
						num++;
					}
					return beatmapObject;
				}
				return null;
			}

			public BeatmapObject()
			{
				events = new List<List<EventKeyframe>>
				{
					new List<EventKeyframe>(),
					new List<EventKeyframe>(),
					new List<EventKeyframe>(),
					new List<EventKeyframe>()
				};
			}

			public BeatmapObject(float startTime)
			{
				events = new List<List<EventKeyframe>>
				{
					new List<EventKeyframe>(),
					new List<EventKeyframe>(),
					new List<EventKeyframe>(),
					new List<EventKeyframe>()
				};
				StartTime = startTime;
			}

			public BeatmapObject(bool _active, float _startTime, string _name, int _shape, string _text, List<List<EventKeyframe>> _events)
			{
				id = LSText.randomString(16);
				active = _active;
				StartTime = _startTime;
				name = _name;
				shape = _shape;
				text = _text;
				events = _events;
			}

			public float GetLongestSequence()
			{
				float num = 0f;
				foreach (var list in events)
				{
					foreach (var eventKeyframe in list)
					{
						if (num < eventKeyframe.eventTime)
						{
							num = eventKeyframe.eventTime;
						}
					}
				}
				return num;
			}

			public float GetObjectLifeLength(float _offset = 0f, bool _oldStyle = false, bool _takeCollapseIntoConsideration = false)
			{
				float result = 0f;
				if (_takeCollapseIntoConsideration && editorData.collapse)
				{
					return 0.2f;
				}
				switch (autoKillType)
				{
					case DataManager.GameData.BeatmapObject.AutoKillType.OldStyleNoAutokill:
						result = (_oldStyle ? (AudioManager.inst.CurrentAudioSource.clip.length - startTime) : (GetLongestSequence() + _offset));
						break;
					case DataManager.GameData.BeatmapObject.AutoKillType.LastKeyframe:
						result = GetLongestSequence() + _offset;
						break;
					case DataManager.GameData.BeatmapObject.AutoKillType.LastKeyframeOffset:
						result = GetLongestSequence() + autoKillOffset + _offset;
						break;
					case DataManager.GameData.BeatmapObject.AutoKillType.FixedTime:
						result = autoKillOffset;
						break;
					case DataManager.GameData.BeatmapObject.AutoKillType.SongTime:
						result = ((startTime >= autoKillOffset) ? 0.1f : (autoKillOffset - startTime));
						break;
				}
				return result;
			}

			public bool active;

			public bool fromPrefab;

			public string id = LSText.randomString(16);

			public string prefabID = "";

			public string prefabInstanceID = "";

			public string parent = "";

			string parentType = "101";

			List<float> parentOffsets = new List<float>
			{
				0f,
				0f,
				0f
			};

			int depth = 15;

			public OGObject.AutoKillType autoKillType;

			public float autoKillOffset;

			public OGObject.ObjectType objectType;

			private float startTime;

			public string name = "";

			public string text = "";

			public Vector2 origin = Vector2.zero;

			public int shape;

			public int shapeOption;

			public EditorData editorData = new EditorData();

			public UnityData unityData = new UnityData();

			public List<List<EventKeyframe>> events = new List<List<EventKeyframe>>();
			public List<LevelParentObject> parentObjects = new List<LevelParentObject>();

			[Serializable]
			public class UnityData
            {
				public GameObject gameObject;
				public Renderer renderer;
            }

			private GameObject InitParentChain(BeatmapObject beatmapObject, List<LevelParentObject> parentObjects)
			{
				GameObject gameObject = new GameObject(beatmapObject.name);

				parentObjects.Add(InitLevelParentObject(beatmapObject, gameObject));

				// Has parent - init parent (recursive)
				if (!string.IsNullOrEmpty(beatmapObject.parent) && instance.beatmapObjectsDictionary.ContainsKey(beatmapObject.parent))
				{
					GameObject parentObject = InitParentChain(instance.beatmapObjectsDictionary[beatmapObject.parent], parentObjects);

					gameObject.transform.SetParent(parentObject.transform);
				}

				return gameObject;
			}

			private LevelParentObject InitLevelParentObject(BeatmapObject beatmapObject, GameObject gameObject)
			{
				CachedSequences cachedSequences = null;

				if (instance.cachedSequences.ContainsKey(beatmapObject.id))
					cachedSequences = instance.cachedSequences[beatmapObject.id];

				LevelParentObject levelParentObject = null;

				if (cachedSequences != null)
					if (beatmapObject.events[0][0].eventValues.Length > 2)
					{
						levelParentObject = new LevelParentObject
						{
							Position3DSequence = cachedSequences.Position3DSequence,
							ScaleSequence = cachedSequences.ScaleSequence,
							RotationSequence = cachedSequences.RotationSequence,

							TimeOffset = beatmapObject.StartTime,

							ParentAnimatePosition = beatmapObject.GetParentType(0),
							ParentAnimateScale = beatmapObject.GetParentType(1),
							ParentAnimateRotation = beatmapObject.GetParentType(2),

							ParentOffsetPosition = beatmapObject.getParentOffset(0),
							ParentOffsetScale = beatmapObject.getParentOffset(1),
							ParentOffsetRotation = beatmapObject.getParentOffset(2),

							GameObject = gameObject,
							Transform = gameObject.transform
						};
					}
					else
					{
						levelParentObject = new LevelParentObject
						{
							PositionSequence = cachedSequences.PositionSequence,
							ScaleSequence = cachedSequences.ScaleSequence,
							RotationSequence = cachedSequences.RotationSequence,

							TimeOffset = beatmapObject.StartTime,

							ParentAnimatePosition = beatmapObject.GetParentType(0),
							ParentAnimateScale = beatmapObject.GetParentType(1),
							ParentAnimateRotation = beatmapObject.GetParentType(2),

							ParentOffsetPosition = beatmapObject.getParentOffset(0),
							ParentOffsetScale = beatmapObject.getParentOffset(1),
							ParentOffsetRotation = beatmapObject.getParentOffset(2),

							GameObject = gameObject,
							Transform = gameObject.transform
						};
					}

				return levelParentObject;
			}

			public Sequence<Vector3> GetVector3Sequence(List<EventKeyframe> eventKeyframes, Vector3Keyframe defaultKeyframe, bool relative = false)
			{
				List<IKeyframe<Vector3>> keyframes = new List<IKeyframe<Vector3>>(eventKeyframes.Count);

				var currentValue = Vector3.zero;
				foreach (var eventKeyframe in eventKeyframes)
				{
					Vector3 value = new Vector3(eventKeyframe.eventValues[0], eventKeyframe.eventValues[1], eventKeyframe.eventValues[2]);
					if (eventKeyframe.random != 0)
					{
						Vector2 random = ObjectManager.inst.RandomVector2Parser(eventKeyframe);
						value.x = random.x;
						value.y = random.y;
					}

					currentValue = relative ? currentValue + value : value;

					keyframes.Add(new Vector3Keyframe(eventKeyframe.eventTime, value, Ease.GetEaseFunction(eventKeyframe.curveType.Name)));
				}

				// If there is no keyframe, add default
				if (keyframes.Count == 0)
				{
					keyframes.Add(defaultKeyframe);
				}

				return new Sequence<Vector3>(keyframes);
			}

			public Sequence<Vector2> GetVector2Sequence(List<EventKeyframe> eventKeyframes, Vector2Keyframe defaultKeyframe, bool relative = false)
			{
				List<IKeyframe<Vector2>> keyframes = new List<IKeyframe<Vector2>>(eventKeyframes.Count);

				var currentValue = Vector2.zero;
				foreach (EventKeyframe eventKeyframe in eventKeyframes)
				{
					var value = new Vector2(eventKeyframe.eventValues[0], eventKeyframe.eventValues[1]);
					if (eventKeyframe.random != 0)
					{
						Vector2 random = ObjectManager.inst.RandomVector2Parser(eventKeyframe);
						value.x = random.x;
						value.x = random.y;
					}

					currentValue = relative ? currentValue + value : value;

					keyframes.Add(new Vector2Keyframe(eventKeyframe.eventTime, value, Ease.GetEaseFunction(eventKeyframe.curveType.Name)));
				}

				// If there is no keyframe, add default
				if (keyframes.Count == 0)
				{
					keyframes.Add(defaultKeyframe);
				}

				return new Sequence<Vector2>(keyframes);
			}

			public Sequence<float> GetFloatSequence(List<EventKeyframe> eventKeyframes, FloatKeyframe defaultKeyframe, bool relative = false)
			{
				List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>(eventKeyframes.Count);

				float currentValue = 0.0f;
				foreach (EventKeyframe eventKeyframe in eventKeyframes)
				{
					float value = eventKeyframe.eventValues[0];
					if (eventKeyframe.random != 0)
					{
						value = ObjectManager.inst.RandomFloatParser(eventKeyframe);
					}

					currentValue = relative ? currentValue + value : value;

					keyframes.Add(new FloatKeyframe(eventKeyframe.eventTime, currentValue, Ease.GetEaseFunction(eventKeyframe.curveType.Name)));
				}

				// If there is no keyframe, add default
				if (keyframes.Count == 0)
				{
					keyframes.Add(defaultKeyframe);
				}

				return new Sequence<float>(keyframes);
			}

			public Sequence<float> GetOpacitySequence(List<EventKeyframe> eventKeyframes, int val, FloatKeyframe defaultKeyframe, bool relative = false)
			{
				List<IKeyframe<float>> keyframes = new List<IKeyframe<float>>(eventKeyframes.Count);

				float currentValue = 0.0f;
				foreach (EventKeyframe eventKeyframe in eventKeyframes)
				{
					float value = eventKeyframe.eventValues[val];
					if (eventKeyframe.random != 0)
					{
						value = ObjectManager.inst.RandomFloatParser(eventKeyframe);
					}

					currentValue = relative ? currentValue + value : value;

					keyframes.Add(new FloatKeyframe(eventKeyframe.eventTime, currentValue, Ease.GetEaseFunction(eventKeyframe.curveType.Name)));
				}

				// If there is no keyframe, add default
				if (keyframes.Count == 0)
				{
					keyframes.Add(defaultKeyframe);
				}

				return new Sequence<float>(keyframes);
			}

			public Sequence<Color> GetColorSequence(List<EventKeyframe> eventKeyframes, ThemeKeyframe defaultKeyframe)
			{
				List<IKeyframe<Color>> keyframes = new List<IKeyframe<Color>>(eventKeyframes.Count);

				foreach (EventKeyframe eventKeyframe in eventKeyframes)
				{
					int value = (int)eventKeyframe.eventValues[0];

					value = Mathf.Clamp(value, 0, GameManager.inst.LiveTheme.objectColors.Count - 1);

					keyframes.Add(new ThemeKeyframe(eventKeyframe.eventTime, value, Ease.GetEaseFunction(eventKeyframe.curveType.Name)));
				}

				// If there is no keyframe, add default
				if (keyframes.Count == 0)
				{
					keyframes.Add(defaultKeyframe);
				}

				return new Sequence<Color>(keyframes);
			}

			[Serializable]
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
						if (value == 5)
						{
							layer = 4;
							value = 4;
						}

						layer = value;
					}
				}

				int bin;

				int layer;

				public bool locked;

				public bool collapse;
			}

			public class LevelParentObject
			{
				public Sequence<Vector2> PositionSequence { get; set; }
				public Sequence<Vector3> Position3DSequence { get; set; }
				public Sequence<Vector2> ScaleSequence { get; set; }
				public Sequence<float> RotationSequence { get; set; }

				public float TimeOffset { get; set; }

				public bool ParentAnimatePosition { get; set; }
				public bool ParentAnimateScale { get; set; }
				public bool ParentAnimateRotation { get; set; }

				public float ParentOffsetPosition { get; set; }
				public float ParentOffsetScale { get; set; }
				public float ParentOffsetRotation { get; set; }

				public GameObject GameObject { get; set; }
				public Transform Transform { get; set; }
			}
		}
    }
}
