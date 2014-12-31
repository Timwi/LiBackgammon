﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RT.Servers;
using RT.Util.Json;
using RT.Util.Serialization;

namespace LiBackgammon
{
    sealed class LiBackgammonAjax
    {
        [AjaxMethod]
        public JsonValue lang(string hashName)
        {
            if (string.IsNullOrWhiteSpace(hashName))
                return null;

            using (var db = new Db())
            {
                var lang = db.Languages.FirstOrDefault(l => l.HashName == hashName);
                if (lang == null)
                    return null;
                return ClassifyJson.Deserialize<LanguageData>(JsonValue.Parse(lang.Data)).Translations.ToJsonDict(kvp => kvp.Key, kvp => kvp.Value);
            }
        }
    }
}