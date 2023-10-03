using Dalamud.Game;
using Dalamud.Game.Internal;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Logging;

namespace EnemyListDebuffs
{
    public class PluginAddressResolver
    {
        public IntPtr AddonEnemyListFinalizeAddress { get; private set;  }

        private const string AddonEnemyListFinalizeSignature = "48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 33 ED 48 8D 99 ?? ?? ?? ?? 8B FD 48 8B F1";

        public IntPtr AddonEnemyListVTBLAddress { get; private set; }

        private const string AddonEnemyListVTBLSignature = "48 8D 05 ?? ?? ?? ?? C7 83 ?? ?? ?? ?? ?? ?? ?? ?? 33 D2";

        public void Setup(ISigScanner scanner)
        {
            AddonEnemyListFinalizeAddress = scanner.ScanText(AddonEnemyListFinalizeSignature);
            AddonEnemyListVTBLAddress = scanner.GetStaticAddressFromSig(AddonEnemyListVTBLSignature);

            PluginLog.Verbose("===== EnemyList Debuffs =====");
            PluginLog.Verbose($"{nameof(AddonEnemyListFinalizeAddress)} {AddonEnemyListFinalizeAddress.ToInt64():X}");
            PluginLog.Verbose($"{nameof(AddonEnemyListVTBLAddress)} {AddonEnemyListVTBLAddress.ToInt64():X}");
        }
    }
}
