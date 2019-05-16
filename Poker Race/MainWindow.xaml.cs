using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Poker_Race
{
    public partial class MainWindow : Window
    {
        private static Random rnd = new Random();
        private static ArrayList backUpListOfCards, listOfCards;
        public static string[] cards = new string[5];
        public static Dictionary<string, Dictionary<string, int>> cardMap;
        public MainWindow()
        {
            InitializeComponent();
            backUpListOfCards = new ArrayList();
            DirectoryInfo d = new DirectoryInfo(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Poker Cards");
            FileInfo[] Files = d.GetFiles("*.png"); //Getting Text files
            foreach (FileInfo file in Files)
            {
                backUpListOfCards.Add(file.Name);
            }
            listOfCards = new ArrayList(backUpListOfCards);
            createCardDictionary();
            image1.Source = generateRandomCard();
            image2.Source = generateRandomCard();
            image3.Source = generateRandomCard();
            image4.Source = generateRandomCard();
            image5.Source = generateRandomCard();
            handCard1.Source = null;
            handCard2.Source = null;
            handCard3.Source = null;
            handCard4.Source = null;
            handCard5.Source = null;
        }

        private BitmapImage generateRandomCard()
        {
            int num = rnd.Next(listOfCards.Count);
            uriSource = new Uri("Poker Cards/" + listOfCards[num].ToString(), UriKind.Relative);//random number generated between 0 - 51
            listOfCards.RemoveAt(num);
            return new BitmapImage(uriSource);
        }

        private static Uri uriSource;
        //Button onClick actions
        private void Image1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHand(image1.Source.ToString().Split('/').Last());
            image1.Source = generateRandomCard();
        }

        private void Image2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHand(image2.Source.ToString().Split('/').Last());
            image2.Source = generateRandomCard();
        }

        private void Image3_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHand(image3.Source.ToString().Split('/').Last());
            image3.Source = generateRandomCard();
        }

        private void Image4_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHand(image4.Source.ToString().Split('/').Last());
            image4.Source = generateRandomCard();
        }

        private void Image5_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHand(image5.Source.ToString().Split('/').Last());
            image5.Source = generateRandomCard();
        }

        private async void addCardToHand(string selectedCard)
        {
            uriSource = new Uri("Poker Cards/" + selectedCard, UriKind.Relative);
            if (handCard1.Source == null)
            {
                handCard1.Source = new BitmapImage(uriSource);
                cards[0] = selectedCard.Substring(0, selectedCard.IndexOf('.'));
            }
            else if(handCard2.Source == null)
            {
                handCard2.Source = new BitmapImage(uriSource);
                cards[1] = selectedCard.Substring(0, selectedCard.IndexOf('.'));
            }
            else if(handCard3.Source == null)
            {
                handCard3.Source = new BitmapImage(uriSource);
                cards[2] = selectedCard.Substring(0, selectedCard.IndexOf('.'));
            }
            else if(handCard4.Source == null)
            {
                handCard4.Source = new BitmapImage(uriSource);
                cards[3] = selectedCard.Substring(0, selectedCard.IndexOf('.'));
            }
            else if(handCard5.Source == null)
            {
                handCard5.Source = new BitmapImage(uriSource);
                cards[4] = selectedCard.Substring(0, selectedCard.IndexOf('.'));
                //determine hand
                label1_Copy.Content = PokerHand.determinePokerHand(int.Parse(label1_Copy.Content.ToString()), label1_Copy2);
                //disable temp
                image1.IsEnabled = false;
                image2.IsEnabled = false;
                image3.IsEnabled = false;
                image4.IsEnabled = false;
                image5.IsEnabled = false;
                listOfCards.Clear();
                listOfCards = new ArrayList(backUpListOfCards);//reset cards to full deck after round played
                await Task.Delay(1500);//wait 1.5 seconds to show new card hands
                image1.IsEnabled = true;
                image2.IsEnabled = true;
                image3.IsEnabled = true;
                image4.IsEnabled = true;
                image5.IsEnabled = true;
                handCard1.Source = null;
                handCard2.Source = null;
                handCard3.Source = null;
                handCard4.Source = null;
                handCard5.Source = null;

                //create new random hand
                image1.Source = generateRandomCard();
                image2.Source = generateRandomCard();
                image3.Source = generateRandomCard();
                image4.Source = generateRandomCard();
                image5.Source = generateRandomCard();
            }
        }
        //

        private void createCardDictionary()
        {
            //cardSuit, Dictionary (outer)
            //cardName, cardValue (inner)
            cardMap = new Dictionary<string, Dictionary<string, int>>();//key is suit, submap's key is card name
            string card, cardSuit;
            int cardValue;
            for (int i = 0; i < listOfCards.Count; i++)
            {
                card = listOfCards[i].ToString().Substring(0, listOfCards[i].ToString().IndexOf('.'));
                cardSuit = card.Split('_').Last();

                if (!int.TryParse(card.Split('_').First(), out cardValue))//if its a number, it will set cardValue, if not a number returns false but ! makes it true, so enter and check value
                {
                    string value = card.Split('_').First().ToLower();
                    if (value == "jack")
                        cardValue = 11;
                    else if (value == "queen")
                        cardValue = 12;
                    else if (value == "king")
                        cardValue = 13;
                    else if (value == "ace")
                        cardValue = 14;
                }

                if (!cardMap.ContainsKey(cardSuit))//doesn't contain the key yet, so add it
                {
                    cardMap.Add(cardSuit, new Dictionary<string, int>());//adds the suit to be the key and creates a dictionary for it for outer dictionary
                    cardMap[cardSuit].Add(card, cardValue); //added to inner dictionary
                }
                else
                    cardMap[cardSuit].Add(card, cardValue);//added to inner dictionary
            }
        }
    }
}
