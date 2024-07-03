using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace EnemyListDebuffs
{
    [Serializable]
    public class EnemyListDebuffsPluginConfig : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        // General
        public bool Enabled = true;
        public int UpdateInterval = 100;

        // NodeGroup
        public int GroupX = -79;
        public int GroupY = 0;
        public int NodeSpacing = 0;
        public float Scale = 1;
        public bool FillFromRight = true;

        // Node
        public int IconX = 0;
        public int IconY = 0;
        public int IconWidth = 18;
        public int IconHeight = 24;
        public int DurationX = -2;
        public int DurationY = 16;
        public int FontSize = 12;
        public int DurationPadding = 2;
        public Vector4 DurationTextColor = new Vector4(1, 1, 1, 1);
        public Vector4 DurationEdgeColor = new Vector4(0, 0, 0, 1);

        public void SetDefaults()
        {
            // General
            Enabled = true;
            UpdateInterval = 100;

            // NodeGroup
            GroupX = -79;
            GroupY = 0;
            NodeSpacing = 0;
            Scale = 1;
            FillFromRight = true;

            // Node
            IconX = 0;
            IconY = 0;
            IconWidth = 18;
            IconHeight = 24;
            DurationX = -2;
            DurationY = 16;
            FontSize = 12;
            DurationPadding = 2;
            DurationTextColor.X = 1;
            DurationTextColor.Y = 1;
            DurationTextColor.Z = 1;
            DurationTextColor.W = 1;
            DurationEdgeColor.X = 0;
            DurationEdgeColor.Y = 0;
            DurationEdgeColor.Z = 0;
            DurationEdgeColor.W = 1;
        }

        [NonSerialized] private IDalamudPluginInterface _pluginInterface;

        public void Initialize(IDalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;
        }

        public void Save()
        {
            _pluginInterface.SavePluginConfig(this);
        }
    }
}
