$(function()
{
    var sidebars = ['chat', 'info', 'settings', 'translate', 'translating'];

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
            return 50 - 1.425 * pieceIndex;
        if (tongue === Tongue.WhitePrison)
            return 34 + 4 * pieceIndex;
        if (tongue === Tongue.BlackPrison)
            return 17 - 4 * pieceIndex;
        if (tongue < 12)
            return 51 - (20 / Math.max(4, numPieces - 1)) * pieceIndex;
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

    function getPipCounts(pos)
    {
        var pips = {
            white: 25 * pos.NumPiecesPerTongue[Tongue.WhitePrison],
            black: 25 * pos.NumPiecesPerTongue[Tongue.BlackPrison]
        };
        for (var t = 0; t < 24; t++)
            if (pos.IsWhitePerTongue[t])
                pips.white += (24 - t) * pos.NumPiecesPerTongue[t];
            else
                pips.black += (t + 1) * pos.NumPiecesPerTongue[t];
        return pips;
    }

    function /* Position */ processMove(/* Position */ pos, /* bool */ whitePlayer, /* Move */ move, /* see processMoveDirect */ options)
    {
        var newPos = 'SourceTongues' in move ? processMoveDirect(pos, whitePlayer, move.SourceTongues, move.TargetTongues, options) : copyPosition(pos);
        if (move.Doubled)
        {
            newPos.WhiteOwnsCube = !whitePlayer;
            newPos.GameValue *= 2;
        }
        return newPos;
    }

    function /* Position */ processMoveDirect(/* Position */ pos, /* bool */ whitePlayer, /* int or int[] */ sourceTongues, /* int or int[] */ targetTongues,
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
            return processMoveDirect(pos, whitePlayer, s, t, options);
        }

        var newPos = copyPosition(pos);
        var animationQueue = [];
        var arrows = {};

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
                        .css({ left: convertFromVw(leftFromTongue(targetTongue)), top: convertFromVw(topFromTongue(targetTongue, newPos.NumPiecesPerTongue[targetTongue] - 1, newPos.NumPiecesPerTongue[targetTongue])) })
                        .data({ tongue: targetTongue, index: newPos.NumPiecesPerTongue[targetTongue] - 1, num: newPos.NumPiecesPerTongue[targetTongue] });
                    if (!(sourceTongue in arrows))
                        arrows[sourceTongue] = {};
                    if (!(targetTongue in arrows[sourceTongue]))
                        arrows[sourceTongue][targetTongue] = [[], []];
                    arrows[sourceTongue][targetTongue][0].push(piecesByTongue[sourceTongue][newPos.NumPiecesPerTongue[sourceTongue]]);
                    arrows[sourceTongue][targetTongue][1][(sourceTongue > 11 && sourceTongue < 24) === (targetTongue > 11 && targetTongue < 24) ? 'unshift' : 'push'](hypo);
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

        for (var i in arrows)
            for (var j in arrows[i])
                for (var k = 0; k < arrows[i][j][0].length; k++)
                    makeArrow(arrows[i][j][0][k], arrows[i][j][1][k]).appendTo('#board');

        function processAnimationQueue()
        {
            if (animationQueue.length === 0)
            {
                var pips = getPipCounts(newPos);
                $('#pipcount-white').text(pips.white);
                $('#pipcount-black').text(pips.black);
                if (callback !== null)
                    callback();
                return;
            }
            var item = animationQueue.shift();
            var num = [];
            num[item.Data.tongue] = getPiecesOnTongue(item.Data.tongue).length + 1;
            num[item.Piece.data('tongue')] = getPiecesOnTongue(item.Piece.data('tongue')).length - 1;
            var otherPieces = $('#board>.piece')
                .filter(function(i, elem) { return $(elem).data('tongue') === item.Data.tongue || $(elem).data('tongue') === item.Piece.data('tongue'); })
                .not(item.Piece)
                .get()
                .map(function(elem)
                {
                    var tongue = $(elem).data('tongue');
                    return [$(elem), {
                        left: convertFromVw(leftFromTongue(tongue)),
                        top: convertFromVw(topFromTongue(tongue, $(elem).data('index'), num[tongue]))
                    }];
                });
            if ((item.Piece.data('tongue') < 24 && num[item.Piece.data('tongue')] > 4) || (item.Data.tongue < 24 && num[item.Data.tongue] > 5))
                setTimeout(function() { otherPieces.forEach(function(inf) { inf[0].animate(inf[1], 200); }); }, 100);
            item.Piece
                .data(item.Data)
                .insertAfter($('#board>.piece').last())
                .animate({
                    left: convertFromVw(leftFromTongue(item.Data.tongue)),
                    top: convertFromVw(topFromTongue(item.Data.tongue, item.Data.index, item.Data.num))
                }, {
                    duration: 400,
                    complete: function()
                    {
                        setTimeout(function()
                        {
                            if (item.Data.tongue < 12)
                                item.Piece.insertBefore($('#board>.piece').first());
                            processAnimationQueue();
                        }, 10);
                    }
                });
        }
        if (mode === 'animate' && animationQueue.length > 0)
            processAnimationQueue();
        else if (callback)
            setTimeout(callback, 100);
        return newPos;
    }

    function /* $ */ getPiecesOnTongue(tongue)
    {
        // $('#board>.piece[data-tongue="x"]') does not work; jQuery’s .data() does not actually change the attributes
        return $('#board>.piece').filter(function() { return $(this).data('tongue') === tongue; });
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
            var pMove = processMoveDirect(position, whitePlayer, accessibleTongues[accIx], target);
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

    function /* Move[] */ getAllMoves(/* Position */ position, /* bool */ whitePlayer, /* int[][] */ diceSequences)
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
        return $('#main.player-white.state-White.state-ToMove,#main.player-black.state-Black.state-ToMove').length > 0;
    }

    function isSidebarOn()
    {
        return $('body.hash-sidebar').length > 0;
    }

    function setState(newState, skipHighlight)
    {
        if (newState)
        {
            state = newState;
            LiBackgammon.removeClassPrefix(main, 'state-').removeClass('auto-0 auto-1 undoable committable');
            newState.split('_').forEach(function(cl) { main.addClass('state-' + cl); });
        }
        if (isPlayerToMove() && !skipHighlight)
        {
            if (lastMove.Dice1 === lastMove.Dice2)
            {
                allValidMoves = getAllMoves(position, playerIsWhite, [[lastMove.Dice1, lastMove.Dice1, lastMove.Dice1, lastMove.Dice1]]);
                for (var k = 0; k < 4 - allValidMoves[0].DiceSequence.length; k++)
                    $('#board>.dice:not(.crossed)').last().addClass('crossed');
            }
            else
            {
                allValidMoves = getAllMoves(position, playerIsWhite, [[lastMove.Dice1, lastMove.Dice2], [lastMove.Dice2, lastMove.Dice1]]);
                if (allValidMoves[0].DiceSequence.length < 2)
                {
                    $('#board>.dice').addClass('crossed');
                    for (var i = 0; i < allValidMoves.length; i++)
                        for (var j = 0; j < allValidMoves[i].DiceSequence.length; j++)
                            $('#board>.dice.val-' + allValidMoves[i].DiceSequence[j]).removeClass('crossed');
                }
            }
            allValidRestMoves = allValidMoves;
            if (!main.hasClass('viewing-history'))
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
        $('#board>.piece.hypo-target, #board>.arrow, #board>.tongue.selectable, #board>.home.selectable, #board>.automove, #board>.percentage').remove();
        $('#board>.piece').removeClass('selectable selected');
        selectedPiece = null;

        if (isPlayerToMove() && !skipHighlight)
        {
            // Highlight all the clickable pieces
            var clickable = getClickableSourceTongues();
            for (var tongue = 0; tongue < clickable.length; tongue++)
                if (position.IsWhitePerTongue[clickable[tongue]] === playerIsWhite)
                    topPieceOfTongue(clickable[tongue]).addClass('selectable');

            // Highlight all the auto-move targets
            var autoSourceMoves = {}, autoTargetMoves = {};
            var processTongue = function(j, tongue, tongueInfos)
            {
                if (tongue in tongueInfos)
                {
                    tongueInfos[tongue].num++;
                    tongueInfos[tongue].numOthers = j - tongueInfos[tongue].num + 1;
                }
                else
                    tongueInfos[tongue] = { num: 1, numOthers: j };
            };
            var considerAutoMove = function(tongueInfos, autoMoves, move)
            {
                for (var tongue in tongueInfos)
                    if (tongueInfos[tongue].num > 1)
                    {
                        if (autoMoves[tongue] && (autoMoves[tongue].num > tongueInfos[tongue].num || (autoMoves[tongue].num === tongueInfos[tongue].num && autoMoves[tongue].numOthers < tongueInfos[tongue].numOthers)))
                            continue;

                        if (!autoMoves[tongue] || (autoMoves[tongue].num < tongueInfos[tongue].num || (autoMoves[tongue].num === tongueInfos[tongue].num && autoMoves[tongue].numOthers > tongueInfos[tongue].numOthers)))
                            autoMoves[tongue] = {
                                num: tongueInfos[tongue].num,
                                numOthers: tongueInfos[tongue].numOthers,
                                moves: []
                            };

                        autoMoves[tongue].moves.push({
                            SourceTongues: move.SourceTongues,
                            TargetTongues: move.TargetTongues,
                            DiceSequence: move.DiceSequence,
                            OpponentPieceTaken: move.OpponentPieceTaken
                        });
                    }
            };
            var tongueCssClass = function(tongue)
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
            };
            for (var i = 0; i < allValidRestMoves.length; i++)
            {
                var move = allValidRestMoves[i];
                var sourceTongues = {}, targetTongues = {};
                for (var j = 0; j < move.SourceTongues.length; j++)
                {
                    processTongue(j, move.SourceTongues[j], sourceTongues);
                    processTongue(j, move.TargetTongues[j], targetTongues);
                }
                considerAutoMove(sourceTongues, autoSourceMoves, move);
                considerAutoMove(targetTongues, autoTargetMoves, move);
            }
            function selectBestAutoMove(inf)
            {
                if (inf.moves.length === 1)
                    return inf.moves[0];
                return {
                    SourceTongues: inf.moves[0].SourceTongues.slice(0, inf.num + inf.numOthers),
                    TargetTongues: inf.moves[0].TargetTongues.slice(0, inf.num + inf.numOthers),
                    DiceSequence: inf.moves[0].DiceSequence.slice(0, inf.num + inf.numOthers),
                    OpponentPieceTaken: inf.moves[0].OpponentPieceTaken.slice(0, inf.num + inf.numOthers)
                };
            }
            for (var tongue in autoSourceMoves)
                if (position.NumPiecesPerTongue[tongue] > 1)
                    $('<div>')
                        .addClass('automove source ' + tongueCssClass(+tongue))
                        .data('move', selectBestAutoMove(autoSourceMoves[tongue]))
                        .css('top', convertFromVw(topFromTongue(tongue, position.NumPiecesPerTongue[tongue] - 1, position.NumPiecesPerTongue[tongue])))
                        .appendTo('#board');
            for (var tongue in autoTargetMoves)
                $('<div>').addClass('automove target ' + tongueCssClass(+tongue)).data('move', selectBestAutoMove(autoTargetMoves[tongue])).appendTo('#board');

            // Calculate probabilities
            var landedPerTongue = calculateProbabilities(position, playerIsWhite);
            for (var k = 0; k < 24; k++)
                $('#board').append($('<div>').addClass('percentage ' + tongueCssClass(k)).text(Math.round((landedPerTongue[k] / 36) * 100) + '%'));
        }
    }

    function calculateProbabilities(pos, isWhite)
    {
        var landedPerTongue = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        //var stackablePerTongue = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
        for (var dice1 = 1; dice1 <= 6; dice1++)
        {
            for (var dice2 = 1; dice2 <= 6; dice2++)
            {
                var moves = getAllMoves(pos, !isWhite, dice1 === dice2 ? [[dice1, dice1, dice1, dice1]] : [[dice1, dice2], [dice2, dice1]]);
                var anyLanded = [false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false];
                //var anyStackable = [false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false, false];
                for (var i = 0; i < moves.length; i++)
                {
                    for (var j = 0; j < moves[i].TargetTongues.length; j++)
                        anyLanded[moves[i].TargetTongues[j]] = true;
                    //for (var t = 0; t < 24; t++)
                    //    anyStackable[t] |= moves[i].EndPosition.NumPiecesPerTongue[t] >= 2 && moves[i].EndPosition.IsWhitePerTongue[t] === !isWhite;
                }
                for (var k = 0; k < 24; k++)
                {
                    if (anyLanded[k])
                        landedPerTongue[k]++;
                    //if (anyStackable[k])
                    //    stackablePerTongue[k]++;
                }
            }
        }
        return landedPerTongue;
        //return stackablePerTongue;
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

    function getTargetMoves(tongue)
    {
        var targetMoves = {};
        for (var i = 0; i < allValidRestMoves.length; i++)
        {
            var acc = [tongue];
            for (var k = 0; k < allValidRestMoves[i].SourceTongues.length; k++)
                if (acc.indexOf(allValidRestMoves[i].SourceTongues[k]) !== -1)
                {
                    var targetTongue = allValidRestMoves[i].TargetTongues[k];
                    acc.push(targetTongue);
                    if (targetTongue in targetMoves && targetMoves[targetTongue].Index < k)
                        continue;

                    var piecesTaken = allValidRestMoves[i].EndPosition.NumPiecesPerTongue[getPrison(!playerIsWhite)];
                    if (targetTongue in targetMoves && targetMoves[targetTongue].Index === k && targetMoves[targetTongue].SourceTongues.length === k + 1 && targetMoves[targetTongue].PiecesTaken > piecesTaken)
                        continue;

                    var indexTo = (targetTongue in targetMoves && targetMoves[targetTongue].Index === k) ? k + 1 : allValidRestMoves[i].SourceTongues.length;
                    if (targetTongue in targetMoves && targetMoves[targetTongue].Index <= k && targetMoves[targetTongue].SourceTongues.length < indexTo)
                        continue;

                    targetMoves[targetTongue] = {
                        SourceTongues: allValidRestMoves[i].SourceTongues.slice(0, indexTo),
                        TargetTongues: allValidRestMoves[i].TargetTongues.slice(0, indexTo),
                        OpponentPieceTaken: allValidRestMoves[i].OpponentPieceTaken.slice(0, indexTo),
                        DiceSequence: allValidRestMoves[i].DiceSequence.slice(0, indexTo),
                        PiecesTaken: piecesTaken,
                        Index: k
                    };
                }
        }
        return targetMoves;
    }

    function selectPiece(tongue)
    {
        deselectPiece(true);
        selectedPiece = topPieceOfTongue(tongue).addClass('selected');

        var targetMoves = getTargetMoves(tongue);
        Object.keys(targetMoves).map(function(i)
        {
            var rawTongueElem = $('.tongue-' + i);
            var selectable = rawTongueElem.length
                ? rawTongueElem.clone().addClass('selectable')
                : $('<div>').addClass('selectable ' + (+i === Tongue.WhiteHome ? 'white home' : 'black home'));
            selectable.data('move', targetMoves[i]).insertBefore('#overlay-bottom');
        });
    }

    function executeMove(move)
    {
        position = processMove(position, playerIsWhite, move, { mode: 'animate', callback: deselectPiece });
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
    }

    function convertFromVw(val)
    {
        if (lastWide)
            return (val * 100 / boardHeight) + 'vh';
        if (isSidebarOn())
            return (val * 100 / (100 + sidebarWidth)) + 'vw';
        return val + 'vw';
    }

    function setupPosition(pos)
    {
        $('#board>.piece').stop();
        var whites = $('#board>.piece.white'), blacks = $('#board>.piece.black');
        var whiteIndex = 0, blackIndex = 0;
        for (var tng = 0; tng < Tongue.NumTongues; tng++)
            for (var i = (tng < 12 ? pos.NumPiecesPerTongue[tng] - 1 : 0) ; (tng < 12) ? (i >= 0) : (i < pos.NumPiecesPerTongue[tng]) ; (tng < 12 ? i-- : i++))
                $(pos.IsWhitePerTongue[tng] ? whites[whiteIndex++] : blacks[blackIndex++])
                    .css({ left: convertFromVw(leftFromTongue(tng)), top: convertFromVw(topFromTongue(tng, i, pos.NumPiecesPerTongue[tng])) })
                    .data({ tongue: tng, index: i, num: pos.NumPiecesPerTongue[tng] });
        var pips = getPipCounts(pos);
        $('#pipcount-white').text(pips.white);
        $('#pipcount-black').text(pips.black);
        $('#board>#cube>#cube-text').text(pos.GameValue);
    }

    function onResize(force)
    {
        var wasWide = lastWide;
        lastWide = ($(window).width() / $(window).height()) >= (100 + (isSidebarOn() ? sidebarWidth : 0)) / boardHeight;

        if (force || wasWide !== lastWide)
        {
            // If the aspect ratio has changed, move all the pieces into the right place
            var hist = $('#info-game-history>.move.current');
            if (hist.length)
                setHistory(hist.data('index'), '');
            else
            {
                setupPosition(position);

                if (isPlayerToMove())
                {
                    if (selectedPiece !== null)
                        selectPiece(selectedPiece.data('tongue'));
                    else
                        deselectPiece();
                }
            }
        }

        preventCmoScrollEventUntil = Date.now() + 100;
        var cmo = $('#chat-msgs-outer');
        if ($('#chat-msgs').height() > cmo.height())
            $('#chat-msgs').removeClass('few').addClass('many');
        else
            $('#chat-msgs').removeClass('many').addClass('few');
        if (chatLastScrolledBottom)
            cmo[0].scrollTop = cmo[0].scrollHeight;
    }

    function recomputeValidRestMoves()
    {
        allValidRestMoves = allValidMoves.filter(function(move)
        {
            for (var i = 0; i < moveSoFar.DiceSequence.length; i++)
                if (move.DiceSequence[i] !== moveSoFar.DiceSequence[i] ||
                    move.SourceTongues[i] !== moveSoFar.SourceTongues[i] ||
                    move.TargetTongues[i] !== moveSoFar.TargetTongues[i])
                    return false;
            return true;
        }).map(function(move)
        {
            return {
                DiceSequence: move.DiceSequence.slice(moveSoFar.DiceSequence.length),
                SourceTongues: move.SourceTongues.slice(moveSoFar.DiceSequence.length),
                TargetTongues: move.TargetTongues.slice(moveSoFar.DiceSequence.length),
                OpponentPieceTaken: move.OpponentPieceTaken.slice(moveSoFar.DiceSequence.length),
                EndPosition: move.EndPosition
            };
        });
    }

    function sidebar(id)
    {
        var hash = LiBackgammon.hash.values;
        if ($('#info-game-history>.move.current').length)
            historyLeaveAll();

        if (hash.indexOf('sidebar') !== -1 && hash.indexOf(id) !== -1)
            LiBackgammon.hashRemove(['sidebar', id]);
        else
        {
            LiBackgammon.hashRemove(sidebars);
            LiBackgammon.hashAdd(['sidebar', id]);
        }

        if (id === 'chat')
        {
            if (!main.hasClass('spectating'))
            {
                var unseenIds = $('.chat-msg:not(.seen)').get().map(function(e) { return $(e).data('id'); });
                if (unseenIds.length > 0)
                    socketSend({ chatSeen: { ids: unseenIds } });
                $('.chat-msg:not(.seen)').addClass('seen');
                $('#btn-chat>.notification').removeClass('shown');
            }
        }
    }

    function processSocketQueue()
    {
        if (!socketQueue.length)
        {
            socketQueueProcessing = false;
            return;
        }

        socketQueueProcessing = true;
        var json = socketQueue.shift();
        var keys = Object.keys(json);

        if (keys.length === 1 && keys[0] in socketMethods && socketMethods[keys[0]](json[keys[0]]) !== true)
            processSocketQueue();
    }

    function newSocket()
    {
        socket = new WebSocket(main.data('socket-url'));
        socket.onopen = function()
        {
            main.removeClass('connecting online-White online-Black');
            for (var i = 0; i < sendQueue.length; i++)
                socket.send(JSON.stringify(sendQueue[i]));
            sendQueue = [];
            socket.send(JSON.stringify({ resync: { moveCount: moves.length, lastMoveDone: lastMove && 'SourceTongues' in lastMove } }));
        };
        socket.onclose = function()
        {
            reconnect(true);
        };
        socket.onmessage = function(msg)
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
    }

    function reconnect(useDelay)
    {
        main.addClass('connecting');
        try { socket.close(); } catch (e) { }
        if (useDelay)
        {
            reconnectInterval = (reconnectInterval === 0) ? 1000 : reconnectInterval * 2;
            setTimeout(newSocket, reconnectInterval);
        }
        else
        {
            reconnectInterval = 0;
            newSocket();
        }
    }

    function socketSend(msg)
    {
        if (socket && socket.readyState === socket.OPEN)
            socket.send(JSON.stringify(msg));
        else
            sendQueue.push(msg);
    }

    function hashChange()
    {
        var values = LiBackgammon.hash.values, dict = LiBackgammon.hash.dict, c = values.length, pos;

        for (var a = values.length - 1; a >= 0; a--)
        {
            if (values[a].length < 1)
                values.splice(a, 1);
            else
                for (var b = values.length - 1; b > a; b--)
                    if (values[a] === values[b])
                        values.splice(b, 1);
        }

        if ((pos = values.indexOf('translating')) !== -1 && !('translator' in dict))
            values.splice(pos, 1);

        if ((pos = values.indexOf('sidebar')) !== -1)
        {
            var sides = [];
            for (var i = 0; i < sidebars.length; i++)
                if (values.indexOf(sidebars[i]) !== -1)
                    sides.push(sidebars[i]);
            if (sides.length === 0)
                values.splice(pos, 1);
            else
                for (var i = 1; i < sides.length; i++)
                    values.splice(values.indexOf(sides[i]), 1);
        }
        else
            for (var i = 0; i < sidebars.length; i++)
                if ((pos = values.indexOf(sidebars[i])) !== -1)
                    values.splice(pos, 1);
        if (values.length !== c)
            LiBackgammon.setHash(values, dict);

        onResize(true);

        $('#settings-helpers-select').prop('checked', values.indexOf('helpers') !== -1);
        $('#settings-percentages-select').prop('checked', values.indexOf('percentages') !== -1);

        if (values.indexOf('sidebar') !== -1 && values.indexOf('translate') !== -1)
            socketSend({ getLanguages: 1 });

        if (values.indexOf('sidebar') !== -1 && values.indexOf('settings') !== -1 && !settingsLoaded)
        {
            socketSend({ settings: 1 });
            settingsLoaded = true;
        }

        if (values.indexOf('sidebar') !== -1 && values.indexOf('translating') !== -1 && 'translator' in dict)
        {
            var t = $('#translating').empty();
            if (!LiBackgammon.translation)
            {
                socketSend({ translate: { hashName: dict.lang, token: dict.translator } });
                t.append('<div>Loading...</div>');
            }
            else
            {
                $('select.translating').remove();
                var s = $('<select class="translating">').change(pretendState);
                var opts = [
                    'White_Waiting',
                    'Black_Waiting',
                    'Random_Waiting',
                    'White_ToRoll',
                    'Black_ToRoll',
                    'White_ToConfirmDouble',
                    'Black_ToConfirmDouble',
                    'White_ToMove',
                    'Black_ToMove',
                    'White_Won_Finished',
                    'Black_Won_Finished',
                    'White_Won_DeclinedDouble',
                    'Black_Won_DeclinedDouble',
                    'White_Won_Resignation',
                    'Black_Won_Resignation'
                ];
                for (var i = 0; i < opts.length; i++)
                    s.append($('<option>').attr('value', opts[i]).text(opts[i]));
                $('#sidebar').append(s);
                t.append($('<div class="important">').text('Thank you for volunteering to translate! Please bookmark the URL of this page so that you can come back and continue translating later.'));
                var u = $('<div class="missing"><div class="title">Missing translations:</div></div>').appendTo(t), ua = false;
                for (var selector in LiBackgammon.strings)
                {
                    var inf = LiBackgammon.strings[selector], translation = LiBackgammon.translation && LiBackgammon.translation.strings && LiBackgammon.translation.strings[selector], obj;
                    if (translation)
                        obj = t;
                    else
                    {
                        obj = u;
                        ua = true;
                    }
                    obj.append(
                        $('<div class="translatable">')
                            .append($('<div class="orig">').text(inf.text))
                            .append(inf.hint ? $('<div class="hint">').text(inf.hint) : null)
                            .append($('<div class="trans">')
                                .append($('<input type="text">')
                                    .data({ sel: selector, orig: inf.text })
                                    .val(translation)
                                    .change(submitTranslation)
                                    .focus(focusTranslation)
                                    .blur(blurTranslation))));
                }
                if (!ua)
                    u.remove();
                updateTranslation();
            }
        }
    }

    function pretendState()
    {
        setState($(this).val());
    }

    function submitTranslation()
    {
        var hash = LiBackgammon.hash.dict, i = $(this);
        i.parent().removeClass('saved unsaved').addClass('submitting');
        socketSend({ translateSubmit: { hashName: hash.lang, token: hash.translator, sel: i.data('sel'), trans: i.val() } });
        LiBackgammon.translation.strings[i.data('sel')] = i.val();
        updateTranslation();
    }

    function updateTranslation()
    {
        var rules = [];
        for (var sel in LiBackgammon.translation.strings)
            if (LiBackgammon.translation.strings.hasOwnProperty(sel) && LiBackgammon.strings.hasOwnProperty(sel))
                rules.push(LiBackgammon.toCssRule(LiBackgammon.strings[sel], LiBackgammon.translation.strings[sel]));
        $('#translated-content-2').text(rules.join('\n'));
    }

    function focusTranslation()
    {
        $(this).parent().addClass('unsaved').removeClass('submitting');
    }

    function blurTranslation()
    {
        $(this).parent().removeClass('unsaved');
    }

    function setHistory(i, mode)
    {
        deselectPiece(true);
        var e = $('#info-game-history>.move').filter(function() { return $(this).data('index') === i; }).first(), move = moves[i], pos = e.data('pos');
        main.removeClass('history-dice-2 history-dice-4 history-dice-start history-white history-black history-cube-white history-cube-black viewing-history-hover');
        main.addClass('viewing-history');
        if (mode === 'indicate')
            main.addClass('viewing-history-hover');
        main.addClass((moves[0].Dice1 > moves[0].Dice2) ^ (i % 2 === 0) ? 'history-black' : 'history-white');
        if (i === 0)
            main.addClass('history-dice-start');
        if (pos.GameValue !== null && pos.WhiteOwnsCube !== null)
            main.addClass(pos.WhiteOwnsCube ? 'history-cube-white' : 'history-cube-black');
        main.addClass(moves[i].Dice1 === moves[i].Dice2 ? 'history-dice-4' : 'history-dice-2');
        LiBackgammon.removeClassPrefix($('#board>#dice-0'), 'history-val-').addClass('history-val-' + moves[i].Dice1);
        LiBackgammon.removeClassPrefix($('#board>#dice-1,#board>#dice-2,#board>#dice-3'), 'history-val-').addClass('history-val-' + moves[i].Dice2);
        $('#board>#cube>#cube-text').text(pos.GameValue);
        if (mode === '')
            setupPosition('SourceTongues' in move ? processMove(pos, e.data('isWhite'), move) : pos);
        else
        {
            setupPosition(pos);
            if (!('SourceTongues' in move))
                return pos;
            return processMove(pos, e.data('isWhite'), move, { mode: mode });
        }
    }

    function historyEnter()
    {
        if (!$(this).hasClass('current'))
        {
            $('#board>.piece').stop();
            setHistory($(this).data('index'), 'indicate');
        }
    }

    function historyLeave()
    {
        $('#board>.piece.hypo-target, #board>.arrow').remove();
        var hist = $('#info-game-history>.move.current');
        if (hist.length)
            setHistory(hist.data('index'), '');
        else
        {
            main.removeClass('viewing-history');
            setupPosition(position);
            deselectPiece(false);
        }
    }

    function historyLeaveAll()
    {
        $('#board>.piece.hypo-target, #board>.arrow').remove();
        main.removeClass('viewing-history');

        // jQuery’s .addClass() / .removeClass() don’t work on SVG elements :(
        $('#info-game-history>.move.current').removeClass('current');
        $('#info-game-history>.game-graph>svg>.column.current').attr('class', 'column');
        setupPosition(position);
        deselectPiece(false);
    }

    function historyClick()
    {
        // jQuery’s .addClass() / .removeClass() don’t work on SVG elements :(
        $('#info-game-history>.move.current').removeClass('current');
        $('#info-game-history>.game-graph>svg>.column.current').attr('class', 'column');
        var move = $(this).data('index');
        var newPos = setHistory(move, 'animate');
        $('#info-game-history>.move').filter(function() { return $(this).data('index') === move; }).addClass('current');
        $('#info-game-history>.game-graph>svg>.column').filter(function() { return $(this).data('index') === move; }).attr('class', 'column current');
    }

    function createSvgGraph(datas, widthInMoves, moves, columnClasses, height, ticks, id)
    {
        var moveWidth = 7,
            svg = '',
            labels = '',
            viewbox = { x1: -3, y1: -3, x2: widthInMoves * moveWidth + 3, y2: height + 3 };
        for (var i = 25; i < height; i += 25)
            svg += '<path d="M 0,' + (height - i) + ' ' + (widthInMoves * moveWidth) + ',' + (height - i) + '" class="grid" />';
        for (var i = 0; i < datas.length; i++)
        {
            var d = 'M';
            for (var j = 0; j < datas[i].d.length; j++)
                if (datas[i].d[j] !== null)
                {
                    var n = datas[i].d[j], x = moveWidth * j, y = height - n;
                    if (typeof datas[i].d[j] === 'object')
                    {
                        n = datas[i].d[j].n;
                        y = height - n;
                        r = datas[i].d[j].d ? " rotate(180)" : "";
                        labels += '<g class="label" filter="url(#drop-shadow)">';
                        labels += '<path class="label ' + datas[i].c + '" d="M 0,0 C 0,0 0,-5 5,-10 10,-15 10,-15 10,-20 10,-25 5,-30 0,-30 -5,-30 -10,-25 -10,-20 -10,-15 -10,-15 -5,-10 0,-5 0,0 0,0 z" transform="translate(' + x + ',' + y + ')' + r + '" />';
                        labels += '</g>';
                        labels += '<text class="' + datas[i].c + '" x="' + x + '" y="' + (datas[i].d[j].d ? y + 24 : y - 13) + '"><tspan>' + datas[i].d[j].l + '</tspan></text>';
                        viewbox.x1 = Math.min(viewbox.x1, x - 13);
                        viewbox.x2 = Math.max(viewbox.x2, x + 13);
                        viewbox.y1 = Math.min(viewbox.y1, datas[i].d[j].d ? y - 3 : y - 33);
                        viewbox.y2 = Math.max(viewbox.y2, datas[i].d[j].d ? y + 33 : y + 3);
                    }
                    d += ' ' + x + ',' + y;
                }
            svg += '<path filter="url(#drop-shadow)" class="data ' + datas[i].c + '" d="' + d + '" />';
        }
        for (var i = 0; i < moves; i++)
            svg += '<rect class="column' + (columnClasses && columnClasses[i] && columnClasses[i].length ? ' ' + columnClasses[i].join(' ') : '') + '" data-index="' + i + '" x="' + ((i + .5) * moveWidth) + '" y="0" width="' + moveWidth + '" height="' + height + '" />';
        svg += '<path d="M 0,0 0,' + height + ' ' + (widthInMoves * moveWidth) + ',' + height + '" class="axes" />';
        svg += labels;
        svg = '<svg width="100%" viewBox="' + viewbox.x1 + ' ' + viewbox.y1 + ' ' + (viewbox.x2 - viewbox.x1) + ' ' + (viewbox.y2 - viewbox.y1) + '" xmlns="http://www.w3.org/2000/svg">' + svg + '</svg>';
        return $('<div>').attr('id', id).addClass('game-graph').append(svg);
    }

    function getWinMultiplier(pos, whiteWon)
    {
        // Single
        if (pos.NumPiecesPerTongue[whiteWon ? Tongue.BlackHome : Tongue.WhiteHome] > 0)
            return 1;

        // Backgammon
        if (pos.NumPiecesPerTongue[whiteWon ? Tongue.BlackPrison : Tongue.WhitePrison] > 0)
            return 3;
        for (var i = 0; i < 6; i++)
        {
            var j = whiteWon ? 18 + i : i;
            if (pos.NumPiecesPerTongue[j] > 0 && pos.IsWhitePerTongue[j] === !whiteWon)
                return 3;
        }

        // Gammon
        return 2;
    }

    function isCrossedOver(pos)
    {
        if (pos.NumPiecesPerTongue[Tongue.WhitePrison] > 0 || pos.NumPiecesPerTongue[Tongue.BlackPrison] > 0)
            return false;
        var firstWhite = null, lastBlack = null;
        for (var i = 0; i < 24; i++)
            if (pos.NumPiecesPerTongue[i] > 0)
            {
                if (!pos.IsWhitePerTongue[i])
                    lastBlack = i;
                else if (firstWhite === null)
                    firstWhite = i;
            }
        return firstWhite > lastBlack;
    }

    function updateGameHistory()
    {
        var hist = $('#info-game-history>.move.current').data('move'),
            h = $('#info-game-history').empty(),
            isWhite = moves.length > 0 && moves[0].Dice1 > moves[0].Dice2,
            diceTotals = { white: 0, whiteData: [0], black: 0, blackData: [0], max: 0 },
            pips = { whiteData: [167], blackData: [167], max: 167 },
            pos = main.data('initial'),
            prevWinMult = { white: 3, black: 3 },
            crossoverFound = false,
            maxCollapsed = 4,
            specialMoves = moves.map(function(m) { return m.Doubled ? ['doubled'] : []; });

        function tongueName(t)
        {
            if (t === Tongue.BlackPrison || t === Tongue.WhitePrison)
                return 'P';
            if (t === Tongue.BlackHome || t === Tongue.WhiteHome)
                return 'H';
            return t + 1;
        }

        if (moves.length > maxCollapsed)
            h.append('<a href="#" class="expand-collapse">');

        for (var i = 0; i < moves.length; i++)
        {
            var moveStr = '', origPos = pos;
            var dt = (diceTotals[isWhite ? 'white' : 'black'] += moves[i].Dice1 === moves[i].Dice2 ? 4 * moves[i].Dice1 : moves[i].Dice1 + moves[i].Dice2);
            diceTotals.whiteData.push(isWhite ? dt : null);
            diceTotals.blackData.push(isWhite ? null : dt);
            diceTotals.max = Math.max(diceTotals.max, dt);
            pos = processMove(pos, isWhite, moves[i]);

            if ('SourceTongues' in moves[i])
            {
                if (moves[i].SourceTongues.length === 0)
                    moveStr = '(no moves)';
                else
                {
                    for (var j = 0; j < moves[i].SourceTongues.length; j++)
                    {
                        if (moveStr.length)
                            moveStr += j % 2 ? '\u2003' : '\n';
                        moveStr += tongueName(moves[i].SourceTongues[j]) + '→' + tongueName(moves[i].TargetTongues[j]);
                    }
                }
                if (!crossoverFound && isCrossedOver(pos))
                {
                    crossoverFound = true;
                    specialMoves[i].push('crossover');
                }
                var pp = getPipCounts(pos);
                pips.max = Math.max(pips.max, Math.max(pp.white, pp.black));

                for (var wh = 0; wh < 2; wh++)
                {
                    var winMult = getWinMultiplier(pos, wh === 1);
                    var wb = wh ? 'white' : 'black';
                    if (winMult !== prevWinMult[wb])
                        pp[wb] = { n: pp[wb], l: winMult, d: pp[wb] < pp[wh ? 'black' : 'white'] };
                    prevWinMult[wb] = winMult;
                }

                pips.whiteData.push(pp.white);
                pips.blackData.push(pp.black);
            }
            h.append($('<div>')
                .addClass('row move' +
                    (isWhite ? ' white' : ' black') +
                    (i === hist ? ' current' : '') +
                    (i === 0 ? ' first' : '') +
                    (i === moves.length - 1 ? ' last' : '') +
                    (i < moves.length - maxCollapsed ? ' expandable' : ''))
                .append(moves[i].Doubled ? $('<div>').addClass('cube').append($('<div>').addClass('cube-text').text(pos.GameValue)) : null)
                .append(LiBackgammon.removeClassPrefix($('#dice-0').clone().attr('id', ''), 'val-').removeClass('crossed').addClass('dice-0 val-' + moves[i].Dice1))
                .append(LiBackgammon.removeClassPrefix($('#dice-0').clone().attr('id', ''), 'val-').removeClass('crossed').addClass('dice-1 val-' + moves[i].Dice2))
                .append($('<div>').addClass('move').text(moveStr))
                .data('index', i)
                .data('pos', origPos)
                .data('isWhite', isWhite)
                .mouseenter(historyEnter)
                .mouseleave(historyLeave)
                .click(historyClick));
            isWhite = !isWhite;
        }

        h.append('<hr>')
            .append($('<div>')
                .addClass('row totals')
                .append(position.GameValue === null ? null : $('<div>').addClass('cube').append($('<div>').addClass('cube-text').text(position.GameValue)))
                .append($('<div>').addClass('white dice-total').append($('<div>').text(diceTotals.white)))
                .append($('<div>').addClass('black dice-total').append($('<div>').text(diceTotals.black))))
            .append(createSvgGraph([{ d: pips.whiteData, c: 'white' }, { d: pips.blackData, c: 'black' }], Math.max(50, moves.length), moves.length, specialMoves, pips.max, 25, 'graph-pips'))
            .append(createSvgGraph([{ d: diceTotals.whiteData, c: 'white' }, { d: diceTotals.blackData, c: 'black' }], Math.max(50, moves.length), moves.length, specialMoves, diceTotals.max, 25, 'graph-dicetotals'));

        $('#info-game-history>.game-graph>svg>.column')
            .click(historyClick)
            .mouseenter(historyEnter)
            .mouseleave(historyLeave);
    }

    function updateMatchGraph()
    {
        $('#info-match-history>.game-graph').remove();
        var white = [0], black = [0], max = 0, w = 0, b = 0, games = $('#info-match-history>.game'), cur;
        games.each(function(i, e)
        {
            if (!$(e).hasClass('last'))
            {
                white.push(w += +$(e).find('.piece.white>.number').text());
                black.push(b += +$(e).find('.piece.black>.number').text());
                max = Math.max(Math.max(w, b), max);
            }
            if (!$(e).attr('href'))
                cur = i;
        });
        var ratio = 4 * games.length / max;
        for (var i = 0; i < white.length; i++)
        {
            white[i] *= ratio;
            black[i] *= ratio;
        }
        $('#info-match-history').append(createSvgGraph([{ d: white, c: 'white' }, { d: black, c: 'black' }], Math.max(games.length, 20), games.length, null, 4 * games.length, 25 * ratio, 'graph-match'))
        var columns = $('#info-match-history>.game-graph>svg>.column');
        columns.not(columns[cur]).click(function() { window.location.href = $(games[$(this).data('index')]).attr('href'); return false; });
        $(columns[cur]).attr('class', 'column current');
    }

    function stopAnimations()
    {
        var anim = $('#board>.piece:animated');
        if (anim.length)
        {
            anim.stop(
                true,   // clearQueue: remove all queued animations from the same element
                false   // jumpToEnd: important because we also don’t want callback functions called
            );
            setupPosition(position);
        }
    }

    function recalcPosition()
    {
        var pos = main.data('initial');
        if (moves.length > 0)
        {
            var whiteStarts = moves[0].Dice1 > moves[0].Dice2;
            for (var i = 0; i < moves.length; i++)
                pos = processMove(pos, whiteStarts ? (i % 2 === 0) : (i % 2 !== 0), moves[i]);
        }
        return pos;
    }

    function setNextGame(url, hasCube)
    {
        setNextUrl(url);
        var ex = $('#info-match-history>.row.game.after-this');
        if (ex.length === 0)
        {
            $('#info-match-history>.row.game.last').removeClass('last');
            ex = $('<a><div class="piece white"><div class="number"></div></div><div class="piece black"><div class="number"></div></div></a>')
                .addClass('row game after-this last')
                .insertBefore('#info-match-history>hr');
        }
        ex.removeClass('cube no-cube').addClass(hasCube ? 'cube' : 'no-cube');
        ex.attr('href', url + window.location.hash);
        updateMatchGraph();
    }

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
    var chatLastScrolledBottom = true;
    var preventCmoScrollEventUntil = null;
    var lastCmoScrollPos = null;
    var settingsLoaded = false;
    var translationNotes = {};
    var reconnectInterval = 0;

    var windowTitleFlash = false;
    window.setInterval(function()
    {
        windowTitleFlash = !windowTitleFlash;
        document.title =
            main.hasClass('debug') ? '(𝐃𝐄𝐁𝐔𝐆) LiBackgammon' :
            main.hasClass('state-Won') ? '(Game over) LiBackgammon' :
            ((main.hasClass('player-white') && main.hasClass('state-White')) || (main.hasClass('player-black') && main.hasClass('state-Black'))) && (main.hasClass('state-ToMove') || main.hasClass('state-ToRoll') || main.hasClass('state-ToConfirmDouble')) && !main.hasClass('auto-0') && !main.hasClass('auto-1')
                ? (windowTitleFlash ? "▲▼▲▼ Your turn" : "▼▲▼▲ Your turn")
                : 'LiBackgammon';
    }, 750);

    var position = recalcPosition();
    var moveSoFar = { SourceTongues: [], TargetTongues: [], OpponentPieceTaken: [], DiceSequence: [] };
    var selectedPiece = null;

    setState();

    var socket;
    var sendQueue = [];
    var socketQueue = [];
    var socketQueueProcessing = false;

    function setNextUrl(url) { main.data('next-game', url).addClass('has-next-game'); }
    function setRematchOffer(offer) { LiBackgammon.removeClassPrefix(main, 'rematch-').addClass('rematch-' + offer); }

    var socketMethods = {
        nextUrl: function(args) { setNextUrl(args) },
        state: function(args)
        {
            if (!main.hasClass('spectating') && args === (playerIsWhite ? 'White_ToRoll' : 'Black_ToRoll') && $('#settings-autoroll-select:checked').length)
                socketSend({ roll: 1 });
            else
                setState(args);
        },
        on: function(args) { main.addClass('online-' + args); },
        off: function(args) { main.removeClass('online-' + args); },
        chatid: function(args) { $('#chat-token-' + args.token).attr('id', 'chat-' + args.id); },
        rematch: function(args) { setRematchOffer(args); },

        resync: function(args)
        {
            moves = args.moves;
            lastMove = moves[moves.length - 1];
            position = recalcPosition();
            setState(args.state, false);
            onResize(true);
            updateGameHistory();
            updateMatchGraph();
            deselectPiece(false);
            if (args.nextUrl)
                setNextGame(args.nextUrl, args.nextCube);
            if (args.rematch)
                setRematchOffer(args.rematch);
        },

        player: function(args)
        {
            playerIsWhite = args === 'White';
            main.removeClass('player-random').addClass(playerIsWhite ? 'player-white' : 'player-black');
        },

        move: function(args)
        {
            if (!main.hasClass('viewing-history'))
            {
                deselectPiece(true);
                if ('auto' in args)
                    main.addClass('auto-' + args.auto);
            }

            moves[moves.length - 1].SourceTongues = args.sourceTongues;
            moves[moves.length - 1].TargetTongues = args.targetTongues;

            position = processMoveDirect(position, main.hasClass('state-White'), args.sourceTongues, args.targetTongues, { mode: main.hasClass('viewing-history') ? null : 'animate' });
            setTimeout(processSocketQueue, main.hasClass('viewing-history') ? 100 : 'auto' in args ? 1800 : 200 + 400 * args.sourceTongues.length);
            updateGameHistory();
            return true;
        },

        dice: function(args)
        {
            moveSoFar = { SourceTongues: [], TargetTongues: [], OpponentPieceTaken: [], DiceSequence: [] };
            moves.push({ Dice1: args.dice1, Dice2: args.dice2, Doubled: args.doubled });
            lastMove = moves[moves.length - 1];
            updateGameHistory();

            main
                .removeClass('dice-2 dice-4 dice-start dice-white dice-black white-starts black-starts')
                .addClass((lastMove.Dice1 === lastMove.Dice2 ? 'dice-4' : 'dice-2') + (moves.length === 1 ? ' dice-start' : ''));
            $('#board>.dice').removeClass('val-1 val-2 val-3 val-4 val-5 val-6 crossed');
            $('#board>#dice-0').addClass('val-' + lastMove.Dice1);
            $('#board>#dice-1,#board>#dice-2,#board>#dice-3').addClass('val-' + lastMove.Dice2);

            setState(args.state, args.skipHighlight);

            main.addClass(moves.length > 1
                ? (main.hasClass('state-White') ? 'dice-white' : 'dice-black')
                : (main.hasClass('state-White') ? 'white-starts' : 'black-starts'));
        },

        cube: function(args)
        {
            $('#cube-text').text(args.gameValue);
            var oldTop = $('#cube').position().top;
            main.removeClass('cube-white cube-black').addClass(args.whiteOwnsCube ? 'cube-white' : 'cube-black');
            var newTop = $('#cube').position().top;
            $('#cube').css('top', oldTop).animate({ top: newTop }, { duration: 1000, complete: function() { $('#cube').css('top', ''); } });
            position.GameValue = args.gameValue;
            position.WhiteOwnsCube = args.whiteOwnsCube;
            setTimeout(processSocketQueue, 1000);
            return true;
        },

        win: function(args)
        {
            setState(args.state);
            $('#win>.points>.number,#main.state-White #info-match-history>a.game:not([href])>.white>.number,#main.state-Black #info-match-history>a.game:not([href])>.black>.number').text(args.score);
            $('#win>.points').removeClass('singular plural').addClass(args.score === 1 ? "singular" : "plural");

            if ('whiteMatchScore' in args)
                $('#matchscore-white,#info-match-history>.totals>.white>.number').text(args.whiteMatchScore);
            if ('blackMatchScore' in args)
                $('#matchscore-black,#info-match-history>.totals>.black>.number').text(args.blackMatchScore);
            if ('matchOver' in args)
                main.addClass('end-of-match');
            if ('nextGame' in args)
                setNextGame(main.data('next-game'), args.nextGame.cube);
        },

        chat: function(args)
        {
            var chatList = args instanceof Array ? args : [args];
            var chatOpen = $('body.hash-sidebar.hash-chat').length > 0;
            var justSeenIds = [];
            for (var i = 0; i < chatList.length; i++)
            {
                var msg = chatList[i];
                var ownMsg = msg.player === (playerIsWhite ? 'White' : 'Black');
                if (chatOpen && !ownMsg && !msg.seen)
                    justSeenIds.push(msg.id);
                if (chatOpen || ownMsg)
                    msg.seen = true;
                var obj = $('#chat-' + msg.id);
                if (!obj.length)
                    obj = $('<div><div class="time"></div><div class="msg"></div></div>').attr('id', 'chat-' + msg.id).data('id', msg.id).addClass('chat-msg').appendTo('#chat-msgs');
                obj.removeClass('Black White seen').addClass(msg.player);
                if (chatList[i].game !== main.data('game-id'))
                    obj.addClass('other-game');
                if (msg.seen)
                    obj.addClass('seen');
                var d = new Date(msg.time);
                obj.find('.time').text(d.getHours() + ':' + (d.getMinutes() < 10 ? '0' : '') + d.getMinutes())
                    .attr('title', d.toLocaleFormat ? d.toLocaleFormat('%a %d %b %Y, %H:%M:%S') : msg.time);
                obj.find('.msg').text(msg.msg);
            }
            onResize();

            if (!main.hasClass('spectating') && justSeenIds.length > 0)
                socketSend({ chatSeen: { ids: justSeenIds } });

            var unseenMsgs = $('.chat-msg:not(.seen)').length;
            $('#btn-chat>.notification>.notification-inner').text(unseenMsgs);
            $('#btn-chat>.notification')[unseenMsgs ? 'addClass' : 'removeClass']('shown');
        },

        settings: function(args)
        {
            ['style', 'lang'].forEach(function(e)
            {
                var s = $('#settings-' + e + '-select').empty().append($('<option value="">').text('(default)'));
                for (var i in args[e])
                    s.append($('<option>').attr('value', i).text(args[e][i]));
                s.val(LiBackgammon.hash.dict[e] || '');
            });
        },

        languages: function(args)
        {
            var d = $('#translate-select').empty();
            for (var i = 0; i < args.length; i++)
            {
                var opt = $('<option>').attr('value', args[i].hash).text(args[i].name).appendTo(d);
                if (args[i].isApproved)
                    opt.addClass('approved');
                if (args[i].isComplete)
                    opt.addClass('complete');
            }
        },

        translateError: function(args)
        {
            sidebar('translate');
            $('#translate-error').text(args.error).show();
            if ('hash' in args && 'name' in args)
            {
                if (!$('#translate-select>option').filter(function(_, e) { return $(e).attr('value') === args.hash; }).length)
                    $('<option>').attr('value', args.hash).text(args.name).appendTo('#translate-select');
                $('#translate-select').val(args.hash);
            }
        },

        translate: function(args)
        {
            LiBackgammon.translation = args;
            LiBackgammon.hashRemove(sidebars);
            LiBackgammon.hashAdd(['sidebar', 'translating'], { lang: args.hash, translator: args.token });
        },

        translationSaved: function(args)
        {
            var i = $('#translating .translatable>.trans>input').filter(function(_, e) { return $(e).data('sel') === args.sel; });
            if (i.length)
                i.parent().removeClass('unsaved submitting').addClass('saved');
            if (args.removed)
            {
                delete LiBackgammon.translation.strings[args.sel];
                updateTranslation();
            }
        }
    };

    if (main.hasClass('spectating'))
        $('#undo,#commit,#roll,#double,#accept,#decline,#btn-resign,#resign-confirm,#resign-cancel,#offer-rematch,#accept-rematch,#cancel-rematch').click(function() { return false; });
    else
    {
        deselectPiece();

        function processClickOrDblclick(isWhite, tongue, isDblClick)
        {
            if (!isPlayerToMove() || main.hasClass('viewing-history'))
                return false;

            // Only allow clicking on a piece of the player’s color
            if (isWhite !== playerIsWhite)
                return false;

            // See if the player has any valid moves (or they’ve already entered their move)
            var clickableTongues = getClickableSourceTongues();
            if (clickableTongues.length === 0)
                return false;

            // Only allow clicking on a tongue that is a valid source tongue
            if (clickableTongues.indexOf(tongue) === -1)
                return false;

            if (isDblClick)
            {
                stopAnimations();
                deselectPiece(true);
                var targetMoves = getTargetMoves(tongue);
                if ((Tongue.BlackHome in targetMoves) || (Tongue.WhiteHome in targetMoves))
                    executeMove(targetMoves[Tongue.BlackHome] || targetMoves[Tongue.WhiteHome]);
                else
                {
                    for (var i = (playerIsWhite ? 23 : 0) ; playerIsWhite ? (i >= 0) : (i < 24) ; playerIsWhite ? i-- : i++)
                    {
                        if (i in targetMoves)
                        {
                            executeMove(targetMoves[i]);
                            break;
                        }
                    }
                }
            }
            else if (topPieceOfTongue(tongue).is(selectedPiece))
                deselectPiece();
            else
                selectPiece(tongue);
        }

        $('#board>.piece')
            .click(function() { processClickOrDblclick($(this).hasClass('white'), $(this).data('tongue'), false); return false; })
            .dblclick(function() { processClickOrDblclick($(this).hasClass('white'), $(this).data('tongue'), true); return false; });

        $('#board').on('mouseenter', '.tongue.selectable, .home.selectable, .automove', function()
        {
            processMove(position, playerIsWhite, $(this).data('move'), { mode: 'indicate' });
        });

        $('#board').on('mouseleave', '.tongue.selectable, .home.selectable, .automove', function()
        {
            $('#board>.piece.hypo-target, #board>.arrow').remove();
        });

        $('#board').on('click', '.tongue.selectable, .home.selectable, .automove', function()
        {
            var move = $(this).data('move');
            deselectPiece(true);
            executeMove(move);
        });

        $(document).keydown(function(e)
        {
            if (e.keyCode === 27)
            {
                if ($('#info-game-history>.move.current').length)
                    historyLeaveAll();
                else if (selectedPiece !== null)
                    deselectPiece();
            }
        });

        $('#undo').click(function()
        {
            if (main.hasClass('spectating') || main.hasClass('viewing-history') || !isPlayerToMove())
                return false;
            var lastIndex = moveSoFar.DiceSequence.length - 1;
            if (lastIndex >= 0)
            {
                stopAnimations();
                deselectPiece(true);
                position = processMoveDirect(position, playerIsWhite, moveSoFar.TargetTongues[lastIndex], moveSoFar.SourceTongues[lastIndex], {
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

        var getGeneralisedButtonClick = function(msgToSend, condition, removeClasses)
        {
            return function()
            {
                if (!main.hasClass('spectating') && (!condition || condition()))
                {
                    socketSend(typeof msgToSend === 'function' ? msgToSend() : msgToSend);
                    if (removeClasses)
                        main.removeClass(removeClasses);
                }
                return false;
            };
        };

        $('#commit').click(function()
        {
            if (!main.hasClass('spectating') && main.hasClass('committable'))
            {
                socketSend({ move: { sourceTongues: moveSoFar.SourceTongues, targetTongues: moveSoFar.TargetTongues } });
                main.removeClass('undoable committable');
                moves[moves.length - 1].SourceTongues = moveSoFar.SourceTongues;
                moves[moves.length - 1].TargetTongues = moveSoFar.TargetTongues;
                updateGameHistory();
            }
            return false;
        });

        $('#roll').click(getGeneralisedButtonClick({ roll: 1 }, function() { return $('#main.state-ToRoll').length > 0; }));
        $('#double').click(getGeneralisedButtonClick({ double: 1 }));
        $('#accept').click(getGeneralisedButtonClick({ accept: 1 }));
        $('#decline').click(getGeneralisedButtonClick({ decline: 1 }));
        $('#resign-confirm').click(getGeneralisedButtonClick({ resign: 1 }, function() { return $('#main.resigning:not(.state-Won)').length > 0; }, 'resigning'));
        $('#resign-cancel').click(function() { main.removeClass('resigning'); return false; });
        $('#btn-resign').click(function() { if (!$('#main.state-Won,#main.state-Waiting').length) main.addClass('resigning'); return false; });

        $('#offer-rematch').click(getGeneralisedButtonClick(
            { rematch: 1 },
            function()
            {
                return $(
                    '#main.end-of-match:not(.rematch-White):not(.rematch-Black):not(.rematch-WhiteDeclined):not(.rematch-BlackDeclined):not(.rematch-Accepted):not(.spectating),' +
                    '#main:not(.in-match):not(.rematch-White):not(.rematch-Black):not(.rematch-WhiteDeclined):not(.rematch-BlackDeclined):not(.rematch-Accepted):not(.spectating),' +
                    '#main.rematch-WhiteDeclined.player-white,' +
                    '#main.rematch-BlackDeclined.player-black').length > 0;
            }));
        var rematchAcceptable = function() { return $('#main.rematch-White.player-black, #main.rematch-Black.player-white').length > 0; };
        $('#accept-rematch').click(getGeneralisedButtonClick({ acceptRematch: 1 }, rematchAcceptable));
        $('#cancel-rematch').click(getGeneralisedButtonClick({ cancelRematch: 1 }, rematchAcceptable));
    }

    $('#btn-chat').click(function() { sidebar('chat'); return false; });
    $('#btn-info').click(function() { sidebar('info'); return false; });
    $('#join').click(function() { return main.hasClass('state-Waiting') && main.hasClass('spectating'); });
    $('#goto-next-game').click(function() { if (main.data('next-game')) window.location.href = main.data('next-game') + window.location.hash; return false; });
    $('#settings-helpers-select').change(function() { LiBackgammon[$('#settings-helpers-select:checked').length ? 'hashAdd' : 'hashRemove']('helpers'); });
    $('#settings-percentages-select').change(function() { LiBackgammon[$('#settings-percentages-select:checked').length ? 'hashAdd' : 'hashRemove']('percentages'); });

    $('#settings-style-select').change(function()
    {
        var style = $(this).val();
        if (style === '')
            LiBackgammon.hashRemove([], ['style']);
        else
            LiBackgammon.hashAdd([], { style: style });
    });

    $('#settings-lang-select').change(function()
    {
        var lang = $(this).val();
        if (lang === '')
            LiBackgammon.hashRemove([], ['lang']);
        else
            LiBackgammon.hashAdd([], { lang: lang });
        LiBackgammon.hashRemove([], ['translator']);
    });

    $('#settings-lang-custom').click(function()
    {
        sidebar('translate');
        $('#translate-select').empty().append('<option>Loading...</option>');
        socketSend({ getLanguages: 1 });
        return false;
    });

    $('#translate-create').click(function()
    {
        $('#translate-error').hide();
        socketSend({ createLanguage: { name: $('#translate-name').val(), hashName: $('#translate-code').val() } });
        return false;
    });

    $('#translate-edit').click(function()
    {
        $('#translate-error').hide();
        socketSend({ translate: { hashName: $('#translate-select').val(), token: LiBackgammon.hash.dict.translator } });
        return false;
    });

    $('#btn-settings').click(function()
    {
        sidebar('settings');
        return false;
    });

    $('#leave-history').click(historyLeaveAll);

    $('#translating-link').click(function()
    {
        var hash = LiBackgammon.hash;
        if ('translator' in hash.dict && 'lang' in hash.dict)
            sidebar('translating');
        return false;
    });

    var chatToken = 0;
    $('#chat-msg').keypress(function(e)
    {
        if (e.keyCode === 13)   // Enter
        {
            var msg = $('#chat-msg').val().trim();
            if (msg.length)
            {
                var tk = chatToken++;
                var obj = $('<div><div class="time"></div><div class="msg"></div></div>')
                    .attr('id', 'chat-token-' + tk).addClass('chat-msg').addClass(playerIsWhite ? 'White' : 'Black').appendTo('#chat-msgs');
                obj.find('.time').text('Sending...');
                obj.find('.msg').text(msg);
                socketSend({ chat: { msg: msg, token: tk } });
                onResize();
            }
            $('#chat-msg').val('');
        }
    });

    $(document.body).on('click', '.expand-collapse', function() { $(this).parent().toggleClass('expanded'); return false; });

    // Dummy SVG to define SVG elements to be referenced by other SVGs
    $('#main>#sidebar>#info').append('<svg class="defs" width="0" height="0"><defs>' +
        // #doubled-crossover-gradient: Linear gradient, used by the game history graphs
        '<linearGradient id="doubled-crossover-gradient" x1="0" y1="0" x2="0" y2="1"><stop offset="0" class="start"/><stop offset="1" class="end"/></linearGradient>' +
        // #drop-shadow: Used by the game history graphs
        '<filter id="drop-shadow" x="-1000%" y="-1000%" width="2100%" height="2100%"><feGaussianBlur in="SourceAlpha" stdDeviation="2" /><feOffset dx="2" dy="2" result="b"/><feFlood flood-color="rgba(0,0,0,0.5)"/><feComposite in2="b" operator="in"/><feMerge><feMergeNode/><feMergeNode in="SourceGraphic"/></feMerge></filter>' +
    '</defs></svg>');

    // Add extra CSS
    var cssWithSidebar = [];                    // CSS for when the sidebar is visible
    var cssInMedia = [];                          // CSS for viewports wider than the playing area’s aspect ratio
    var cssInMediaWithSidebar = [];      // CSS for when the sidebar is visible and the viewport is wider than the whole UI
    var vwRe = /(-?\b\d*\.?\d+)vw\b/g;
    for (var ss = 0; ss < document.styleSheets.length; ss++)
    {
        var rules;
        try { rules = document.styleSheets[ss].cssRules || document.styleSheets[ss].rules; }
        catch (exc) { continue; }
        for (var ruleix = 0; ruleix < rules.length; ruleix++)
        {
            if (!rules[ruleix].selectorText)
                continue;
            var oldSelectors = rules[ruleix].selectorText.split(/\s*,\s*/);
            var newSelectors = [];
            for (var i = 0; i < oldSelectors.length; i++)
            {
                var result = oldSelectors[i].match(/^\s*body\b(?!-)/);
                newSelectors.push(result ? 'body.hash-sidebar' + oldSelectors[i].substr(result[0].length) : 'body.hash-sidebar ' + oldSelectors[i]);
            }

            var props = rules[ruleix].style;
            var sidebarProps = [], inMediaProps = [], sidebarInMediaProps = [];
            for (var propix = 0; propix < props.length; propix++)
            {
                var propName = props[propix].replace(/-value$/, '');
                var val = props.getPropertyValue(propName);
                if (vwRe.test(val) || val === '0px')
                {
                    var imp = props.getPropertyPriority(propName);
                    if (imp)
                        imp = '!' + imp;
                    sidebarProps.push(propName + ':' + val.replace(vwRe, function(_, vw) { return (vw * 100 / (100 + sidebarWidth)) + 'vw'; }) + imp);
                    inMediaProps.push(propName + ':' + val.replace(vwRe, function(_, vw) { return (vw * 100 / boardHeight) + 'vh'; }) + imp);
                    sidebarInMediaProps.push(propName + ':' + val.replace(vwRe, function(_, vw) { return (vw * 100 / boardHeight) + 'vh'; }) + imp);
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
    $('#converted-css').text(cssWithSidebar.join('\n') +
        '\n\n@media screen and (min-aspect-ratio: 100/' + boardHeight + ') {\n    ' + cssInMedia.join('\n    ') + '}' +
        '\n\n@media screen and (min-aspect-ratio: ' + (100 + sidebarWidth) + '/' + boardHeight + ') {\n    ' + cssInMediaWithSidebar.join('\n    ') + '}');

    $('#chat-msgs-outer').scroll(function()
    {
        var elem = this;
        // In case this scrolling is triggered by a window resize, we need the window resize handled first.
        setTimeout(function()
        {
            if (preventCmoScrollEventUntil > Date.now())
                return;
            if (elem.scrollTop === lastCmoScrollPos)
                return;

            lastCmoScrollPos = elem.scrollTop;
            elem.scrollTop = elem.scrollHeight;
            chatLastScrolledBottom = elem.scrollTop === lastCmoScrollPos || elem.scrollTop === lastCmoScrollPos + 1;
            elem.scrollTop = lastCmoScrollPos;
        }, 1);
    });

    $(window)
        .on('resize', onResize)
        .on('hashchange', hashChange);

    onResize();
    reconnect();
    hashChange();
    updateGameHistory();
    updateMatchGraph();

});
