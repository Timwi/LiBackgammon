using System;
using System.Linq;
using RT.Servers;
using RT.TagSoup;
using RT.Util;
using RT.Util.Json;
using RT.Util.Serialization;

namespace LiBackgammon
{
    partial class LiBackgammonPropellerModule
    {
        private DbAuthenticator _authenticator = new DbAuthenticator();

        private HttpResponse withLoggedInUser(HttpRequest req, Func<UserFlags, bool> isAllowed, Func<DbSession, User, Db, HttpResponse> handler)
        {
            using (var tr = Program.NewTransaction())
            using (var db = new Db())
            {
                return Session.EnableManual<DbSession>(req, sess =>
                {
                    if (sess.LoggedInUserId == null)
                        throw new HttpException(HttpStatusCode._401_Unauthorized);

                    var user = db.Users.FirstOrDefault(u => u.UserID == sess.LoggedInUserId.Value);
                    if (user == null || !isAllowed(user.Flags))
                        throw new HttpException(HttpStatusCode._401_Unauthorized);

                    var ret = handler(sess, user, db);
                    tr.Complete();
                    return ret;
                });
            }
        }

        private HttpResponse admin(HttpRequest req)
        {
            return withLoggedInUser(req, flags => (flags & (UserFlags.CanApproveStyles | UserFlags.CanApproveTranslations)) != 0, (sess, user, db) =>
            {
                if (req.Method == HttpMethod.Post && req.Post["accept"].Value != null)
                {
                    if (req.Post["accept"].Value != "0" && req.Post["accept"].Value != "1")
                        return HttpResponse.Redirect(req.Url.ToHref());

                    var lang = req.Post["lang"].Value;
                    var token = req.Post["token"].Value;
                    var accept = req.Post["accept"].Value == "1";

                    var language = db.Languages.FirstOrDefault(l => l.HashName == lang);
                    if (language == null)
                        return HttpResponse.Redirect(req.Url.ToHref());

                    var data = ClassifyJson.Deserialize<LanguageData>(JsonValue.Parse(language.Data));
                    LanguageSuggestion suggestion;
                    if (!data.Suggestions.TryGetValue(token, out suggestion))
                        return HttpResponse.Redirect(req.Url.ToHref());

                    if (accept)
                        foreach (var kvp in suggestion.Translations)
                            data.Translations[kvp.Key] = kvp.Value;

                    data.Suggestions.Remove(token);

                    language.Data = ClassifyJson.Serialize(data).ToString();
                    db.SaveChanges();

                    return HttpResponse.Redirect(req.Url.ToHref());
                }

                if (req.Method == HttpMethod.Post && req.Post["approve"].Value != null)
                {
                    if (req.Post["approve"].Value != "0" && req.Post["approve"].Value != "1")
                        return HttpResponse.Redirect(req.Url.ToHref());

                    var lang = req.Post["lang"].Value;
                    var language = db.Languages.FirstOrDefault(l => l.HashName == lang);
                    if (language == null)
                        return HttpResponse.Redirect(req.Url.ToHref());

                    language.Approved = req.Post["approve"].Value == "1";
                    db.SaveChanges();

                    return HttpResponse.Redirect(req.Url.ToHref());
                }

                var styles = (user.Flags & UserFlags.CanApproveStyles) == 0 ? null : db.Styles.ToArray();
                var languages = (user.Flags & UserFlags.CanApproveTranslations) == 0 ? null : db.Languages.ToArray();
                var languageData = languages.ToDictionary(l => l.HashName, l => ClassifyJson.Deserialize<LanguageData>(JsonValue.Parse(l.Data)));

                return page(req,

                    (styles == null || styles.Length == 0) && (languages == null || languages.Length == 0)
                        ? new BODY(new H1("Nothing to do"))
                        : new BODY(
                            new SCRIPTLiteral(JsonValue.Fmt(@"LiBackgammon.translations={{dict}};", "dict", ClassifyJson.Serialize(languageData.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Translations)))),

                            styles == null || styles.Length == 0 ? null : Ut.NewArray<object>(
                                new H1("Styles"),
                                new TABLE(
                                    new TR(new TH("✓?"), new TH("Hashname"), new TH("Name")),
                                    styles.Select(s => new TR(new TD(s.Approved ? "✓" : ""), new TD(s.HashName), new TD(s.Name))))),

                            languages == null || languages.Length == 0 ? null : Ut.NewArray<object>(
                                new H1("Translations"),
                                new TABLE(
                                    new TR(new TH("Language"), new TH("Suggestions")),
                                    languages.OrderByDescending(l => languageData[l.HashName].Suggestions.Count).Select(l => new TR { class_ = "lang" + (l.Approved ? " approved" : null) }.Data("lang", l.HashName)._(
                                        new TD(
                                            new DIV { class_ = "lang-hashname" }._(l.HashName),
                                            new DIV { class_ = "lang-name" }._(l.Name),
                                            new DIV { class_ = "lang-status" }._("error"),
                                            new DIV { class_ = "lang-lastchange" }._(l.LastChange.ToLocalTime()),
                                            new FORM { class_ = "lang-approve", method = method.post, action = req.Url.ToHref() }._(
                                                new INPUT { type = itype.hidden, name = "lang", value = l.HashName },
                                                new INPUT { type = itype.hidden, name = "approve", value = l.Approved ? "0" : "1" },
                                                new BUTTON { type = btype.submit }._(l.Approved ? "Unapprove" : "Approve"))),
                                        new TD(languageData[l.HashName].Apply(data => data.Suggestions.Count == 0 ? null :
                                            data.Suggestions.Select(kvp1 => Ut.NewArray<object>(
                                                new H2(
                                                    kvp1.Key,
                                                    new[] { true, false }.Select(accept => new FORM { method = method.post, action = req.Url.ToHref(), class_ = "acc-rej-suggestion" }._(
                                                        new INPUT { type = itype.hidden, name = "token", value = kvp1.Key },
                                                        new INPUT { type = itype.hidden, name = "lang", value = l.HashName },
                                                        new INPUT { type = itype.hidden, name = "accept", value = accept ? "1" : "0" },
                                                        new BUTTON { type = btype.submit }._(accept ? "Accept" : "Reject")))),
                                                new TABLE(
                                                    new TR(new TH("Original"), new TH("Suggested translation"), new TH("Existing translation")),
                                                    kvp1.Value.Translations.Select(kvp2 => new TR { class_ = "translation", title = kvp2.Key }.Data("selector", kvp2.Key)._(
                                                        new TD { class_ = "original" },
                                                        new TD(kvp2.Value),
                                                        new TD(data.Translations.ContainsKey(kvp2.Key) ? data.Translations[kvp2.Key] : null))))))))))))),
                    "js/admin",
                    admin: true);
            });
        }
    }
}
