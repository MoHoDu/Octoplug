import re

with open('Assets/01_Scripts/RoomGeneration.Unity/RoomContentGenerationController.cs', 'r', encoding='utf-8') as f:
    content = f.read()

old_resolve = """        private void ResolveOverlap(GameObject instance, RoomPlacement room)
        {
            var colliders = instance.GetComponentsInChildren<Collider2D>();
            if (colliders.Length == 0) return;

            var roomCenter = new Vector3(
                (room.Bounds.MinX + room.Bounds.MaxX) * 0.5f,
                (room.Bounds.MinY + room.Bounds.MaxY) * 0.5f,
                0f);

            // Try adjusting position up to 15 times
            for (int iteration = 0; iteration < 15; iteration++)
            {
                bool overlapped = false;
                Physics2D.SyncTransforms();
                foreach (var col in colliders)
                {
                    var hits = new List<Collider2D>();
                    var filter = new ContactFilter2D { useTriggers = false };
                    Physics2D.OverlapCollider(col, filter, hits);

                    foreach (var hit in hits)
                    {
                        if (hit.transform.IsChildOf(instance.transform) || hit.isTrigger) continue;

                        // If it overlaps a solid collider (like wall, outlet, door)
                        overlapped = true;
                        break;
                    }
                    if (overlapped) break;
                }

                if (!overlapped) break;

                // Move slightly towards the room center
                var dir = (roomCenter - instance.transform.position).normalized;
                if (dir.sqrMagnitude < 0.01f)
                {
                    // If already at center, just nudge randomly
                    dir = UnityEngine.Random.insideUnitCircle.normalized;
                }
                instance.transform.position += dir * 0.1f;
            }
        }"""

new_resolve = """        private void ResolveOverlap(GameObject instance, RoomPlacement room)
        {
            var colliders = instance.GetComponentsInChildren<Collider2D>();
            if (colliders.Length == 0) return;

            // Try adjusting position up to 15 times
            for (int iteration = 0; iteration < 15; iteration++)
            {
                bool overlapped = false;
                Physics2D.SyncTransforms();
                foreach (var col in colliders)
                {
                    if (col.isTrigger) continue;
                    var hits = new List<Collider2D>();
                    var filter = new ContactFilter2D { useTriggers = false };
                    Physics2D.OverlapCollider(col, filter, hits);

                    foreach (var hit in hits)
                    {
                        if (hit.transform.IsChildOf(instance.transform) || hit.isTrigger) continue;

                        var dist = Physics2D.Distance(col, hit);
                        if (dist.isOverlapped)
                        {
                            overlapped = true;
                            // normal points from hit to col. distance is negative.
                            // Move col out of hit.
                            var pushDir = dist.normal;
                            if (pushDir.sqrMagnitude < 0.001f)
                            {
                                pushDir = UnityEngine.Random.insideUnitCircle.normalized;
                            }
                            instance.transform.position += (Vector3)(pushDir * (-dist.distance + 0.01f));
                        }
                    }
                }

                if (!overlapped) break;
            }
            
            // Constrain inside room bounds just in case it got pushed out
            var pos = instance.transform.position;
            pos.x = Mathf.Clamp(pos.x, room.Bounds.MinX + 0.5f, room.Bounds.MaxX - 0.5f);
            pos.y = Mathf.Clamp(pos.y, room.Bounds.MinY + 0.5f, room.Bounds.MaxY - 0.5f);
            instance.transform.position = pos;
        }"""

content = content.replace(old_resolve, new_resolve)

with open('Assets/01_Scripts/RoomGeneration.Unity/RoomContentGenerationController.cs', 'w', encoding='utf-8') as f:
    f.write(content)

