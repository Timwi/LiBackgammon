$(function ()
{
    var board = $('#board');
    if (!board.length)
        return;

    // Game states
    var State = {
        WhiteWaiting: 0,
        BlackWaiting: 1,
        WhiteToRoll: 2,
        BlackToRoll: 3,
        WhiteToConfirmDouble: 4,
        BlackToConfirmDouble: 5,
        WhiteToMove: 6,
        BlackToMove: 7,
        WhiteFinished: 8,
        BlackFinished: 9,
        WhiteDoubledBlackRejected: 10,
        BlackDoubledWhiteRejected: 11,
        WhiteResigned: 12,
        BlackResigned: 13
    };

    // Special tongues
    var Tongue = {
        WhitePrison: 24,
        BlackPrison: 25,
        WhiteHome: 26,
        BlackHome: 27,
        NumTongues: 28
    };

    var moves = board.data('moves');
    var lastMove = moves[moves.length - 1];
    var state = board.data('state');
    var player = board.data('player');
    var playerIsWhite = player === 'White';
    var playerIsSpectator = !playerIsWhite && player !== 'Black';
    board.addClass(playerIsSpectator ? 'spectating' : playerIsWhite ? 'player-white' : 'player-black');
    var allValidMoves;

    function makeArrow(source, dest)
    {
        function midPoint(elem)
        {
            var pos = elem.position();
            return { left: pos.left + elem.width() / 2, top: pos.top + elem.height() / 2 };
        }

        var srcPos = midPoint(source), dstPos = midPoint(dest);
        var dx = dstPos.left - srcPos.left, dy = dstPos.top - srcPos.top;
        var dist = Math.sqrt(dx * dx + dy * dy);
        var angle = Math.atan2(dy, dx);
        var arrowThickness = 40;
        var arrowLength = dist - 80;

        return $('<div class="arrow">')
            .appendTo('#board')
            .css('left', (srcPos.left + dstPos.left) / 2 - arrowLength / 2)
            .css('top', (srcPos.top + dstPos.top) / 2 - arrowThickness / 2)
            .css('width', arrowLength - 35)
            .css('transform-origin', (arrowLength / 2) + 'px 50%')
            .css('transform', 'rotate(' + angle + 'rad)')
            .show();
    }

    function leftFromTongue(tongue)
    {
        if (tongue === Tongue.WhiteHome || tongue === Tongue.BlackHome)
            return 92.5;
        if (tongue === Tongue.WhitePrison || tongue === Tongue.BlackPrison)
            return 43.5;
        if (tongue < 12)
            return 3 + 7 * (11 - tongue) + (tongue < 6 ? 4 : 0);
        return 3 + 7 * (tongue - 12) + (tongue >= 18 ? 4 : 0);
    }

    function topFromTongue(tongue, pieceIndex)
    {
        if (tongue === Tongue.WhiteHome)
            return 2 + 1 + 1.425 * pieceIndex;
        if (tongue === Tongue.BlackHome)
            return (60 - 2 - 1 - 5) - 1.425 * pieceIndex;
        if (tongue === Tongue.WhitePrison)
            return 36 + 4 * pieceIndex;
        if (tongue === Tongue.BlackPrison)
            return 19 - 4 * pieceIndex;
        if (tongue < 12)
            return (60 - 2 - 5) - 5 * pieceIndex;
        return 2 + 5 * pieceIndex;
    }

    function resetUi(/* bool */ recalculateValidMoves)
    {
        board.removeClass('waiting joinable no-cube cube-white cube-black dice-2 dice-4 dice-start white-starts black-starts to-move roll-or-double waiting-to-roll-or-double confirm-double waiting-to-confirm-double');
        lastMove = moves[moves.length - 1];

        var whiteIndex = 0, blackIndex = 0;
        for (var tng = 0; tng < Tongue.NumTongues; tng++)
            for (var i = (tng < 12 ? position.NumPiecesPerTongue[tng] - 1 : 0) ; (tng < 12) ? (i >= 0) : (i < position.NumPiecesPerTongue[tng]) ; (tng < 12 ? i-- : i++))
                $('#' + (position.IsWhitePerTongue[tng] ? ('white-' + whiteIndex++) : ('black-' + blackIndex++))).insertBefore('#overlay-bottom').css({ left: leftFromTongue(tng) + "vw", top: topFromTongue(tng, i) + "vw" }).data('tongue', tng).show();

        if (position.GameValue === null)
            board.addClass('no-cube');
        else if (position.WhiteOwnsCube !== null)
            board.addClass(position.WhiteOwnsCube === true ? 'cube-white' : 'cube-black');
        $('#cube-text').text(position.GameValue);

        switch (state)
        {
            case State.WhiteWaiting:
            case State.BlackWaiting:
                board.addClass(((state === State.WhiteWaiting) === playerIsWhite) ? 'waiting' : 'joinable');
                break;

            case State.WhiteToRoll:
            case State.BlackToRoll:
                if (isPlayerToRollOrDouble())
                    board.addClass('roll-or-double');
                else
                    board.addClass('waiting-to-roll-or-double');
                break;

            case State.WhiteToConfirmDouble:
            case State.BlackToConfirmDouble:
                if (isPlayerToConfirmDouble())
                    board.addClass('confirm-double');
                else
                    board.addClass('waiting-to-confirm-double');
                break;

            case State.WhiteToMove:
            case State.BlackToMove:
                board.addClass(lastMove.Dice1 === lastMove.Dice2 ? 'dice-4' : 'dice-2');
                $('.dice').removeClass('val-1 val-2 val-3 val-4 val-5 val-6');
                $('#dice-0').addClass('val-' + lastMove.Dice1);
                $('#dice-1,#dice-2,#dice-3').addClass('val-' + lastMove.Dice2);
                if (moves.length === 1)
                    board.addClass('dice-start ' + (lastMove.Dice1 > lastMove.Dice2 ? 'white-starts' : 'black-starts'));
                if (isPlayerToMove())
                {
                    if (recalculateValidMoves)
                        allValidMoves = getAllMoves(position, playerIsWhite,
                            lastMove.Dice1 === lastMove.Dice2
                                ? [[lastMove.Dice1, lastMove.Dice1, lastMove.Dice1, lastMove.Dice1]]
                                : [[lastMove.Dice1, lastMove.Dice2], [lastMove.Dice2, lastMove.Dice1]]);
                    deselectPiece();
                    board.addClass('to-move');
                }
                break;

            case State.WhiteDoubledBlackRejected:
            case State.BlackDoubledWhiteRejected:
            case State.WhiteFinished:
            case State.BlackFinished:
            case State.WhiteResigned:
            case State.BlackResigned:
                board.addClass(
                    state === State.WhiteDoubledBlackRejected ? 'doubled white-wins' :
                    state === State.BlackDoubledWhiteRejected ? 'doubled black-wins' :
                    state === State.WhiteFinished ? 'white-wins' :
                    state === State.BlackFinished ? 'black-wins' :
                    state === State.WhiteResigned ? 'resigned black-wins' : 'resigned white-wins');
                $('#win>.points>.number').text(position.GameValue);
                $('#win>.points>.word').text(position.GameValue === 1 ? "point" : "points");
                break;
        }
    }

    function getPrison(/* bool */ whitePlayer) { return whitePlayer ? Tongue.WhitePrison : Tongue.BlackPrison; }
    function getHome(/* bool */ whitePlayer) { return whitePlayer ? Tongue.WhiteHome : Tongue.BlackHome; }

    function copyPosition(pos)
    {
        return {
            NumPiecesPerTongue: pos.NumPiecesPerTongue.slice(0),
            IsWhitePerTongue: pos.IsWhitePerTongue.slice(0),
            GameValue: pos.GameValue,
            WhiteOwnsCube: pos.WhiteOwnsCube
        };
    }

    function /* Position */ processMove(/* Position */ pos, /* bool */ whitePlayer, /* int or int[] */ sourceTongues, /* int or int[] */ targetTongues,
        /* 
            {
                mode: 'animate', 'indicate' or none
                callback: function to call after animation is done (mode 'animate' only)
                undoOpponentPieceTaken: bool
            }
        */ options)
    {
        var mode = (options && options.mode) || null;
        var callback = (options && options.callback) || null;
        var undoOpponentPieceTaken = (options && options.undoOpponentPieceTaken) || false;

        if (typeof sourceTongues === 'number')
        {
            var s = [sourceTongues];
            var t = [targetTongues];
            if (undoOpponentPieceTaken)
            {
                s.push(getPrison(!whitePlayer));
                t.push(sourceTongues);
            }
            return processMove(pos, whitePlayer, s, t, options);
        }

        var newPos = copyPosition(pos);
        var animationQueue = [];

        var piecesByTongue = [];
        for (var i = 0; i < Tongue.NumTongues; i++)
        {
            piecesByTongue[i] = [];
            var tonguePieces = $('.piece').filter(function () { return $(this).data('tongue') === i; });
            for (var j = 0; j < tonguePieces.length; j++)
                piecesByTongue[i].push($(tonguePieces[j]));
            if (i < 12)
                piecesByTongue[i].reverse();
        }

        function processSubmove(isWhite, sourceTongue, targetTongue)
        {
            newPos.NumPiecesPerTongue[sourceTongue]--;
            newPos.NumPiecesPerTongue[targetTongue]++;
            newPos.IsWhitePerTongue[targetTongue] = newPos.IsWhitePerTongue[sourceTongue];

            switch (mode)
            {
                case 'animate':
                    animationQueue.push({
                        Piece: piecesByTongue[sourceTongue][newPos.NumPiecesPerTongue[sourceTongue]],
                        Props: {
                            left: leftFromTongue(targetTongue) + "vw",
                            top: topFromTongue(targetTongue, newPos.NumPiecesPerTongue[targetTongue] - 1) + "vw"
                        }
                    });
                    piecesByTongue[targetTongue].push(piecesByTongue[sourceTongue].pop());
                    break;

                case 'indicate':
                    var hypo = $('<div class="piece hypo-target">').addClass(isWhite ? 'white' : 'black').appendTo(board).css({
                        left: leftFromTongue(targetTongue) + "vw",
                        top: topFromTongue(targetTongue, newPos.NumPiecesPerTongue[targetTongue] - 1) + "vw"
                    });
                    makeArrow(piecesByTongue[sourceTongue][newPos.NumPiecesPerTongue[sourceTongue]], hypo);
                    piecesByTongue[sourceTongue].pop();
                    piecesByTongue[targetTongue].push(hypo);
                    break;
            }
        }

        for (var k = 0; k < sourceTongues.length; k++)
        {
            var sourceTongue = sourceTongues[k];
            var targetTongue = targetTongues[k];

            if ((newPos.NumPiecesPerTongue[sourceTongue] === 0 || newPos.IsWhitePerTongue[sourceTongue] !== whitePlayer) && !undoOpponentPieceTaken)
                // There are no pieces on the source tongue belonging to the correct player
                return null;

            var swap = false;
            if (newPos.IsWhitePerTongue[targetTongue] === !whitePlayer)
            {
                if (newPos.NumPiecesPerTongue[targetTongue] > 1)
                    // The target tongue is blocked by the opponent
                    return null;

                if (newPos.NumPiecesPerTongue[targetTongue] === 1)
                {
                    // Move is permissible, but an opponent piece is taken
                    processSubmove(!whitePlayer, targetTongue, getPrison(!whitePlayer));
                    swap = true;
                }
            }
            processSubmove(whitePlayer, sourceTongue, targetTongue);
            if (swap && mode === 'animate')
            {
                var t = animationQueue[animationQueue.length - 1];
                animationQueue[animationQueue.length - 1] = animationQueue[animationQueue.length - 2];
                animationQueue[animationQueue.length - 2] = t;
            }
        }

        function processAnimationQueue()
        {
            if (animationQueue.length === 0)
            {
                resetUi(false);
                deselectPiece();
                if (callback !== null)
                    callback();
                return;
            }
            var item = animationQueue.shift();
            item.Piece.insertBefore('#overlay-bottom');
            item.Piece.animate(item.Props, { complete: processAnimationQueue });
        }
        if (animationQueue.length > 0)
            processAnimationQueue();
        return newPos;
    }

    function /* int */ getTargetTongue(/* int */ sourceTongue, /* int */ furthestFromHome, /* int */ dice, /* bool */ whitePlayer)
    {
        if (sourceTongue === getPrison(whitePlayer))
            return whitePlayer ? dice - 1 : 24 - dice;
        var fromHome = whitePlayer ? 24 - sourceTongue : sourceTongue + 1;
        if ((fromHome === dice && furthestFromHome !== null && furthestFromHome <= 6) || (fromHome <= dice && fromHome === furthestFromHome))
            return getHome(whitePlayer);
        var target = whitePlayer ? sourceTongue + dice : sourceTongue - dice;
        return (target >= 0 && target < 24) ? target : null;
    }

    function /* string */ stringifyPosition(/* Position */ position)
    {
        var str = "";
        for (var i = 0; i < Tongue.NumTongues; i++)
            str += (position.NumPiecesPerTongue[i] > 0 ? (position.IsWhitePerTongue[i] ? "W" : "B") : "-") + position.NumPiecesPerTongue[i];
        return str;
    }

    function /* void */ addValidMoves(/* Dict */ movesByLength, /* Position */ position, /* bool */ whitePlayer, /* int[] */ diceSequence, /* int[] */ prevDiceSequence, /* int[] */ sourceTongues, /* int[] */ targetTongues, /* bool[] */ opponentPieceTaken)
    {
        if (sourceTongues.length > 0 && sourceTongues.length >= movesByLength.curMax)
        {
            var move = { SourceTongues: sourceTongues, TargetTongues: targetTongues, OpponentPieceTaken: opponentPieceTaken, DiceSequence: prevDiceSequence, EndPosition: position };
            if (sourceTongues.length > movesByLength.curMax)
            {
                movesByLength.curMax = sourceTongues.length;
                movesByLength.moves = [];
            }
            movesByLength.moves.push(move);
        }

        if (diceSequence.length === 0)
            return;

        var prison = getPrison(whitePlayer);
        var home = getHome(whitePlayer);

        // Which tongues can the player move a piece from?
        var accessibleTongues = [];
        // How far from home is the furthest piece? (if it’s 4, say, then you can use a 6 to move the 4-away pieces into home)
        var furthestFromHome = null;

        if (position.NumPiecesPerTongue[prison] > 0)
            accessibleTongues.push(prison);
        else
        {
            for (var tng = 0; tng < Tongue.NumTongues; tng++)
            {
                if (tng === prison || tng === home || position.NumPiecesPerTongue[tng] === 0 || position.IsWhitePerTongue[tng] !== whitePlayer)
                    continue;
                accessibleTongues.push(tng);
                furthestFromHome = Math.max(furthestFromHome || 0, whitePlayer ? (24 - tng) : (tng + 1));
            }
        }

        for (var accIx = 0; accIx < accessibleTongues.length; accIx++)
        {
            var target = getTargetTongue(accessibleTongues[accIx], furthestFromHome, diceSequence[0], whitePlayer);
            if (target === null || (position.NumPiecesPerTongue[target] > 1 && position.IsWhitePerTongue[target] !== whitePlayer))
                continue;
            var pieceTaken = position.NumPiecesPerTongue[target] === 1 && position.IsWhitePerTongue[target] !== whitePlayer;
            var pMove = processMove(position, whitePlayer, accessibleTongues[accIx], target);
            if (pMove === null)
                continue;
            var p = prevDiceSequence.slice(0);
            p.push(diceSequence[0]);
            var s = sourceTongues.slice(0);
            s.push(accessibleTongues[accIx]);
            var t = targetTongues.slice(0);
            t.push(target);
            var o = opponentPieceTaken.slice(0);
            o.push(pieceTaken);
            addValidMoves(movesByLength, pMove, whitePlayer, diceSequence.slice(1), p, s, t, o);
        }
    }

    function /* Dict */ getAllMoves(/* Position */ position, /* bool */ whitePlayer, /* int[][] */ diceSequences)
    {
        var validMoves = { curMax: 0, moves: [] };
        for (var seqIx = 0; seqIx < diceSequences.length; seqIx++)
            addValidMoves(validMoves, position, whitePlayer, diceSequences[seqIx], [], [], [], []);
        return validMoves.moves;
    }

    function topPieceOfTongue(tongue)
    {
        // $('.piece[data-tongue="x"]') does not work due to a bug in jQuery
        var tonguePieces = $('.piece').filter(function () { return $(this).data('tongue') === tongue; });
        return $(tonguePieces[tongue < 12 ? 0 : tonguePieces.length - 1]);
    }

    function isWhiteToMove()
    {
        return state === State.WhiteToMove || state === State.WhiteToRoll;
    }

    function isPlayerToMove()
    {
        return !playerIsSpectator && (
            (state === State.WhiteToMove && playerIsWhite) ||
            (state === State.BlackToMove && !playerIsWhite));
    }

    function isPlayerToRollOrDouble()
    {
        return !playerIsSpectator && (
            (state === State.WhiteToRoll && playerIsWhite) ||
            (state === State.BlackToRoll && !playerIsWhite));
    }

    function isPlayerToConfirmDouble()
    {
        return !playerIsSpectator && (
            (state === State.WhiteToConfirmDouble && playerIsWhite) ||
            (state === State.BlackToConfirmDouble && !playerIsWhite));
    }

    var position = board.data('initial');
    if (moves.length > 0)
    {
        var whiteStarts = moves[0].Dice1 > moves[0].Dice2;
        for (var i = 0; i < moves.length; i++)
        {
            var whitePlayer = whiteStarts ? (i % 2 === 0) : (i % 2 !== 0);
            if (moves[i].Doubled && position.GameValue !== null)
            {
                position.GameValue *= 2;
                position.WhiteOwnsCube = !whitePlayer;
            }
            if ('SourceTongues' in moves[i])
                for (var j = 0; j < moves[i].SourceTongues.length; j++)
                    position = processMove(position, whitePlayer, moves[i].SourceTongues[j], moves[i].TargetTongues[j]);
        }
    }

    var moveSoFar = { SourceTongues: [], TargetTongues: [], OpponentPieceTaken: [], DiceSequence: [] };
    var selectedPiece = null;

    resetUi(true);

    var socket;
    var sendQueue = [];
    var socketQueue = [];
    var socketQueueProcessing = false;

    var processSocketQueue = function ()
    {
        if (!socketQueue.length)
        {
            socketQueueProcessing = false;
            return;
        }

        socketQueueProcessing = true;
        var json = socketQueue.shift();

        if ('move' in json)
        {
            moves[moves.length - 1].SourceTongues = json.move.SourceTongues;
            moves[moves.length - 1].TargetTongues = json.move.TargetTongues;
            position = processMove(position, isWhiteToMove(), json.move.SourceTongues, json.move.TargetTongues, { mode: 'animate', callback: processSocketQueue });
        }
        if ('dice' in json)
        {
            $('.dice').removeClass('crossed');
            moveSoFar = { SourceTongues: [], TargetTongues: [], OpponentPieceTaken: [], DiceSequence: [] };
            moves.push({ Dice1: json.dice[0], Dice2: json.dice[1] });
            // TODO: Dice roll animation
            processSocketQueue();
        }
        if ('cube' in json)
        {
            $('#cube-text').text(json.cube[0]);
            board.removeClass('cube-white cube-black').addClass(json.cube[1] ? 'cube-white' : 'cube-black');
            position.GameValue = json.cube[0];
            position.WhiteOwnsCube = json.cube[1];
            window.setTimeout(processSocketQueue, 1000);
        }
        if ('state' in json)
        {
            state = json.state;
            resetUi(true);
            processSocketQueue();
        }
    };

    var newSocket = function ()
    {
        socket = new WebSocket((window.location.protocol === 'http:' ? 'ws://' : 'wss://') + window.location.host + '/socket/' + board.data('gameid') + board.data('token'));
        socket.onopen = function ()
        {
            board.removeClass('connecting');
            for (var i = 0; i < sendQueue.length; i++)
                socket.send(JSON.stringify(sendQueue[i]));
            sendQueue = [];
        };
        socket.onclose = function ()
        {
            reconnect(true);
        };
        socket.onmessage = function (msg)
        {
            var json = JSON.parse(msg.data);
            if (json instanceof Array)
                for (var i = 0; i < json.length; i++)
                    socketQueue.push(json[i]);
            else
                socketQueue.push(json);
            if (!socketQueueProcessing)
                processSocketQueue();
        };
    };
    var reconnectInterval = 0;
    var reconnect = function (useDelay)
    {
        board.addClass('connecting');
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

    var socketSend = function (msg)
    {
        if (socket && socket.readyState === socket.OPEN)
            socket.send(JSON.stringify(msg));
        else
            sendQueue.push(msg);
    };

    function getClickableSourceTongues()
    {
        var result = [];
        if (allValidMoves.length === 0 || allValidMoves[0].SourceTongues.length === moveSoFar.DiceSequence.length)
            return result;
        for (var i = 0; i < allValidMoves.length; i++)
        {
            var applicable = true;
            for (var j = 0; j < moveSoFar.DiceSequence.length; j++)
                if (allValidMoves[i].DiceSequence[j] !== moveSoFar.DiceSequence[j] ||
                    allValidMoves[i].SourceTongues[j] !== moveSoFar.SourceTongues[j] ||
                    allValidMoves[i].TargetTongues[j] !== moveSoFar.TargetTongues[j])
                    applicable = false;
            if (!applicable)
                continue;
            for (var k = moveSoFar.DiceSequence.length; k < allValidMoves[i].SourceTongues.length; k++)
            {
                var tongue = allValidMoves[i].SourceTongues[k];
                if (position.NumPiecesPerTongue[tongue] > 0 && position.IsWhitePerTongue[tongue] === playerIsWhite && result.indexOf(tongue) === -1)
                    result.push(tongue);
            }
        }
        return result;
    }

    function deselectPiece(skipHighlight)
    {
        $('.piece.hypo-target, .arrow, .tongue.selectable, .home.selectable').remove();
        $('.piece').removeClass('selectable selected');
        selectedPiece = null;

        // Highlight all the clickable pieces
        if (isPlayerToMove() && !skipHighlight)
        {
            var clickable = getClickableSourceTongues();
            for (var tongue = 0; tongue < clickable.length; tongue++)
                topPieceOfTongue(clickable[tongue]).addClass('selectable');
        }
    }

    function selectPiece(tongue)
    {
        deselectPiece(true);
        selectedPiece = topPieceOfTongue(tongue).addClass('selected');

        // Find valid target tongues
        var targetMoves = {};
        for (var i = 0; i < allValidMoves.length; i++)
        {
            var applicable = true;
            for (var j = 0; j < moveSoFar.DiceSequence.length; j++)
                if (allValidMoves[i].DiceSequence[j] !== moveSoFar.DiceSequence[j])
                    applicable = false;
            if (applicable)
            {
                var acc = [tongue];
                for (var k = moveSoFar.DiceSequence.length; k < allValidMoves[i].SourceTongues.length; k++)
                    if (acc.indexOf(allValidMoves[i].SourceTongues[k]) !== -1)
                    {
                        var targetTongue = allValidMoves[i].TargetTongues[k];
                        acc.push(targetTongue);
                        var m = {
                            SourceTongues: allValidMoves[i].SourceTongues.slice(moveSoFar.DiceSequence.length, k + 1),
                            TargetTongues: allValidMoves[i].TargetTongues.slice(moveSoFar.DiceSequence.length, k + 1),
                            OpponentPieceTaken: allValidMoves[i].OpponentPieceTaken.slice(moveSoFar.DiceSequence.length, k + 1),
                            DiceSequence: allValidMoves[i].DiceSequence.slice(moveSoFar.DiceSequence.length, k + 1),
                            Priority: allValidMoves[i].EndPosition.NumPiecesPerTongue[getPrison(!playerIsWhite)]
                        };
                        if (!(targetTongue in targetMoves) || m.Priority > targetMoves[targetTongue].Priority || (m.Priority === targetMoves[targetTongue].Priority && m.SourceTongues.length < targetMoves[targetTongue].SourceTongues.length))
                            targetMoves[targetTongue] = m;
                    }
            }
        }

        Object.keys(targetMoves).map(function (i)
        {
            var rawTongueElem = $('.tongue-' + i);
            return (rawTongueElem.length
                ? rawTongueElem.clone().addClass('selectable')
                : $('<div>').addClass('selectable ' + (+i === Tongue.WhiteHome ? 'white home' : 'black home'))
            ).data('move', targetMoves[i]).insertBefore('#overlay-bottom');
        });
    }

    if (!playerIsSpectator)
    {
        deselectPiece();
        $('.piece').click(function ()
        {
            if (!isPlayerToMove())
                return false;

            // Only allow clicking on a piece of the player’s color
            if ($(this).hasClass('white') !== playerIsWhite)
                return false;

            // See if the player has any valid moves (or they’ve already entered their move)
            var clickableTongues = getClickableSourceTongues();
            if (clickableTongues.length === 0)
                return false;

            // Only allow clicking on a tongue that is a valid source tongue
            var tongue = $(this).data('tongue');
            if (clickableTongues.indexOf(tongue) === -1)
                return false;

            if (topPieceOfTongue(tongue).is(selectedPiece))
                deselectPiece();
            else
                selectPiece(tongue);
            return false;
        });

        board.on('mouseenter', '.tongue.selectable, .home.selectable', function ()
        {
            var move = $(this).data('move');
            processMove(position, playerIsWhite, move.SourceTongues, move.TargetTongues, { mode: 'indicate' });
        });

        board.on('mouseleave', '.tongue.selectable, .home.selectable', function ()
        {
            $('.piece.hypo-target, .arrow').hide();
        });

        board.on('click', '.tongue.selectable, .home.selectable', function ()
        {
            $('.piece.hypo-target, .arrow').hide();
            var move = $(this).data('move');
            deselectPiece(true);
            position = processMove(position, playerIsWhite, move.SourceTongues, move.TargetTongues, { mode: 'animate' });
            for (var i = 0; i < move.DiceSequence.length; i++)
            {
                moveSoFar.DiceSequence.push(move.DiceSequence[i]);
                moveSoFar.SourceTongues.push(move.SourceTongues[i]);
                moveSoFar.TargetTongues.push(move.TargetTongues[i]);
                moveSoFar.OpponentPieceTaken.push(move.OpponentPieceTaken[i]);
                $('.dice:not(.crossed).val-' + move.DiceSequence[i]).first().addClass('crossed');
            }
            board.addClass('undoable');
            if (moveSoFar.DiceSequence.length === allValidMoves[0].DiceSequence.length)
                board.addClass('committable');
        });

        $(document).keypress(function (e)
        {
            if (e.keyCode === 27 && selectedPiece !== null)
                deselectPiece();
        });

        $('#undo').click(function ()
        {
            if (playerIsSpectator)
                return false;
            var lastIndex = moveSoFar.DiceSequence.length - 1;
            if (lastIndex >= 0)
            {
                position = processMove(position, playerIsWhite, moveSoFar.TargetTongues[lastIndex], moveSoFar.SourceTongues[lastIndex], { mode: 'animate', undoOpponentPieceTaken: moveSoFar.OpponentPieceTaken[lastIndex] });
                $('.dice.crossed.val-' + moveSoFar.DiceSequence[lastIndex]).last().removeClass('crossed');
                moveSoFar.DiceSequence.pop();
                moveSoFar.SourceTongues.pop();
                moveSoFar.TargetTongues.pop();
                moveSoFar.OpponentPieceTaken.pop();
            }
            board.removeClass('committable');
            if (moveSoFar.DiceSequence.length === 0)
                board.removeClass('undoable');
            return false;
        });

        function getGeneralisedButtonClick(msgToSend)
        {
            return function ()
            {
                if (!playerIsSpectator)
                {
                    socketSend(typeof msgToSend === 'function' ? msgToSend() : msgToSend);
                    board.removeClass('undoable committable roll-or-double confirm-double');
                }
                return false;
            };
        }

        $('#commit').click(getGeneralisedButtonClick(function () { return { move: { SourceTongues: moveSoFar.SourceTongues, TargetTongues: moveSoFar.TargetTongues } }; }));
        $('#roll').click(getGeneralisedButtonClick({ roll: 1 }));
        $('#double').click(getGeneralisedButtonClick({ double: 1 }));
        $('#accept').click(getGeneralisedButtonClick({ accept: 1 }));
        $('#reject').click(getGeneralisedButtonClick({ reject: 1 }));
    }
});
