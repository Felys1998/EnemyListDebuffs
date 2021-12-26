using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dalamud;
using Dalamud.Hooking;
using Dalamud.Logging;
using Dalamud.Memory;
using Dalamud.Plugin;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace EnemyListDebuffs
{
    public unsafe class AddonEnemyListHooks : IDisposable
    {
        private readonly EnemyListDebuffsPlugin _plugin;

        private readonly Stopwatch Timer;
        private long Elapsed;
        private Hook<AddonEnemyListFinalizePrototype> hookAddonEnemyListFinalize;

        private AddonEnemyListDrawPrototype OrigDrawFunc;

        private IntPtr OrigEnemyListDrawFuncPtr;
        private AddonEnemyListDrawPrototype ReplaceDrawFunc;

        public AddonEnemyListHooks(EnemyListDebuffsPlugin p)
        {
            _plugin = p;

            Timer = new Stopwatch();
            Elapsed = 0;
        }

        public void Dispose()
        {
            hookAddonEnemyListFinalize.Dispose();
            var vtblFuncAddr = _plugin.Address.AddonEnemyListVTBLAddress + 40 * IntPtr.Size;
            MemoryHelper.ChangePermission(vtblFuncAddr, 8, MemoryProtection.ReadWrite, out var oldProtect);
            SafeMemory.Write(_plugin.Address.AddonEnemyListVTBLAddress + 40 * IntPtr.Size, OrigEnemyListDrawFuncPtr);
            MemoryHelper.ChangePermission(vtblFuncAddr, 8, oldProtect, out oldProtect);
        }
        
        public void Initialize()
        {
            hookAddonEnemyListFinalize =
                new Hook<AddonEnemyListFinalizePrototype>(_plugin.Address.AddonEnemyListFinalizeAddress,
                    AddonEnemyListFinalizeDetour);

            OrigEnemyListDrawFuncPtr = Marshal.ReadIntPtr(_plugin.Address.AddonEnemyListVTBLAddress, 40 * IntPtr.Size);
            OrigDrawFunc = Marshal.GetDelegateForFunctionPointer<AddonEnemyListDrawPrototype>(OrigEnemyListDrawFuncPtr);

            PluginLog.Log($"{OrigEnemyListDrawFuncPtr.ToInt64():X}");

            ReplaceDrawFunc = AddonEnemyListDrawDetour;
            var replaceDrawFuncPtr = Marshal.GetFunctionPointerForDelegate(ReplaceDrawFunc);

            var vtblFuncAddr = _plugin.Address.AddonEnemyListVTBLAddress + 40 * IntPtr.Size;
            MemoryHelper.ChangePermission(vtblFuncAddr, 8, MemoryProtection.ReadWrite, out var oldProtect);
            SafeMemory.Write(vtblFuncAddr, replaceDrawFuncPtr);
            MemoryHelper.ChangePermission(vtblFuncAddr, 8, oldProtect, out oldProtect);

            hookAddonEnemyListFinalize.Enable();
        }

        public void AddonEnemyListDrawDetour(AddonEnemyList* thisPtr)
        {
            if (!_plugin.Config.Enabled || _plugin.InPvp)
            {
                if (Timer.IsRunning)
                {
                    Timer.Stop();
                    Timer.Reset();
                    Elapsed = 0;
                }

                if (_plugin.StatusNodeManager.Built)
                {
                    _plugin.StatusNodeManager.DestroyNodes();
                    _plugin.StatusNodeManager.SetEnemyListAddonPointer(null);
                }

                OrigDrawFunc(thisPtr);
                return;
            }

            Elapsed += Timer.ElapsedMilliseconds;
            Timer.Restart();

            if (Elapsed >= _plugin.Config.UpdateInterval)
            {
                if (!_plugin.StatusNodeManager.Built)
                {
                    _plugin.StatusNodeManager.SetEnemyListAddonPointer(thisPtr);
                    if (!_plugin.StatusNodeManager.BuildNodes())
                        return;
                }

                var numArray = Framework.Instance()->GetUiModule()->RaptureAtkModule.AtkModule.AtkArrayDataHolder
                    .NumberArrays[19];

                for (var i = 0; i < thisPtr->EnemyCount; i++)
                    if (_plugin.UI.IsConfigOpen)
                    {
                        _plugin.StatusNodeManager.ForEachNode(node =>
                            node.SetStatus(StatusNode.StatusNode.DefaultIconId, 20));
                    }
                    else
                    {
                        var localPlayerId = _plugin.ClientState.LocalPlayer?.ObjectId;
                        if (localPlayerId is null)
                        {
                            _plugin.StatusNodeManager.HideUnusedStatus(i, 0);
                            continue;
                        }

                        var enemyObjectId = numArray->IntArray[8 + i * 6];
                        var enemyChara = CharacterManager.Instance()->LookupBattleCharaByObjectId(enemyObjectId);

                        if (enemyChara is null) continue;

                        var targetStatus = enemyChara->StatusManager;

                        var statusArray = (Status*)targetStatus.Status;

                        var count = 0;

                        for (var j = 0; j < 30; j++)
                        {
                            var status = statusArray[j];
                            if (status.StatusID == 0) continue;
                            if (status.SourceID != localPlayerId) continue;

                            _plugin.StatusNodeManager.SetStatus(i, count, status.StatusID, (int)status.RemainingTime);
                            count++;

                            if (count == 4)
                                break;
                        }

                        _plugin.StatusNodeManager.HideUnusedStatus(i, count);
                    }

                Elapsed = 0;
            }

            OrigDrawFunc(thisPtr);
        }

        public void AddonEnemyListFinalizeDetour(AddonEnemyList* thisPtr)
        {
            _plugin.StatusNodeManager.DestroyNodes();
            _plugin.StatusNodeManager.SetEnemyListAddonPointer(null);
            hookAddonEnemyListFinalize.Original(thisPtr);
        }

        private delegate void AddonEnemyListFinalizePrototype(AddonEnemyList* thisPtr);

        private delegate void AddonEnemyListDrawPrototype(AddonEnemyList* thisPtr);
    }
}