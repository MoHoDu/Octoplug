using System.Linq;
using NUnit.Framework;
using Octoplug.Power;
using Octoplug.RoomGeneration;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Octoplug.RoomGeneration.Tests
{
    public class StarterSetupVerificationTests
    {
        [Test]
        public void StarterSetup_Room1_HasFan_Room2_HasTV()
        {
            var starters = DefaultRoomContentBalance.GetStarterConfigs();
            var r1 = starters.FirstOrDefault(s => s.StarterRoomIndex == 1);
            var r2 = starters.FirstOrDefault(s => s.StarterRoomIndex == 2);

            Assert.IsNotNull(r1);
            Assert.IsNotNull(r2);

            Assert.AreEqual(1, r1.FanCount);
            Assert.AreEqual(0, r1.TvCount);
            Assert.AreEqual(0, r1.HeaterCount);
            Assert.AreEqual(0, r1.InductionCount);
            Assert.AreEqual(0, r1.AirConditionerCount);

            Assert.AreEqual(0, r2.FanCount);
            Assert.AreEqual(1, r2.TvCount);
            Assert.AreEqual(0, r2.HeaterCount);
            Assert.AreEqual(0, r2.InductionCount);
            Assert.AreEqual(0, r2.AirConditionerCount);
        }

        [Test]
        public void StarterPower_Validation()
        {
            var starters = DefaultRoomContentBalance.GetStarterConfigs();
            var r1 = starters.FirstOrDefault(s => s.StarterRoomIndex == 1);
            var r2 = starters.FirstOrDefault(s => s.StarterRoomIndex == 2);

            Assert.IsNotNull(r1);
            Assert.IsNotNull(r2);

#if UNITY_EDITOR
            var fanGuid = AssetDatabase.FindAssets("Fan t:GameObject").FirstOrDefault();
            var tvGuid = AssetDatabase.FindAssets("TV t:GameObject").FirstOrDefault();

            if (fanGuid != null)
            {
                var fanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(fanGuid));
                var fanSrc = fanPrefab.GetComponent<ApplianceSource>();
                // It should be within ProductPowerMin/Max (2~3)
                Assert.IsTrue(fanSrc.PowerConsumptionWatts >= r1.ProductPowerMin && fanSrc.PowerConsumptionWatts <= r1.ProductPowerMax, "Fan power exceeds starter bounds.");
            }

            if (tvGuid != null)
            {
                var tvPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(tvGuid));
                var tvSrc = tvPrefab.GetComponent<ApplianceSource>();
                // It should be within ProductPowerMin/Max (2~3)
                Assert.IsTrue(tvSrc.PowerConsumptionWatts >= r2.ProductPowerMin && tvSrc.PowerConsumptionWatts <= r2.ProductPowerMax, $"TV power ({tvSrc.PowerConsumptionWatts}) exceeds starter bounds ({r2.ProductPowerMin}~{r2.ProductPowerMax}).");
            }
#endif
        }
    }
}