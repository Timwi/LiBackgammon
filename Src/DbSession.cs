using System.Linq;
using System;
using RT.Servers;

namespace LiBackgammon
{
    public sealed class DbSession : Session
    {
        private int? _loggedInUserId;

        public int? LoggedInUserId
        {
            get { return _loggedInUserId; }
            set
            {
                _loggedInUserId = value;
                SessionModified = true;
            }
        }

        protected override bool ReadSession()
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var sess = db.Sessions.FirstOrDefault(s => s.SessionID == SessionID);
                if (sess != null)
                    _loggedInUserId = sess.UserID;

                // Always return true to pretend like all sessions exist but have no logged in user
                // without having to create a DB row for millions of empty sessions.
                return true;
            }
        }

        protected override void SaveSession()
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var sess = db.Sessions.FirstOrDefault(s => s.SessionID == SessionID);

                if (sess == null && _loggedInUserId == null)
                    return;

                if (_loggedInUserId == null)
                    db.Sessions.Remove(sess);
                else if (sess == null)
                    db.Sessions.Add(new UserSession { SessionID = SessionID, UserID = _loggedInUserId.Value });
                else
                    sess.UserID = _loggedInUserId.Value;
                db.SaveChanges();
                tr.Complete();
            }
        }

        protected override void DeleteSession()
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                var sess = db.Sessions.FirstOrDefault(s => s.SessionID == SessionID);
                if (sess != null)
                    db.Sessions.Remove(sess);
                db.SaveChanges();
                tr.Complete();
            }
        }
    }
}
