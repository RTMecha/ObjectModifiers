using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace ObjectModifiers.Functions
{
    public class HomingLogic : MonoBehaviour
    {
        public Transform target;
        public Rigidbody2D rb;
        public Material mat;
        public float angleChangingSpeed = 1f;
        public float movementSpeed = 1f;
        public bool followPos;
        public float posRange = 5f;

        public bool followRot;
        public float rotRange = 5f;

        public bool followColor;
        public float colRange = 5f;
        public List<Color> colors = new List<Color>
        {
            Color.black,
            Color.white
        };

        private void Awake()
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.inertia = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void FixedUpdate()
        {
            if (target != null && rb != null && followPos && Vector2.Distance(transform.position, target.position) < posRange)
            {
                rb.velocity = transform.up * movementSpeed;
            }

            if (target != null && mat != null && followColor && Vector2.Distance(transform.position, target.position) < colRange)
            {
                float x = Vector2.Distance(transform.position, target.position);
                float dis = -x + colRange;
                mat.color = Color.Lerp(colors[0], colors[1], dis / colRange);
            }

            if (target != null && rb != null && followRot && Vector2.Distance(transform.position, target.position) < rotRange)
            {
                Vector2 direction = (Vector2)target.position - rb.position;
                direction.Normalize();
                float rotateAmount = Vector3.Cross(direction, transform.up).z;
                rb.angularVelocity = -angleChangingSpeed * rotateAmount;
            }
        }
    }
}
