using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Extensions;

namespace Oxide.Ext.Data.Core
{
   public class DataExtension : Extension
   {
      public override string Name => "Data";
      public override string Author => "SettLe";
      public override VersionNumber Version => CurrentVersion;
      public override bool SupportsReloading => true;
      internal static readonly VersionNumber CurrentVersion = new VersionNumber(1, 0, 6);
      
      internal static DataConfig Config;

      public DataExtension(ExtensionManager manager) : base(manager)
      {
      }

      public override void Load()
      {
         Config = ConfigFile.Load<DataConfig>($"{Interface.Oxide.ConfigDirectory}/Ext.Data.json");
         DataManager.checkVersion = Config.CheckPluginVersions;
         Manager.RegisterPluginLoader(new ExtDataPluginLoader());
      }

      public override void Unload()
      {
         DataManager.stopped = true;
      }

      public override void OnShutdown()
      {
         DataManager.stopped = true;
      }
   }
}