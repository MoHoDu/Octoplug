import re

with open('Assets/01_Scripts/RoomGeneration.Unity/RoomContentGenerationController.cs', 'r', encoding='utf-8') as f:
    content = f.read()

old_code = """            // Constrain inside room bounds just in case it got pushed out
            var pos = instance.transform.position;
            pos.x = Mathf.Clamp(pos.x, room.Bounds.MinX + 0.5f, room.Bounds.MaxX - 0.5f);
            pos.y = Mathf.Clamp(pos.y, room.Bounds.MinY + 0.5f, room.Bounds.MaxY - 0.5f);
            instance.transform.position = pos;
        }"""

new_code = """            // Constrain inside room bounds just in case it got pushed out
            var pos = instance.transform.position;
            pos.x = Mathf.Clamp(pos.x, room.Bounds.MinX + 0.5f, room.Bounds.MaxX - 0.5f);
            pos.y = Mathf.Clamp(pos.y, room.Bounds.MinY + 0.5f, room.Bounds.MaxY - 0.5f);
            instance.transform.position = pos;

            // Re-reserve grid cells if it moved
            var footprint = instance.GetComponent<PlacementFootprint>();
            if (footprint != null)
            {
                var service = CableRoutingGridService.Instance;
                if (service != null && service.Grid != null)
                {
                    footprint.TryReserveAt(service.Grid, pos);
                }
            }
        }"""

content = content.replace(old_code, new_code)

with open('Assets/01_Scripts/RoomGeneration.Unity/RoomContentGenerationController.cs', 'w', encoding='utf-8') as f:
    f.write(content)

