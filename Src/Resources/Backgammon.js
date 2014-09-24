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

    function getHash()
    {
        var hash = window.location.hash.replace(/^#/, '').split('/');
        if (hash[0] === '')
            hash.splice(0, 1);
        return hash;
    }

    function hashChange()
    {
        body
            .removeClass(function (_, cl) { return cl.split(' ').filter(function (c) { return c.substr(0, "hash-".length) === "hash-"; }).join(' '); })
            .addClass(getHash().map(function (e) { return 'hash-' + e; }).join(' '));
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

    // Add extra CSS
    var contentCss = [];                            // User-visible text (“content” property)
    for (var ss = 0; ss < document.styleSheets.length; ss++)
    {
        var rules = document.styleSheets[ss].cssRules || document.styleSheets[ss].rules;
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
                }
            }
        }
    }
    $('#converted-content').text(contentCss.join('\n'));
});
