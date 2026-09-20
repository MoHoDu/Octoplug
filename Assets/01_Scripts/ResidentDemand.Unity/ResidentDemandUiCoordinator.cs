using System.Collections.Generic;
using Octoplug.Power.UI;
using UnityEngine;

namespace Octoplug.ResidentDemand.Unity
{
    public sealed class ResidentDemandUiCoordinator : MonoBehaviour
    {
        [SerializeField]
        private ResidentDemandController controller;

        [SerializeField]
        private Transform residentsArea;

        [SerializeField]
        [Tooltip("The preplaced Resident 01 card in Residents_Area.")]
        private ResidentCardView residentOneCard;

        [SerializeField]
        private ResidentCardView residentCardPrefab;

        [SerializeField]
        private UsageTypeIconLibrary usageTypeIconLibrary;

        private readonly Dictionary<int, ResidentCardView> _cardsByResident = new();
        private readonly HashSet<UseInfoView> _knownUseInfoViews = new();

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void Subscribe()
        {
            if (controller == null)
            {
                return;
            }

            Unsubscribe();
            controller.ResidentsChanged += RefreshResidentCards;
            controller.AssignmentsChanged += RefreshProductUsage;
            RefreshResidentCards();
            RefreshProductUsage();
        }

        private void Unsubscribe()
        {
            if (controller == null)
            {
                return;
            }

            controller.ResidentsChanged -= RefreshResidentCards;
            controller.AssignmentsChanged -= RefreshProductUsage;
        }

        public void RefreshResidentCards()
        {
            if (controller == null || residentsArea == null || usageTypeIconLibrary == null)
            {
                return;
            }

            var residents = controller.Residents;
            for (var index = 0; index < residents.Count; index++)
            {
                var resident = residents[index];
                var card = GetOrCreateCard(resident.ResidentNumber.Value);
                if (card == null)
                {
                    continue;
                }

                card.transform.SetSiblingIndex(index);
                card.Bind(resident, usageTypeIconLibrary);
            }
        }

        public void RefreshProductUsage()
        {
            if (controller == null)
            {
                return;
            }

            foreach (var knownView in _knownUseInfoViews)
            {
                if (knownView != null)
                {
                    knownView.Hide();
                }
            }

            foreach (var snapshot in controller.ProductUsage)
            {
                var view = UseInfoView.FindFor(snapshot.Product);
                if (view == null)
                {
                    continue;
                }

                _knownUseInfoViews.Add(view);
                view.Bind(snapshot);
            }
        }

        private ResidentCardView GetOrCreateCard(int residentNumber)
        {
            if (_cardsByResident.TryGetValue(residentNumber, out var card) && card != null)
            {
                return card;
            }

            if (residentNumber == 1)
            {
                card = residentOneCard;
            }
            else if (residentCardPrefab != null)
            {
                card = Instantiate(residentCardPrefab, residentsArea);
            }

            if (card != null)
            {
                _cardsByResident[residentNumber] = card;
            }

            return card;
        }
    }
}
