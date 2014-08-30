$(function ()
{
    // Keyboard shortcut handling
    $('*[accesskey]').each(function ()
    {
        $(this).append($('<span>').addClass('shortcut').text($(this).attr('accesskey')));
    });

    $(document).keydown(function (e)
    {
        if (e.keyCode === 18)  // ALT key
            $(document.body).addClass('show-shortcuts');
    });

    $(document).keyup(function (e)
    {
        if (e.keyCode === 18)  // ALT key
            $(document.body).removeClass('show-shortcuts');
    });

    window.onblur = function ()
    {
        $(document.body).removeClass('show-shortcuts');
    };
});
