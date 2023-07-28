/**
 * SimFish v 0.0.1
 * After doing some research, made some improvements
 * Strengths:
 *  - Can now look a few moves into the future
 *  - Being able to look ahead means it makes good trades
 *  - Will now move its pieces out of danger
 *  - Can find some cool ideas like forks and skewers
 * Weaknesses:
 *  - Still no opening/endgame awareness
 *  - Often has a hard time finding good moves in winning endgames, leads to draws/losses
 *  - Currently implements a slow minimax function for searching for moves - sometimes loses by timeout
 *  - Has a strange bug where it won't evaluate during the first few moves some games
 *  - Very rudimentary evaluation - counts material, that's it
 * 
 * Estimated Rating - <200 (Beginner)
 * 
 * Next Steps:
 *  - Optimize minimax search function - alpha/beta pruning
 *  - Fix bug with opening
 *  - Improve board evaluation - seek checks, move king to safety, promote pawns
 */

using ChessChallenge.API;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

public class MyBot : IChessBot 
{
    // private const int maxSearchDepth = 4;
    // private int searchDepth = 0;
    // Initialize piece values 
    private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    const int MAX_SEARCH_DEPTH = 3;

    public Move Think(Board board, Timer timer)
    {
        Random rnd = new Random();

        // Store all legal moves in an array
        Move[] moves = board.GetLegalMoves();
        // Evaluate each position in moves
        Move bestMove = moves[rnd.Next(moves.Length)];
        float bestEval = evaluateBoard(board);
        foreach (Move move in moves) {
            board.MakeMove(move);
            float eval = searchForMove(board, MAX_SEARCH_DEPTH, board.IsWhiteToMove);
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

    // Returns approximate evaluation of the board for the current moving player
    float evaluateBoard(Board board) {
        if (board.IsInCheckmate()) {
            return 99999.9f;
        }
        return countMaterial(board);
    }

    // Minimax searching algorithm, returns best evaluation based on evaluateBoard function
    float searchForMove(Board board, int depth, bool whiteToPlay) {
        if (depth == 0 || board.GetLegalMoves().Length == 0) {
            return evaluateBoard(board);
        }
        if (whiteToPlay) {
            float maxEval = -100000.0f; // set default evaluation value to very low
            Move[] legalMoves = board.GetLegalMoves();
            foreach(Move move in legalMoves) {
                board.MakeMove(move);
                float eval = searchForMove(board, depth - 1, false);
                maxEval = Math.Max(maxEval, eval);
                board.UndoMove(move);
            }
            return maxEval;
        }
        else {
            float minEval = 100000.0f;
            Move[] legalMoves = board.GetLegalMoves();
            foreach (Move move in legalMoves) {
                board.MakeMove(move);
                float eval = searchForMove(board, depth - 1, true);
                minEval = Math.Min(eval, minEval);
                board.UndoMove(move);
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
}
