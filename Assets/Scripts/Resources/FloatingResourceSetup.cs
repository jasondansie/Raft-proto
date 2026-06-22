using UnityEngine;

namespace RaftProto.Resources
{
    /// <summary>
    /// Ensures kit prefabs are usable as drifting resources: a single trigger sphere sized to
    /// the mesh bounds, no rigidbody, and a <see cref="FloatingResource"/> component.
    /// Mesh colliders are removed because non-convex mesh triggers break raycasts.
    /// </summary>
    public static class FloatingResourceSetup
    {
        public static FloatingResource Configure(GameObject instance, ResourceType type)
        {
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>())
            {
                Object.Destroy(body);
            }

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>())
            {
                Object.Destroy(collider);
            }

            SphereCollider sphere = instance.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            ApplyBoundsToSphere(instance, sphere);

            FloatingResource resource = instance.GetComponent<FloatingResource>();
            if (resource == null)
            {
                resource = instance.AddComponent<FloatingResource>();
            }

            resource.SetResourceType(type);
            return resource;
        }

        private static void ApplyBoundsToSphere(GameObject instance, SphereCollider sphere)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                sphere.radius = 0.75f;
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            sphere.center = instance.transform.InverseTransformPoint(bounds.center);
            sphere.radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
        }
    }
}
