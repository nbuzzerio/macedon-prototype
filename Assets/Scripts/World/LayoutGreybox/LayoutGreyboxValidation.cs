using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Macedon.LayoutGreybox
{
    public sealed class LayoutValidationIssue
    {
        public string Code { get; }
        public string Message { get; }
        public string PathId { get; }

        public LayoutValidationIssue(string code, string message, string pathId = null)
        { Code = code; Message = message; PathId = pathId; }
    }

    public sealed class LayoutValidationResult
    {
        public LayoutV1 Layout { get; internal set; }
        public List<LayoutValidationIssue> Errors { get; } = new();
        public List<LayoutValidationIssue> GenerationBlockers { get; } = new();
        public bool IsValid => Layout != null && Errors.Count == 0;
        public bool CanGenerate => IsValid && GenerationBlockers.Count == 0;
    }

    public readonly struct ModularFitResult
    {
        public readonly bool Exact;
        public readonly double? LowerMeters;
        public readonly double? HigherMeters;

        public ModularFitResult(bool exact, double? lowerMeters, double? higherMeters)
        { Exact = exact; LowerMeters = lowerMeters; HigherMeters = higherMeters; }
    }

    public static class LayoutGreyboxValidator
    {
        public const int MillimetersPerMeter = 1000;
        private const double MillimeterToleranceMeters = 0.001;
        private const string FoundationLevel = "FOUNDATION_LEVEL";
        private const string TerrainConforming = "TERRAIN_CONFORMING";

        public static LayoutValidationResult ParseAndValidate(string json, IReadOnlyList<LayoutAssetContract> contracts)
        {
            var result = new LayoutValidationResult();
            if (string.IsNullOrWhiteSpace(json))
            {
                result.Errors.Add(Issue("JSON_EMPTY", "Assign a non-empty Layout JSON TextAsset."));
                return result;
            }

            try { result.Layout = JsonUtility.FromJson<LayoutV1>(json); }
            catch (Exception exception)
            {
                result.Errors.Add(Issue("JSON_PARSE", $"Layout JSON could not be parsed: {exception.Message}"));
                return result;
            }

            Validate(result, contracts);
            return result;
        }

        public static ModularFitResult GetModularFit(double requestedMeters, IEnumerable<double> widthsMeters)
        {
            if (!Finite(requestedMeters) || requestedMeters <= 0 || requestedMeters > 10000)
                return new ModularFitResult(false, null, null);
            int requestedMm = (int)Math.Round(requestedMeters * MillimetersPerMeter);
            int[] widths = widthsMeters.Where(width => Finite(width) && width > 0)
                .Select(width => (int)Math.Round(width * MillimetersPerMeter)).Where(width => width > 0)
                .Distinct().OrderByDescending(value => value).ToArray();
            if (requestedMm <= 0 || widths.Length == 0) return new ModularFitResult(false, null, null);
            int limit = checked(requestedMm + widths.Max());
            var reachable = new bool[limit + 1];
            reachable[0] = true;
            for (int length = 1; length <= limit; length++)
                foreach (int width in widths)
                    if (length >= width && reachable[length - width]) { reachable[length] = true; break; }

            int lower = Math.Min(requestedMm, limit);
            while (lower > 0 && !reachable[lower]) lower--;
            int higher = Math.Max(1, requestedMm);
            while (higher <= limit && !reachable[higher]) higher++;
            bool precise = Math.Abs(requestedMeters - requestedMm / (double)MillimetersPerMeter) <= MillimeterToleranceMeters;
            return new ModularFitResult(precise && reachable[requestedMm], lower > 0 ? lower / 1000d : null,
                higher <= limit ? higher / 1000d : null);
        }

        public static bool TryGetModuleSequence(double requestedMeters, IReadOnlyList<LayoutAssetContract> family,
            out List<LayoutAssetContract> sequence)
        {
            sequence = new List<LayoutAssetContract>();
            if (!Finite(requestedMeters) || requestedMeters <= 0 || requestedMeters > 10000) return false;
            int target = (int)Math.Round(requestedMeters * MillimetersPerMeter);
            var modules = family.Where(contract => contract.PathRole == LayoutPathRole.Segment && contract.WidthMeters > 0)
                .OrderByDescending(contract => contract.WidthMeters).ThenBy(contract => contract.Id).ToArray();
            if (target <= 0) return false;
            var previous = Enumerable.Repeat(-1, target + 1).ToArray();
            previous[0] = 0;
            for (int length = 1; length <= target; length++)
                for (int index = 0; index < modules.Length; index++)
                {
                    int width = (int)Math.Round(modules[index].WidthMeters * MillimetersPerMeter);
                    if (length >= width && previous[length - width] >= 0) { previous[length] = index; break; }
                }
            if (previous[target] < 0) return false;
            for (int remaining = target; remaining > 0;)
            {
                LayoutAssetContract module = modules[previous[remaining]];
                sequence.Add(module);
                remaining -= (int)Math.Round(module.WidthMeters * MillimetersPerMeter);
            }
            return true;
        }

        private static void Validate(LayoutValidationResult result, IReadOnlyList<LayoutAssetContract> contracts)
        {
            LayoutV1 layout = result.Layout;
            if (layout == null) { result.Errors.Add(Issue("JSON_ROOT", "Layout JSON root is invalid.")); return; }
            if (layout.formatVersion != 1) result.Errors.Add(Issue("FORMAT_VERSION", "formatVersion must be 1."));
            if (layout.units != "METERS") result.Errors.Add(Issue("UNITS", "units must be METERS."));
            if (string.IsNullOrWhiteSpace(layout.name)) result.Errors.Add(Issue("NAME", "Layout name is required."));
            if (layout.grid == null || !Finite(layout.grid.spacingMeters) || layout.grid.spacingMeters <= 0) result.Errors.Add(Issue("GRID", "grid.spacingMeters must be finite and greater than zero."));
            if (layout.anchor == null || layout.anchor.position == null || !Finite(layout.anchor.position.x) ||
                !Finite(layout.anchor.position.y) || !Finite(layout.anchor.position.z) || !Finite(layout.anchor.rotationDegrees))
                result.Errors.Add(Issue("ANCHOR", "Anchor position and rotation must contain finite values."));
            if (!ValidMode(layout.defaultPlacementMode)) result.Errors.Add(Issue("PLACEMENT_MODE", "defaultPlacementMode must be FOUNDATION_LEVEL or TERRAIN_CONFORMING."));
            if (layout.deterministicSeed < 0 || layout.deterministicSeed > uint.MaxValue) result.Errors.Add(Issue("SEED", "deterministicSeed must be from 0 through 4294967295."));
            if (layout.paths == null) { result.Errors.Add(Issue("PATHS", "paths must be present.")); return; }
            if (layout.overlays == null) result.Errors.Add(Issue("OVERLAYS", "overlays must be present (use an empty array in V1)."));
            else if (layout.overlays.Length > 0) result.GenerationBlockers.Add(Issue("OVERLAYS_UNSUPPORTED", "Overlays are reserved but not supported for V1 generation."));

            var byId = contracts.GroupBy(contract => contract.Id).ToDictionary(group => group.Key, group => group.ToArray());
            foreach (var duplicate in byId.Where(pair => pair.Value.Length > 1)) result.Errors.Add(Issue("CONTRACT_ID_DUPLICATE", $"Registry contract ID {duplicate.Key} is duplicated."));
            if (!byId.TryGetValue(0, out LayoutAssetContract[] empty) || empty[0].PathRole != LayoutPathRole.Empty)
                result.Errors.Add(Issue("CONTRACT_EMPTY", "Registry ID 0 must remain reserved for Empty."));

            var pathIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (LayoutPath path in layout.paths)
            {
                if (path == null) { result.Errors.Add(Issue("PATH_NULL", "paths contains a null entry.")); continue; }
                if (string.IsNullOrWhiteSpace(path.id)) result.Errors.Add(Issue("PATH_ID", "Every path requires a non-empty ID."));
                else if (!pathIds.Add(path.id)) result.Errors.Add(Issue("PATH_ID_DUPLICATE", $"Path ID '{path.id}' is duplicated.", path.id));
                if (!byId.TryGetValue(path.assetContractId, out LayoutAssetContract[] matches) || matches.Length != 1 || path.assetContractId == 0)
                { result.Errors.Add(Issue("PATH_CONTRACT", $"Path '{path.id}' references unknown or invalid contract ID {path.assetContractId}.", path.id)); continue; }
                LayoutAssetContract contract = matches[0];
                if (contract.PathRole != LayoutPathRole.Segment)
                { result.Errors.Add(Issue("PATH_ROLE", $"Path '{path.id}' must reference a SEGMENT contract.", path.id)); continue; }
                if (path.points == null || path.points.Length != 2 || path.points.Any(point => point == null || !Finite(point.x) || !Finite(point.z)))
                { result.Errors.Add(Issue("PATH_GEOMETRY", $"Path '{path.id}' must contain exactly two finite local X/Z points.", path.id)); continue; }
                double length = Distance(path.points[0], path.points[1]);
                if (length <= 0) { result.Errors.Add(Issue("PATH_LENGTH", $"Path '{path.id}' must have positive length.", path.id)); continue; }
                string mode = string.IsNullOrWhiteSpace(path.placementMode) ? layout.defaultPlacementMode : path.placementMode;
                if (!ValidMode(mode)) result.Errors.Add(Issue("PATH_PLACEMENT_MODE", $"Path '{path.id}' has an invalid placementMode.", path.id));
                else if (mode == TerrainConforming)
                {
                    if (!contract.TerrainConformingAllowed) result.Errors.Add(Issue("TERRAIN_NOT_ALLOWED", $"{contract.DisplayName} does not allow terrain-conforming placement.", path.id));
                    else result.GenerationBlockers.Add(Issue("TERRAIN_UNSUPPORTED", $"Path '{path.id}' requests TERRAIN_CONFORMING, which is not yet supported for generation.", path.id));
                }
                double[] widths = contracts.Where(candidate => candidate.StructuralFamilyKey == contract.StructuralFamilyKey && candidate.PathRole == LayoutPathRole.Segment).Select(candidate => candidate.WidthMeters).ToArray();
                ModularFitResult fit = GetModularFit(length, widths);
                if (!fit.Exact) result.Errors.Add(Issue("MODULAR_FIT", $"Path '{path.id}' requested {Meters(length)}. Exact fit unavailable. Nearest lower valid length: {Meters(fit.LowerMeters)}. Nearest higher valid length: {Meters(fit.HigherMeters)}.", path.id));
            }
        }

        private static bool ValidMode(string mode) => mode == FoundationLevel || mode == TerrainConforming;
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static double Distance(LayoutPoint2 a, LayoutPoint2 b) => Math.Sqrt((b.x - a.x) * (b.x - a.x) + (b.z - a.z) * (b.z - a.z));
        private static string Meters(double? value) => value.HasValue ? $"{value.Value:0.###}m" : "none";
        private static LayoutValidationIssue Issue(string code, string message, string pathId = null) => new(code, message, pathId);
    }
}
