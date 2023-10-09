using UnityEngine;

namespace ObjectModifiers.Functions.Components
{
    public class DelayTracker : MonoBehaviour
    {
		private void Start()
		{
		}

		private void LateUpdate()
		{
			if (active && leader != null && leader.gameObject.activeSelf && leader.gameObject.activeInHierarchy)
			{
				target = leader.position + offset * leader.transform.right;
				float p = Time.deltaTime * 60f * AudioManager.inst.CurrentAudioSource.pitch;
				float moveDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(moveSharpness, 0.001f, 1f), p);
				float rotateDelay = 1f - Mathf.Pow(1f - Mathf.Clamp(rotateSharpness, 0.001f, 1f), p);
				if (move)
				{
					transform.position += (target - transform.position) * moveDelay;
				}
				else
                {
					transform.position = Vector3.zero;
                }
				if (rotate)
					transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, leader.transform.rotation.eulerAngles.z), rotateDelay);
				else
					transform.rotation = Quaternion.Euler(Vector3.zero);
			}
			else
            {
				transform.position = Vector3.zero;
				transform.rotation = Quaternion.Euler(Vector3.zero);
            }
		}

		private void OnDrawGizmos()
		{
		}

		public bool active;
		public bool rotate;
		public bool move;

		public Transform leader;

		public float moveSharpness = 0.1f;
		public float rotateSharpness = 0.1f;

		public float offset;

		public Vector3 target;
	}
}
