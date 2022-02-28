using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Extensions;

namespace Oxide.Ext.Data
{
   public class DataExtension : Extension
   {
      public override string Name => "Data";
      public override string Author => "SettLe";
      public override VersionNumber Version => CurrentVersion;
      internal static readonly VersionNumber CurrentVersion = new VersionNumber(1, 0, 4);
      
      internal static DataConfig Config;

      public DataExtension(ExtensionManager manager) : base(manager)
      {
      }

      public override void Load()
      {
         Config = ConfigFile.Load<DataConfig>($"{Interface.Oxide.ConfigDirectory}/Ext.Data.json");
         DataManager.checkVersion = Config.CheckPluginVersions;
         Manager.RegisterPluginLoader(new ExtDataPluginLoader());
         DataManager.ToggleDebug();
      }

      public override void Unload()
      {
         DataManager.stopped = true;
      }

      public override void OnShutdown()
      {
         DataManager.stopped = true;
         if (ExtDataAutoUpdater.updater != null)
            ExtDataAutoUpdater.updater.Cancel();
      }
   }
}