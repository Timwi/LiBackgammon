using Microsoft.EntityFrameworkCore;
using RT.PropellerApi;
using RT.Servers;
using RT.Util.ExtensionMethods;

namespace LiBackgammon
{
    public sealed partial class LiBackgammonPropellerModule : PropellerModuleBase<LiBackgammonSettings>
    {
        public override string Name => "LiBackgammon";
        public override string[] FileFiltersToBeMonitoredForChanges => null;
        public override bool MustReinitialize => false;
        public override void Shutdown() { }

        public override void Init()
        {
            Db.ConnectionString = Settings.ConnectionString;

            // Trigger any pending migrations (without this, transactions that don’t commit mess up the migrations)
            using (var db = new Db())
            {
                db.Database.Migrate();
                Log.Info("Number of games in the database: {0}".Fmt(db.Games.Count()));
            }
            _resolver = makeResolver();
        }

        private UrlResolver _resolver;
        public override HttpResponse Handle(HttpRequest req) => _resolver.Handle(req);
    }
}
