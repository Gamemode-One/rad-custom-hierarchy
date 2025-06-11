using UnityEngine;
using static Febucci.HierarchyData.HierarchyDataProfile;

namespace Febucci.HierarchyData
{
    public interface IHierarchyData
    {
        public bool HasProfile { get; }
        public bool Enabled { get; set; }
        public bool UpdateInPlayMode { get; set; }
        public bool DrawActivationToggle { get; set; }
        public IconsData Icons { get; set; }
        public PrefabsData PrefabData { get; set; }
        public AlternatingBGData AlternatingBackground { get; set; }
        public SeparatorData Separator { get; set; }
        public TreeData Tree { get; set; }
    }
}