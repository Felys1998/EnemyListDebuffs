using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace EnemyListDebuffs.StatusNode
{
    public unsafe class StatusNode(EnemyListDebuffsPlugin p)
    {
        public AtkResNode* RootNode { get; private set; }
        public AtkImageNode* IconNode { get; private set; }
        public AtkTextNode* DurationNode { get; private set; }
        
        public bool Visible { get; private set; }

        public const uint DefaultIconId = 210205;

        private uint _currentIconId = DefaultIconId;
        private int _currentTimer = 60;

        public void SetVisibility(bool enable)
        {
            RootNode->ToggleVisibility(enable);
        }

        public void SetStatus(uint id, int timer)
        {
            SetVisibility(true);

            if (id != _currentIconId)
            {
                IconNode->LoadIconTexture(id, 0);
                _currentIconId = id;
            }

            if (timer != _currentTimer)
            {
                DurationNode->SetNumber(timer);
                _currentTimer = timer;
            }
        }

        public void LoadConfig()
        {
            if (!Built()) return;

            IconNode->AtkResNode.SetPositionShort((short)p.Config.IconX, (short)p.Config.IconY);
            IconNode->AtkResNode.SetHeight((ushort)p.Config.IconHeight);
            IconNode->AtkResNode.SetWidth((ushort)p.Config.IconWidth);
            DurationNode->AtkResNode.SetPositionShort((short)p.Config.DurationX, (short)p.Config.DurationY);
            DurationNode->FontSize = (byte) p.Config.FontSize;
            ushort outWidth = 0;
            ushort outHeight = 0;
            DurationNode->GetTextDrawSize(&outWidth, &outHeight);
            DurationNode->AtkResNode.SetWidth((ushort)(outWidth + 2 * p.Config.DurationPadding));
            DurationNode->AtkResNode.SetHeight((ushort)(outHeight + 2 * p.Config.DurationPadding));

            var iconHeight = (ushort)(p.Config.IconY + p.Config.IconHeight);
            var durationHeight = (ushort)(p.Config.DurationY + DurationNode->AtkResNode.Height);

            RootNode->SetHeight(durationHeight > iconHeight ? durationHeight : iconHeight);
            RootNode->SetWidth((ushort)(DurationNode->AtkResNode.Width > p.Config.IconWidth ? DurationNode->AtkResNode.Width : p.Config.IconWidth));

            DurationNode->TextColor.R = (byte)(p.Config.DurationTextColor.X * 255);
            DurationNode->TextColor.G = (byte)(p.Config.DurationTextColor.Y * 255);
            DurationNode->TextColor.B = (byte)(p.Config.DurationTextColor.Z * 255);
            DurationNode->TextColor.A = (byte)(p.Config.DurationTextColor.W * 255);

            DurationNode->EdgeColor.R = (byte)(p.Config.DurationEdgeColor.X * 255);
            DurationNode->EdgeColor.G = (byte)(p.Config.DurationEdgeColor.Y * 255);
            DurationNode->EdgeColor.B = (byte)(p.Config.DurationEdgeColor.Z * 255);
            DurationNode->EdgeColor.A = (byte)(p.Config.DurationEdgeColor.W * 255);
        }

        public bool Built() => RootNode != null && IconNode != null && DurationNode != null;

        public bool BuildNodes(uint baseNodeId)
        {
            if (Built()) return true;

            var rootNode = CreateRootNode();
            if (rootNode == null) return false;
            RootNode = rootNode;

            var iconNode = CreateIconNode();
            if (iconNode == null)
            {
                DestroyNodes();
                return false;
            }
            IconNode = iconNode;

            var durationNode = CreateDurationNode();
            if (durationNode == null)
            {
                DestroyNodes();
                return false;
            }
            DurationNode = durationNode;

            RootNode->NodeId = baseNodeId;
            RootNode->ChildCount = 2;
            RootNode->ChildNode = (AtkResNode*) IconNode;

            IconNode->AtkResNode.NodeId = baseNodeId + 1;
            IconNode->AtkResNode.ParentNode = RootNode;
            IconNode->AtkResNode.PrevSiblingNode = (AtkResNode*)DurationNode;

            DurationNode->AtkResNode.NodeId = baseNodeId + 2;
            DurationNode->AtkResNode.ParentNode = RootNode;
            DurationNode->AtkResNode.NextSiblingNode = (AtkResNode*)IconNode;

            LoadConfig();

            return true;
        }

        public void DestroyNodes()
        {
            if (IconNode != null)
            {
                IconNode->UnloadTexture();
                IMemorySpace.Free(IconNode->PartsList->Parts->UldAsset, (ulong)sizeof(AtkUldAsset));
                IMemorySpace.Free(IconNode->PartsList->Parts, (ulong)sizeof(AtkUldPart));
                IMemorySpace.Free(IconNode->PartsList, (ulong)sizeof(AtkUldPartsList));
                IconNode->PartsList = null;
                IconNode->AtkResNode.Destroy(true);
                IconNode = null;
            }
            if (DurationNode != null)
            {
                DurationNode->AtkResNode.Destroy(true);
                DurationNode = null;
            }
            if (RootNode != null)
            {
                RootNode->Destroy(true);
                RootNode = null;
            }
        }

        private AtkResNode* CreateRootNode()
        {
            var newResNode = (AtkResNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkResNode), 8);
            if (newResNode == null)
            {
                p.PluginLog.Debug("Failed to allocate memory for res node");
                return null;
            }
            IMemorySpace.Memset(newResNode, 0, (ulong)sizeof(AtkResNode));
            newResNode->Ctor();

            newResNode->Type = NodeType.Res;
            newResNode->NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
            newResNode->DrawFlags = 0;

            return newResNode;
        }

        private AtkImageNode* CreateIconNode()
        {
            var newImageNode = (AtkImageNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkImageNode), 8);
            if (newImageNode == null)
            {
                p.PluginLog.Debug("Failed to allocate memory for image node");
                return null;
            }
            IMemorySpace.Memset(newImageNode, 0, (ulong)sizeof(AtkImageNode));
            newImageNode->Ctor();

            newImageNode->AtkResNode.Type = NodeType.Image;
            newImageNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
            newImageNode->AtkResNode.DrawFlags = 0;

            newImageNode->WrapMode = 1;
            newImageNode->Flags |= (byte)ImageNodeFlags.AutoFit;

            var partsList = (AtkUldPartsList*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPartsList), 8);
            if (partsList == null)
            {
                p.PluginLog.Debug("Failed to allocate memory for parts list");
                newImageNode->AtkResNode.Destroy(true);
                return null;
            }

            partsList->Id = 0;
            partsList->PartCount = 1;

            var part = (AtkUldPart*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldPart), 8);
            if (part == null)
            {
                p.PluginLog.Debug("Failed to allocate memory for part");
                IMemorySpace.Free(partsList, (ulong)sizeof(AtkUldPartsList));
                newImageNode->AtkResNode.Destroy(true);
            }

            part->U = 0;
            part->V = 0;
            part->Width = 24;
            part->Height = 32;

            partsList->Parts = part;

            var asset = (AtkUldAsset*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkUldAsset), 8);
            if (asset == null)
            {
                p.PluginLog.Debug("Failed to allocate memory for asset");
                IMemorySpace.Free(part, (ulong)sizeof(AtkUldPart));
                IMemorySpace.Free(partsList, (ulong)sizeof(AtkUldPartsList));
                newImageNode->AtkResNode.Destroy(true);
            }

            asset->Id = 0;
            asset->AtkTexture.Ctor();

            part->UldAsset = asset;

            newImageNode->PartsList = partsList;

            newImageNode->LoadIconTexture((uint)DefaultIconId, 0);

            return newImageNode;
        }

        private AtkTextNode* CreateDurationNode()
        {
            var newTextNode = (AtkTextNode*)IMemorySpace.GetUISpace()->Malloc((ulong)sizeof(AtkTextNode), 8);
            if (newTextNode == null)
            {
                p.PluginLog.Debug("Failed to allocate memory for text node");
                return null;
            }
            IMemorySpace.Memset(newTextNode, 0, (ulong)sizeof(AtkTextNode));
            newTextNode->Ctor();

            newTextNode->AtkResNode.Type = NodeType.Text;
            newTextNode->AtkResNode.NodeFlags = NodeFlags.AnchorLeft | NodeFlags.AnchorTop;
            newTextNode->AtkResNode.DrawFlags = 12;
            newTextNode->AtkResNode.SetWidth(24);
            newTextNode->AtkResNode.SetHeight(17);

            newTextNode->LineSpacing = 12;
            newTextNode->AlignmentFontType = 4;
            newTextNode->FontSize = 12;
            newTextNode->TextFlags = (TextFlags.AutoAdjustNodeSize | TextFlags.Edge);

            newTextNode->SetNumber(20);

            return newTextNode;
        }
    }
}
