$(function ()
{
    function makeArrow(source, dest)
    {
        var pieceSize = 5; // vw

        function midPoint(elem)
        {
            var tongue = +elem.data('tongue');
            return {
                left: leftFromTongue(tongue) + pieceSize / 2,
                top: topFromTongue(tongue, +elem.data('index'), +elem.data('num')) + pieceSize / 2
            };
        }

        var srcPos = midPoint($(source)), dstPos = midPoint($(dest));
        var dx = dstPos.left - srcPos.left, dy = dstPos.top - srcPos.top;
        var dist = Math.sqrt(dx * dx + dy * dy);
        var angle = Math.atan2(dy, dx);
        var arrowThickness = 2.5; // vw
        var arrowLength = dist - 4; // vw

        return $('<div class="arrow">')
            .css({
                left: convertFromVw((srcPos.left + dstPos.left) / 2 - arrowLength / 2),
                top: convertFromVw((srcPos.top + dstPos.top) / 2 - arrowThickness / 2),
                width: convertFromVw(arrowLength - 2),
                transformOrigin: convertFromVw(arrowLength / 2) + ' 50%',
                transform: 'rotate(' + angle + 'rad)'
            });
    }

    function leftFromTongue(tongue)
    {
        if (tongue === Tongue.WhiteHome || tongue === Tongue.BlackHome)
            return 90.5;
        if (tongue === Tongue.WhitePrison || tongue === Tongue.BlackPrison)
            return 41.5;
        if (tongue < 12)
            return 1 + 7 * (11 - tongue) + (tongue < 6 ? 4 : 0);
        return 1 + 7 * (tongue - 12) + (tongue >= 18 ? 4 : 0);
    }

    function topFromTongue(tongue, pieceIndex, numPieces)
    {
        if (tongue === Tongue.WhiteHome)
            return 1 + 1.425 * pieceIndex;
        if (tongue === Tongue.BlackHome)
            return (58 - 2 - 1 - 5) - 1.425 * pieceIndex;
        if (tongue === Tongue.WhitePrison)
            return 34 + 4 * pieceIndex;
        if (tongue === Tongue.BlackPrison)
            return 17 - 4 * pieceIndex;
        if (tongue < 12)
            return (58 - 2 - 5) - (20 / Math.max(4, numPieces - 1)) * pieceIndex;
        return (20 / Math.max(4, numPieces - 1)) * pieceIndex;
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
        var arrows = [];

        var piecesByTongue;
        if (options && (options.mode === 'animate' || options.mode === 'indicate'))
        {
            piecesByTongue = [];
            for (var i = 0; i < Tongue.NumTongues; i++)
            {
                piecesByTongue[i] = getPiecesOnTongue(i).get();
                if (i < 12)
                    piecesByTongue[i].reverse();
            }
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
                        Piece: $(piecesByTongue[sourceTongue][newPos.NumPiecesPerTongue[sourceTongue]]),
                        Data: {
                            tongue: targetTongue,
                            index: newPos.NumPiecesPerTongue[targetTongue] - 1,
                            num: newPos.NumPiecesPerTongue[targetTongue]
                        }
                    });
                    piecesByTongue[targetTongue].push(piecesByTongue[sourceTongue].pop());
                    break;

                case 'indicate':
                    var hypo = $('<div class="piece hypo-target">')
                        .addClass(isWhite ? 'white' : 'black')
                        .appendTo($('#board'))
                        .css({ left: convertFromVw(leftFromTongue(targetTongue)), top: convertFromVw(topFromTongue(targetTongue, newPos.NumPiecesPerTongue[targetTongue] - 1, newPos.NumPiecesPerTongue[targetTongue] - 1)) })
                        .data({ tongue: targetTongue, index: newPos.NumPiecesPerTongue[targetTongue] - 1, num: newPos.NumPiecesPerTongue[targetTongue] - 1 });
                    arrows.push(makeArrow(piecesByTongue[sourceTongue][newPos.NumPiecesPerTongue[sourceTongue]], hypo));
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

        for (var i = 0; i < arrows.length; i++)
            arrows[i].appendTo($('#board'));

        function processAnimationQueue()
        {
            if (animationQueue.length === 0)
            {
                var pipsWhite = 0, pipsBlack = 0;
                for (var t = 0; t < 24; t++)
                    if (newPos.IsWhitePerTongue[t])
                        pipsWhite += (24 - t) * newPos.NumPiecesPerTongue[t];
                    else
                        pipsBlack += (t + 1) * newPos.NumPiecesPerTongue[t];
                $('#pipcount-white').text(pipsWhite + 25 * newPos.NumPiecesPerTongue[Tongue.WhitePrison]);
                $('#pipcount-black').text(pipsBlack + 25 * newPos.NumPiecesPerTongue[Tongue.BlackPrison]);

                if (callback !== null)
                    callback();
                return;
            }
            var item = animationQueue.shift();
            var num = new Array();
            num[item.Data.tongue] = getPiecesOnTongue(item.Data.tongue).length + 1;
            num[item.Piece.data('tongue')] = getPiecesOnTongue(item.Piece.data('tongue')).length - 1;
            var otherPieces = $('#board>.piece')
                .filter(function (i, elem) { return $(elem).data('tongue') === item.Data.tongue || $(elem).data('tongue') === item.Piece.data('tongue'); })
                .not(item.Piece)
                .get()
                .map(function (elem)
                {
                    var tongue = $(elem).data('tongue');
                    return [$(elem), {
                        left: convertFromVw(leftFromTongue(tongue)),
                        top: convertFromVw(topFromTongue(tongue, $(elem).data('index'), num[tongue]))
                    }];
                });
            if ((item.Piece.data('tongue') < 24 && num[item.Piece.data('tongue')] > 4) || (item.Data.tongue < 24 && num[item.Data.tongue] > 5))
                setTimeout(function () { otherPieces.forEach(function (inf) { inf[0].animate(inf[1], 200); }); }, 100);
            item.Piece
                .data(item.Data)
                .insertAfter($('#board>.piece').last())
                .animate({
                    left: convertFromVw(leftFromTongue(item.Data.tongue)),
                    top: convertFromVw(topFromTongue(item.Data.tongue, item.Data.index, item.Data.num))
                }, {
                    duration: 400,
                    complete: function ()
                    {
                        setTimeout(function ()
                        {
                            if (item.Data.tongue < 12)
                                item.Piece.insertBefore($('#board>.piece').first());
                            processAnimationQueue();
                        }, 10);
                    }
                });
        }
        if (animationQueue.length > 0)
            processAnimationQueue();
        else if (callback)
            setTimeout(callback, 100);
        return newPos;
    }

    function /* $ */ getPiecesOnTongue(tongue)
    {
        // $('#board>.piece[data-tongue="x"]') does not work; jQuery’s .data() does not actually change the attributes
        return $('#board>.piece').filter(function () { return $(this).data('tongue') === tongue; });
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
        var tonguePieces = getPiecesOnTongue(tongue);
        return $(tonguePieces[tongue < 12 ? 0 : tonguePieces.length - 1]);
    }

    function isPlayerToMove()
    {
        return !!$('#main.player-white.state-White.state-ToMove,#main.player-black.state-Black.state-ToMove').length;
    }

    function isSidebarOn()
    {
        return !!$('#main.with-sidebar').length;
    }

    function setState(newState, skipHighlight)
    {
        if (newState)
        {
            state = newState;
            main.removeClass(function (_, cl) { return 'auto-0 auto-1 undoable committable ' + cl.split(' ').filter(function (c) { return c.substr(0, "state-".length) === "state-"; }).join(' '); });
            newState.split('_').forEach(function (cl) { main.addClass('state-' + cl); });
        }
        if (!main.hasClass('state-ToMove'))
            main.removeClass('dice-start dice-2 dice-4');
        if (isPlayerToMove() && !skipHighlight)
        {
            allValidMoves = getAllMoves(position, playerIsWhite,
                lastMove.Dice1 === lastMove.Dice2
                    ? [[lastMove.Dice1, lastMove.Dice1, lastMove.Dice1, lastMove.Dice1]]
                    : [[lastMove.Dice1, lastMove.Dice2], [lastMove.Dice2, lastMove.Dice1]]);
            allValidRestMoves = allValidMoves;
            deselectPiece();
        }
    }

    function getClickableSourceTongues()
    {
        var result = [];
        for (var i = 0; i < allValidRestMoves.length; i++)
            for (var k = 0; k < allValidRestMoves[i].SourceTongues.length; k++)
                if (result.indexOf(allValidRestMoves[i].SourceTongues[k]) === -1)
                    result.push(allValidRestMoves[i].SourceTongues[k]);
        return result;
    }

    function deselectPiece(skipHighlight)
    {
        $('#board>.piece.hypo-target, #board>.arrow, #board>.tongue.selectable, #board>.home.selectable, #board>.automove').remove();
        $('#board>.piece').removeClass('selectable selected');
        selectedPiece = null;

        if (isPlayerToMove() && !skipHighlight)
        {
            // Highlight all the clickable pieces
            var clickable = getClickableSourceTongues();
            for (var tongue = 0; tongue < clickable.length; tongue++)
                topPieceOfTongue(clickable[tongue]).addClass('selectable');

            // Highlight all the auto-move targets
            var autoSourceMoves = {}, autoTargetMoves = {};
            function processTongue(tongue, tongueInfos)
            {
                if (tongue in tongueInfos)
                {
                    tongueInfos[tongue].num++;
                    tongueInfos[tongue].numOthers = j - tongueInfos[tongue].num + 1;
                }
                else
                    tongueInfos[tongue] = { num: 1, numOthers: j };
            }
            function considerAutoMove(tongueInfos, autoMoves, move)
            {
                for (var tongue in tongueInfos)
                    if (tongueInfos[tongue].num > 1)
                        if (!autoMoves[tongue] || autoMoves[tongue].numOthers > tongueInfos[tongue].numOthers || (autoMoves[tongue].numOthers === tongueInfos[tongue].numOthers && autoMoves[tongue].num < tongueInfos[tongue].num))
                        {
                            var index = tongueInfos[tongue].num + tongueInfos[tongue].numOthers;
                            autoMoves[tongue] = {
                                num: tongueInfos[tongue].num,
                                numOthers: tongueInfos[tongue].numOthers,
                                move: {
                                    SourceTongues: move.SourceTongues.slice(0, index),
                                    TargetTongues: move.TargetTongues.slice(0, index),
                                    DiceSequence: move.DiceSequence.slice(0, index),
                                    OpponentPieceTaken: move.OpponentPieceTaken.slice(0, index)
                                }
                            };
                        }
            }
            function tongueCssClass(tongue)
            {
                switch (tongue)
                {
                    case Tongue.WhiteHome:
                        return 'white home';
                    case Tongue.BlackHome:
                        return 'black home';
                    case Tongue.WhitePrison:
                        return 'white prison';
                    case Tongue.BlackPrison:
                        return 'black prison';
                    default:
                        return 'tongue-' + tongue + (tongue < 12 ? ' bottom' : ' top');
                }
            }
            for (var i = 0; i < allValidRestMoves.length; i++)
            {
                var move = allValidRestMoves[i];
                var sourceTongues = {}, targetTongues = {};
                for (var j = 0; j < move.SourceTongues.length; j++)
                {
                    processTongue(move.SourceTongues[j], sourceTongues);
                    processTongue(move.TargetTongues[j], targetTongues);
                }
                considerAutoMove(sourceTongues, autoSourceMoves, move);
                considerAutoMove(targetTongues, autoTargetMoves, move);
            }
            for (var tongue in autoSourceMoves)
                $('<div>').addClass('automove source ' + tongueCssClass(+tongue)).data('move', autoSourceMoves[tongue].move).appendTo('#board');
            for (var tongue in autoTargetMoves)
                $('<div>').addClass('automove target ' + tongueCssClass(+tongue)).data('move', autoTargetMoves[tongue].move).appendTo('#board');
        }
    }

    function filterMoveByTargetTongue(move, tt)
    {
        var lastIndex = -1;
        for (var i = 0; i < move.TargetTongues.length; i++)
            if (move.TargetTongues[i] === tt)
                lastIndex = i;

        var ret = {
            SourceTongues: [],
            TargetTongues: [],
            DiceSequence: [],
            OpponentPieceTaken: []
        };
        for (var i = 0; i < move.TargetTongues.length; i++)
            if (move.TargetTongues[i] === tt)
            {
                ret.SourceTongues.push(move.SourceTongues[i]);
                ret.TargetTongues.push(move.TargetTongues[i]);
                ret.DiceSequence.push(move.DiceSequence[i]);
                ret.OpponentPieceTaken.push(move.OpponentPieceTaken[i]);
            }
        return ret;
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
            var selectable = rawTongueElem.length
                ? rawTongueElem.clone().addClass('selectable')
                : $('<div>').addClass('selectable ' + (+i === Tongue.WhiteHome ? 'white home' : 'black home'));
            selectable.data('move', targetMoves[i]).insertBefore('#overlay-bottom');
        });
    }

    function convertFromVw(val)
    {
        if (lastWide)
            return (val * 100 / boardHeight) + 'vh';
        if (isSidebarOn())
            return (val * 100 / (100 + sidebarWidth)) + 'vw';
        return val + 'vw';
    }

    function onResize(force)
    {
        var wasWide = lastWide;
        lastWide = ($(window).width() / $(window).height()) >= (100 + (isSidebarOn() ? sidebarWidth : 0)) / boardHeight;

        if (force || wasWide !== lastWide)
        {
            // If the aspect ratio has changed, move all the pieces into the right place
            var whites = $('#board>.piece.white'), blacks = $('#board>.piece.black');
            var whiteIndex = 0, blackIndex = 0;
            for (var tng = 0; tng < Tongue.NumTongues; tng++)
                for (var i = (tng < 12 ? position.NumPiecesPerTongue[tng] - 1 : 0) ; (tng < 12) ? (i >= 0) : (i < position.NumPiecesPerTongue[tng]) ; (tng < 12 ? i-- : i++))
                    $(position.IsWhitePerTongue[tng] ? whites[whiteIndex++] : blacks[blackIndex++])
                        .css({ left: convertFromVw(leftFromTongue(tng)), top: convertFromVw(topFromTongue(tng, i, position.NumPiecesPerTongue[tng])) })
                        .data({ tongue: tng, index: i, num: position.NumPiecesPerTongue[tng] });

            if (isPlayerToMove())
            {
                if (selectedPiece !== null)
                    selectPiece(selectedPiece.data('tongue'));
                else
                    deselectPiece();
            }
        }
    }

    function recomputeValidRestMoves()
    {
        allValidRestMoves = allValidMoves.filter(function (move)
        {
            for (var i = 0; i < moveSoFar.DiceSequence.length; i++)
                if (move.DiceSequence[i] !== moveSoFar.DiceSequence[i] ||
                    move.SourceTongues[i] !== moveSoFar.SourceTongues[i] ||
                    move.TargetTongues[i] !== moveSoFar.TargetTongues[i])
                    return false;
            return true;
        }).map(function (move)
        {
            return {
                DiceSequence: move.DiceSequence.slice(moveSoFar.DiceSequence.length),
                SourceTongues: move.SourceTongues.slice(moveSoFar.DiceSequence.length),
                TargetTongues: move.TargetTongues.slice(moveSoFar.DiceSequence.length),
                OpponentPieceTaken: move.OpponentPieceTaken.slice(moveSoFar.DiceSequence.length)
            };
        });
    }

    if (!$('#main>#board').length)
        return;
    var main = $('#main');

    // Special tongues
    var Tongue = {
        WhitePrison: 24,
        BlackPrison: 25,
        WhiteHome: 26,
        BlackHome: 27,
        NumTongues: 28
    };

    var moves = main.data('moves');
    var lastMove = moves[moves.length - 1];
    var playerIsWhite = main.hasClass('player-white');
    var allValidMoves, allValidRestMoves;
    var boardHeight = 68;   // vw
    var sidebarWidth = 30;  // vw
    var lastWide = null;

    var position = main.data('initial');
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

    setState();

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
            if ('auto' in json)
                main.addClass('auto-' + json.auto);

            moves[moves.length - 1].SourceTongues = json.move.SourceTongues;
            moves[moves.length - 1].TargetTongues = json.move.TargetTongues;
            position = processMove(position, main.hasClass('state-White'), json.move.SourceTongues, json.move.TargetTongues, {
                mode: 'animate',
                callback: function ()
                {
                    if ('auto' in json)
                        setTimeout(processSocketQueue, json.auto ? 1000 : 2000);
                    else
                        processSocketQueue();
                }
            });
        }
        else if ('dice' in json)
        {
            setState(json.dice.state, true);
            moveSoFar = { SourceTongues: [], TargetTongues: [], OpponentPieceTaken: [], DiceSequence: [] };
            moves.push({ Dice1: json.dice.dice1, Dice2: json.dice.dice2 });
            lastMove = moves[moves.length - 1];

            main
                .removeClass('dice-2 dice-4 dice-start')
                .addClass((lastMove.Dice1 === lastMove.Dice2 ? 'dice-4' : 'dice-2') + (moves.length === 1 ? ' dice-start' : ''));
            $('#board>.dice').removeClass('val-1 val-2 val-3 val-4 val-5 val-6 crossed');
            $('#board>#dice-0').addClass('val-' + lastMove.Dice1);
            $('#board>#dice-1,#board>#dice-2,#board>#dice-3').addClass('val-' + lastMove.Dice2);

            processSocketQueue();
        }
        else if ('cube' in json)
        {
            $('#cube-text').text(json.cube.GameValue);
            var oldTop = $('#cube').position().top;
            main.removeClass('cube-white cube-black').addClass(json.cube.WhiteOwnsCube ? 'cube-white' : 'cube-black');
            var newTop = $('#cube').position().top;
            $('#cube').css('top', oldTop).animate({ top: newTop }, { duration: 1000, complete: function () { $('#cube').css('top', ''); } });
            position.GameValue = json.cube.GameValue;
            position.WhiteOwnsCube = json.cube.WhiteOwnsCube;
            setTimeout(processSocketQueue, 1000);
        }
        else if ('state' in json)
        {
            setState(json.state);

            if ('win' in json)
            {
                $('#win>.points>.number').text(json.win);
                $('#win>.points').removeClass('singular plural').addClass(json.win === 1 ? "singular" : "plural");
            }

            processSocketQueue();
        }
    };

    var newSocket = function ()
    {
        socket = new WebSocket(main.data('socket-url'));
        socket.onopen = function ()
        {
            main.removeClass('connecting');
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

    var socketSend = function (msg)
    {
        if (socket && socket.readyState === socket.OPEN)
            socket.send(JSON.stringify(msg));
        else
            sendQueue.push(msg);
    };

    if (!main.hasClass('spectating'))
    {
        deselectPiece();
        $('#board>.piece').click(function ()
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

        $('#board').on('mouseenter', '.tongue.selectable, .home.selectable, .automove', function ()
        {
            var move = $(this).data('move');
            processMove(position, playerIsWhite, move.SourceTongues, move.TargetTongues, { mode: 'indicate' });
        });

        $('#board').on('mouseleave', '.tongue.selectable, .home.selectable, .automove', function ()
        {
            $('#board>.piece.hypo-target, #board>.arrow').remove();
        });

        $('#board').on('click', '.tongue.selectable, .home.selectable, .automove', function ()
        {
            $('#board>.piece.hypo-target, #board>.arrow').hide();
            var move = $(this).data('move');
            deselectPiece(true);
            position = processMove(position, playerIsWhite, move.SourceTongues, move.TargetTongues, { mode: 'animate', callback: deselectPiece });
            for (var i = 0; i < move.DiceSequence.length; i++)
            {
                moveSoFar.DiceSequence.push(move.DiceSequence[i]);
                moveSoFar.SourceTongues.push(move.SourceTongues[i]);
                moveSoFar.TargetTongues.push(move.TargetTongues[i]);
                moveSoFar.OpponentPieceTaken.push(move.OpponentPieceTaken[i]);
                $('#board>.dice:not(.crossed).val-' + move.DiceSequence[i]).first().addClass('crossed');
            }
            main.addClass('undoable');
            if (moveSoFar.DiceSequence.length === allValidMoves[0].DiceSequence.length)
                main.addClass('committable');
            recomputeValidRestMoves();
        });

        $(document).keydown(function (e)
        {
            if (e.keyCode === 27 && selectedPiece !== null)
                deselectPiece();
        });

        $('#undo').click(function ()
        {
            if (main.hasClass('spectating'))
                return false;
            var lastIndex = moveSoFar.DiceSequence.length - 1;
            if (lastIndex >= 0)
            {
                deselectPiece(true);
                position = processMove(position, playerIsWhite, moveSoFar.TargetTongues[lastIndex], moveSoFar.SourceTongues[lastIndex], {
                    mode: 'animate',
                    undoOpponentPieceTaken: moveSoFar.OpponentPieceTaken[lastIndex],
                    callback: deselectPiece
                });
                $('#board>.dice.crossed.val-' + moveSoFar.DiceSequence[lastIndex]).last().removeClass('crossed');
                moveSoFar.DiceSequence.pop();
                moveSoFar.SourceTongues.pop();
                moveSoFar.TargetTongues.pop();
                moveSoFar.OpponentPieceTaken.pop();
            }
            main.removeClass('committable');
            if (moveSoFar.DiceSequence.length === 0)
                main.removeClass('undoable');
            recomputeValidRestMoves();
            return false;
        });

        function getGeneralisedButtonClick(msgToSend)
        {
            return function ()
            {
                if (!main.hasClass('spectating'))
                {
                    socketSend(typeof msgToSend === 'function' ? msgToSend() : msgToSend);
                    main.removeClass('undoable committable resigning roll-or-double confirm-double');
                }
                return false;
            };
        }

        $('#commit').click(getGeneralisedButtonClick(function () { return { move: { SourceTongues: moveSoFar.SourceTongues, TargetTongues: moveSoFar.TargetTongues } }; }));
        $('#roll').click(getGeneralisedButtonClick({ roll: 1 }));
        $('#double').click(getGeneralisedButtonClick({ double: 1 }));
        $('#accept').click(getGeneralisedButtonClick({ accept: 1 }));
        $('#reject').click(getGeneralisedButtonClick({ reject: 1 }));
        $('#resign-confirm').click(getGeneralisedButtonClick({ resign: 1 }));

        $('#resign').click(function ()
        {
            if (!$('#main.state-Won').length)
                main.addClass('resigning');
            return false;
        });
        $('#resign-cancel').click(function () { main.removeClass('resigning'); return false; });
    }

    // Add extra CSS
    var cssWithSidebar = [];                    // CSS for when the sidebar is visible
    var cssInMedia = [];                          // CSS for viewports wider than the playing area’s aspect ratio
    var cssInMediaWithSidebar = [];      // CSS for when the sidebar is visible and the viewport is wider than the whole UI
    var vwRe = /(-?\b\d*\.?\d+)vw\b/g;
    for (var ss = 0; ss < document.styleSheets.length; ss++)
    {
        var rules = document.styleSheets[ss].cssRules || document.styleSheets[ss].rules;
        for (var ruleix = 0; ruleix < rules.length; ruleix++)
        {
            if (!rules[ruleix].selectorText)
                continue;
            var oldSelectors = rules[ruleix].selectorText.split(/\s*,\s*/);
            var newSelectors = [];
            for (var i = 0; i < oldSelectors.length; i++)
            {
                var result = oldSelectors[i].match(/^\s*#main\b(?!-)/);
                if (!result)
                    continue;
                newSelectors.push("#main.with-sidebar" + oldSelectors[i].substr(result[0].length));
            }

            var props = rules[ruleix].style;
            var sidebarProps = [], inMediaProps = [], sidebarInMediaProps = [];
            for (var propix = 0; propix < props.length; propix++)
            {
                var propName = props[propix].replace(/-value$/, '');
                var val = props.getPropertyValue(propName);
                if (vwRe.test(val))
                {
                    var imp = props.getPropertyPriority(propName);
                    if (imp)
                        imp = '!' + imp;
                    sidebarProps.push(propName + ':' + val.replace(vwRe, function (_, vw) { return (vw * 100 / (100 + sidebarWidth)) + 'vw'; }) + imp);
                    inMediaProps.push(propName + ':' + val.replace(vwRe, function (_, vw) { return (vw * 100 / boardHeight) + 'vh'; }) + imp);
                    sidebarInMediaProps.push(propName + ':' + val.replace(vwRe, function (_, vw) { return (vw * 100 / boardHeight) + 'vh'; }) + imp);
                }
            }
            if (sidebarProps.length)
            {
                newSelectors = newSelectors.join(',');
                cssWithSidebar.push(newSelectors + '{' + sidebarProps.join(';') + '}');
                cssInMedia.push(rules[ruleix].selectorText + '{' + inMediaProps.join(';') + '}');
                cssInMediaWithSidebar.push(newSelectors + '{' + sidebarInMediaProps.join(';') + '}');
            }
        }
    }
    var cssText =
        cssWithSidebar.join('') +
        '@media screen and (min-aspect-ratio: 100/' + boardHeight + ') {' + cssInMedia.join('') + '}' +
        '@media screen and (min-aspect-ratio: ' + (100 + sidebarWidth) + '/' + boardHeight + ') {' + cssInMediaWithSidebar.join('') + '}';
    $('#converted-css').text(cssText);

    $(window).resize(onResize);
    onResize();

});
