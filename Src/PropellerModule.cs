using System.Data.Entity;
using System.Linq;
using LiBackgammon.Migrations;
using RT.Json;
using RT.PropellerApi;
using RT.Servers;
using RT.Util;
using RT.Util.ExtensionMethods;

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
            // Trigger any pending migrations (without this, transactions that don’t commit mess up the migrations)
            using (var db = new Db())
                log.Info("Number of games in the database: {0}".Fmt(db.Games.Count()));
            _resolver = makeResolver();
        }

        private UrlResolver _resolver;
        HttpResponse IPropellerModule.Handle(HttpRequest req)
        {
            return _resolver.Handle(req);
        }
    }
}
