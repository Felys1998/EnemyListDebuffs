using Dalamud.Game;
using System;

namespace EnemyListDebuffs
{
    public class PluginAddressResolver
    {
        private readonly EnemyListDebuffsPlugin _plugin;
        public IntPtr AddonEnemyListFinalizeAddress { get; private set;  }

        private const string AddonEnemyListFinalizeSignature = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 33 ED 48 8D 99 ?? ?? ?? ?? 8B FD 48 8B F1";

        public IntPtr AddonEnemyListVTBLAddress { get; private set; }

        private const string AddonEnemyListVTBLSignature = "48 8D 05 ?? ?? ?? ?? C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 33 D2 48 89 03";

        public PluginAddressResolver(EnemyListDebuffsPlugin p)
        {
            _plugin = p;
        }
        
        public void Setup(ISigScanner scanner)
        {
            AddonEnemyListFinalizeAddress = scanner.ScanText(AddonEnemyListFinalizeSignature);
            AddonEnemyListVTBLAddress = scanner.GetStaticAddressFromSig(AddonEnemyListVTBLSignature);

            _plugin.PluginLog.Verbose("===== EnemyList Debuffs =====");
            _plugin.PluginLog.Verbose($"{nameof(AddonEnemyListFinalizeAddress)} {AddonEnemyListFinalizeAddress.ToInt64():X}");
            _plugin.PluginLog.Verbose($"{nameof(AddonEnemyListVTBLAddress)} {AddonEnemyListVTBLAddress.ToInt64():X}");
        }
    }
}
