using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

using BeatmapObject = DataManager.GameData.BeatmapObject;

using ObjectModifiers.Modifiers;

namespace ObjectModifiers.Functions
{
    public class ObjectOptimization : MonoBehaviour
    {
        public BeatmapObject beatmapObject;

        public bool hovered;

		public bool bulletOver;

		public ModifierObject modifierObject;

		List<Collider2D> colliders = new List<Collider2D>();

		void Update()
        {
			bulletOver = false;
        }

        void OnMouseEnter()
        {
            hovered = true;
        }

        void OnMouseExit()
        {
            hovered = false;
        }

		bool CheckCollider(Collider other)
        {
			return other.tag != "Player" && other.gameObject.name.Contains("bullet (Player");
		}
		bool CheckCollider(Collider2D other)
        {
			return other.tag != "Player" && other.gameObject.name.Contains("bullet (Player") && !colliders.Contains(other);
		}

		void OnTriggerEnter2D(Collider2D other)
		{
			Debug.LogFormat("{0}OnTriggerEnter2D: {1}", ObjectModifiersPlugin.className, other.name);
			if (CheckCollider(other))
            {
				bulletOver = true;
				if (!colliders.Contains(other))
					colliders.Add(other);
			}
		}

		void OnTriggerEnter(Collider other)
		{
			Debug.LogFormat("{0}OnTriggerEnter: {1}", ObjectModifiersPlugin.className, other.name);
			if (CheckCollider(other))
			{
				bulletOver = true;
			}
		}

		void OnTriggerExit2D(Collider2D other)
		{
			Debug.LogFormat("{0}OnTriggerExit2D: {1}", ObjectModifiersPlugin.className, other.name);
			if (CheckCollider(other))
			{
				bulletOver = false;
				if (colliders.Contains(other))
					colliders.Remove(other);
			}
		}

		void OnTriggerExit(Collider other)
		{
			Debug.LogFormat("{0}OnTriggerExit: {1}", ObjectModifiersPlugin.className, other.name);
			if (CheckCollider(other))
			{
				bulletOver = false;
			}
		}

		void OnTriggerStay2D(Collider2D other)
		{
			if (CheckCollider(other))
			{
				bulletOver = true;
			}
		}

		void OnTriggerStay(Collider other)
		{
			if (CheckCollider(other))
			{
				bulletOver = true;
			}
		}
	}
}
