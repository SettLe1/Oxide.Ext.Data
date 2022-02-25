using Oxide.Core;
using Oxide.Plugins;

namespace Oxide.Ext.Data
{
   enum LogType : byte
   {
      Error,
      Warning,
      Info
   }
   /// <summary>
   /// This class allows you to manage data from any plugins that are written using this library.
   /// </summary>
   public static class DataManager
   {
      private const string BackupDataFolder = "Backup/",
         PlayersDataFolder = "PlayersData/",
         PluginsDataFolder = "PluginsData/",
         WarningMsgBackupCreated = "Backup created for ",
         ErrorMsgNotLoaded = " data not loaded for ",
         ErrorMsgNotCreated = " data not created for ",
         ErrorMsgAlreadyLoaded = " data already loaded for ",
         SuccessMsgLoaded = " data loaded from ",
         SuccessMsgUnloaded = " data unloaded from ",
         SuccessMsgSaved = " data saved for ",
         SuccessMsgCreated = " data created for ";
      
      internal static bool debug, isUnloading, checkVersion;

      private static Hash<ulong, Hash<string, BaseData>> _playersData = new Hash<ulong, Hash<string, BaseData>>();
      private static Hash<string, Hash<string, BaseData>> _pluginsData = new Hash<string, Hash<string, BaseData>>();

      public static void ToggleDebug() => debug = !debug;

      #region Get

      public static bool TryGetPlayerData<T>(string pluginName, ulong playerId, out T data) where T : BaseData
      {
         data = null;
         if (!IsLoadedPlayerData(pluginName, playerId))
            return false;

         return _playersData[playerId][pluginName].TryGetData(out data);
      }

      public static bool TryGetPluginData<T>(string pluginName, string dataName, out T data) where T : BaseData
      {
         data = null;
         if (!IsLoadedPluginData(pluginName, dataName))
            return false;

         return _pluginsData[pluginName][dataName].TryGetData(out data);
      }

      #endregion

      #region Set

      private static bool TrySetPlayerData<T>(string pluginName, ulong playerId, T data) where T : BaseData
      {
         if (!_playersData.ContainsKey(playerId))
            _playersData.Add(playerId, new Hash<string, BaseData> {{pluginName, data}});
         else
         {
            var playerData = _playersData[playerId];
            if (playerData.ContainsKey(pluginName))
            {
               if (debug)
                  SendLog(LogType.Error, string.Format("[TrySetPlayerData<{0}>] Player{1}\"{2}\" from plugin \"{3}\".", data.GetType(), ErrorMsgAlreadyLoaded, playerId, pluginName));
               return false;
            }

            playerData.Add(pluginName, data);
         }

         if (debug)
            SendLog(LogType.Info, string.Format("[TrySetPlayerData<{0}>] Player{1}\"{2}\" from plugin \"{3}\".", data.GetType(), SuccessMsgLoaded, playerId, pluginName));
         return true;
      }

      private static bool TrySetPluginData<T>(string pluginName, string dataName, T data) where T : BaseData
      {
         if (!_pluginsData.ContainsKey(pluginName))
            _pluginsData.Add(pluginName, new Hash<string, BaseData> {{dataName, data}});
         else
         {
            var pluginData = _pluginsData[pluginName];
            if (pluginData.ContainsKey(dataName))
            {
               if (debug)
                  SendLog(LogType.Error, string.Format("[TrySetPluginData<{0}>] Plugin{1}\"{2}\".", data.GetType(), ErrorMsgAlreadyLoaded, pluginName));
               return false;
            }

            pluginData.Add(dataName, data);
         }

         if (debug)
            SendLog(LogType.Info, string.Format("[TrySetPluginData<{0}>] Plugin{1}\"{2}\".", data.GetType(), SuccessMsgLoaded, pluginName));
         return true;
      }

      #endregion

      #region Create

      private static bool TryCreatePlayerData<T>(string pluginName, ulong playerId, T data) where T : BaseData
      {
         if (data == null)
         {
            if (debug)
               SendLog(LogType.Error, string.Format("[TryCreatePlayerData] Player{0}\"{1}\", coz data from plugin \"{2}\" is null.", ErrorMsgNotCreated, playerId, pluginName));
            return false;
         }

         if (!TrySetPlayerData(pluginName, playerId, data))
            return false;

         if (debug)
            SendLog(LogType.Info, string.Format("[TryCreatePlayerData<{0}>] Player{1}\"{2}\" from plugin \"{3}\".", data.GetType(), SuccessMsgCreated, playerId, pluginName));
         Interface.Oxide.DataFileSystem.WriteObject($"{PlayersDataFolder}{playerId}/{pluginName}", data);
         return true;
      }

      private static bool TryCreatePluginData<T>(string pluginName, string dataName, T data) where T : BaseData
      {
         if (data == null)
         {
            if (debug)
               SendLog(LogType.Error, string.Format("[TryCreatePluginData] Plugin{0}\"{1}\", coz data is null.", ErrorMsgNotCreated, pluginName));
            return false;
         }

         if (!TrySetPluginData(pluginName, dataName, data))
            return false;

         if (debug)
            SendLog(LogType.Info, string.Format("[TryCreatePluginData<{0}>] Plugin{1}\"{2}\" with name \"{3}\".", data.GetType(), SuccessMsgCreated, pluginName, dataName));
         Interface.Oxide.DataFileSystem.WriteObject($"{PluginsDataFolder}{pluginName}/{dataName}", data);
         return true;
      }

      #endregion

      #region Save

      public static bool TrySaveAllData()
      {
         if (isUnloading)
            return false;

         if (_playersData.Count == 0 && _pluginsData.Count == 0)
         {
            if (debug)
               SendLog(LogType.Error, "[TrySaveAllData] Any data does not exist.");
            return false;
         }

         foreach (var kvp in _playersData)
         {
            TrySavePlayerData(kvp.Key);
         }

         foreach (var kvp in _pluginsData)
         {
            TrySavePluginData(kvp.Key);
         }

         return true;
      }

      private static bool TrySavePlayerData<T>(string pluginName, ulong playerId) where T : BaseData
      {
         T data;
         if (!TryGetPlayerData(pluginName, playerId, out data))
            return false;

         if (debug)
            SendLog(LogType.Info, string.Format("[TrySavePlayerData<{0}>] Player{1}\"{2}\" by plugin \"{3}\".", data.GetType(), SuccessMsgSaved, playerId, pluginName));
         Interface.Oxide.DataFileSystem.WriteObject($"{PlayersDataFolder}{playerId}/{pluginName}", data);
         return true;
      }

      private static bool TrySavePlayerData(ulong playerId)
      {
         if (!IsLoadedPlayerData(playerId))
            return false;

         foreach (var kvp in _playersData[playerId])
         {
            TrySavePlayerData<BaseData>(kvp.Key, playerId);
         }

         if (debug)
            SendLog(LogType.Info, string.Format("[TrySavePlayerData] All player{0}\"{1}\".", SuccessMsgSaved, playerId));
         return true;
      }

      private static bool TrySavePluginData<T>(string pluginName, string dataName) where T : BaseData
      {
         T data;
         if (!TryGetPluginData(pluginName, dataName, out data))
            return false;

         if (debug)
            SendLog(LogType.Info, string.Format("[TrySavePluginData<{0}>] Plugin{1}\"{2}\" by plugin \"{3}\".", data.GetType(), SuccessMsgSaved, dataName, pluginName));
         Interface.Oxide.DataFileSystem.WriteObject($"{PluginsDataFolder}{pluginName}/{dataName}", data);
         return true;
      }

      private static bool TrySavePluginData(string pluginName)
      {
         if (!IsLoadedPluginData(pluginName))
            return false;

         foreach (var kvp in _pluginsData[pluginName])
         {
            TrySavePluginData<BaseData>(pluginName, kvp.Key);
         }

         if (debug)
            SendLog(LogType.Info, string.Format("[TrySavePluginData] All plugin{0}\"{1}\".", SuccessMsgSaved, pluginName));
         return true;
      }

      #endregion

      #region Load

      public static bool TryLoadPlayerData<T>(string pluginName, ulong playerId) where T : BaseData, new()
      {
         if (isUnloading)
            return false;

         var plugin = Interface.Oxide.RootPluginManager.GetPlugin(pluginName);
         if (plugin == null || !plugin.IsLoaded)
         {
            if (debug)
               SendLog(LogType.Error, string.Format("[TryLoadPlayerData] Player{0}\"{1}\", coz the plugin \"{2}\" does not exist or not loaded.", ErrorMsgNotLoaded, playerId, pluginName));
            return false;
         }

         var path = $"{PlayersDataFolder}{playerId}/{pluginName}";
         if (!Interface.Oxide.DataFileSystem.ExistsDatafile(path))
            return TryCreatePlayerData(pluginName, playerId, new T());

         var data = Interface.Oxide.DataFileSystem.ReadObject<T>(path);
         if (data == null)
         {
            if (debug)
               SendLog(LogType.Error, string.Format("[TryLoadPlayerData] Player{0}\"{1}\", coz the data from plugin \"{2}\" is null.", ErrorMsgNotLoaded, playerId, pluginName));
            return false;
         }

         if (checkVersion && data.Version != plugin.Version)
         {
            BackupData(path, data);

            return TryCreatePlayerData(pluginName, playerId, new T());
         }

         return TrySetPlayerData(pluginName, playerId, data);
      }

      public static bool TryLoadPluginData<T>(string pluginName, string dataName) where T : BaseData, new()
      {
         if (isUnloading)
            return false;

         var plugin = Interface.Oxide.RootPluginManager.GetPlugin(pluginName);
         if (plugin == null || !plugin.IsLoaded)
         {
            if (debug)
               SendLog(LogType.Error, string.Format("[TryLoadPluginData] Plugin{0}\"{1}\", coz the plugin \"{2}\" does not exist or not loaded.", ErrorMsgNotLoaded, dataName, pluginName));
            return false;
         }

         var path = $"{PluginsDataFolder}{pluginName}/{dataName}";
         if (!Interface.Oxide.DataFileSystem.ExistsDatafile(path))
            return TryCreatePluginData(pluginName, dataName, new T());

         var data = Interface.Oxide.DataFileSystem.ReadObject<T>(path);
         if (data == null)
         {
            if (debug)
               SendLog(LogType.Error, string.Format("[TryLoadPluginData] Plugin{0}\"{1}\", coz the data from plugin \"{2}\" is null.", ErrorMsgNotLoaded, dataName, pluginName));
            return false;
         }

         if (checkVersion && data.Version != plugin.Version)
         {
            BackupData(path, data);

            return TryCreatePluginData(pluginName, dataName, new T());
         }

         return TrySetPluginData(pluginName, dataName, data);
      }

      #endregion

      #region Unload

      public static bool TryUnloadPlayerData<T>(string pluginName, ulong playerId) where T : BaseData
      {
         if (isUnloading || !IsLoadedPlayerData(pluginName, playerId))
            return false;

         TrySavePlayerData<T>(pluginName, playerId);

         var playerData = _playersData[playerId];

         if (playerData.Count == 1)
            _playersData.Remove(playerId);
         else playerData.Remove(pluginName);

         if (debug)
            SendLog(LogType.Info, string.Format("[TryUnloadPlayerData] Player{0}\"{1}\", \"{2}\".", SuccessMsgUnloaded, pluginName, playerId));
         return true;
      }

      public static bool TryUnloadPlayerData(ulong playerId)
      {
         if (isUnloading || !IsLoadedPlayerData(playerId))
            return false;

         TrySavePlayerData(playerId);

         if (debug)
            SendLog(LogType.Info, string.Format("[TryUnloadPlayerData] Player{0}\"{1}\".", SuccessMsgUnloaded, playerId));
         _playersData.Remove(playerId);
         return true;
      }

      public static bool TryUnloadPluginData<T>(string pluginName, string dataName) where T : BaseData
      {
         if (isUnloading || !IsLoadedPluginData(pluginName, dataName))
            return false;

         TrySavePluginData<T>(pluginName, dataName);

         var pluginData = _pluginsData[pluginName];

         if (pluginData.Count == 1)
            _pluginsData.Remove(pluginName);
         else pluginData.Remove(dataName);

         if (debug)
            SendLog(LogType.Info, string.Format("[TryUnloadPluginData] Plugin{0}\"{1}\", \"{2}\".", SuccessMsgUnloaded, dataName, pluginName));
         return true;
      }

      public static bool TryUnloadPluginData(string pluginName)
      {
         if (isUnloading || !IsLoadedPluginData(pluginName))
            return false;

         TrySavePluginData(pluginName);

         if (debug)
            SendLog(LogType.Info, string.Format("[TryUnloadPluginData] Plugin{0}\"{1}\".", SuccessMsgUnloaded, pluginName));
         _pluginsData.Remove(pluginName);
         return true;
      }

      #endregion

      #region Backup

      private static void BackupData<T>(string path, T data)
      {
         SendLog(LogType.Warning, string.Format("{0}\"{1}\", coz the datafile is outdated. Created new datafile.", WarningMsgBackupCreated, path));
         Interface.Oxide.DataFileSystem.WriteObject($"{BackupDataFolder}{path}", data);
      }

      #endregion

      #region Checks

      private static bool IsLoadedPlayerData(ulong playerId)
      {
         if (!_playersData.ContainsKey(playerId))
         {
            if (debug)
               SendLog(LogType.Error, string.Format("[IsLoadedPlayerData] Player{0}\"{1}\".", ErrorMsgNotLoaded, playerId));
            return false;
         }

         return true;
      }

      private static bool IsLoadedPlayerData(string pluginName, ulong playerId)
      {
         if (!IsLoadedPlayerData(playerId))
            return false;

         if (!_playersData[playerId].ContainsKey(pluginName))
         {
            if (debug)
               SendLog(LogType.Error, string.Format("[IsLoadedPlayerData] Player{0}\"{1}\", coz data for plugin \"{2}\" does not exist.", ErrorMsgNotLoaded, playerId, pluginName));
            return false;
         }

         return true;
      }

      private static bool IsLoadedPluginData(string pluginName)
      {
         if (!_pluginsData.ContainsKey(pluginName))
         {
            if (debug)
               SendLog(LogType.Error, string.Format("[IsLoadedPluginData] Plugin{0}\"{1}\".", ErrorMsgNotLoaded, pluginName));
            return false;
         }

         return true;
      }

      private static bool IsLoadedPluginData(string pluginName, string dataName)
      {
         if (!IsLoadedPluginData(pluginName))
            return false;

         if (!_pluginsData[pluginName].ContainsKey(dataName))
         {
            if (debug)
               SendLog(LogType.Error, string.Format("[IsLoadedPluginData] Plugin{0}\"{1}\", coz data with the name \"{2}\" does not exist.", ErrorMsgNotLoaded, pluginName, dataName));
            return false;
         }

         return true;
      }

      #endregion
      
      internal static void SendLog(LogType type, string msg)
      {
         switch (type)
         {
            case LogType.Info:
               Interface.Oxide.LogInfo("[Ext.Data] " + msg);
               break;
            case LogType.Warning:
               Interface.Oxide.LogWarning("[Ext.Data] " + msg);
               break;
            case LogType.Error:
               Interface.Oxide.LogError("[Ext.Data] " + msg);
               break;
         }
      }
   }
}