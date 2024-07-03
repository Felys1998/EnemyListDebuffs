using Dalamud.Hooking;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EnemyListDebuffs.StatusNode
{
    public unsafe class StatusNodeManager : IDisposable
    {
        private EnemyListDebuffsPlugin _plugin;

        private AddonEnemyList* enemyListAddon;

        private StatusNodeGroup[] NodeGroups;

        private ExcelSheet<Status> StatusSheet;

        private static uint StartingNodeId = 50000;

        public bool Built { get; private set; }

        internal StatusNodeManager(EnemyListDebuffsPlugin p)
        {
            _plugin = p; 

            NodeGroups = new StatusNodeGroup[AddonEnemyList.MaxEnemyCount];

            StatusSheet = _plugin.DataManager.GetExcelSheet<Status>();
        }

        public void Dispose()
        {
            DestroyNodes();
        }

        public void SetEnemyListAddonPointer(AddonEnemyList* addon)
        {
            enemyListAddon = addon;
        }

        public void ForEachGroup(Action<StatusNodeGroup> func)
        {
            foreach(var group in NodeGroups)
                if (group != null)
                    func(group);
        }

        public void ForEachNode(Action<StatusNode> func)
        {
            foreach (var group in NodeGroups)
                if (group != null)
                    group.ForEachNode(func);
        }

        public void SetGroupVisibility(int index, bool enable, bool setChildren = false)
        {
            var group = NodeGroups[index];

            if (group == null)
                return;

            group.SetVisibility(enable, setChildren);
        }

        public void SetStatus(int groupIndex, int statusIndex, int id, int timer)
        {
            var group = NodeGroups[groupIndex];

            if (group == null)
                return;

            var row = StatusSheet.GetRow((uint) id);
            
            group.SetStatus(statusIndex, row.Icon, timer);
        }

        public void HideUnusedStatus(int groupIndex, int statusCount)
        {
            var group = NodeGroups[groupIndex];

            if (group == null)
                return;

            group.HideUnusedStatus(statusCount);
        }

        public void SetDepthPriority(int groupIndex, bool enable)
        {
            var group = NodeGroups[groupIndex];

            if (group == null)
                return;

            group.RootNode->SetUseDepthBasedPriority(enable);

            group.ForEachNode(node =>
            {
                node.RootNode->SetUseDepthBasedPriority(enable);
                node.DurationNode->AtkResNode.SetUseDepthBasedPriority(enable);
                node.IconNode->AtkResNode.SetUseDepthBasedPriority(enable);
            });
        }

        public void LoadConfig()
        {
            ForEachNode(node => node.LoadConfig());
            ForEachGroup(group => group.LoadConfig());
        }

        public bool BuildNodes(bool rebuild = false)
        {
            if (enemyListAddon == null) return false;
            if (Built && !rebuild) return true;
            if (rebuild) DestroyNodes();
 
            for(byte i = 0; i < AddonEnemyList.MaxEnemyCount; i++)
            {
                var nodeGroup = new StatusNodeGroup(_plugin);
                var buttonComponent = *(&enemyListAddon->EnemyOneComponent)[i];
                if (!nodeGroup.BuildNodes(StartingNodeId))
                {
                    DestroyNodes();
                    return false;
                }

                var lastChild = buttonComponent->AtkComponentBase.UldManager.RootNode;
                while (lastChild->PrevSiblingNode != null) lastChild = lastChild->PrevSiblingNode;

                lastChild->PrevSiblingNode = nodeGroup.RootNode;
                nodeGroup.RootNode->NextSiblingNode = lastChild;
                nodeGroup.RootNode->ParentNode = (AtkResNode*) buttonComponent->AtkComponentBase.UldManager.RootNode;

                buttonComponent->AtkComponentBase.UldManager.UpdateDrawNodeList();

                NodeGroups[i] = nodeGroup;
            }

            Built = true;

            return true;
        }

        public void DestroyNodes()
        {
            if (enemyListAddon == null) return;

            for(byte i = 0; i < AddonEnemyList.MaxEnemyCount; i++)
            {
                var buttonComponent = *(&enemyListAddon->EnemyOneComponent)[i];

                if (NodeGroups[i] != null)
                {
                    var lastDefaultNode = NodeGroups[i].RootNode->NextSiblingNode;
                    lastDefaultNode->PrevSiblingNode = null;
                    NodeGroups[i].DestroyNodes();
                }
                NodeGroups[i] = null;

                buttonComponent->AtkComponentBase.UldManager.UpdateDrawNodeList();
            }

            Built = false;
        }
    }
}
