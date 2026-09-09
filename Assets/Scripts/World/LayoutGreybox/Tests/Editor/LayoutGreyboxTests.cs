using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Utils;

namespace Macedon.LayoutGreybox.Tests
{
    public sealed class LayoutGreyboxTests
    {
        private static readonly LayoutAssetContract[] Contracts = LayoutGreyboxContractRegistry.Create(null).ToArray();

        [Test]
        public void ParsesAndValidatesLayoutV1()
        {
            LayoutValidationResult result = Validate(PathJson("a", 1, 0, 0, 6, 0));
            Assert.That(result.IsValid, Is.True, Issues(result));
            Assert.That(result.Layout.name, Is.EqualTo("Test Fort"));
            Assert.That(result.Layout.anchor.position.z, Is.EqualTo(20));
        }

        [Test]
        public void MissingAnchorCoordinateDoesNotSilentlyBecomeZero()
        {
            string json = PathJson("a", 1, 0, 0, 3, 0).Replace("\"x\":10,", "");
            LayoutValidationResult result = Validate(json);
            Assert.That(result.Errors.Any(item => item.Code == "ANCHOR"), Is.True);
        }

        [Test]
        public void ScaleGuardRestoresOnlyUnitScale()
        {
            var gameObject = new GameObject("Layout Greybox Scale Guard Test");
            try
            {
                LayoutGreyboxBuilder builder = gameObject.AddComponent<LayoutGreyboxBuilder>();
                Vector3 position = new(4, 5, 6);
                Quaternion rotation = Quaternion.Euler(10, 20, 30);
                gameObject.transform.SetPositionAndRotation(position, rotation);
                gameObject.transform.localScale = new Vector3(2, 3, 4);

                bool corrected = Editor.LayoutGreyboxScaleGuard.Enforce(builder);

                Assert.That(corrected, Is.True);
                Assert.That(gameObject.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(gameObject.transform.position, Is.EqualTo(position));
                Assert.That(gameObject.transform.rotation, Is.EqualTo(rotation).Using(QuaternionEqualityComparer.Instance));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void CoordinatesAndAnchorMapDirectlyToUnityLocalXZ()
        {
            LayoutValidationResult result = Validate(PathJson("north", 1, 0, 0, 0, 6));
            var placements = LayoutGreyboxPlacement.Build(result.Layout, Contracts);
            Assert.That(placements.Select(item => item.LocalPosition), Is.EqualTo(new[]
            {
                new Vector3(0, 0, 1.5f), new Vector3(0, 0, 4.5f)
            }));
            Assert.That(placements[0].LocalRotation * Vector3.right, Is.EqualTo(Vector3.forward).Using(Vector3ComparerWithEqualsOperator.Instance));
            Assert.That(LayoutGreyboxPlacement.AnchorLocalPosition(result.Layout), Is.EqualTo(new Vector3(10, 2, 20)));
        }

        [Test]
        public void PositiveNinetyDegreeAnchorRotationTurnsRightTowardPositiveZ()
        {
            LayoutValidationResult result = Validate(PathJson("east", 1, 0, 0, 3, 0).Replace("\"rotationDegrees\":0", "\"rotationDegrees\":90"));
            Vector3 rotatedRight = LayoutGreyboxPlacement.AnchorLocalRotation(result.Layout) * Vector3.right;
            Assert.That(rotatedRight, Is.EqualTo(Vector3.forward).Using(Vector3ComparerWithEqualsOperator.Instance));
        }

        [Test]
        public void SixMetersFitsTwoThreeMeterModulesAtDeterministicCenters()
        {
            LayoutValidationResult result = Validate(PathJson("east", 1, 0, 0, 6, 0));
            Assert.That(result.CanGenerate, Is.True, Issues(result));
            var placements = LayoutGreyboxPlacement.Build(result.Layout, Contracts);
            Assert.That(placements.Count, Is.EqualTo(2));
            Assert.That(placements[0].LocalPosition, Is.EqualTo(new Vector3(1.5f, 0, 0)));
            Assert.That(placements[1].LocalPosition, Is.EqualTo(new Vector3(4.5f, 0, 0)));
            Assert.That(placements.All(item => item.Contract.Id == 1), Is.True);
        }

        [Test]
        public void ElevenMetersReportsNearestNineAndTwelve()
        {
            LayoutValidationResult result = Validate(PathJson("bad", 1, 0, 0, 11, 0));
            LayoutValidationIssue issue = result.Errors.Single(item => item.Code == "MODULAR_FIT");
            StringAssert.Contains("Nearest lower valid length: 9m", issue.Message);
            StringAssert.Contains("Nearest higher valid length: 12m", issue.Message);
        }

        [Test]
        public void SubMillimeterRunCannotValidateAsAZeroModuleFit()
        {
            LayoutValidationResult result = Validate(PathJson("tiny", 1, 0, 0, 0.0004, 0));
            Assert.That(result.Errors.Any(item => item.Code == "MODULAR_FIT"), Is.True);
        }

        [Test]
        public void RejectsNonSegmentPathContract()
        {
            var contracts = Contracts.Concat(new[]
            {
                new LayoutAssetContract(2, "Palisade_Gate", "Palisade Gate", "Palisade", "Palisade_Wall",
                    LayoutPathRole.Gate, 3, 4, 0.4, true, null)
            }).ToArray();
            LayoutValidationResult result = LayoutGreyboxValidator.ParseAndValidate(PathJson("gate", 2, 0, 0, 3, 0), contracts);
            Assert.That(result.Errors.Any(item => item.Code == "PATH_ROLE"), Is.True);
        }

        [Test]
        public void RejectsDuplicatePathIds()
        {
            string paths = "{\"id\":\"same\",\"assetContractId\":1,\"points\":[{\"x\":0,\"z\":0},{\"x\":3,\"z\":0}]}," +
                           "{\"id\":\"same\",\"assetContractId\":1,\"points\":[{\"x\":0,\"z\":3},{\"x\":3,\"z\":3}]}";
            LayoutValidationResult result = Validate(LayoutJson(paths));
            Assert.That(result.Errors.Any(item => item.Code == "PATH_ID_DUPLICATE"), Is.True);
        }

        [Test]
        public void RejectsUnknownContractId()
        {
            LayoutValidationResult result = Validate(PathJson("unknown", 999, 0, 0, 3, 0));
            Assert.That(result.Errors.Any(item => item.Code == "PATH_CONTRACT"), Is.True);
        }

        private static LayoutValidationResult Validate(string json) => LayoutGreyboxValidator.ParseAndValidate(json, Contracts);
        private static string PathJson(string id, int contractId, double x1, double z1, double x2, double z2) =>
            LayoutJson($"{{\"id\":\"{id}\",\"assetContractId\":{contractId},\"points\":[{{\"x\":{x1},\"z\":{z1}}},{{\"x\":{x2},\"z\":{z2}}}]}}");
        private static string LayoutJson(string paths) =>
            $"{{\"formatVersion\":1,\"name\":\"Test Fort\",\"units\":\"METERS\",\"grid\":{{\"spacingMeters\":1}},\"anchor\":{{\"position\":{{\"x\":10,\"y\":2,\"z\":20}},\"rotationDegrees\":0}},\"defaultPlacementMode\":\"FOUNDATION_LEVEL\",\"deterministicSeed\":7,\"paths\":[{paths}],\"overlays\":[]}}";
        private static string Issues(LayoutValidationResult result) => string.Join(" | ", result.Errors.Concat(result.GenerationBlockers).Select(item => item.Message));
    }
}
