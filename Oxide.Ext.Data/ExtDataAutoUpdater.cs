using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using UnityEngine;
using UnityEngine.Networking;

namespace Oxide.Ext.Data
{
   internal class ExtDataAutoUpdater : CSPlugin
   {
      private const string GITHUB_API = @"https://api.github.com/repos/SettLe1/Oxide.Ext.Data/releases/latest",
         DLL_URI = @"https://github.com/SettLe1/Oxide.Ext.Data/raw/master/Oxide.Ext.Data.dll";

      internal static ExtDataAutoUpdaterComponent updater;

      public ExtDataAutoUpdater()
      {
         Author = "SettLe";
         Name = "ExtDataAutoUpdater";
         Title = "ExtDataAutoUpdater";
         Version = DataExtension.CurrentVersion;
         
         if (updater != null)
            updater.Cancel();
         else if (DataExtension.Config.AutoUpdateExtension)
         {
            DataManager.stopped = true;
            updater = new GameObject().AddComponent<ExtDataAutoUpdaterComponent>();
         }
      }

      internal class ExtDataAutoUpdaterComponent : FacepunchBehaviour
      {
         private void Awake()
         {
            StartCoroutine(CheckAndDownloadLatestVersionCor());
         }

         internal void Cancel()
         {
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