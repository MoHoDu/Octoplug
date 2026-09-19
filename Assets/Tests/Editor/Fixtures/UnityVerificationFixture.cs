using System;
using System.Collections.Generic;
using Octoplug.Power;
using Octoplug.Power.Cable;
using Octoplug.Power.Connection;
using Octoplug.Power.Grid;
using UnityEditor;
using UnityEngine;

namespace Octoplug.Tests.Editor
{
    public abstract class UnityVerificationFixture
    {
        protected const string MultitapPrefabPath =
            "Assets/03_Prefabs/Multitaps/Multitap.prefab";
        protected const string WallOutletPrefabPath =
            "Assets/03_Prefabs/Wall_Outlets/Wall_Outlet.prefab";
        protected const string ProductPrefabFolder =
            "Assets/03_Prefabs/Products";

        private readonly List<GameObject> createdObjects = new();
        private readonly List<PlugConnector> connectedPlugs = new();

        protected CableRoutingGridService GridService { get; private set; }
        protected CableRoutingGrid Grid => GridService.Grid;

        protected virtual void SetUpVerificationFixture()
        {
            var context = new GameObject("UnityVerificationContext");
            createdObjects.Add(context);
            GridService = context.AddComponent<CableRoutingGridService>();

            // EditMode does not invoke runtime lifecycle methods automatically.
            // Use the production-owned verification entry point rather than reflection
            // or private-state writes.
            GridService.InitializeForVerification();
        }

        protected virtual void TearDownVerificationFixture()
        {
            for (var i = connectedPlugs.Count - 1; i >= 0; i--)
            {
                if (connectedPlugs[i] != null)
                {
                    PlugSocketConnection.Disconnect(connectedPlugs[i]);
                }
            }

            connectedPlugs.Clear();
            for (var i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
            GridService = null;
        }

        protected PowerStrip CreatePowerStrip(Vector2 position = default)
        {
            var instance = InstantiatePrefab(MultitapPrefabPath, position);
            var strip = instance.GetComponent<PowerStrip>();
            if (strip == null)
            {
                throw new InvalidOperationException(
                    $"Prefab '{MultitapPrefabPath}' has no PowerStrip component.");
            }

            var footprint = RequireComponent<PlacementFootprint>(instance);
            // EditMode does not invoke PlacementFootprint.Awake, so use the
            // explicit-count production overload instead of its cached owner.
            MarkWalkableForPowerStrip(strip, strip.Sockets.Count);
            if (!footprint.TryReserveAt(
                Grid,
                strip.transform.position,
                strip.ActiveSocketCount))
            {
                throw new InvalidOperationException("Unable to reserve the authored PowerStrip footprint.");
            }

            return strip;
        }

        protected ApplianceSource CreateProduct(
            string prefabName,
            Vector2 position = default)
        {
            var path = $"{ProductPrefabFolder}/{prefabName}.prefab";
            return RequireComponent<ApplianceSource>(InstantiatePrefab(path, position));
        }

        protected WallOutlet CreateWallOutlet(Vector2 position = default)
        {
            return RequireComponent<WallOutlet>(
                InstantiatePrefab(WallOutletPrefabPath, position));
        }

        protected CableRoutingController GetRoutingController(
            ApplianceSource product)
        {
            var controller = product.Cable.GetComponent<
                CableRoutingController>();
            if (controller == null)
            {
                throw new InvalidOperationException(
                    $"'{product.name}' Cable has no " +
                    $"{nameof(CableRoutingController)} component.");
            }

            return controller;
        }

        protected void MarkCell(
            GridCoord coord,
            GridCellState state)
        {
            Grid.MarkArea(
                new Bounds(
                    Grid.CellToWorld(coord),
                    Vector3.one * Grid.CellSize * 0.9f),
                state);
        }

        protected void MarkWalkableRectangle(
            int minX,
            int maxX,
            int minY,
            int maxY)
        {
            for (var x = minX; x <= maxX; x++)
            {
                for (var y = minY; y <= maxY; y++)
                {
                    MarkCell(
                        new GridCoord(x, y),
                        GridCellState.Walkable);
                }
            }
        }

        protected static bool PathContainsCell(
            IReadOnlyList<GridCoord> path,
            GridCoord cell)
        {
            for (var i = 0; i < path.Count; i++)
            {
                if (path[i] == cell)
                {
                    return true;
                }
            }

            return false;
        }

        protected void UpgradeSocketCountTo(PowerStrip strip, int targetCount)
        {
            while (strip.ActiveSocketCount < targetCount)
            {
                if (!strip.TryUpgradeActiveSocketCount(out var failure))
                {
                    throw new InvalidOperationException(
                        $"Socket upgrade to {targetCount} failed: {failure}.");
                }
            }
        }

        protected void UpgradeSocketCountTo(WallOutlet outlet, int targetCount)
        {
            while (outlet.ActiveSocketCount < targetCount)
            {
                if (!outlet.TryUpgradeActiveSocketCount(out var failure))
                {
                    throw new InvalidOperationException(
                        $"Wall Outlet socket upgrade to {targetCount} failed: {failure}.");
                }
            }
        }

        protected void UpgradeAllowedPowerTo(PowerStrip strip, float targetWatts)
        {
            while (strip.AllowedPowerWatts < targetWatts)
            {
                if (!strip.TryUpgradeAllowedPowerWatts(out var failure))
                {
                    throw new InvalidOperationException(
                        $"Allowed Power upgrade to {targetWatts} failed: {failure}.");
                }
            }
        }

        protected void UpgradeCableLengthTo(CableInfo cable, float targetLength)
        {
            while (cable.CableLength < targetLength)
            {
                if (!cable.TryUpgradeCableLength(out var failure))
                {
                    throw new InvalidOperationException(
                        $"Cable Length upgrade to {targetLength} failed: {failure}.");
                }
            }
        }

        protected bool Connect(PlugConnector plug, SocketConnector socket)
        {
            var connected = PlugSocketConnection.Connect(plug, socket);
            if (connected && !connectedPlugs.Contains(plug))
            {
                connectedPlugs.Add(plug);
            }

            return connected;
        }

        protected void Disconnect(PlugConnector plug)
        {
            PlugSocketConnection.Disconnect(plug);
            connectedPlugs.Remove(plug);
        }

        protected void MarkWalkableForPowerStrip(PowerStrip strip, int socketCount)
        {
            var footprint = RequireComponent<PlacementFootprint>(strip.gameObject);
            var cells = new List<GridCoord>();
            footprint.GetCoveredCells(Grid, strip.transform.position, socketCount, cells);
            for (var i = 0; i < cells.Count; i++)
            {
                Grid.MarkArea(
                    new Bounds(Grid.CellToWorld(cells[i]), Vector3.one * Grid.CellSize),
                    GridCellState.Walkable);
            }
        }

        protected static T RequireComponent<T>(GameObject gameObject)
            where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(
                    $"'{gameObject.name}' has no {typeof(T).Name} component.");
            }

            return component;
        }

        private GameObject InstantiatePrefab(string assetPath, Vector2 position)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Prefab not found: {assetPath}");
            }

            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new InvalidOperationException($"Unable to instantiate prefab: {assetPath}");
            }

            instance.transform.position = position;
            createdObjects.Add(instance);
            return instance;
        }
    }
}
