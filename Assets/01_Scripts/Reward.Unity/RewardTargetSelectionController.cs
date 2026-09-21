using System;
using System.Collections.Generic;
using Octoplug.GameFlow;
using Octoplug.Power;
using Octoplug.Power.UI;
using Octoplug.RoomGeneration.Unity;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Octoplug.Reward.Unity
{
    public enum RewardTargetSelectionState
    {
        None,
        SelectingPowerStrip,
        SelectingCableOwner,
        SelectingRoom
    }

    public class RewardTargetSelectionController : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Color overlayColor = new(0f, 0f, 0f, 0.7f);
        [SerializeField] private int highlightSortingOrder = 1000;
        [SerializeField] private TMPro.TextMeshProUGUI alertText;

        private readonly Dictionary<Renderer, (int order, string layer)> originalSorting = new();
        private readonly Dictionary<SpriteRenderer, Color> originalColors = new();
        private readonly Dictionary<Collider2D, object> validTargets = new();
        private RewardBalanceRecord currentReward;
        private IRewardContext currentContext;
        private RewardPlacementService placementService;
        private Func<object, bool> onComplete;
        private GameObject overlay;
        private Sprite overlaySprite;
        private Texture2D overlayTexture;
        private int selectionStartFrame;

        public bool IsSelecting => State != RewardTargetSelectionState.None;
        public RewardTargetSelectionState State { get; private set; }

        private void OnDisable() => CancelSelection();

        public void BeginSelection(
            RewardBalanceRecord reward,
            IRewardContext context,
            Func<object, bool> completion,
            RewardPlacementService placement = null)
        {
            CancelSelection();
            currentReward = reward;
            currentContext = context;
            placementService = placement;
            onComplete = completion;
            State = GetState(reward.TargetType);
            selectionStartFrame = Time.frameCount;
            GameplayInputLock.AllowCameraMovementDuringLock = true;
            if (State != RewardTargetSelectionState.SelectingRoom)
            {
                CreateOverlay();
            }
            CollectEligibleTargets();

            var message = GetAlert(State);
            if (PowerUiCoordinator.Instance != null)
            {
                PowerUiCoordinator.Instance.ShowPersistent(message);
            }
            else if (alertText != null)
            {
                alertText.text = message;
                alertText.gameObject.SetActive(true);
            }
        }

        public void CancelSelection() => Cleanup();

        private void Update()
        {
            if (!IsSelecting
                || Time.frameCount == selectionStartFrame
                || Mouse.current == null
                || !Mouse.current.leftButton.wasPressedThisFrame
                || (EventSystem.current != null
                    && EventSystem.current.IsPointerOverGameObject()))
            {
                return;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            var worldPosition = (Vector2)camera.ScreenToWorldPoint(
                Mouse.current.position.ReadValue());
            var target = ResolveClick(worldPosition);
            if (target != null)
            {
                CompleteSelection(target);
            }
        }

        private object ResolveClick(Vector2 worldPosition)
        {
            if (State == RewardTargetSelectionState.SelectingRoom)
            {
                return placementService != null
                    && placementService.TryResolvePowerStripPlacement(
                        worldPosition,
                        out var roomTarget)
                    ? roomTarget
                    : null;
            }

            foreach (var pair in validTargets)
            {
                if (pair.Key != null
                    && pair.Key.isActiveAndEnabled
                    && pair.Key.OverlapPoint(worldPosition))
                {
                    return pair.Value;
                }
            }

            return null;
        }

        private void CompleteSelection(object target)
        {
            if (!IsSelecting)
            {
                return;
            }

            var completion = onComplete;
            GameplayInputLock.SuppressUntilPointerRelease = true;
            if (completion != null && completion(target))
            {
                Cleanup();
            }
        }

        private void CollectEligibleTargets()
        {
            if (currentReward == null || currentContext == null)
            {
                return;
            }

            switch (State)
            {
                case RewardTargetSelectionState.SelectingPowerStrip:
                    foreach (var strip in currentContext.GetPowerStrips())
                    {
                        if (strip != null
                            && RewardCandidateGenerator.AreEffectsApplicable(
                                currentReward.Effects,
                                currentContext,
                                strip))
                        {
                            AddTarget(strip, strip);
                        }
                    }
                    break;
                case RewardTargetSelectionState.SelectingCableOwner:
                    foreach (var cableOwner in currentContext.GetCableOwners())
                    {
                        if (cableOwner?.Owner != null
                            && RewardCandidateGenerator.AreEffectsApplicable(
                                currentReward.Effects,
                                currentContext,
                                cableOwner))
                        {
                            AddTarget(cableOwner.Owner, cableOwner);
                        }
                    }
                    break;
                case RewardTargetSelectionState.SelectingRoom:
                    DimPlaceableEquipment();
                    break;
            }
        }

        private void DimPlaceableEquipment()
        {
            foreach (var product in RuntimeWorldRegistry.GetProducts())
            {
                DimRenderers(product);
            }

            foreach (var strip in RuntimeWorldRegistry.GetPowerStrips())
            {
                DimRenderers(strip);
            }

            foreach (var outlet in RuntimeWorldRegistry.GetWallOutlets())
            {
                DimRenderers(outlet);
            }
        }

        private void DimRenderers(Component root)
        {
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (originalColors.ContainsKey(renderer))
                {
                    continue;
                }

                originalColors.Add(renderer, renderer.color);
                var color = renderer.color;
                color.r *= 0.3f;
                color.g *= 0.3f;
                color.b *= 0.3f;
                renderer.color = color;
            }
        }

        private void AddTarget(Component root, object target)
        {
            AddRenderers(root);
            foreach (var collider in root.GetComponentsInChildren<Collider2D>())
            {
                if (collider != null && collider.enabled)
                {
                    validTargets[collider] = target;
                }
            }
        }

        private void AddRenderers(Component root)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!originalSorting.ContainsKey(renderer))
                {
                    originalSorting.Add(
                        renderer,
                        (renderer.sortingOrder, renderer.sortingLayerName));
                    renderer.sortingOrder += highlightSortingOrder;
                    renderer.sortingLayerName = "Default";
                }
            }
        }

        private static RewardTargetSelectionState GetState(RewardTargetType targetType)
        {
            return targetType switch
            {
                RewardTargetType.PowerStrip => RewardTargetSelectionState.SelectingPowerStrip,
                RewardTargetType.CableOwner => RewardTargetSelectionState.SelectingCableOwner,
                RewardTargetType.RoomPlacement => RewardTargetSelectionState.SelectingRoom,
                _ => RewardTargetSelectionState.None
            };
        }

        private static string GetAlert(RewardTargetSelectionState state)
        {
            return state switch
            {
                RewardTargetSelectionState.SelectingPowerStrip => "강화할 멀티탭을 선택하세요.",
                RewardTargetSelectionState.SelectingCableOwner => "케이블을 연장할 제품 또는 멀티탭을 선택하세요.",
                RewardTargetSelectionState.SelectingRoom => "멀티탭을 배치할 위치를 선택하세요.",
                _ => string.Empty
            };
        }

        private void CreateOverlay()
        {
            overlayTexture = new Texture2D(1, 1);
            overlayTexture.SetPixel(0, 0, Color.white);
            overlayTexture.Apply();
            overlaySprite = Sprite.Create(
                overlayTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
            overlay = new GameObject("RewardTargetSelectionOverlay");
            var renderer = overlay.AddComponent<SpriteRenderer>();
            renderer.sprite = overlaySprite;
            renderer.color = overlayColor;
            renderer.sortingOrder = highlightSortingOrder - 1;
            renderer.sortingLayerName = "Default";

            var camera = Camera.main;
            overlay.transform.SetParent(
                camera != null ? camera.transform : transform,
                false);
            overlay.transform.localPosition = camera != null
                ? new Vector3(0f, 0f, 10f)
                : Vector3.zero;
            overlay.transform.localScale = new Vector3(10000f, 10000f, 1f);
        }

        private void Cleanup()
        {
            State = RewardTargetSelectionState.None;
            GameplayInputLock.AllowCameraMovementDuringLock = false;
            currentReward = null;
            currentContext = null;
            placementService = null;
            onComplete = null;

            if (PowerUiCoordinator.Instance != null)
            {
                PowerUiCoordinator.Instance.ClearPersistent();
            }
            else if (alertText != null)
            {
                alertText.text = string.Empty;
                alertText.gameObject.SetActive(false);
            }

            foreach (var pair in originalSorting)
            {
                if (pair.Key != null)
                {
                    pair.Key.sortingOrder = pair.Value.order;
                    pair.Key.sortingLayerName = pair.Value.layer;
                }
            }

            originalSorting.Clear();
            foreach (var pair in originalColors)
            {
                if (pair.Key != null)
                {
                    pair.Key.color = pair.Value;
                }
            }

            originalColors.Clear();
            validTargets.Clear();
            DestroyRuntimeObject(overlay);
            DestroyRuntimeObject(overlaySprite);
            DestroyRuntimeObject(overlayTexture);
            overlay = null;
            overlaySprite = null;
            overlayTexture = null;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object value)
        {
            if (value == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(value);
            }
            else
            {
                DestroyImmediate(value);
            }
        }
    }
}
