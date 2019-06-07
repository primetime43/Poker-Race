using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace Poker_Race
{
    class PokerHand : GameMainWindow
    {
        private static int awardedPoints;
        private static List<int> values;
        private static List<string> suits;
        private static bool isFlush;
        public static int determinePokerHand(int score, Label handlbl)
        {
            values = new List<int>();
            suits = new List<string>();
            isFlush = true;
            string checkFlush = cards[0].Split('_').Last();//get the suit of the first card and use to check if all cards have the same
            for (int i = 0; i < cards.Length; i++)
            {
                string cardSuit = cards[i].Split('_').Last();
                Dictionary<string, int> dict = cardMap[cardSuit];//returns the dictionary for the correct suit
                suits.Add(cardSuit);
                values.Add(dict[cards[i]]);

                if (cardSuit != checkFlush)
                    isFlush = false;

            }
            values.Sort();

            if (isRoyalFlush())
            {
                awardedPoints = 37500;
                handlbl.Content = "Royal Flush";
            }
            else if (isStraightFlush())
            {
                awardedPoints = 28500;
                handlbl.Content = "Straight Flush";
            }
            else if (isFourOfAKind())
            {
                awardedPoints = 22500;
                handlbl.Content = "Four of a kind";
            }
            else if (isFullHouse())
            {
                awardedPoints = 18700;
                handlbl.Content = "Full House";
            }
            else if (isFlush)
            {
                awardedPoints = 15000;
                handlbl.Content = "Flush";
            }
            else if (isStraight())
            {
                awardedPoints = 12000;
                handlbl.Content = "Straight";
            }
            else if (isThreeOfAKind())
            {
                awardedPoints = 9000;
                handlbl.Content = "Three of a kind";
            }
            else if (isTwoPair())
            {
                awardedPoints = 6700;
                handlbl.Content = "Two Pair";
            }
            else if (isPair())
            {
                awardedPoints = 4500;
                handlbl.Content = "Pair";
            }
            else//high card
            {
                awardedPoints = values[4];
                handlbl.Content = "High Card";
            }
            return score += awardedPoints;
        }


        public static bool isPair()
        {
            var result = values.GroupBy(i => i).Select(i => new { Value = i.Key, Count = i.Count() });
            foreach (var item in result)
            {
                if (item.Count == 2)
                    return true;
            }
            return false;
        }

        public static bool isTwoPair()
        {
            int numOfPairs = 0;
            var result = values.GroupBy(i => i).Select(i => new { Value = i.Key, Count = i.Count() });
            foreach (var item in result)
            {
                if (item.Count == 2)
                    numOfPairs++;
            }
            return (numOfPairs == 2) ? true : false;
        }

        public static bool isThreeOfAKind()
        {
            var result = values.GroupBy(i => i).Select(i => new { Value = i.Key, Count = i.Count() });
            foreach (var item in result)
            {
                if (item.Count == 3)
                    return true;
            }
            return false;
        }

        public static bool isStraight()
        {
            //consider ace (14) as a 1 or 14 below in if
            if (values[0] == 2 && values[1] == 3 && values[2] == 4 && values[3] == 5 && values[4] == 14)
            {
                values[4] = 1;
                values.Sort();
                return values.Zip(values.Skip(1), (a, b) => (a + 1) == b).All(x => x);
            }
            else if(values[0] == 10 && values[1] == 11 && values[2] == 12 && values[3] == 13 && values[4] == 14)
                return values.Zip(values.Skip(1), (a, b) => (a + 1) == b).All(x => x);

            return values.Zip(values.Skip(1), (a, b) => (a + 1) == b).All(x => x);
        }

        public static bool isFullHouse()
        {
            return isThreeOfAKind() && isPair();
        }

        public static bool isFourOfAKind()
        {
            var result = values.GroupBy(i => i).Select(i => new { Suit = i.Key, Count = i.Count() });
            foreach (var item in result)
            {
                if (item.Count == 4)
                    return true;
            }
            return false;
        }

        public static bool isStraightFlush()
        {
            return isStraight() && isFlush;
        }

        public static bool isRoyalFlush()
        {
            return isStraight() && isFlush && values[4] == 14;//14 is ace
        }
    }
}
