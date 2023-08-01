/**
 * SimFish v 0.0.4
 * Added more optimization to search function with Zobrist hashing/transposition table lookup
 * Can finally sometimes win easily winning endgames!
 * Improvements:
 *  - Added bonus tables for each piece - the bot will now move its pieces during opening/middlegame to favorable positions
 *  - No longer aimlessly slides its king back and forth
 *  - Massive improvement to evaluation function makes this the strongest bot by far
 * Weaknesses:
 *  - Still has a hard time converting winning endgames
 *  - Doesn't seem to know that castling exists
 *  - Search function is still quite slow - needs further optimization
 * 
 * Estimated Rating - ~700 (Beginner/Intermediate)
 * 
 * Next Steps:
 *  - Finish search function by evaluating "best first" - evaluate promising lines sooner
 *  - Compress bonus boards to fit back within legal threshold for the challenge
 *  - Explore ways to make endgame stronger
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
    private int[] pieceValues = { 0, 100, 320, 330, 500, 900, 10000 };
    //private Dictionary<ulong, TranspositionEntry> transpositionTable = new Dictionary<ulong, TranspositionEntry>();


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
        float eval = 0.0f;
        PieceList[] pieces = board.GetAllPieceLists();
        Func<bool, int> isWhite = white => white ? 1 : -1;
        foreach (PieceList pieceList in pieces) {
            eval += (float)(pieceValues[(int)pieceList.TypeOfPieceInList] * pieceList.Count * isWhite(pieceList.IsWhitePieceList));
            foreach (Piece piece in pieceList) {
                eval += getBonus(piece, board.IsWhiteToMove);
            }
        }
        return eval;
    }

    float getBonus(Piece piece, bool isWhiteToMove) {
        int pieceType = (int)piece.PieceType;
        int squareIndex = piece.Square.Index;
        return (pieceType == 1 ? pawn_bonuses[piece.Square.Index] :
            pieceType == 2 ? knight_bonuses[squareIndex] :
            pieceType == 3 ? bishop_bonuses[squareIndex] :
            pieceType == 4 ? rook_bonuses[squareIndex] :
            pieceType == 5 ? queen_bonuses[squareIndex] :
            king_bonuses[piece.Square.Index]);
    }

    // Minimax searching algorithm, returns best evaluation based on evaluateBoard function
    float searchForMove(Board board, int depth, float alpha, float beta, bool whiteToPlay) {
        /*
        ulong zobrist_init = board.ZobristKey;
        if (transpositionTable.TryGetValue(zobrist_init, out TranspositionEntry entry)) {
            if (entry.searchDepth >= depth) {
                return entry.evalScore - 1.0f;
            }
        }
        */
        if (depth == 0 || board.GetLegalMoves().Length == 0) {
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
                    break;
                }
                board.UndoMove(move);
                /*
                ulong zobrist = board.ZobristKey;
                TranspositionEntry tr = new TranspositionEntry {
                    zobristKey = zobrist,
                    evalScore = eval,
                    searchDepth = depth
                };
                transpositionTable[zobrist] = tr;
                */
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
                    break;
                }
                board.UndoMove(move);
                /*
                ulong zobrist = board.ZobristKey;
                TranspositionEntry tr = new TranspositionEntry
                {
                    zobristKey = zobrist,
                    evalScore = eval,
                    searchDepth = depth
                };
                transpositionTable[zobrist] = tr;
                */
            }
            return minEval;
        }
    }

    /*
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
    */

    // helper function for getting popcount of bitboard
    int popcount(ulong num) {
        int count = 0;
        while (num != 0) {
            count++;
            num &= num - 1;
        }
        return count;
    }

    // piece bonus tables
    // encourage pawns to favourable positions
    short[] pawn_bonuses = {
        0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,
        5,  5, 10, 25, 25, 10,  5,  5,
        0,  0,  0, 20, 20,  0,  0,  0,
        5, -5,-10,  0,  0,-10, -5,  5,
        5, 10, 10,-20,-20, 10, 10,  5,
        0,  0,  0,  0,  0,  0,  0,  0
    };

    short[] knight_bonuses = {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,
        -30,  0, 15, 20, 20, 15,  0,-30,
        -30,  5, 10, 15, 15, 10,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50
    };

    short[] bishop_bonuses = {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20
    };

    short[] rook_bonuses = {
          0,  0,  0,  0,  0,  0,  0,  0,
          5, 10, 10, 10, 10, 10, 10,  5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
         -5,  0,  0,  0,  0,  0,  0, -5,
          0,  0,  0,  5,  5,  0,  0,  0
    };

    short[] queen_bonuses = {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    };

    short[] king_bonuses = {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,
         20, 30, 10,  0,  0, 10, 30, 20
    };
}
