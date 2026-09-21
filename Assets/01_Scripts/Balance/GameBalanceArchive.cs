using System;
using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Balance
{
    [Serializable]
    public struct DemandSheetRow {
        public string id;
        public bool enabled;
        public int requiredRoomCount;
        public int weight;
        public string firstNeed;
        public string secondNeed;
        public float satisfactionFillSeconds;
        public float patienceFillSeconds;
        public int experienceReward;
        public int globalSatisfactionOnSuccess;
        public int globalSatisfactionOnFailure;
        public float cooldownSeconds;
    }

    [Serializable]
    public struct RoomConfigSheetRow {
        public string roomConfigId;
        public bool enabled;
        public int minRoomCount;
        public int weight;
        public int widthWorld;
        public int heightWorld;
        public bool allowRotation;
        public int tvCount;
        public int fanCount;
        public int heaterCount;
        public int inductionCount;
        public int airConditionerCount;
        public int wallOutletSocketMin;
        public int wallOutletSocketMax;
    }

    [Serializable]
    public struct ProductRedistributionSheetRow {
        public int minRoomCount;
        public int maxRoomCount;
        public int minExtraProducts;
        public int maxExtraProducts;
        public string roomSelectionWeightFormula;
        public bool distinctRoomPerProduct;
        public string notes;
    }

    [Serializable]
    public struct ProductSpawnPoolSheetRow {
        public string productType;
        public bool enabled;
        public int minRoomCount;
        public int weight;
        public string notes;
    }

    [Serializable]
    public struct ProgressionSheetRow {
        public int roomCount;
        public int requiredEXP;
    }

    [Serializable]
    public struct RewardSheetRow {
        public string rewardId;
        public string targetType;
        public string effectType;
        public int effectValue;
        public int weight;
        public int minRoomCount;
    }

    [Serializable]
    public struct SocketSpawnSheetRow {
        public int socketCount;
        public int weight;
    }

    [Serializable]
    public struct StarterConfigSheetRow {
        public int starterRoomIndex;
        public bool enabled;
        public string productType;
        public int productCount;
        public int productPowerMin;
        public int productPowerMax;
        public int wallOutletCount;
        public int wallOutletSocketMin;
        public int wallOutletSocketMax;
        public string notes;
    }

    [CreateAssetMenu(fileName = "GameBalanceArchive", menuName = "Octoplug/Balance/Game Balance Archive")]
    public class GameBalanceArchive : ScriptableObject
    {
        public List<DemandSheetRow> DemandRows = new List<DemandSheetRow>();
        public List<RoomConfigSheetRow> RoomConfigRows = new List<RoomConfigSheetRow>();
        public List<ProductRedistributionSheetRow> ProductRedistributionRows = new List<ProductRedistributionSheetRow>();
        public List<ProductSpawnPoolSheetRow> ProductSpawnPoolRows = new List<ProductSpawnPoolSheetRow>();
        public List<ProgressionSheetRow> ProgressionRows = new List<ProgressionSheetRow>();
        public List<RewardSheetRow> RewardRows = new List<RewardSheetRow>();
        public List<SocketSpawnSheetRow> PowerStripSpawnRows = new List<SocketSpawnSheetRow>();
        public List<SocketSpawnSheetRow> WallOutletSpawnRows = new List<SocketSpawnSheetRow>();
        public List<StarterConfigSheetRow> StarterConfigRows = new List<StarterConfigSheetRow>();
    }
}
