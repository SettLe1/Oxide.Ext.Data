using Oxide.Core;

namespace Oxide.Ext.Data
{
   /// <summary>
   /// This shows how to create the class. (you need repeat the constructor)
   /// <code>
   /// public class SomeData : BaseData
   /// {
   ///     
   ///  ---public SomeData() : base(this.Version)
   ///  ---{
   ///         
   ///  ---}
   /// }
   /// </code>
   /// </summary>
   public abstract class BaseData
   {
      public VersionNumber Version { get; }

      public BaseData(VersionNumber pluginVersion)
      {
         Version = pluginVersion;
      }

      public bool TryGetData<T>(out T data) where T : BaseData
      {
         data = (T) this;
         if (data == null)
         {
            if (DataManager.debug)
               DataManager.SendLog(LogType.Error,"[TryGetData] Data not loaded for the class, coz the data is null.");
            return false;
         }

         if (DataManager.debug)
            DataManager.SendLog(LogType.Info, string.Format("[TryGetData<{0}>] Data recieved for the class.", data.GetType()));
         return true;
      }
   }
}