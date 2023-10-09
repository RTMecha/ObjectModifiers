using System.Collections.Generic;

using UnityEngine;

using ObjectModifiers.Modifiers;

using BeatmapObject = DataManager.GameData.BeatmapObject;

namespace ObjectModifiers.Functions
{
    public class ObjectOptimization : MonoBehaviour
    {
        public BeatmapObject beatmapObject;

        public bool hovered;

		public bool bulletOver;

		public ModifierObject modifierObject;

		List<Collider2D> colliders = new List<Collider2D>();

		void Update() => bulletOver = false;

        void OnMouseEnter() => hovered = true;

        void OnMouseExit() => hovered = false;

		bool CheckCollider(Collider other) => other.tag != "Player" && other.gameObject.name.Contains("bullet (Player");
		bool CheckCollider(Collider2D other) => other.tag != "Player" && other.gameObject.name.Contains("bullet (Player") && !colliders.Contains(other);

		void OnTriggerEnter2D(Collider2D other)
		{
			//Debug.LogFormat("{0}OnTriggerEnter2D: {1}", ObjectModifiersPlugin.className, other.name);
			if (CheckCollider(other))
            {
				bulletOver = true;
				if (!colliders.Contains(other))
					colliders.Add(other);
			}
		}

		void OnTriggerEnter(Collider other)
		{
			//Debug.LogFormat("{0}OnTriggerEnter: {1}", ObjectModifiersPlugin.className, other.name);
			if (CheckCollider(other))
				bulletOver = true;
		}

		void OnTriggerExit2D(Collider2D other)
		{
			//Debug.LogFormat("{0}OnTriggerExit2D: {1}", ObjectModifiersPlugin.className, other.name);
			if (CheckCollider(other))
			{
				bulletOver = false;
				if (colliders.Contains(other))
					colliders.Remove(other);
			}
		}

		void OnTriggerExit(Collider other)
		{
			//Debug.LogFormat("{0}OnTriggerExit: {1}", ObjectModifiersPlugin.className, other.name);
			if (CheckCollider(other))
				bulletOver = false;
		}

		void OnTriggerStay2D(Collider2D other)
		{
			if (CheckCollider(other))
				bulletOver = true;
		}

		void OnTriggerStay(Collider other)
		{
			if (CheckCollider(other))
				bulletOver = true;
		}
	}
}
