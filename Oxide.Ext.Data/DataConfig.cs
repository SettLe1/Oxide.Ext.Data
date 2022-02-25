using System;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;

namespace Oxide.Ext.Data
{
   public class DataConfig : ConfigFile
   {
      [JsonProperty("Automatically update this extension")]
      public bool AutoUpdateExtension;
      [JsonProperty("Compare version of plugins with its data file and replace if there is a discrepancy")]
      public bool CheckPluginVersions;
      public VersionNumber Version;

      public DataConfig(string filename) : base(filename)
      {
      }
      
      public override void Load(string filename = null)
      {
         try
         {
            base.Load(filename);
            if (Version != DataExtension.CurrentVersion)
            {
               DataManager.SendLog(LogType.Warning, "Config file is outdated. Created default config.");
               CreateDefault(filename);
            }
         }
         catch (Exception)
         {
            DataManager.SendLog(LogType.Warning, "Failed to load config file. Created default config.");
            CreateDefault(filename);
         }
      }

      private void CreateDefault(string filename)
      {
         AutoUpdateExtension = true;
         CheckPluginVersions = true;
         Version = DataExtension.CurrentVersion;
         Save(filename);
      }
   }
}