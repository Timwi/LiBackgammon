LiBackgammon = {
    hashAdd: function (vals, obj)
    {
        if (!(vals instanceof Array))
            vals = [vals];
        for (var i = 0; i < vals.length; i++)
            if (LiBackgammon.hash.values.indexOf(vals[i]) === -1)
                LiBackgammon.hash.values.push(vals[i]);
        if (typeof obj === "object")
            for (var i in obj)
                LiBackgammon.hash.dict[i] = obj[i];
        LiBackgammon.setHash(LiBackgammon.hash.values, LiBackgammon.hash.dict);
    },

    hashRemove: function (vals, keys)
    {
        if (!(vals instanceof Array))
            vals = [vals];
        var pos;
        for (var i = 0; i < vals.length; i++)
            while ((pos = LiBackgammon.hash.values.indexOf(vals[i])) !== -1)
                LiBackgammon.hash.values.splice(pos, 1);
        if (keys instanceof Array)
            for (var i = 0; i < keys.length; i++)
                if (keys[i] in LiBackgammon.hash.dict)
                    delete LiBackgammon.hash.dict[keys[i]];
        LiBackgammon.setHash(LiBackgammon.hash.values, LiBackgammon.hash.dict);
    },

    hashAddKeys: function (obj)
    {
        var keys = Object.keys(obj);
        for (var i = 0; i < keys.length; i++)
            LiBackgammon.hash.dict[keys[i]] = obj[keys[i]];
        LiBackgammon.setHash(LiBackgammon.hash.values, LiBackgammon.hash.dict);
    },

    hashRemoveKeys: function (keys)
    {
        if (!(keys instanceof Array))
            keys = [keys];
        for (var i = 0; i < keys.length; i++)
            if (keys[i] in LiBackgammon.hash.dict)
                delete LiBackgammon.hash.dict[keys[i]];
        LiBackgammon.setHash(LiBackgammon.hash.values, LiBackgammon.hash.dict);
    },

    setHash: function (values, dict)
    {
        var elems = values.slice(0);
        var keys = Object.keys(dict);
        for (var i = 0; i < keys.length; i++)
            elems.push(keys[i] + '=' + dict[keys[i]]);
        window.location.hash = elems.join('/');
    },

    removeClassPrefix: function ($obj, prefix)
    {
        return $obj.removeClass(function (_, cl) { return cl.split(' ').filter(function (c) { return c.substr(0, prefix.length) === prefix; }).join(' '); });
    },

    toCssRule: function (json, text)
    {
        text = text || json.text;
        var str = '', pos, cssEsc = function (s) { return s.replace(/\\/g, '\\\\').replace(/'/g, '\\\''); }, extraCss = '';
        while ((pos = text.indexOf("{")) !== -1)
        {
            str += " '" + cssEsc(text.substr(0, pos)) + "'";
            if (text[pos + 1] === '{')
            {
                str += " '{'";
                pos++;
            }
            else if (text[pos + 1] === '}')
            {
                str += " '{}'";
                pos++;
            }
            else
            {
                var pos2 = text.indexOf("}", pos);
                if (pos2 === -1)
                {
                    text = text.substr(pos);
                    break;
                }
                var code = text.substr(pos + 1, pos2 - pos - 1);
                if (code.substr(0, 4) === 'css:')
                    extraCss += code.substr(4);
                else
                    str += ' ' + json[code];
                pos = pos2;
            }
            text = text.substr(pos + 1);
        }
        return json.sel + '{content:' + str + " '" + cssEsc(text) + "';" + extraCss + "}";
    },

    // Populated by the code that looks through the 'content' properties of the CSS
    strings: {},

    // Populated by hashChange
    hash: null
};

$(function ()
{
    window.onerror = function (msg, url, l, c)
    {
        alert('Error: ' + msg + "\nFile: " + url + "\nLine: " + l + "\nColumn: " + c);
    };

    var body = $(document.body);

    // Keyboard shortcut handling
    $('*[accesskey]').each(function ()
    {
        $(this).append($('<span>').addClass('shortcut').text($(this).attr('accesskey')));
    });

    $(document)
        .keydown(function (e)
        {
            if (e.keyCode === 18)  // ALT key
                body.addClass('show-shortcuts');
        })
        .keyup(function (e)
        {
            if (e.keyCode === 18)  // ALT key
                body.removeClass('show-shortcuts');
        });

    var translations = {};
    var styles = {};

    function hashChange()
    {
        // Decode window.location.hash
        var values = [];
        var dict = {};
        var hash = window.location.hash.replace(/^#/, '').split('/');
        for (var i = 0; i < hash.length; i++)
            if (hash[i].length)
            {
                if (/^(.*)=(.*)$/.test(hash[i]))
                    dict[RegExp.$1] = RegExp.$2;
                else
                    values.push(hash[i]);
            }

        // Set CSS classes hash-*
        LiBackgammon.removeClassPrefix(body, "hash-");
        for (var i = 0; i < values.length; i++)
            body.addClass('hash-' + values[i]);
        for (var j in dict)
            body.addClass('hash-' + j);

        // Set translated CSS
        if ('lang' in dict)
        {
            var lang = dict.lang;   // for persistence in lambdas
            var setTranslation = function ()
            {
                var css = [];
                for (var i in translations[lang])
                    if (translations[lang].hasOwnProperty(i) && i in LiBackgammon.strings)
                        css.push(LiBackgammon.toCssRule(LiBackgammon.strings[i], translations[lang][i]));
                $('#translated-content').text(css.join("\n"));
            };
            if (lang in translations)
                setTranslation();
            else
                $.post(body.data('ajax') + '/lang', { data: JSON.stringify({ hashName: dict.lang }) }, function (resp)
                {
                    translations[lang] = resp.result;
                    setTranslation();
                }, 'json');
        }
        else
            $('#translated-content').text('');

        if ('style' in dict)
        {
            var style = dict.style;   // for persistence in lambdas
            var setStyle = function (css) { $('#style-css').text(styles[style] || ''); };
            if (style in styles)
                setStyle();
            else
                $.post(body.data('ajax') + '/style', { data: JSON.stringify({ hashName: dict.style }) }, function (resp)
                {
                    styles[style] = resp.result;
                    setStyle();
                }, 'json');
        }
        else
            $('#style-css').text('');

        if (!('translator' in dict))
            $('#translated-content-2').text('');

        // Make every form and link use the same hash (URL fragment) as the current page
        $('form').each(function (_, f)
        {
            f = $(f);
            if (!f.data('action'))
                f.data('action', f.attr('action'));
            f.attr('action', f.data('action') + window.location.hash);
        });
        $('a').each(function (_, a)
        {
            a = $(a);
            if (a.attr('href') && a.attr('href')[0] !== '#')
            {
                if (!a.data('href'))
                    a.data('href', a.attr('href'));
                a.attr('href', a.data('href') + window.location.hash);
            }
        });

        LiBackgammon.hash = { values: values, dict: dict };
    }

    $(window)
        .on('blur', function () { body.removeClass('show-shortcuts'); })
        .on('hashchange', hashChange);

    hashChange();

    // Add extra CSS and populate LiBackgammon.strings
    var contentCss = [];                            // User-visible text (“content” property)
    for (var sheetIx = 0; sheetIx < document.styleSheets.length; sheetIx++)
    {
        if (document.styleSheets[sheetIx].ownerNode.id !== 'main-css')
            continue;
        var rules = document.styleSheets[sheetIx].cssRules || document.styleSheets[sheetIx].rules;
        for (var ruleix = 0; ruleix < rules.length; ruleix++)
        {
            if (!rules[ruleix].selectorText)
                continue;
            var props = rules[ruleix].style;
            for (var propix = 0; propix < props.length; propix++)
            {
                var propName = props[propix].replace(/-value$/, '');
                if (propName === 'content')
                {
                    var val = props.getPropertyValue(propName);
                    if (val[0] === "'" || val[0] === '"')
                        val = val.substr(1, val.length - 2).replace(/\\([0-9a-f]{1,6} ?|[\\'"])/g, function(_, m) { return m.length === 1 ? m : String.fromCharCode(parseInt(m.substr(1, m.length - 2), 16)); });
                    var selectorTextNormalized = rules[ruleix].selectorText.replace(/::(?=(before|after)\b)/g, ':');
                    if (val[0] === '{')
                    {
                        var json = JSON.parse(val);
                        json.sel = rules[ruleix].selectorText;
                        if (!json.notranslate)
                            LiBackgammon.strings[selectorTextNormalized] = json;
                        contentCss.push(LiBackgammon.toCssRule(json));
                    }
                    else if (val.length > 0)
                        LiBackgammon.strings[selectorTextNormalized] = { text: val, sel: rules[ruleix].selectorText };
                }
            }
        }
    }
    $('#converted-content').text(contentCss.join('\n'));
});
