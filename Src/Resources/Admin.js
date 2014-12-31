$(function ()
{
    var totalStrings = 0;
    for (var i in LiBackgammon.strings)
        if (LiBackgammon.strings.hasOwnProperty(i))
            totalStrings++;

    $('tr.lang').each(function (_, e)
    {
        var data = LiBackgammon.translations[$(e).data('lang')];
        var doneStrings = 0;
        for (var i in data)
            if (data.hasOwnProperty(i) && LiBackgammon.strings.hasOwnProperty(i))
                doneStrings++;

        $(e).find('.lang-status').text(doneStrings === 0 ? 'Empty' : doneStrings === totalStrings ? 'Complete' : Math.round(doneStrings / totalStrings * 100) + '% complete');
        $(e).find('tr.translation').each(function (_, f)
        {
            var obj = LiBackgammon.strings[$(f).data('selector')];
            $(f).find('td.original').text(obj ? obj.text : '<invalid string>');
        });
    });
});