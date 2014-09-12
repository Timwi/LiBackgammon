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
});
