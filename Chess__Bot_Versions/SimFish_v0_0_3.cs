/**
 * SimFish v 0.0.3
 * Added more optimization to search function with Zobrist hashing/transposition table lookup
 * Can finally sometimes win easily winning endgames!
 * Improvements:
 *  - Now discouraged from making the same move over and over, therefore no longer draws by repition in easy endgames
 *  - Can evaluate quickly during opening and endgame positions
 *  - Much more effective searching
 * Weaknesses:
 *  - Still has a weak evaluation function
 *  - Stalls during tough middle game position and sometimes loses on time
 *  - Poor awareness during endgames - struggles to promote, check, etc.
 *  - Sometimes gets confused and starts shuffling king around repeatedly
 * 
 * Estimated Rating - ~300? (Beginner)
 * 
 * Next Steps:
 *  - Finish search function by evaluating "best first" - evaluate promising lines sooner
 *  - Begin slowly improving evaluation function: dynamic piece values depending on position/situation, etc.
 */

using ChessChallenge.API;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography.X509Certificates;

public class MyBot : IChessBot
{
    public class TranspositionEntry {
        public ulong zobristKey { get; set; }
        public float evalScore { get; set; }
        public int searchDepth { get; set; }
    }

    // Initialize piece values 
    private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    private Dictionary<ulong, TranspositionEntry> transpositionTable = new Dictionary<ulong, TranspositionEntry>();

    public Move Think(Board board, Timer timer)
    {
        Random rnd = new Random();
        // Change search depth depending on current position
        // Small search depth in opening moves, larger as game goes on
        int MAX_SEARCH_DEPTH = getSearchDepth(board, timer);
        if (MAX_SEARCH_DEPTH == 0) {
            if (board.PlyCount == 0) {
                return new Move("e2e4", board);
            }
            else {
                if (board.SquareIsAttackedByOpponent(new Square("e5"))) {
                    return new Move("d7d5", board);
                }
                else {
                    return new Move("e7e5", board);
                }
            }
        }

        // Store all legal moves in an array
        Move[] moves = board.GetLegalMoves();
        Move[] captures = board.GetLegalMoves(true);
        // Vector<Move> interestingMoves;
        // Evaluate each position in moves
        Move bestMove = moves[rnd.Next(moves.Length)];
        float bestEval = board.IsWhiteToMove ? -1000000 : 1000000;
        foreach (Move move in moves) {
            board.MakeMove(move);
            float eval = searchForMove(board, MAX_SEARCH_DEPTH, 
                100000, -100000, board.IsWhiteToMove);
            board.UndoMove(move);
            if (eval > bestEval && board.IsWhiteToMove) {
                bestEval = eval;
                bestMove = move;
            }
            if (eval < bestEval && !board.IsWhiteToMove) {
                bestEval = eval;
                bestMove = move;
            }
        }
        Console.WriteLine("eval == " + bestEval);
        return bestMove;
    }

    int getSearchDepth(Board board, Timer timer) {
        // play less specific moves in opening
        if (board.PlyCount < 2) {
            return 0;
        }
        if (board.PlyCount <= 10) {
            return 2;
        }
        // higher depth in endgame
        else if (board.PlyCount >= 100 || popcount(board.AllPiecesBitboard) <= 8) {
            return 4;
        }
        // reduce search depth with lower time left
        return Math.Min(3, (timer.MillisecondsRemaining / 1000) / 10);
    }

    // Returns approximate evaluation of the board for the current moving player
    float evaluateBoard(Board board) {
        if (board.IsInCheckmate()) {
            return 99999.9f;
        }
        if (board.IsInCheck() || popcount(board.AllPiecesBitboard) <= 10) {
            return (countMaterial(board) + 10.0f);
        }
        float material = countMaterial(board);
        return material;
    }

    // Minimax searching algorithm, returns best evaluation based on evaluateBoard function
    float searchForMove(Board board, int depth, float alpha, float beta, bool whiteToPlay) {
        ulong zobrist_init = board.ZobristKey;
        if (transpositionTable.TryGetValue(zobrist_init, out TranspositionEntry entry)) {
            if (entry.searchDepth >= depth) {
                return entry.evalScore - 1.0f;
            }
        }
        if (depth == 0 || board.GetLegalMoves().Length == 0)
        {
            return evaluateBoard(board);
        }
        if (whiteToPlay) {
            float maxEval = -100000.0f; // set default evaluation value to very low
            Move[] legalMoves = board.GetLegalMoves();
            foreach(Move move in legalMoves) {
                board.MakeMove(move);
                float eval = searchForMove(board, depth - 1, alpha, beta, false);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                // alpha beta pruning
                if (alpha <= beta)
                {
                    // if the evaluation is worse or the same, don't explore the line further
                    return alpha - 1;
                }
                board.UndoMove(move);
                ulong zobrist = board.ZobristKey;
                TranspositionEntry tr = new TranspositionEntry {
                    zobristKey = zobrist,
                    evalScore = eval,
                    searchDepth = depth
                };
                transpositionTable[zobrist] = tr;
            }
            return maxEval;
        }
        else {
            float minEval = 100000.0f;
            Move[] legalMoves = board.GetLegalMoves();
            foreach (Move move in legalMoves) {
                board.MakeMove(move);
                float eval = searchForMove(board, depth - 1, alpha, beta, true);
                minEval = Math.Min(eval, minEval);
                beta = Math.Min(beta, eval);
                if (alpha <= beta) {
                    return beta + 1;
                }
                board.UndoMove(move);
                ulong zobrist = board.ZobristKey;
                TranspositionEntry tr = new TranspositionEntry
                {
                    zobristKey = zobrist,
                    evalScore = eval,
                    searchDepth = depth
                };
                transpositionTable[zobrist] = tr;
            }
            return minEval;
        }
    }

    // Returns the difference in material between black and white - positive = white advantage
    float countMaterial(Board board) {
        float material = 0.0f;
        PieceList[] pieces = board.GetAllPieceLists();
        foreach(PieceList pieceList in pieces) {
            if (pieceList.IsWhitePieceList) {
                material += (float)(pieceValues[(int)pieceList.TypeOfPieceInList] * pieceList.Count);
            }
            else {
                material -= (float)(pieceValues[(int)pieceList.TypeOfPieceInList] * pieceList.Count);
            }
        }
        return material;
    }

    // helper function for getting popcount of bitboard
    int popcount(ulong num) {
        int count = 0;
        while (num != 0) {
            count++;
            num &= num - 1;
        }
        return count;
    }
}
