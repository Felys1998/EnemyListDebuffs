using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Logging;
using EnemyListDebuffs.StatusNode;

namespace EnemyListDebuffs
{
    public class EnemyListDebuffsPlugin : IDalamudPlugin
    {
        public string Name => "EnemyListDebuffs";
        
        public ClientState ClientState { get; private set; } = null!;
        public static CommandManager CommandManager { get; private set; } = null!;
        public DalamudPluginInterface Interface { get; private set; } = null!;
        public DataManager DataManager { get; private set; } = null!;
        public Framework Framework { get; private set; } = null!;
        public PluginAddressResolver Address { get; private set; } = null!;
        public StatusNodeManager StatusNodeManager { get; private set; } = null!;
        public static SigScanner SigScanner { get; private set; } = null!;
        public static AddonEnemyListHooks Hooks { get; private set; } = null!;
        public EnemyListDebuffsPluginUI UI { get; private set; } = null!;
        public EnemyListDebuffsPluginConfig Config { get; private set; } = null!;

        internal bool InPvp;

        public EnemyListDebuffsPlugin(
            ClientState clientState,
            CommandManager commandManager, 
            DalamudPluginInterface pluginInterface, 
            DataManager dataManager,
            Framework framework, 
            SigScanner sigScanner)
        {
            ClientState = clientState;
            CommandManager = commandManager;
            DataManager = dataManager;
            Interface = pluginInterface;
            Framework = framework;
            SigScanner = sigScanner;

            Config = pluginInterface.GetPluginConfig() as EnemyListDebuffsPluginConfig ?? new EnemyListDebuffsPluginConfig();
            Config.Initialize(pluginInterface);

            Address = new PluginAddressResolver();
            Address.Setup();

            StatusNodeManager = new StatusNodeManager(this);

            Hooks = new AddonEnemyListHooks(this);
            Hooks.Initialize();

            UI = new EnemyListDebuffsPluginUI(this);

            ClientState.TerritoryChanged += OnTerritoryChange;

            CommandManager.AddHandler("/eldebuffs", new CommandInfo(this.ToggleConfig)
            {
                HelpMessage = "Toggles config window."
            });
        }
        public void Dispose()
        {
            ClientState.TerritoryChanged -= OnTerritoryChange;
            CommandManager.RemoveHandler("/eldebuffs");

            UI.Dispose();
            Hooks.Dispose();
            StatusNodeManager.Dispose();
        }

        private void OnTerritoryChange(object sender, ushort e)
        {
            try
            {
                var territory = DataManager.GetExcelSheet<TerritoryType>()?.GetRow(e);
                if (territory != null) InPvp = territory.IsPvpZone;
            }
            catch (KeyNotFoundException)
            {
                PluginLog.Warning("Could not get territory for current zone");
            }
        }

        private void ToggleConfig(string command, string args)
        {
            UI.ToggleConfig();
        }
    }
}
