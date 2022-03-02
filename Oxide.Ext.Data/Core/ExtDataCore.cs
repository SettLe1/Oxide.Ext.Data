using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Plugins;
using UnityEngine;
using UnityEngine.Networking;

namespace Oxide.Ext.Data.Core
{
   public class ExtDataCore : RustPlugin
   {
      private const string GITHUB_API = @"https://api.github.com/repos/SettLe1/Oxide.Ext.Data/releases/latest",
         DLL_URI = @"https://github.com/SettLe1/Oxide.Ext.Data/raw/master/Oxide.Ext.Data.dll";

      internal static ExtDataCore Instance;
      internal static ExtDataCoreComponent Core;

      public ExtDataCore()
      {
         Author = "SettLe";
         Name = "ExtDataCore";
         Title = "ExtDataCore";
         Version = DataExtension.CurrentVersion;
         
         if (Core != null)
            Core.Remove();

         Instance = this;
         
         Core = new GameObject().AddComponent<ExtDataCoreComponent>();
         
         if (DataExtension.Config.AutoUpdateExtension)
         {
            Core.CheckAndDownloadLatestVersion();
         }
      }
      
      private void Loaded()
      {
         Unsubscribe(nameof(OnFrame));
      }
      
      // private void OnServerInitialized(bool initial)
      // {
      //    
      // }
      
      private void Unload()
      {
         DataManager.stopped = true;
         Core.Remove();
         Instance = null;
      }
      
      // private void OnNewSave(string filename)
      // {
      //    
      // }
      
      private void OnServerSave()
      {
         DataManager.TrySaveAllData();
      }
      
      // private void OnPlayerConnected(BasePlayer player)
      // {
      //    DataManager.TryLoadPlayerData(player.userID);
      // }
      
      // private void OnPlayerDisconnected(BasePlayer player, string reason)
      // {
      //    DataManager.TryUnloadPlayerData(player.userID);
      // }
      
      // private void OnPluginLoaded(Plugin name)
      // {
      //    
      // }
      
      // private void OnPluginUnloaded(Plugin name)
      // {
      //    
      // }

      internal void EnableSaving()
      {
         Subscribe(nameof(OnFrame));
         // try
         // {
         //    Subscribe(nameof(OnFrame));
         // }
         // catch (Exception)
         // {
         // }
      }
      
      private void OnFrame()
      {
         if (DataManager.stopped)
            return;
            
         if (DataManager._savingQueue.Count != 0)
         {
            var save = DataManager._savingQueue.Dequeue();
            Interface.Oxide.DataFileSystem.WriteObject(save.Path, save.Data);
            return;
         }

         if (DataManager.savingAllPlayersData)
            DataManager.savingAllPlayersData = false;
            
         if (DataManager.savingAllPluginsData)
            DataManager.savingAllPluginsData = false;
         
         Unsubscribe(nameof(OnFrame));
      }

      internal class ExtDataCoreComponent : FacepunchBehaviour
      {
         internal void Remove()
         {
            StopAllCoroutines();
            DestroyImmediate(this);
         }

         private void Error(string msg)
         {
            DataManager.SendLog(LogType.Error, msg);
            DataManager.stopped = false;
            Destroy(this);
         }
         
         private void Success(string msg)
         {
            DataManager.SendLog(LogType.Info, msg);
            DataManager.stopped = false;
            Destroy(this);
         }
         
         internal void CheckAndDownloadLatestVersion()
         {
            DataManager.stopped = true;
            StartCoroutine(CheckAndDownloadLatestVersionCor());
         }
         
         private IEnumerator CheckAndDownloadLatestVersionCor()
         {
            DataManager.SendLog(LogType.Warning, "Start checking a new update.");
            UnityWebRequest requestVersion = UnityWebRequest.Get(GITHUB_API);
            yield return requestVersion.SendWebRequest();
            
            if (requestVersion.isNetworkError || requestVersion.isHttpError || requestVersion.downloadHandler?.data == null)
            {
               requestVersion.Dispose();
               Error("Checking update failed.");
               yield break;
            }
            
            var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(requestVersion.downloadHandler.text);
            requestVersion.Dispose();
            
            ushort latestVersion = 0;
            if (result != null && result.ContainsKey("tag_name"))
            {
               var raw = result["tag_name"] as string;
               if (string.IsNullOrEmpty(raw))
               {
                  Error("Checking update failed.");
                  yield break;
               }
               
               latestVersion = ushort.Parse(raw.Replace("v", "").Replace(".", ""));
            }

            if (latestVersion == 0)
            {
               Error("Checking update failed.");
               yield break;
            }
         
            ushort curVersion = Convert.ToUInt16(DataExtension.CurrentVersion.ToString().Replace(".", ""));
            if (curVersion == latestVersion)
            {
               Success("The extension has the latest version.");
               yield break;
            }
            
            DataManager.SendLog(LogType.Warning, "The extension is outdated. Updating...");

            UnityWebRequest requestDLL = UnityWebRequest.Get(DLL_URI);
            yield return requestDLL.SendWebRequest();
            
            if (requestDLL.isNetworkError || requestDLL.isHttpError || requestDLL.downloadHandler?.data == null)
            {
               requestDLL.Dispose();
               Error("Downloading update failed.");
               yield break;
            }
            
            byte[] buffer = requestDLL.downloadHandler.data;
            requestDLL.Dispose();
            
            File.WriteAllBytes($"{Interface.Oxide.ExtensionDirectory}/Oxide.Ext.Data.dll", buffer);
            
            Interface.Oxide.ReloadExtension("Oxide.Ext.Data");
            Interface.Oxide.ReloadAllPlugins();
            Success("The extension has been updated.");
         }
      }
   }
}