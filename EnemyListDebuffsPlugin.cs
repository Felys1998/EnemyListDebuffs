using Dalamud.Game.Command;
using Dalamud.Plugin;
using Lumina.Excel.GeneratedSheets;
using System.Collections.Generic;
using Dalamud.Game;
using Dalamud.Plugin.Services;
using EnemyListDebuffs.StatusNode;

namespace EnemyListDebuffs
{
    public class EnemyListDebuffsPlugin : IDalamudPlugin
    {
        public string Name => "EnemyListDebuffs";

        public IClientState ClientState { get; private set; } = null!;
        public static ICommandManager CommandManager { get; private set; } = null!;
        public IDalamudPluginInterface Interface { get; private set; } = null!;
        public IDataManager DataManager { get; private set; } = null!;
        public IFramework Framework { get; private set; } = null!;
        public PluginAddressResolver Address { get; private set; } = null!;
        public StatusNodeManager StatusNodeManager { get; private set; } = null!;
        public static ISigScanner SigScanner { get; private set; } = null!;
        public static AddonEnemyListHooks Hooks { get; private set; } = null!;
        public EnemyListDebuffsPluginUI UI { get; private set; } = null!;
        public EnemyListDebuffsPluginConfig Config { get; private set; } = null!;
        public IGameInteropProvider GameInteropProvider { get; private set; } = null!;
        public IAddonLifecycle AddonLifecycle { get; private set; } = null!;
        public IPluginLog PluginLog { get; private set; } = null!;

    internal bool InPvp;

        public EnemyListDebuffsPlugin(
            IClientState clientState,
            ICommandManager commandManager, 
            IDalamudPluginInterface pluginInterface, 
            IDataManager dataManager,
            IFramework framework, 
            ISigScanner sigScanner,
            IGameInteropProvider gameInteropProvider,
            IAddonLifecycle addonLifecycle,
            IPluginLog pluginLog)
        {
            ClientState = clientState;
            CommandManager = commandManager;
            DataManager = dataManager;
            Interface = pluginInterface;
            Framework = framework;
            SigScanner = sigScanner;
            GameInteropProvider = gameInteropProvider;
            AddonLifecycle = addonLifecycle;
            PluginLog = pluginLog;

            Config = pluginInterface.GetPluginConfig() as EnemyListDebuffsPluginConfig ?? new EnemyListDebuffsPluginConfig();
            Config.Initialize(pluginInterface);

            Address = new PluginAddressResolver(this);
            Address.Setup(sigScanner);

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

        private void OnTerritoryChange(ushort e)
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
