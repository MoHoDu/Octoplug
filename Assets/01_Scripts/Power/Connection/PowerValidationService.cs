using UnityEngine;

namespace Octoplug.Power.Connection
{
    /// <summary>
    /// Computes current House/PowerStrip power usage from whichever
    /// products are currently <see cref="ApplianceSource.IsPowered"/>, and
    /// validates whether a new connection would exceed either budget.
    /// Deliberately recomputes from scratch every call instead of keeping a
    /// running total — with Powered as the single source of truth, there is
    /// no separate counter that can ever go stale on connect/disconnect.
    /// </summary>
    public static class PowerValidationService
    {
        /// <summary>Sum of <see cref="ApplianceSource.PowerConsumptionWatts"/> for every currently-Powered product in the scene.</summary>
        public static float GetHouseUsage()
        {
            var total = 0f;
#if UNITY_2023_1_OR_NEWER
            var products = Object.FindObjectsByType<ApplianceSource>(FindObjectsSortMode.None);
#else
            var products = Object.FindObjectsOfType<ApplianceSource>();
#endif
            foreach (var product in products)
            {
                if (product.IsPowered)
                {
                    total += product.PowerConsumptionWatts;
                }
            }

            return total;
        }

        /// <summary>Sum of <see cref="ApplianceSource.PowerConsumptionWatts"/> for every currently-Powered product plugged into one of <paramref name="strip"/>'s sockets.</summary>
        public static float GetStripUsage(PowerStrip strip)
        {
            if (strip == null)
            {
                return 0f;
            }

            var total = 0f;
#if UNITY_2023_1_OR_NEWER
            var products = Object.FindObjectsByType<ApplianceSource>(FindObjectsSortMode.None);
#else
            var products = Object.FindObjectsOfType<ApplianceSource>();
#endif
            foreach (var product in products)
            {
                if (!product.IsPowered || product.Cable == null || product.Cable.Plug == null)
                {
                    continue;
                }

                var connectedSocket = product.Cable.Plug.ConnectedSocket;
                if (connectedSocket == null)
                {
                    continue;
                }

                foreach (var socket in strip.Sockets)
                {
                    if (socket == connectedSocket)
                    {
                        total += product.PowerConsumptionWatts;
                        break;
                    }
                }
            }

            return total;
        }

        /// <summary>
        /// Whether connecting <paramref name="product"/> to
        /// <paramref name="targetSocket"/> would stay within budget:
        /// House always, and — if the socket belongs to a
        /// <see cref="PowerStrip"/> — that strip's own budget too. Neither
        /// side is reserved/mutated here; the caller applies
        /// <see cref="ApplianceSource.SetPowered"/> only after this returns
        /// true and the connection itself succeeds.
        /// </summary>
        public static bool TryValidate(ApplianceSource product, SocketConnector targetSocket, HousePowerBudget house, out string failureReason)
        {
            failureReason = null;

            if (product == null || targetSocket == null)
            {
                failureReason = "missing product or socket reference";
                return false;
            }

            if (house != null)
            {
                var projectedHouseUsage = GetHouseUsage() + product.PowerConsumptionWatts;
                if (projectedHouseUsage > house.AllowedPowerWatts)
                {
                    failureReason = $"House power limit exceeded ({projectedHouseUsage}W > {house.AllowedPowerWatts}W allowed)";
                    return false;
                }
            }

            var strip = targetSocket.GetComponentInParent<PowerStrip>();
            if (strip != null)
            {
                var projectedStripUsage = GetStripUsage(strip) + product.PowerConsumptionWatts;
                if (projectedStripUsage > strip.AllowedPowerWatts)
                {
                    failureReason = $"Power Strip limit exceeded ({projectedStripUsage}W > {strip.AllowedPowerWatts}W allowed)";
                    return false;
                }
            }

            return true;
        }
    }
}
