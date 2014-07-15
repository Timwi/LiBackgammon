$(function ()
{
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
        WhiteWon: 8,
        BlackWon: 9
    };

    // Special tongues
    var Tongue = {
        WhitePrison: 24,
        BlackPrison: 25,
        WhiteHome: 26,
        BlackHome: 27,
        NumTongues: 28
    };

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
            return 92;
        if (tongue === Tongue.WhitePrison || tongue === Tongue.BlackPrison)
            return 43.5;
        if (tongue < 12)
            return 3 + 7 * (11 - tongue) + (tongue < 6 ? 4 : 0);
        return 3 + 7 * (tongue - 12) + (tongue >= 18 ? 4 : 0);
    }

    function topFromTongue(tongue, pieceIndex)
    {
        if (tongue === Tongue.WhiteHome)
            return 2 + pieceIndex;
        if (tongue === Tongue.BlackHome)
            return (60 - 2 - 5) - pieceIndex;
        if (tongue === Tongue.WhitePrison)
            return 36 + 4 * pieceIndex;
        if (tongue === Tongue.BlackPrison)
            return 19 - 4 * pieceIndex;
        if (tongue < 12)
            return (60 - 2 - 5) - 5 * pieceIndex;
        return 2 + 5 * pieceIndex;
    }

    function resetUi()
    {
        board.removeClass('no-cube cube-white cube-black dice-2 dice-4 dice-start to-move');

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
            case State.WhiteToConfirmDouble:
            case State.BlackToConfirmDouble:
                break;

            case State.WhiteToMove:
            case State.BlackToMove:
                if (lastMove.Dice1 === lastMove.Dice2)
                {
                    board.addClass('dice-4');
                    for (var j = 0; j < 4; j++)
                        $('#dice-' + j).addClass('val-' + lastMove.Dice1);
                }
                else
                {
                    board.addClass('dice-2');
                    $('#dice-0').addClass('val-' + lastMove.Dice1);
                    $('#dice-1').addClass('val-' + lastMove.Dice2);
                }
                if (moves.length === 1)
                    board.addClass('dice-start');
                if (isPlayerToMove())
                    board.addClass('to-move');
                break;

            case State.WhiteWon:
            case State.BlackWon:
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

    function /* Position */ processMove(/* Position */ pos, /* bool */ whitePlayer, /* int or int[] */ sourceTongues, /* int or int[] */ targetTongues, /* enum('animate', 'indicate')? */ mode)
    {
        if (typeof sourceTongues === 'number')
            return processMove(pos, whitePlayer, [sourceTongues], [targetTongues], mode);

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
            newPos.IsWhitePerTongue[targetTongue] = isWhite;

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

            if (newPos.NumPiecesPerTongue[sourceTongue] === 0 || newPos.IsWhitePerTongue[sourceTongue] !== whitePlayer)
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
                resetUi();
                deselectPiece();
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

    function /* void */ addValidMoves(/* Dict */ movesByLength, /* Position */ position, /* bool */ whitePlayer, /* int[] */ diceSequence, /* int[] */ prevDiceSequence, /* int[] */ sourceTongues, /* int[] */ targetTongues)
    {
        if (sourceTongues.length > 0)
        {
            var move = { SourceTongues: sourceTongues, TargetTongues: targetTongues, DiceSequence: prevDiceSequence, EndPosition: position };
            if (!(sourceTongues.length in movesByLength))
                movesByLength[sourceTongues.length] = [];
            movesByLength[sourceTongues.length].push(move);
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
            var pMove = processMove(position, whitePlayer, accessibleTongues[accIx], target);
            if (pMove === null)
                continue;
            var p = prevDiceSequence.slice(0);
            p.push(diceSequence[0]);
            var s = sourceTongues.slice(0);
            s.push(accessibleTongues[accIx]);
            var t = targetTongues.slice(0);
            t.push(target);
            addValidMoves(movesByLength, pMove, whitePlayer, diceSequence.slice(1), p, s, t);
        }
    }

    function /* Dict */ getAllMoves(/* Position */ position, /* bool */ whitePlayer, /* int[][] */ diceSequences)
    {
        var validMoves = [];
        for (var seqIx = 0; seqIx < diceSequences.length; seqIx++)
            addValidMoves(validMoves, position, whitePlayer, diceSequences[seqIx], [], [], []);

        // Only the moves with the greatest length are valid
        var greatestLength = null;
        for (var i = 0; i < validMoves.length; i++)
            if (i in validMoves)
                greatestLength = greatestLength === null ? i : Math.max(i, greatestLength);
        return validMoves[greatestLength];
    }

    function topPieceOfTongue(tongue)
    {
        // $('.piece[data-tongue="x"]') does not work due to a bug in jQuery
        var tonguePieces = $('.piece').filter(function () { return $(this).data('tongue') === tongue; });
        return $(tonguePieces[tongue < 12 ? 0 : tonguePieces.length - 1]);
    }

    var board = $('#board');
    var moves = board.data('moves');
    var lastMove = moves[moves.length - 1];
    var state = board.data('state');
    var player = board.data('player');
    var playerIsWhite = player === 'White';
    var playerIsSpectator = !playerIsWhite && player !== 'Black';
    board.addClass(playerIsSpectator ? 'spectating' : playerIsWhite ? 'player-white' : 'player-black');

    function isPlayerToMove()
    {
        if (playerIsSpectator)
            return false;
        if (state === State.WhiteToMove && playerIsWhite)
            return true;
        if (state === State.BlackToMove && !playerIsWhite)
            return true;
        return false;
    }

    var position = board.data('initial');
    var whiteStarts = moves[0].Dice1 > moves[0].Dice2;
    for (var i = 0; i < moves.length; i++)
        if ('SourceTongues' in moves[i])
            for (var j = 0; j < moves[i].SourceTongues.length; j++)
                position = processMove(position, whiteStarts ^ (i % 2 === 0), moves[i].SourceTongues[j], moves[i].TargetTongues[j], false);

    var allValidMoves = getAllMoves(position, playerIsWhite,
        lastMove.Dice1 === lastMove.Dice2
            ? [[lastMove.Dice1, lastMove.Dice1, lastMove.Dice1, lastMove.Dice1]]
            : [[lastMove.Dice1, lastMove.Dice2], [lastMove.Dice2, lastMove.Dice1]]);
    var moveSoFar = { SourceTongues: [], TargetTongues: [], DiceSequence: [] };
    var selectedPiece = null;

    resetUi();

    function getClickableSourceTongues()
    {
        var result = [];
        if (allValidMoves.length === 0 || allValidMoves[0].SourceTongues.length === moveSoFar.DiceSequence.length)
            return result;
        for (var i = 0; i < allValidMoves.length; i++)
        {
            var applicable = true;
            for (var j = 0; j < moveSoFar.DiceSequence.length; j++)
                if (allValidMoves[i].DiceSequence[j] !== moveSoFar.DiceSequence[j])
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
        $('.piece.hypo-target, .arrow, .tongue.selectable').remove();
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
        deselectPiece();
        selectedPiece = topPieceOfTongue(tongue).addClass('selected');

        // Find valid target tongues
        var targetTongues = $();
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
                        targetTongues = targetTongues.add('.tongue-' + targetTongue);
                        acc.push(targetTongue);
                        var m = {
                            SourceTongues: allValidMoves[i].SourceTongues.slice(moveSoFar.DiceSequence.length, k + 1),
                            TargetTongues: allValidMoves[i].TargetTongues.slice(moveSoFar.DiceSequence.length, k + 1),
                            DiceSequence: allValidMoves[i].DiceSequence.slice(moveSoFar.DiceSequence.length, k + 1),
                            Priority: allValidMoves[i].EndPosition.NumPiecesPerTongue[getPrison(!playerIsWhite)]
                        };
                        if (!(targetTongue in targetMoves) || m.Priority > targetMoves[targetTongue].Priority || (m.Priority === targetMoves[targetTongue].Priority && m.SourceTongues.length < targetMoves[targetTongue].SourceTongues.length))
                            targetMoves[targetTongue] = m;
                    }
            }
        }
        var clones = targetTongues
            .clone()
            .addClass('selectable')
            .insertBefore('#overlay-bottom');
        for (var l = 0; l < clones.length; l++)
            $(clones[l]).data('move', targetMoves[$(clones[l]).data('tongue')]);
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

        board.on('mouseenter', '.tongue.selectable', function ()
        {
            var move = $(this).data('move');
            processMove(position, playerIsWhite, move.SourceTongues, move.TargetTongues, 'indicate');
        });

        board.on('mouseleave', '.tongue.selectable', function ()
        {
            $('.piece.hypo-target, .arrow').hide();
        });

        board.on('click', '.tongue.selectable', function ()
        {
            $('.piece.hypo-target, .arrow').hide();
            var move = $(this).data('move');
            deselectPiece(true);
            position = processMove(position, playerIsWhite, move.SourceTongues, move.TargetTongues, 'animate');
            for (var i = 0; i < move.DiceSequence.length; i++)
            {
                moveSoFar.DiceSequence.push(move.DiceSequence[i]);
                moveSoFar.SourceTongues.push(move.SourceTongues[i]);
                moveSoFar.TargetTongues.push(move.TargetTongues[i]);
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
                position = processMove(position, playerIsWhite, moveSoFar.TargetTongues[lastIndex], moveSoFar.SourceTongues[lastIndex], 'animate');
                $('.dice.crossed.val-' + moveSoFar.DiceSequence[lastIndex]).last().removeClass('crossed');
                moveSoFar.DiceSequence.pop();
                moveSoFar.SourceTongues.pop();
                moveSoFar.TargetTongues.pop();
            }
            board.removeClass('committable');
            if (moveSoFar.DiceSequence.length === 0)
                board.removeClass('undoable');
            return false;
        });
    }
});
