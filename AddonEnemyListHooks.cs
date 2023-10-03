﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Dalamud;
using Dalamud.Hooking;
using Dalamud.Memory;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace EnemyListDebuffs
{
    public unsafe class AddonEnemyListHooks : IDisposable
    {
        private readonly int _drawVtblOffset = 43 * IntPtr.Size;
        private readonly EnemyListDebuffsPlugin _plugin;

        private readonly Stopwatch _timer;
        private long _elapsed;
        private Hook<AddonEnemyListFinalizePrototype> _hookAddonEnemyListFinalize;

        private AddonEnemyListDrawPrototype _origDrawFunc;

        private IntPtr _origEnemyListDrawFuncPtr;
        private AddonEnemyListDrawPrototype _replaceDrawFunc;

        public AddonEnemyListHooks(EnemyListDebuffsPlugin p)
        {
            _plugin = p;

            _timer = new Stopwatch();
            _elapsed = 0;
        }

        public void Dispose()
        {
            _hookAddonEnemyListFinalize.Dispose();
            var vtblFuncAddr = _plugin.Address.AddonEnemyListVTBLAddress + _drawVtblOffset;
            MemoryHelper.ChangePermission(vtblFuncAddr, 8, MemoryProtection.ReadWrite, out var oldProtect);
            SafeMemory.Write(_plugin.Address.AddonEnemyListVTBLAddress + _drawVtblOffset, _origEnemyListDrawFuncPtr);
            MemoryHelper.ChangePermission(vtblFuncAddr, 8, oldProtect, out oldProtect);
        }
        
        public void Initialize()
        {
            _hookAddonEnemyListFinalize = _plugin.GameInteropProvider.HookFromAddress<AddonEnemyListFinalizePrototype>
                (_plugin.Address.AddonEnemyListFinalizeAddress, AddonEnemyListFinalizeDetour);
            
            _origEnemyListDrawFuncPtr = Marshal.ReadIntPtr(_plugin.Address.AddonEnemyListVTBLAddress, _drawVtblOffset);
            _origDrawFunc = Marshal.GetDelegateForFunctionPointer<AddonEnemyListDrawPrototype>(_origEnemyListDrawFuncPtr);

            _plugin.PluginLog.Debug($"{_origEnemyListDrawFuncPtr.ToInt64():X}");

            _replaceDrawFunc = AddonEnemyListDrawDetour;
            var replaceDrawFuncPtr = Marshal.GetFunctionPointerForDelegate(_replaceDrawFunc);

            var vtblFuncAddr = _plugin.Address.AddonEnemyListVTBLAddress + _drawVtblOffset;
            MemoryHelper.ChangePermission(vtblFuncAddr, 8, MemoryProtection.ReadWrite, out var oldProtect);
            SafeMemory.Write(vtblFuncAddr, replaceDrawFuncPtr);
            MemoryHelper.ChangePermission(vtblFuncAddr, 8, oldProtect, out oldProtect);

            _hookAddonEnemyListFinalize.Enable();
        }

        public void AddonEnemyListDrawDetour(AddonEnemyList* thisPtr)
        {
            if (!_plugin.Config.Enabled || _plugin.InPvp)
            {
                if (_timer.IsRunning)
                {
                    _timer.Stop();
                    _timer.Reset();
                    _elapsed = 0;
                }

                if (_plugin.StatusNodeManager.Built)
                {
                    _plugin.StatusNodeManager.DestroyNodes();
                    _plugin.StatusNodeManager.SetEnemyListAddonPointer(null);
                }

                _origDrawFunc(thisPtr);
                return;
            }

            _elapsed += _timer.ElapsedMilliseconds;
            _timer.Restart();

            if (_elapsed >= _plugin.Config.UpdateInterval)
            {
                if (!_plugin.StatusNodeManager.Built)
                {
                    _plugin.StatusNodeManager.SetEnemyListAddonPointer(thisPtr);
                    if (!_plugin.StatusNodeManager.BuildNodes())
                        return;
                }

                var numArray = Framework.Instance()->GetUiModule()->GetRaptureAtkModule()->AtkModule.AtkArrayDataHolder
                    .NumberArrays[21];

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
                        var enemyChara = CharacterManager.Instance()->LookupBattleCharaByObjectId((uint)enemyObjectId);

                        if (enemyChara is null) continue;

                        var targetStatus = enemyChara->GetStatusManager;

                        var statusArray = (Status*)targetStatus->Status;

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

                _elapsed = 0;
            }

            _origDrawFunc(thisPtr);
        }

        public void AddonEnemyListFinalizeDetour(AddonEnemyList* thisPtr)
        {
            _plugin.StatusNodeManager.DestroyNodes();
            _plugin.StatusNodeManager.SetEnemyListAddonPointer(null);
            _hookAddonEnemyListFinalize.Original(thisPtr);
        }

        private delegate void AddonEnemyListFinalizePrototype(AddonEnemyList* thisPtr);

        private delegate void AddonEnemyListDrawPrototype(AddonEnemyList* thisPtr);
    }
}