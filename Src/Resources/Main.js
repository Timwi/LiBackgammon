$(function ()
{
    $('#newgame-playto>input').click(function ()
    {
        var curVal = +$('#newgame-playto>input:checked').val();
        var newVals = curVal < 3 ? [1, 2, 3, 4, 5] : [Math.max(1, Math.floor(curVal / 2)), curVal - 1, curVal, curVal + 1, curVal * 2];
        for (var i = 0; i < 5; i++)
        {
            $('#newgame-playto-' + i).val(newVals[i]);
            $('#newgame-playto-label-' + i + ' .text').text(newVals[i]);
        }
        $('#newgame-playto-' + newVals.indexOf(curVal)).prop('checked', true);
        return true;
    });

    var main = $('#waiting-games');
    var socket;
    var newSocket = function ()
    {
        socket = new WebSocket(main.data('socket-url'));
        socket.onopen = function ()
        {
            main.empty().removeClass('connecting');
        };
        socket.onclose = function ()
        {
            main.addClass('connecting loading');
            reconnect(true);
        };
        socket.onmessage = function (msg)
        {
            var json = JSON.parse(msg.data);
            if (json.add)
            {
                main.removeClass('loading');
                for (var i = 0; i < json.add.length; i++)
                {
                    var inf = json.add[i];
                    $('<li>')
                        .append($('<div>').addClass('piece ' + (inf.state === 'White_Waiting' ? 'black' : inf.state === 'Black_Waiting' ? 'white' : 'random')))
                        .append($('<div>').addClass('playto').text(inf.maxscore))
                        .append($('<div>').addClass('doubling-cube ' + inf.cube))
                        .attr('id', 'joingame-' + inf.id)
                        .data('id', inf.id)
                        .appendTo(main);
                }
            }
            if (json.remove)
                $('#joingame-' + json.remove).remove();
        };
    };
    var reconnectInterval = 0;
    var reconnect = function (useDelay)
    {
        main.addClass('connecting');
        try { socket.close(); } catch (e) { }
        if (useDelay)
        {
            reconnectInterval = (reconnectInterval === 0) ? 1000 : reconnectInterval * 2;
            window.setTimeout(newSocket, reconnectInterval);
        }
        else
        {
            reconnectInterval = 0;
            newSocket();
        }
    };
    reconnect();

    main.on('click', 'li', function ()
    {
        window.location.href = main.data('play-url') + '/' + $(this).data('id') + window.location.hash;
        return false;
    });
});
