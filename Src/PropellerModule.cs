using System;
using RT.Util.ExtensionMethods;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LiBackgammon.Migrations;
using RT.PropellerApi;
using RT.Servers;
using RT.Util;
using RT.Util.Json;

namespace LiBackgammon
{
    public sealed partial class LiBackgammonPropellerModule : IPropellerModule
    {
        string IPropellerModule.Name { get { return "LiBackgammon"; } }
        string[] IPropellerModule.FileFiltersToBeMonitoredForChanges { get { return null; } }
        bool IPropellerModule.MustReinitialize { get { return false; } }
        void IPropellerModule.Shutdown() { }

        void IPropellerModule.Init(LoggerBase log, JsonValue settings, ISettingsSaver saver)
        {
            Database.SetInitializer(new MigrateDatabaseToLatestVersion<Db, Configuration>());
            //// Trigger any pending migrations (without this, transactions that don’t commit mess things up)
            //using (var db = new Db())
            //    log.Info("Number of games in the database: {0}".Fmt(db.Games.Count()));
            _resolver = makeResolver();
        }

        private UrlResolver _resolver;
        HttpResponse IPropellerModule.Handle(HttpRequest req)
        {
            return _resolver.Handle(req);
        }
    }
}
