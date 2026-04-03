using UnityEngine;
    /// <summary>
    /// Teleports an object entering a Trigger (Collider set to isTrigger)
    /// to the position of an empty child GameObject.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TeleportFromTrigger : MonoBehaviour
    {
        [Header("Destination (empty child)")]
        [Tooltip("Child index to use as the destination point if destinationPoint is not assigned.")]
        [Min(0)]
        public int destinationChildIndex = 0;

        [Tooltip("Optional: explicitly assign the target Transform. If empty, uses the child by index.")]
        public Transform destinationPoint;

        [Header("Target")]
        [Tooltip("Tag of objects to teleport (if teleportAnyObject = false).")]
        public string targetTag = "Player";

        [Tooltip("If true, teleports any object entering the trigger (no tag filter).")]
        public bool teleportAnyObject = false;

        [Header("Adjustments")]
        [Tooltip("If true, keeps the entering object's height (Y) and replaces only X/Z.")]
        public bool preserveY = false;

        [Tooltip("Y offset applied to the destination (useful to avoid clipping into the ground).")]
        public float yOffset = 0f;

        [Header("Rigidbody")]
        [Tooltip("If the object has a Rigidbody, resets its velocity/rotation to zero after teleporting.")]
        public bool resetRigidbodyMotion = true;

        [Header("Debug / Compatibility")]
        [Tooltip("Shows logs to confirm teleportation runs and which position it targets.")]
        public bool debugLogs = true;

        [Tooltip("When the entering object has no Rigidbody (e.g. CharacterController), moves the root instead of the collider child.")]
        public bool teleportRootIfNoRigidbody = true;

        private void Reset()
        {
            // By default, we expect the collider to be a trigger.
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Awake()
        {
            ResolveDestination();
        }

        private void OnValidate()
        {
            // Ensures predictable behavior when modifying the prefab/scene.
            if (destinationPoint == null)
            {
                ResolveDestination();
            }
        }

        private void ResolveDestination()
        {
            if (destinationPoint != null)
            {
                return;
            }

            if (transform.childCount <= destinationChildIndex)
            {
                destinationPoint = null;
                return;
            }

            destinationPoint = transform.GetChild(destinationChildIndex);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (debugLogs)
            {
                Debug.Log($"[{nameof(TeleportFromTrigger)}] OnTriggerEnter: {other.name} (tag={other.tag})");
                Collider myCol = GetComponent<Collider>();
                if (myCol != null && !myCol.isTrigger)
                {
                    Debug.LogWarning($"[{nameof(TeleportFromTrigger)}] Your collider is not set to isTrigger (on {gameObject.name}).");
                }
            }
            if (other == null)
            {
                Debug.Log($"OnTriggerEnter: other is null");
                return;
            }

            if (!teleportAnyObject && !other.CompareTag(targetTag))
            {
                Debug.Log($"OnTriggerEnter: {other.name} is not a {targetTag}");
                return;
            }

            ResolveDestination();
            if (destinationPoint == null)
            {
                Debug.LogWarning($"[{nameof(TeleportFromTrigger)}] Destination not found: add an empty child (index {destinationChildIndex}) under '{gameObject.name}'.");
                return;
            }

            Vector3 targetPos = destinationPoint.position;
            if (preserveY)
            {
                targetPos.y = other.transform.position.y + yOffset;
            }
            else
            {
                targetPos.y += yOffset;
            }

            Rigidbody rb = other.attachedRigidbody;
            if (rb != null)
            {
                if (debugLogs)
                {
                    Debug.Log($"[{nameof(TeleportFromTrigger)}] Rigidbody teleport -> {targetPos} (before={rb.position})");
                }
                rb.position = targetPos;
                if (resetRigidbodyMotion)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                return;
            }

            // Common case: CharacterController (no Rigidbody) or scripts that rely on the root transform.
            CharacterController cc = other.GetComponentInParent<CharacterController>();
            Transform toMove = cc != null
                ? cc.transform
                : (teleportRootIfNoRigidbody ? other.transform.root : other.transform);

            if (debugLogs)
            {
                Debug.Log($"[{nameof(TeleportFromTrigger)}] Transform teleport -> {targetPos} (before={toMove.position}) via {(cc != null ? "CharacterController" : "Transform")}");
            }

            if (cc != null)
            {
                // Avoid cases where the CharacterController "snaps back" right after setting position.
                StartCoroutine(TeleportCharacterControllerNextFrame(cc, targetPos));
                return;
            }

            toMove.position = targetPos;
        }

        private System.Collections.IEnumerator TeleportCharacterControllerNextFrame(CharacterController cc, Vector3 targetPos)
        {
            cc.enabled = false;
            cc.transform.position = targetPos;
            // Wait 1 frame so Unity can recompute internal state properly.
            yield return null;
            cc.enabled = true;
        }
    }

