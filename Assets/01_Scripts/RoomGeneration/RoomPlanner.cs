using System;
using System.Collections.Generic;

namespace Octoplug.RoomGeneration
{
    /// <summary>Deterministic first-valid planner over caller-ordered room candidates.</summary>
    public static class RoomPlanner
    {
        public static RoomPlanResult PlanNext(
            RoomLayout layout,
            IReadOnlyList<RoomCandidate> orderedCandidates,
            DoorPlanningOptions options,
            IDoorPlacementPolicy doorPolicy)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (orderedCandidates == null) throw new ArgumentNullException(nameof(orderedCandidates));
            if (doorPolicy == null) throw new ArgumentNullException(nameof(doorPolicy));
            options.Validate();

            var rejections = new List<RoomCandidateRejectionRecord>();
            for (var candidateIndex = 0; candidateIndex < orderedCandidates.Count; candidateIndex++)
            {
                var candidate = orderedCandidates[candidateIndex];
                var placement = candidate.ToPlacement();

                if (layout.Contains(candidate.Id))
                {
                    Reject(RoomCandidateRejection.DuplicateRoomId);
                    continue;
                }

                if (OverlapsAny(placement, layout.Rooms))
                {
                    Reject(RoomCandidateRejection.OverlapsExistingRoom);
                    continue;
                }

                var sharedWalls = RoomGeometry.FindSharedWalls(placement, layout.Rooms);
                if (sharedWalls.Count == 0)
                {
                    Reject(RoomCandidateRejection.NoAdjacentRoom);
                    continue;
                }

                var opportunities = BuildOpportunities(placement, sharedWalls, layout, options);
                if (opportunities.Count == 0)
                {
                    Reject(RoomCandidateRejection.NoUsableSharedWall);
                    continue;
                }

                var proposals = doorPolicy.SelectDoors(opportunities);
                if (proposals == null)
                {
                    Reject(RoomCandidateRejection.InvalidDoorProposal);
                    continue;
                }

                if (!TryCreateDoorPlans(opportunities, proposals, options, out var doorPlans))
                {
                    Reject(RoomCandidateRejection.InvalidDoorProposal);
                    continue;
                }

                if (doorPlans.Count == 0)
                {
                    Reject(RoomCandidateRejection.NoDoorSelected);
                    continue;
                }

                var plan = new RoomPlan(placement, sharedWalls, doorPlans, layout.ValidationToken);
                return new RoomPlanResult(plan, candidateIndex, rejections.AsReadOnly());

                void Reject(RoomCandidateRejection reason)
                {
                    rejections.Add(new RoomCandidateRejectionRecord(candidateIndex, candidate.Id, reason));
                }
            }

            return new RoomPlanResult(null, -1, rejections.AsReadOnly());
        }

        private static bool OverlapsAny(RoomPlacement candidate, IReadOnlyList<RoomPlacement> rooms)
        {
            for (var i = 0; i < rooms.Count; i++)
            {
                if (candidate.Bounds.Overlaps(rooms[i].Bounds)) return true;
            }

            return false;
        }

        private static IReadOnlyList<DoorPlacementOpportunity> BuildOpportunities(
            RoomPlacement candidate,
            IReadOnlyList<SharedWall> sharedWalls,
            RoomLayout layout,
            DoorPlanningOptions options)
        {
            var opportunities = new List<DoorPlacementOpportunity>();
            for (var i = 0; i < sharedWalls.Count; i++)
            {
                var sharedWall = sharedWalls[i];
                if (layout.IsWallOccupied(sharedWall.AdjacentWall)) continue;

                var intervals = BuildSafeIntervals(candidate, sharedWall, layout.Rooms, options);
                if (intervals.Count > 0)
                {
                    opportunities.Add(new DoorPlacementOpportunity(sharedWall, intervals));
                }
            }

            return opportunities.AsReadOnly();
        }

        private static IReadOnlyList<CoordinateInterval> BuildSafeIntervals(
            RoomPlacement candidate,
            SharedWall sharedWall,
            IReadOnlyList<RoomPlacement> existingRooms,
            DoorPlanningOptions options)
        {
            var clearance = options.DoorWidth * 0.5f + options.SafetyMargin;
            var baseMin = sharedWall.Span.Start + clearance;
            var baseMax = sharedWall.Span.End - clearance;
            if (baseMax < baseMin) return Array.Empty<CoordinateInterval>();

            var forbiddenPoints = new List<float>();
            AddInteriorCorners(candidate.Bounds, sharedWall.Span, forbiddenPoints);
            for (var i = 0; i < existingRooms.Count; i++)
            {
                AddInteriorCorners(existingRooms[i].Bounds, sharedWall.Span, forbiddenPoints);
            }

            forbiddenPoints.Sort();
            var intervals = new List<CoordinateInterval>();
            var cursor = baseMin;
            for (var i = 0; i < forbiddenPoints.Count; i++)
            {
                if (i > 0 && forbiddenPoints[i] == forbiddenPoints[i - 1]) continue;
                var point = forbiddenPoints[i];
                var before = point - clearance;
                var after = point + clearance;
                if (before >= cursor)
                {
                    intervals.Add(new CoordinateInterval(cursor, Math.Min(before, baseMax)));
                }

                cursor = Math.Max(cursor, after);
                if (cursor > baseMax) break;
            }

            if (cursor <= baseMax)
            {
                intervals.Add(new CoordinateInterval(cursor, baseMax));
            }

            if (options.ExtraBlockedIntervals != null)
            {
                var extraBlocks = new List<CoordinateInterval>();
                if (options.ExtraBlockedIntervals.TryGetValue(sharedWall.AdjacentWall, out var adjacentBlocks))
                {
                    extraBlocks.AddRange(adjacentBlocks);
                }
                if (options.ExtraBlockedIntervals.TryGetValue(sharedWall.CandidateWall, out var candidateBlocks))
                {
                    extraBlocks.AddRange(candidateBlocks);
                }

                if (extraBlocks.Count > 0)
                {
                    intervals = SubtractIntervals(intervals, extraBlocks, clearance);
                }
            }

            return intervals.AsReadOnly();
        }

        private static List<CoordinateInterval> SubtractIntervals(
            List<CoordinateInterval> safeIntervals,
            List<CoordinateInterval> extraBlocks,
            float clearance)
        {
            var result = new List<CoordinateInterval>();
            foreach (var safe in safeIntervals)
            {
                var currentPieces = new List<CoordinateInterval> { safe };
                foreach (var block in extraBlocks)
                {
                    var blockStart = block.Min - clearance;
                    var blockEnd = block.Max + clearance;

                    var nextPieces = new List<CoordinateInterval>();
                    foreach (var piece in currentPieces)
                    {
                        if (blockEnd <= piece.Min || blockStart >= piece.Max)
                        {
                            nextPieces.Add(piece); // No overlap
                        }
                        else
                        {
                            // Overlap. Keep the parts outside the block
                            if (piece.Min < blockStart)
                            {
                                nextPieces.Add(new CoordinateInterval(piece.Min, blockStart));
                            }
                            if (piece.Max > blockEnd)
                            {
                                nextPieces.Add(new CoordinateInterval(blockEnd, piece.Max));
                            }
                        }
                    }
                    currentPieces = nextPieces;
                }
                result.AddRange(currentPieces);
            }
            return result;
        }

        private static void AddInteriorCorners(RoomBounds2D bounds, WallSpan wall, ICollection<float> result)
        {
            if (wall.Orientation == WallOrientation.Vertical)
            {
                if (bounds.MinX != wall.FixedCoordinate && bounds.MaxX != wall.FixedCoordinate) return;
                AddIfInterior(bounds.MinY);
                AddIfInterior(bounds.MaxY);
            }
            else
            {
                if (bounds.MinY != wall.FixedCoordinate && bounds.MaxY != wall.FixedCoordinate) return;
                AddIfInterior(bounds.MinX);
                AddIfInterior(bounds.MaxX);
            }

            void AddIfInterior(float coordinate)
            {
                if (coordinate > wall.Start && coordinate < wall.End) result.Add(coordinate);
            }
        }

        private static bool TryCreateDoorPlans(
            IReadOnlyList<DoorPlacementOpportunity> opportunities,
            IReadOnlyList<DoorPlacementProposal> proposals,
            DoorPlanningOptions options,
            out IReadOnlyList<DoorPlan> doorPlans)
        {
            var result = new List<DoorPlan>();
            var usedOpportunities = new HashSet<int>();
            var occupiedWalls = new HashSet<RoomWallId>();

            for (var i = 0; i < proposals.Count; i++)
            {
                var proposal = proposals[i];
                if (proposal.OpportunityIndex < 0 || proposal.OpportunityIndex >= opportunities.Count
                    || !usedOpportunities.Add(proposal.OpportunityIndex)
                    || float.IsNaN(proposal.CenterCoordinate) || float.IsInfinity(proposal.CenterCoordinate))
                {
                    doorPlans = null;
                    return false;
                }

                var opportunity = opportunities[proposal.OpportunityIndex];
                if (!Contains(opportunity.SafeCenterIntervals, proposal.CenterCoordinate)
                    || !occupiedWalls.Add(opportunity.SharedWall.CandidateWall)
                    || !occupiedWalls.Add(opportunity.SharedWall.AdjacentWall))
                {
                    doorPlans = null;
                    return false;
                }

                result.Add(new DoorPlan(opportunity.SharedWall, proposal.CenterCoordinate, options.DoorWidth));
            }

            doorPlans = result.AsReadOnly();
            return true;
        }

        private static bool Contains(IReadOnlyList<CoordinateInterval> intervals, float value)
        {
            for (var i = 0; i < intervals.Count; i++)
            {
                if (intervals[i].Contains(value)) return true;
            }

            return false;
        }
    }
}
