using System.Collections.Generic;
using UnityEngine;

namespace Octoplug.Power.Connection
{
    /// <summary>
    /// Recomputes power-graph usage and validates prospective connections.
    /// No running totals are cached, so disconnects and nested strip changes
    /// cannot leave stale accounting behind.
    /// </summary>
    public static class PowerValidationService
    {
        /// <summary>
        /// Product load physically connected to a House socket, independent of
        /// the products' current Powered state. Disconnected strip branches do
        /// not consume House capacity until their upstream Plug is connected.
        /// </summary>
        public static float GetHouseUsage()
        {
            var products = new HashSet<ApplianceSource>();
            CollectHouseProducts(null, null, null, products);
            return SumUsage(products);
        }

        /// <summary>Physically connected product load in the strip's complete downstream branch.</summary>
        public static float GetStripUsage(PowerStrip strip)
        {
            var products = new HashSet<ApplianceSource>();
            CollectDownstreamProducts(
                strip,
                null,
                null,
                null,
                new HashSet<PowerStrip>(),
                products);
            return SumUsage(products);
        }

        /// <summary>
        /// Validates House and every upstream PowerStrip budget for either a
        /// Product or a PowerStrip being connected. A strip contributes its
        /// complete physically-connected downstream branch, even while that
        /// branch is currently unpowered.
        /// </summary>
        public static bool TryValidate(
            ApplianceSource product,
            PowerStrip connectingStrip,
            SocketConnector targetSocket,
            HousePowerBudget house,
            out ConnectionFailureReason failureReason)
        {
            failureReason = ConnectionFailureReason.None;
            if (targetSocket == null || (product == null && connectingStrip == null))
            {
                return false;
            }

            if (house != null)
            {
                var prospectiveHouseProducts = new HashSet<ApplianceSource>();
                CollectHouseProducts(
                    product,
                    connectingStrip,
                    targetSocket,
                    prospectiveHouseProducts);
                if (SumUsage(prospectiveHouseProducts) > house.AllowedPowerWatts)
                {
                    failureReason = ConnectionFailureReason.HousePowerExceeded;
                    return false;
                }
            }

            if (connectingStrip != null
                && GetStripUsage(connectingStrip)
                    > connectingStrip.AllowedPowerWatts)
            {
                failureReason =
                    ConnectionFailureReason.PowerStripPowerExceeded;
                return false;
            }

            var upstream = targetSocket.GetComponentInParent<PowerStrip>();
            var visitedAncestors = new HashSet<PowerStrip>();
            while (upstream != null && visitedAncestors.Add(upstream))
            {
                var prospectiveStripProducts = new HashSet<ApplianceSource>();
                CollectDownstreamProducts(
                    upstream,
                    product,
                    connectingStrip,
                    targetSocket,
                    new HashSet<PowerStrip>(),
                    prospectiveStripProducts);
                if (SumUsage(prospectiveStripProducts) > upstream.AllowedPowerWatts)
                {
                    failureReason = ConnectionFailureReason.PowerStripPowerExceeded;
                    return false;
                }

                upstream = GetUpstreamStrip(upstream);
            }

            return true;
        }

        /// <summary>Compatibility overload for Product-only callers.</summary>
        public static bool TryValidate(
            ApplianceSource product,
            SocketConnector targetSocket,
            HousePowerBudget house,
            out ConnectionFailureReason failureReason)
        {
            return TryValidate(product, null, targetSocket, house, out failureReason);
        }

        public static bool TryValidateGraphConnection(
            PowerStrip connectingStrip,
            SocketConnector targetSocket,
            out ConnectionFailureReason failureReason)
        {
            failureReason = ConnectionFailureReason.None;
            if (connectingStrip == null || targetSocket == null)
            {
                return true;
            }

            var targetStrip = targetSocket.GetComponentInParent<PowerStrip>();
            if (targetStrip == null)
            {
                return true;
            }

            if (targetStrip == connectingStrip)
            {
                failureReason = ConnectionFailureReason.SelfConnection;
                return false;
            }

            if (CanReachDownstream(connectingStrip, targetStrip, new HashSet<PowerStrip>()))
            {
                failureReason = ConnectionFailureReason.CircularConnection;
                return false;
            }

            return true;
        }

        public static bool IsSocketSourceLive(SocketConnector socket)
        {
            if (socket == null)
            {
                return false;
            }

            var strip = socket.GetComponentInParent<PowerStrip>();
            return strip == null || strip.IsPowered;
        }

        private static void CollectHouseProducts(
            ApplianceSource proposedProduct,
            PowerStrip proposedStrip,
            SocketConnector proposedTarget,
            HashSet<ApplianceSource> products)
        {
#if UNITY_2023_1_OR_NEWER
            var allProducts = Object.FindObjectsByType<ApplianceSource>(FindObjectsSortMode.None);
            var allStrips = Object.FindObjectsByType<PowerStrip>(FindObjectsSortMode.None);
#else
            var allProducts = Object.FindObjectsOfType<ApplianceSource>();
            var allStrips = Object.FindObjectsOfType<PowerStrip>();
#endif
            foreach (var product in allProducts)
            {
                if (product == null || product == proposedProduct)
                {
                    continue;
                }

                var plug = product.Cable != null ? product.Cable.Plug : null;
                var socket = plug != null ? plug.ConnectedSocket : null;
                if (socket != null && socket.GetComponentInParent<PowerStrip>() == null)
                {
                    products.Add(product);
                }
            }

            var visitedStrips = new HashSet<PowerStrip>();
            foreach (var strip in allStrips)
            {
                if (strip == null || strip == proposedStrip || GetUpstreamStrip(strip) != null)
                {
                    continue;
                }

                var plug = strip.Cable != null ? strip.Cable.Plug : null;
                var socket = plug != null ? plug.ConnectedSocket : null;
                if (socket != null && socket.GetComponentInParent<PowerStrip>() == null)
                {
                    CollectDownstreamProducts(
                        strip,
                        proposedProduct,
                        proposedStrip,
                        proposedTarget,
                        new HashSet<PowerStrip>(),
                        products);
                }
            }

            if (proposedTarget != null
                && proposedTarget.GetComponentInParent<PowerStrip>() == null)
            {
                if (proposedProduct != null)
                {
                    products.Add(proposedProduct);
                }
                else
                {
                    CollectDownstreamProducts(
                        proposedStrip,
                        null,
                        proposedStrip,
                        proposedTarget,
                        new HashSet<PowerStrip>(),
                        products);
                }
            }
        }

        private static void CollectDownstreamProducts(
            PowerStrip strip,
            ApplianceSource proposedProduct,
            PowerStrip proposedStrip,
            SocketConnector proposedTarget,
            HashSet<PowerStrip> visitedStrips,
            HashSet<ApplianceSource> products)
        {
            if (strip == null || !visitedStrips.Add(strip))
            {
                return;
            }

            foreach (var socket in strip.Sockets)
            {
                if (socket == null)
                {
                    continue;
                }

                var plug = socket.ConnectedPlug;
                if (plug != null)
                {
                    var product = plug.GetComponentInParent<ApplianceSource>();
                    var childStrip = product == null
                        ? plug.GetComponentInParent<PowerStrip>()
                        : null;
                    // Bug fix (found while wiring the real, event-driven Power
                    // UI binding): a plain query (GetHouseUsage/GetStripUsage)
                    // calls this with proposedProduct/proposedStrip both null,
                    // so the old `product == proposedProduct` / `childStrip ==
                    // proposedStrip` comparisons degenerated to `null == null`
                    // — always true — and every genuinely-connected downstream
                    // product (or deeper nested strip) was silently treated as
                    // "the edge being replaced" and excluded. Guarding each
                    // half on its own proposed reference actually being set
                    // preserves every existing prospective-validation call site
                    // (TryValidate/TryValidateGraphConnection) exactly as
                    // before, since those always pass a real, non-null
                    // proposedProduct or proposedStrip.
                    var edgeIsBeingReplaced = socket != proposedTarget
                        && ((proposedProduct != null && product == proposedProduct)
                            || (proposedStrip != null && childStrip == proposedStrip));
                    if (!edgeIsBeingReplaced)
                    {
                        if (product != null)
                        {
                            products.Add(product);
                        }
                        else
                        {
                            CollectDownstreamProducts(
                                childStrip,
                                proposedProduct,
                                proposedStrip,
                                proposedTarget,
                                visitedStrips,
                                products);
                        }
                    }
                }

                if (socket != proposedTarget)
                {
                    continue;
                }

                if (proposedProduct != null)
                {
                    products.Add(proposedProduct);
                }
                else
                {
                    CollectDownstreamProducts(
                        proposedStrip,
                        null,
                        proposedStrip,
                        proposedTarget,
                        visitedStrips,
                        products);
                }
            }
        }

        private static float SumUsage(HashSet<ApplianceSource> products)
        {
            var total = 0f;
            foreach (var product in products)
            {
                if (product != null)
                {
                    total += product.PowerConsumptionWatts;
                }
            }

            return total;
        }

        private static bool CanReachDownstream(PowerStrip from, PowerStrip target, HashSet<PowerStrip> visited)
        {
            if (from == null || !visited.Add(from))
            {
                return false;
            }

            if (from == target)
            {
                return true;
            }

            foreach (var socket in from.Sockets)
            {
                var plug = socket != null ? socket.ConnectedPlug : null;
                var child = plug != null ? plug.GetComponentInParent<PowerStrip>() : null;
                if (child != null && CanReachDownstream(child, target, visited))
                {
                    return true;
                }
            }

            return false;
        }

        private static PowerStrip GetUpstreamStrip(PowerStrip strip)
        {
            var plug = strip != null && strip.Cable != null ? strip.Cable.Plug : null;
            var socket = plug != null ? plug.ConnectedSocket : null;
            return socket != null ? socket.GetComponentInParent<PowerStrip>() : null;
        }
    }
}
