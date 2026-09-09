using System.Collections.Generic;
using UnityEngine;

namespace Macedon.LayoutGreybox
{
    [DisallowMultipleComponent]
    [AddComponentMenu("World/Layout Greybox Builder")]
    public sealed class LayoutGreyboxBuilder : MonoBehaviour
    {
        [SerializeField] private TextAsset layoutJson;
        [Tooltip("Asset Contract ID 1: Palisade_Wall_3m (3m x 4m x 0.4m).")]
        [SerializeField] private GameObject palisadeWall3mPrefab;
        [SerializeField, HideInInspector] private Transform generatedRoot;
        [SerializeField, HideInInspector] private List<GameObject> generatedPathRoots = new();
        [SerializeField, HideInInspector] private List<GameObject> generatedInstances = new();

        public TextAsset LayoutJson => layoutJson;
        public GameObject PalisadeWall3mPrefab => palisadeWall3mPrefab;
        public Transform GeneratedRoot { get => generatedRoot; set => generatedRoot = value; }
        public List<GameObject> GeneratedPathRoots => generatedPathRoots;
        public List<GameObject> GeneratedInstances => generatedInstances;
    }
}
