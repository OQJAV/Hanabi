using System;
using System.Collections.Generic;
using System.Linq;

namespace Khanabi
{
    enum Color { Red, Green, Yellow, Blue, White }
    enum CardKnowledge { KnowsNothing, KnowsColor, KnowsRank, KnowsAll }

    class Card
    {
        public Color color { get; }
        public int rank { get; }
        public CardInfo cardInfo;

        public Card(Color color, int rank)
        {
            this.color = color;
            this.rank = rank;
            cardInfo = new CardInfo();
        }
    }

    class CardInfo
    {
        public HashSet<Color> possibleColors;
        public HashSet<int> possibleRanks;

        public CardInfo()
        {
            possibleColors = new HashSet<Color>(Enum.GetValues(typeof(Color)).Cast<Color>());
            possibleRanks = new HashSet<int>(Enumerable.Range(1, 5));
        }
    }

    class Deck
    {
        private Queue<Card> cards;

        public Deck()
        {
            cards = new Queue<Card>();
        }

        public void AddCard(Card card)
        {
            cards.Enqueue(card);
        }

        public Card GetCard()
        {
            return cards.Dequeue() ?? null;
        }

        public bool IsEmpty()
        {
            return cards.Count() == 0;
        }
    }

    class Board
    {
        List<Stack<Card>> board;
        private int columns;
        private int eachColumnLimit;

        public Board()
        {
            columns = 5;
            eachColumnLimit = 5;
            board = new List<Stack<Card>>();
            for (int i = 0; i < columns; i++)
            {
                board.Add(new Stack<Card>());
                board[i].Push(new Card((Color)i, 0));
            }
        }

        public bool isFull()
        {
            int totalOfCards = board.Aggregate(0, (accumulator, stack) => accumulator + stack.Count);
            return totalOfCards == columns * (eachColumnLimit + 1);
        }

        public bool isEmpty()
        {
            return IsAllToppingsEquals(0);
        }

        // If it's able to put card on a board then returns true
        // and puts card.
        // Otherwise returns false
        public bool PutCard(Card card)
        {
            foreach (Stack<Card> column in board)
            {
                Card topCard = column.Peek();
                if (topCard.color == card.color)
                {
                    if (topCard.rank == card.rank - 1)
                    {
                        column.Push(card);
                        return true;
                    }
                }
            }
            return false;
        }

        public bool IsAllToppingsEquals(int rank)
        {
            foreach (Stack<Card> column in board)
            {
                if (column.Peek().rank != rank)
                    return false;
            }
            return true;
        }

        public IEnumerable<Color> GetColorsWithTopRank(int rank)
        {
            HashSet<Color> colors = new HashSet<Color>();
            foreach (Stack<Card> column in board)
            {
                if (column.Peek().rank == rank)
                    colors.Add(column.Peek().color);
            }
            return colors;
        }
    }

    class Player
    {
        public int ID;
        public List<Card> hand { get; }
        public List<CardKnowledge> cardKnowledge { get; }

        public Player(int ID)
        {
            this.ID = ID;
            hand = new List<Card>();
            cardKnowledge = new List<CardKnowledge>();
        }

        public bool TakeCard(Deck deck)
        {
            Card newCard = deck.GetCard();
            if (newCard == null)
                return false;
            hand[hand.Count() - 1] = newCard;
            cardKnowledge[hand.Count() - 1] = CardKnowledge.KnowsNothing;
            return true;
        }

        public bool PlayCard(int position, Board board, Deck deck, out bool isRisked)
        {
            isRisked = !canGuessACard(position, board);
            bool isSuccessfulTurn = board.PutCard(hand[position]);
            DropCard(position);
            return isSuccessfulTurn;
        }

        // if player can guess what card it owns
        // using additional info
        private bool canGuessACard(int position, Board board)
        {
            Card playingCard = hand[position];
            CardKnowledge knowledge = cardKnowledge[position];
            if (knowledge == CardKnowledge.KnowsAll)
                return true;
            bool canGuess = false;
            // if all toppings have rank - 1 then player can put its card whatever color it is
            canGuess = canGuess || (knowledge == CardKnowledge.KnowsRank) && board.IsAllToppingsEquals(playingCard.rank - 1);
            canGuess = canGuess || (knowledge == CardKnowledge.KnowsRank) && playingCard.rank == 1 && board.isEmpty();
            canGuess = canGuess || (knowledge != CardKnowledge.KnowsRank) && (playingCard.cardInfo.possibleRanks.Count == 1);
            canGuess = canGuess || (knowledge != CardKnowledge.KnowsColor) && (playingCard.cardInfo.possibleColors.Count == 1);
            canGuess = canGuess || (knowledge == CardKnowledge.KnowsRank) && IsSubset(playingCard, board);
            return canGuess;
        }

        private bool IsSubset(Card playingCard, Board board)
        {
            var boardColors = board.GetColorsWithTopRank(playingCard.rank - 1);
            return !playingCard.cardInfo.possibleColors.Except(boardColors).Any();
        }

        public bool TellColor(Color color, Player oponent, IEnumerable<int> cardPositions)
        {
            for (int position = 0; position < hand.Count(); position++)
            {
                if (!cardPositions.Contains(position) && oponent.hand[position].color == color)
                    return false;
                if (cardPositions.Contains(position) && oponent.hand[position].color != color)
                    return false;

                if (!cardPositions.Contains(position))
                    oponent.hand[position].cardInfo.possibleColors.Remove(color);
            }
            foreach (int position in cardPositions)
            {
                oponent.RememberCardColor(position);
            }
            return true;
        }

        public bool TellRank(int rank, Player oponent, IEnumerable<int> cardPositions)
        {
            for (int position = 0; position < hand.Count(); position++)
            {
                if (!cardPositions.Contains(position) && oponent.hand[position].rank == rank)
                    return false;
                if (cardPositions.Contains(position) && oponent.hand[position].rank != rank)
                    return false;

                if (!cardPositions.Contains(position))
                    oponent.hand[position].cardInfo.possibleRanks.Remove(rank);
            }
            foreach (int position in cardPositions)
            {
                oponent.RememberCardRank(position);
            }
            return true;
        }

        private void RememberCardColor(int position)
        {
            if (cardKnowledge[position] == CardKnowledge.KnowsNothing)
                cardKnowledge[position] = CardKnowledge.KnowsColor;
            else if (cardKnowledge[position] == CardKnowledge.KnowsRank)
                cardKnowledge[position] = CardKnowledge.KnowsAll;
        }

        private void RememberCardRank(int position)
        {
            if (cardKnowledge[position] == CardKnowledge.KnowsNothing)
                cardKnowledge[position] = CardKnowledge.KnowsRank;
            else if (cardKnowledge[position] == CardKnowledge.KnowsColor)
                cardKnowledge[position] = CardKnowledge.KnowsAll;
        }

        public Card DropCard(int position)
        {
            Card dropped = hand[position];
            RemoveCardAtPosition(position);
            return dropped;
        }

        private void RemoveCardAtPosition(int position)
        {
            for (int i = position; i < hand.Count() - 1; i++)
            {
                hand[i] = hand[i + 1];
                cardKnowledge[i] = cardKnowledge[i + 1];
            }
        }
    }

    class Game
    {
        public Player currentPlayer, nextPlayer;
        int playerMaxCardCount;
        Deck deck;
        Board board;

        public int turns = -1;
        public int riskedTurns = 0;
        public int correctlyPlayedCards = 0;

        public bool isFinished;

        public Game(Player playerOne, Player playerTwo, int playerMaxCardCount)
        {
            currentPlayer = playerOne;
            nextPlayer = playerTwo;
            this.playerMaxCardCount = playerMaxCardCount;
            deck = new Deck();
            board = new Board();
        }

        private void SetPlayerHand(Player player)
        {
            player.hand.Clear();
            player.cardKnowledge.Clear();
            for (int i = 0; i < playerMaxCardCount; i++)
            {
                player.hand.Add(deck.GetCard());
                player.cardKnowledge.Add(CardKnowledge.KnowsNothing);
            }
        }

        private void SetDeck(IEnumerable<string> cards)
        {
            deck = new Deck();
            foreach (string cardID in cards)
            {
                Color color;
                switch (cardID[0])
                {
                    case 'B':
                        color = Color.Blue; break;
                    case 'G':
                        color = Color.Green; break;
                    case 'R':
                        color = Color.Red; break;
                    case 'W':
                        color = Color.White; break;
                    case 'Y':
                        color = Color.Yellow; break;
                    default:
                        color = Color.Blue; break;
                }
                deck.AddCard(new Card(color, int.Parse(cardID[1].ToString())));
            }
        }

        private void NewGame(IEnumerable<string> cards)
        {
            turns = -1;
            correctlyPlayedCards = 0;
            riskedTurns = 0;
            isFinished = false;

            board = new Board();
            SetDeck(cards);
            SetPlayerHand(currentPlayer);
            SetPlayerHand(nextPlayer);
        }

        public void StartGameLoop()
        {
            string command;
            while ((command = Console.ReadLine()) != null)
            {
                bool executedSuccessfully = ExecuteCommand(command);
                if (isFinished)
                    continue;
                turns++;
                if (!executedSuccessfully || deck.IsEmpty() || board.isFull())
                {
                    GameOver();
                }
                else
                {
                    SwitchPlayers();
                }
            }
        }

        private void GameOver()
        {
            isFinished = true;
            Console.WriteLine("Turn: {0}, cards: {1}, with risk: {2}", turns, correctlyPlayedCards, riskedTurns);
        }

        private void SwitchPlayers()
        {
            if (turns <= 0)
                return;
            Player tmpPlayer = currentPlayer;
            currentPlayer = nextPlayer;
            nextPlayer = tmpPlayer;
        }

        private bool ExecuteCommand(string command)
        {
            string[] splittedCommand = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            switch (splittedCommand[0])
            {
                case "Play":
                    bool isRisked = false;
                    bool successfullTurn = currentPlayer.PlayCard(int.Parse(splittedCommand[2]), board, deck, out isRisked);
                    if (successfullTurn)
                        correctlyPlayedCards++;
                    if (successfullTurn && isRisked)
                        riskedTurns++;
                    return successfullTurn && currentPlayer.TakeCard(deck);
                case "Drop":
                    currentPlayer.DropCard(int.Parse(splittedCommand[2]));
                    return currentPlayer.TakeCard(deck);
                case "Tell":
                    var positions = splittedCommand
                        .Skip(5)
                        .Select(x => int.Parse(x));
                    if (splittedCommand[1] == "color")
                        return currentPlayer.TellColor(
                            (Color)Enum.Parse(typeof(Color), splittedCommand[2])
                            , nextPlayer
                            , positions);
                    else if (splittedCommand[1] == "rank")
                        return currentPlayer.TellRank(int.Parse(splittedCommand[2]), nextPlayer, positions);
                    break;
                case "Start":
                    NewGame(splittedCommand.Skip(5));
                    return true;
                default:
                    break;
            }
            return false;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Player player1 = new Player(1);
            Player player2 = new Player(2);

            Game game = new Game(player1, player2, 5);
            game.StartGameLoop();
        }
    }
}