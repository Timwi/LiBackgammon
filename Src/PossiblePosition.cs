using System;
using System.Collections.Generic;
using System.Linq;
using RT.Util;
using RT.Util.ExtensionMethods;
using RT.Util.Serialization;

namespace LiBackgammon
{
    public class PossiblePosition
    {
        public int[] NumPiecesPerTongue;
        public bool[] IsWhitePerTongue;

        public string ToJson() { return ClassifyJson.Serialize(this).ToString(); }

        public PossiblePosition Clone()
        {
            return new PossiblePosition
            {
                NumPiecesPerTongue = NumPiecesPerTongue.ToArray(),
                IsWhitePerTongue = IsWhitePerTongue.ToArray()
            };
        }

        public PossiblePosition ProcessMove(bool whitePlayer, int sourceTongue, int targetTongue)
        {
            return ProcessMove(whitePlayer, new[] { sourceTongue }, new[] { targetTongue });
        }

        public PossiblePosition ProcessMove(bool whitePlayer, int[] sourceTongues, int[] targetTongues, int[] dice = null)
        {
            var newPos = Clone();

            var processSubmove = Ut.Lambda((bool isWhite, int sourceTongue, int targetTongue) =>
            {
                newPos.NumPiecesPerTongue[sourceTongue]--;
                newPos.NumPiecesPerTongue[targetTongue]++;
                newPos.IsWhitePerTongue[targetTongue] = isWhite;
            });

            for (var k = 0; k < sourceTongues.Length; k++)
            {
                var sourceTongue = sourceTongues[k];
                var targetTongue = targetTongues[k];
                if (dice != null)
                {
                    var validDiceIndex = -1;
                    var furthestFromHome =
                        NumPiecesPerTongue[Tongues.Prison(whitePlayer)] > 0
                            ? 25
                            : Enumerable.Range(0, 24).Select(t => IsWhitePerTongue[t] == whitePlayer && NumPiecesPerTongue[t] > 0 ? (whitePlayer ? 24 - t : t + 1) : -1).Max();
                    for (int i = 0; i < dice.Length; i++)
                        if (dice[i] > 0 && getTargetTongue(sourceTongue, furthestFromHome, dice[i], whitePlayer) == targetTongue)
                            validDiceIndex = i;
                    if (validDiceIndex == -1)
                        return null;
                    dice[validDiceIndex] = 0;
                }

                if (newPos.NumPiecesPerTongue[sourceTongue] == 0 || newPos.IsWhitePerTongue[sourceTongue] != whitePlayer)
                    // There are no pieces on the source tongue belonging to the correct player
                    return null;

                if (newPos.IsWhitePerTongue[targetTongue] == !whitePlayer)
                {
                    // The target tongue is blocked by the opponent
                    if (newPos.NumPiecesPerTongue[targetTongue] > 1)
                        return null;

                    // Move is permissible, but an opponent piece is taken
                    if (newPos.NumPiecesPerTongue[targetTongue] == 1)
                        processSubmove(!whitePlayer, targetTongue, Tongues.Prison(!whitePlayer));
                }
                processSubmove(whitePlayer, sourceTongue, targetTongue);
            }

            return newPos;
        }

        public List<PossibleMove> GetAllValidMoves(bool whitePlayer, int dice1, int dice2)
        {
            if (dice1 == dice2)
                return GetAllValidMoves(whitePlayer, new[] { new[] { dice1, dice1, dice1, dice1 } });
            return GetAllValidMoves(whitePlayer, new[] { new[] { dice1, dice2 }, new[] { dice2, dice1 } });
        }

        public List<PossibleMove> GetAllValidMoves(bool whitePlayer, int[][] diceSequences)
        {
            var validMoves = new Dictionary<int, List<PossibleMove>>();
            var e = new int[0];
            foreach (var seq in diceSequences)
                addValidMoves(validMoves, whitePlayer, seq, e, e, e);

            // Only the moves with the greatest length are valid
            if (validMoves.Count == 0)
                return new List<PossibleMove>();
            return validMoves[validMoves.Keys.Max()];
        }

        private void addValidMoves(Dictionary<int, List<PossibleMove>> movesByLength, bool whitePlayer, int[] remainingDiceSequence, int[] diceSequenceSoFar, int[] sourceTongues, int[] targetTongues)
        {
            var prison = Tongues.Prison(whitePlayer);
            var home = Tongues.Home(whitePlayer);

            // Which tongues can the player move a piece from?
            var accessibleTongues = new List<int>();
            // How far from home is the furthest piece? (if it’s 4, say, then you can use a 6 to move the 4-away pieces into home)
            var furthestFromHome = 0;

            if (NumPiecesPerTongue[prison] > 0)
            {
                furthestFromHome = 25;
                accessibleTongues.Add(prison);
            }
            else
            {
                for (var tng = 0; tng < Tongues.NumTongues; tng++)
                {
                    if (tng == prison || tng == home || NumPiecesPerTongue[tng] == 0 || IsWhitePerTongue[tng] != whitePlayer)
                        continue;
                    accessibleTongues.Add(tng);
                    furthestFromHome = Math.Max(furthestFromHome, whitePlayer ? (24 - tng) : (tng + 1));
                }
            }

            foreach (var src in accessibleTongues)
            {
                var target = getTargetTongue(src, furthestFromHome, remainingDiceSequence[0], whitePlayer);
                if (target == null || (NumPiecesPerTongue[target.Value] > 1 && IsWhitePerTongue[target.Value] != whitePlayer))
                    continue;
                var positionAfterMove = ProcessMove(whitePlayer, src, target.Value);
                if (positionAfterMove == null)
                    continue;

                var sourceTs = sourceTongues.Concat(src).ToArray();
                var targetTs = targetTongues.Concat(target.Value).ToArray();
                var diceSeqSoFar = diceSequenceSoFar.Concat(remainingDiceSequence[0]).ToArray();
                var remainingDiceSeq = remainingDiceSequence.Subarray(1);

                movesByLength.AddSafe(sourceTs.Length, new PossibleMove { SourceTongues = sourceTs, TargetTongues = targetTs, DiceSequence = diceSeqSoFar, EndPosition = positionAfterMove });

                if (remainingDiceSeq.Length > 0)
                    positionAfterMove.addValidMoves(movesByLength, whitePlayer, remainingDiceSeq, diceSeqSoFar, sourceTs, targetTs);
            }
        }

        private static int? getTargetTongue(int sourceTongue, int furthestFromHome, int dice, bool whitePlayer)
        {
            if (sourceTongue == Tongues.Prison(whitePlayer))
                return whitePlayer ? dice - 1 : 24 - dice;
            var fromHome = whitePlayer ? 24 - sourceTongue : sourceTongue + 1;
            if ((fromHome == dice && furthestFromHome <= 6) || (fromHome <= dice && fromHome == furthestFromHome))
                return Tongues.Home(whitePlayer);
            var target = whitePlayer ? sourceTongue + dice : sourceTongue - dice;
            return (target >= 0 && target < 24) ? target : (int?) null;
        }

        public sealed class Comparer : IEqualityComparer<PossiblePosition>
        {
            public bool Equals(PossiblePosition x, PossiblePosition y)
            {
                if (x == null && y == null)
                    return true;
                if ((x == null) != (y == null))
                    return false;

                for (int i = 0; i < Tongues.NumTongues; i++)
                {
                    if (x.NumPiecesPerTongue[i] != y.NumPiecesPerTongue[i])
                        return false;
                    if (x.NumPiecesPerTongue[i] > 0 && (x.IsWhitePerTongue[i] != y.IsWhitePerTongue[i]))
                        return false;
                }
                return true;
            }

            public int GetHashCode(PossiblePosition pos)
            {
                int hash = 0;
                for (int i = 0; i < Tongues.NumTongues; i++)
                    hash = 30 * hash + 2 * pos.NumPiecesPerTongue[i] + (pos.IsWhitePerTongue[i] && pos.NumPiecesPerTongue[i] > 0 ? 1 : 0);
                return hash;
            }
        }
    }
}
