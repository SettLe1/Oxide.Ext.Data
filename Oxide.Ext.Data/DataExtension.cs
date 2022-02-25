using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Extensions;

namespace Oxide.Ext.Data
{
   public class DataExtension : Extension
   {
      private const string GITHUB_API = @"https://api.github.com/repos/SettLe1/Oxide.Ext.Data/releases/latest",
         DLL_URI = @"https://github.com/SettLe1/Oxide.Ext.Data/blob/main/Oxide.Ext.Data/bin/Release/netcoreapp3.1/Oxide.Ext.Data.dll";
      public override string Name => "Ext.Data";
      public override string Author => "SettLe";
      public override VersionNumber Version => CurrentVersion;
      internal static readonly VersionNumber CurrentVersion = new VersionNumber(1, 0, 0);
      // private CSharpPluginLoader loader;
      private ushort _latestVersion;
      internal static DataConfig Config;
      private WebClient client = new WebClient();

      public DataExtension(ExtensionManager manager) : base(manager)
      {
      }

      public override void Load()
      {
         // loader = new CSharpPluginLoader(this);
         // Manager.RegisterPluginLoader(loader);
         Config = ConfigFile.Load<DataConfig>($"{Interface.Oxide.ConfigDirectory}/Ext.Data.json");
         DataManager.checkVersion = Config.CheckPluginVersions;
         
         if (Config.AutoUpdateExtension && HaveNewVersion())
         {
            ServerMgr.Instance.StartCoroutine(DownloadAndUpdate());
         }
      }

      public override void Unload()
      {
         DataManager.isUnloading = true;
         Interface.Oxide.UnloadPlugin("DataPlugin");
         DataManager.SendLog(LogType.Warning, "Unloading...");
      }

      public override void OnShutdown()
      {
         DataManager.isUnloading = true;
      }

      private bool HaveNewVersion()
      {
         Dictionary<string, string> result = null;
         try
         {
            result = JsonConvert.DeserializeObject<Dictionary<string, string>>(client.DownloadString(GITHUB_API));
         }
         catch (Exception)
         {
            DataManager.SendLog(LogType.Warning, "Failed to connect to the repository to check the new version.");
            return false;
         }
         
         if (result != null && result.ContainsKey("tag_name"))
         {
            _latestVersion = ushort.Parse(result["tag_name"].Replace("v", "").Replace(".", ""));
         }
         
         if (_latestVersion == 0)
            return false;

         ushort curVersion = Convert.ToUInt16(Version.ToString().Replace(".", ""));

         if (curVersion < _latestVersion)
         {
            DataManager.SendLog(LogType.Warning, "This extension is outdated. Updating...");
            return true;
         }
         return false;
      }

      private IEnumerator DownloadAndUpdate()
      {
         try
         {
            client.DownloadFileCompleted += OnDownloadCompleted;
            client.DownloadFileAsync(new Uri(DLL_URI), "temp_Oxide.Ext.Data");
         }
         catch (Exception)
         {
            DataManager.SendLog(LogType.Error, "Downloading update failed.");
         }
         yield return null;
      }
      
      private void OnDownloadCompleted(object sender, AsyncCompletedEventArgs e)
      {
         try
         {
            File.Delete("Oxide.Ext.Data");
            File.Move("temp_Oxide.Ext.Data", "Oxide.Ext.Data");
         }
         catch (Exception)
         {
            DataManager.SendLog(LogType.Error, "Replacing old file failed.");
         }
      }
   }
}