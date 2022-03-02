using System;
using Oxide.Game.Rust;

namespace Oxide.Ext.Data.Core
{
   public class ExtDataPluginLoader : RustPluginLoader
   {
      public override Type[] CorePlugins
      {
         get
         {
            return new Type[1]{ typeof (ExtDataCore) };
         }
      }
   }
}