/**
 * SimFish v 0.0.0
 * First attempt at making a working chess bot.
 * Strengths:
 *  - Always finds mate in 1
 *  - Doesn't hang the piece it is moving in 1 move
 *  - Can beat the defualt EvilBot (random move generator)
 * Weaknesses:
 *  - Doesn't know how to play openings
 *  - Frequently stalemates easily winning endgames
 *  - Can't calculate complex lines, sacrifices, checks
 *  - Often hangs pieces several moves after moving them
 *  - Promotes pawns to random pieces (knights, bishops instead of queen)
 * 
 * Estimated Rating - <200 (Beginner)
 * 
 * Next Steps:
 *  - Allow the bot to explore more complex lines - look 2-3 moves into the future
 *  - Seek checks and attacks
 *  - Move attacked pieces out of danger
 */

using ChessChallenge.API;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

public class MyBot : IChessBot 
{
    // private const int maxSearchDepth = 4;
    // private int searchDepth = 0;
    // Initialize piece values 
    private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Random rnd = new Random();
        
        // Store all legal moves in an array
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[rnd.Next(moves.Length)];
        int bestCapture = 0;
        bool captureAvailable = false;
        foreach(Move move in moves) {
            // Always play checkmate in 1 if available
            if (isCheckmate(board, move)) {
                bestMove = move;
                break;
            }

            // If no captures are available, move to a random safe square
            if (!captureAvailable) {
                if (checkSafety(move, board)) {
                    bestMove = move;
                }
            }
            // Make neutral/positive trades if available
            if (move.IsCapture)
            {
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = checkGoodTrade(board, move);
                if (capturedPieceValue > bestCapture)
                {
                    captureAvailable = true;
                    bestMove = move;
                    bestCapture = capturedPieceValue;
                }
            }
            
        }

        // Check to see if a piece will be safe after a trade
        int checkGoodTrade(Board board, Move move){
            int tradeValue = 0;
            Square nextSquare = move.TargetSquare;
            PieceType ourPiece = move.MovePieceType;
            PieceType theirPiece = move.CapturePieceType;
            tradeValue += pieceValues[(int)theirPiece];
            // See if the piece we are capturing will lead to our piece being lost
            if (board.SquareIsAttackedByOpponent(nextSquare)) {
                tradeValue -= pieceValues[(int)ourPiece];
            }
            return tradeValue;
        }

        bool isCheckmate(Board board, Move move) {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }

        // Simple function to check if the square being moved to is attacked by the opponent
        bool checkSafety(Move move, Board board) {
            return !board.SquareIsAttackedByOpponent(move.TargetSquare);
        }
        return bestMove;
    }
}
