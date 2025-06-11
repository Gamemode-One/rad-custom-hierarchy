using UnityEngine;

namespace Febucci.HierarchyData
{
    [CreateAssetMenu(fileName = "HierarchyData", menuName = "ScriptableObjects/HierarchyData")]
    public class HierarchyData : ScriptableObject, IHierarchyData
    {
        [SerializeField] private HierarchyDataProfile profile;

        public bool HasProfile => profile != null;
        public bool Enabled { get => profile.Enabled; set => profile.Enabled = value; }
        public bool UpdateInPlayMode { get => profile.UpdateInPlayMode; set => profile.UpdateInPlayMode = value; }
        public bool DrawActivationToggle { get => profile.DrawActivationToggle; set => profile.DrawActivationToggle = value; }
        public HierarchyDataProfile.IconsData Icons { get => profile.Icons; set => profile.Icons = value; }
        public HierarchyDataProfile.PrefabsData PrefabData { get => profile.PrefabData; set => profile.PrefabData = value; }
        public HierarchyDataProfile.AlternatingBGData AlternatingBackground { get => profile.AlternatingBackground; set => profile.AlternatingBackground = value; }
        public HierarchyDataProfile.SeparatorData Separator { get => profile.Separator; set => profile.Separator = value; }
        public HierarchyDataProfile.TreeData Tree { get => profile.Tree; set => profile.Tree = value; }

        private void OnValidate()
        {
            HierarchyDrawer.Initialize();
        }
    }
}