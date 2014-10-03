LiBackgammon = {
    getHash: function ()
    {
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
        return { values: values, dict: dict };
    },

    hashAdd: function (vals, obj)
    {
        if (!(vals instanceof Array))
            vals = [vals];
        var hash = LiBackgammon.getHash();
        for (var i = 0; i < vals.length; i++)
            if (hash.values.indexOf(vals[i]) === -1)
                hash.values.push(vals[i]);
        if (typeof obj === "object")
            for (var i in obj)
                hash.dict[i] = obj[i];
        LiBackgammon.setHash(hash.values, hash.dict);
    },

    hashRemove: function (vals, keys)
    {
        if (!(vals instanceof Array))
            vals = [vals];
        var hash = LiBackgammon.getHash(), pos;
        for (var i = 0; i < vals.length; i++)
            while ((pos = hash.values.indexOf(vals[i])) !== -1)
                hash.values.splice(pos, 1);
        if (keys instanceof Array)
            for (var i = 0; i < keys.length; i++)
                if (keys[i] in hash.dict)
                    delete hash.dict[keys[i]];
        LiBackgammon.setHash(hash.values, hash.dict);
    },

    hashAddKeys: function (obj)
    {
        var hash = LiBackgammon.getHash(), keys = Object.keys(obj);
        for (var i = 0; i < keys.length; i++)
            hash.dict[keys[i]] = obj[keys[i]];
        LiBackgammon.setHash(hash.values, hash.dict);
    },

    hashRemoveKeys: function (keys)
    {
        if (!(keys instanceof Array))
            keys = [keys];
        var hash = LiBackgammon.getHash();
        for (var i = 0; i < keys.length; i++)
            if (keys[i] in hash.dict)
                delete hash.dict[keys[i]];
        LiBackgammon.setHash(hash.values, hash.dict);
    },

    setHash: function (values, dict)
    {
        var elems = values.slice(0);
        var keys = Object.keys(dict);
        for (var i = 0; i < keys.length; i++)
            elems.push(keys[i] + '=' + dict[keys[i]]);
        window.location.hash = elems.join('/');
    },

    // Populated by the code that looks through the 'content' properties of the CSS
    strings: {}
};

$(function ()
{
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

    function hashChange()
    {
        body
            .removeClass(function (_, cl) { return cl.split(' ').filter(function (c) { return c.substr(0, "hash-".length) === "hash-"; }).join(' '); })
            .addClass(LiBackgammon.getHash().values.map(function (e) { return 'hash-' + e; }).join(' '));
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
    }

    $(window)
        .on('blur', function () { body.removeClass('show-shortcuts'); })
        .on('hashchange', hashChange);

    hashChange();

    // Add extra CSS and populate LiBackgammon.strings
    var contentCss = [];                            // User-visible text (“content” property)
    var rules = document.styleSheets[0].cssRules || document.styleSheets[0].rules;
    for (var ruleix = 0; ruleix < rules.length; ruleix++)
    {
        if (!rules[ruleix].selectorText)
            continue;
        var props = rules[ruleix].style;
        for (var propix = 0; propix < props.length; propix++)
        {
            var propName = props[propix].replace(/-value$/, '');
            var val = props.getPropertyValue(propName);
            if (propName === 'content')
            {
                if (val[0] === "'" || val[0] === '"')
                    val = val.substr(1, val.length - 2).replace(/\\([0-9a-f]{1,6} ?|[\\'"])/g, function (_, m) { return m.length === 1 ? m : String.fromCharCode(parseInt(m.substr(1, m.length - 2), 16)); });
                if (val[0] === '{')
                {
                    var json = JSON.parse(val), text = json.text, str = '', pos;
                    if (!json.notranslate)
                        LiBackgammon.strings[rules[ruleix].selectorText] = json;
                    while ((pos = text.indexOf("{")) !== -1)
                    {
                        str += " '" + text.substr(0, pos).replace(/\\/g, '\\\\').replace(/'/g, '\\\'') + "'";
                        if (text[pos + 1] === '{')
                        {
                            str += " '{'";
                            pos++;
                        }
                        else if (text[pos + 1] === '}')
                        {
                            str += " '}'";
                            pos++;
                        }
                        else
                        {
                            var pos2 = text.indexOf("}");
                            if (pos2 === -1 || pos2 < pos)
                                break;
                            str += ' ' + json[text.substr(pos + 1, pos2 - pos - 1)];
                            pos = pos2;
                        }
                        text = text.substr(pos + 1);
                    }
                    contentCss.push(rules[ruleix].selectorText + '{content:' + str + " '" + text.replace(/\\/g, '\\\\').replace(/'/g, '\\\'') + "'}");
                }
                else if (val.length > 0)
                    LiBackgammon.strings[rules[ruleix].selectorText] = { text: val };
            }
        }
    }
    $('#converted-content').text(contentCss.join('\n'));
});
