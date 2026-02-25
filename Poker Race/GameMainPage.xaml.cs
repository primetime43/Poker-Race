using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Poker_Race
{
    /// <summary>
    /// Interaction logic for GameMainPage.xaml
    /// </summary>
    public partial class GameMainPage : Page
    {
        protected static Random rnd = new Random();
        public static List<string> backUpListOfCards, listOfCards;
        public static string[] cards = new string[5];
        public static Dictionary<string, Dictionary<string, int>> cardMap;
        private static string usersHand;
        private static int round, newScore, overallScore, currentHandValue;
        private const int MaxRounds = 10;
        public static ProgressBar _pBar;
        public GameMainPage()
        {
            InitializeComponent();
            _pBar = userProgressBar;
            textBlockUserHand.Text = "";
            backUpListOfCards = new List<string>
            {
                "2_of_clubs.png", "2_of_diamonds.png", "2_of_hearts.png", "2_of_spades.png",
                "3_of_clubs.png", "3_of_diamonds.png", "3_of_hearts.png", "3_of_spades.png",
                "4_of_clubs.png", "4_of_diamonds.png", "4_of_hearts.png", "4_of_spades.png",
                "5_of_clubs.png", "5_of_diamonds.png", "5_of_hearts.png", "5_of_spades.png",
                "6_of_clubs.png", "6_of_diamonds.png", "6_of_hearts.png", "6_of_spades.png",
                "7_of_clubs.png", "7_of_diamonds.png", "7_of_hearts.png", "7_of_spades.png",
                "8_of_clubs.png", "8_of_diamonds.png", "8_of_hearts.png", "8_of_spades.png",
                "9_of_clubs.png", "9_of_diamonds.png", "9_of_hearts.png", "9_of_spades.png",
                "10_of_clubs.png", "10_of_diamonds.png", "10_of_hearts.png", "10_of_spades.png",
                "jack_of_clubs.png", "jack_of_diamonds.png", "jack_of_hearts.png", "jack_of_spades.png",
                "queen_of_clubs.png", "queen_of_diamonds.png", "queen_of_hearts.png", "queen_of_spades.png",
                "king_of_clubs.png", "king_of_diamonds.png", "king_of_hearts.png", "king_of_spades.png",
                "ace_of_clubs.png", "ace_of_diamonds.png", "ace_of_hearts.png", "ace_of_spades.png"
            };
            round = 0;
            overallScore = 0;
            listOfCards = new List<string>(backUpListOfCards);
            createCardDictionary();
            createNewHand();
        }
        private BitmapImage generateRandomCard()
        {
            int num = rnd.Next(listOfCards.Count);
            uriSource = new Uri("Poker Cards/" + listOfCards[num], UriKind.Relative);
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

        private async Task addCardToHand(string selectedCard)
        {
            uriSource = new Uri("Poker Cards/" + selectedCard, UriKind.Relative);
            if (handCard1.Source == null)
            {
                handCard1.Source = new BitmapImage(uriSource);
                cards[0] = selectedCard.Substring(0, selectedCard.IndexOf('.'));
            }
            else if (handCard2.Source == null)
            {
                handCard2.Source = new BitmapImage(uriSource);
                cards[1] = selectedCard.Substring(0, selectedCard.IndexOf('.'));
            }
            else if (handCard3.Source == null)
            {
                handCard3.Source = new BitmapImage(uriSource);
                cards[2] = selectedCard.Substring(0, selectedCard.IndexOf('.'));
            }
            else if (handCard4.Source == null)
            {
                handCard4.Source = new BitmapImage(uriSource);
                cards[3] = selectedCard.Substring(0, selectedCard.IndexOf('.'));
            }
            else if (handCard5.Source == null)
            {
                handCard5.Source = new BitmapImage(uriSource);
                cards[4] = selectedCard.Substring(0, selectedCard.IndexOf('.'));
                newScore = PokerHand.determinePokerHand(int.Parse(scoreLabel.Content.ToString()), ref usersHand, ref currentHandValue);
                textBlockUserHand.Text = usersHand;
                overallScore = (int)(newScore + timerBar.Value);
                scoreLabel.Content = overallScore;

                // Dynamically rescale track when score exceeds 75% of current max
                if (overallScore >= userProgressBar.Maximum * 0.75)
                {
                    userProgressBar.Maximum = Math.Ceiling(overallScore / 10000.0) * 10000 + 20000;
                    updateTrackLabels();
                }

                moveHorse();

                await Task.Run(() => stopWorker());

                if (round >= MaxRounds)
                {
                    new DatabaseTools().connectToDatabase(HomePage.username, overallScore.ToString());
                    MessageBox.Show("Game Over! Final score: " + overallScore + " points!");
                    this.NavigationService.Navigate(new HomePage());
                }
                else
                    startNewRound();
            }
        }

        private void updateTrackLabels()
        {
            double max = userProgressBar.Maximum;
            double step = max / 8.0;
            scoreMarker1.Text = ((int)step).ToString();
            scoreMarker2.Text = ((int)(step * 2)).ToString();
            scoreMarker3.Text = ((int)(step * 3)).ToString();
            scoreMarker4.Text = ((int)(step * 4)).ToString();
            scoreMarker5.Text = ((int)(step * 5)).ToString();
            scoreMarker6.Text = ((int)(step * 6)).ToString();
            scoreMarker7.Text = ((int)(step * 7)).ToString();
            scoreMarker8.Text = ((int)max).ToString();
        }

        private static BackgroundWorker horseWorker;
        private void moveHorse()
        {
            horseWorker = new BackgroundWorker();
            if (!horseWorker.IsBusy)
            {
                horseWorker.WorkerSupportsCancellation = true;
                horseWorker.WorkerReportsProgress = true;
                horseWorker.DoWork += worker_MoveHorse;
                horseWorker.RunWorkerAsync();
            }
            else
                MessageBox.Show("Can't run the worker twice!");
        }

        void worker_MoveHorse(object sender, DoWorkEventArgs e)
        {
            for (int i = 0; i < currentHandValue; i++)
            {
                Dispatcher.Invoke(() =>
                {
                    userProgressBar.Value += 1;
                });
            }
        }

        private async void startNewRound()
        {
            image1.IsEnabled = false;
            image2.IsEnabled = false;
            image3.IsEnabled = false;
            image4.IsEnabled = false;
            image5.IsEnabled = false;
            listOfCards.Clear();
            listOfCards = new List<string>(backUpListOfCards);
            await Task.Delay(1500);
            textBlockUserHand.Text = "";
            createNewHand();
        }

        private void stopWorker()
        {
            worker.CancelAsync();
            while (worker.IsBusy) { }
            return;
        }

        private static BackgroundWorker worker;
        private void startTimerBar()
        {
            worker = new BackgroundWorker();
            if (!worker.IsBusy)
            {
                timerBar.Value = 30;
                worker.WorkerSupportsCancellation = true;
                worker.WorkerReportsProgress = true;
                worker.DoWork += worker_DoWork;
                worker.ProgressChanged += worker_ProgressChanged;
                worker.RunWorkerAsync();
            }
            else
                MessageBox.Show("Can't run the worker twice!");
        }

        private static int counter;
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            counter = 30;
            for (; counter >= 0; counter--)
            {
                if (!worker.CancellationPending)
                {
                    (sender as BackgroundWorker).ReportProgress(counter);
                    Thread.Sleep(1000);
                }
            }
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            timerBar.Value = e.ProgressPercentage;
            if (timerBar.Minimum == e.ProgressPercentage)
            {
                worker.CancelAsync();
                AutoClosingMessageBox.Show("You've run out of time!", "Out of time", 3000);
                startNewRound();
            }
        }

        protected void createNewHand()
        {
            startTimerBar();
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

            image1.Source = generateRandomCard();
            image2.Source = generateRandomCard();
            image3.Source = generateRandomCard();
            image4.Source = generateRandomCard();
            image5.Source = generateRandomCard();
            textBlockRound.Text = "Round " + ++round;
        }

        private void createCardDictionary()
        {
            cardMap = new Dictionary<string, Dictionary<string, int>>();
            string card, cardSuit;
            int cardValue;
            for (int i = 0; i < listOfCards.Count; i++)
            {
                card = listOfCards[i].Substring(0, listOfCards[i].IndexOf('.'));
                cardSuit = card.Split('_').Last();

                if (!int.TryParse(card.Split('_').First(), out cardValue))
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

                if (!cardMap.ContainsKey(cardSuit))
                {
                    cardMap.Add(cardSuit, new Dictionary<string, int>());
                    cardMap[cardSuit].Add(card, cardValue);
                }
                else
                    cardMap[cardSuit].Add(card, cardValue);
            }
        }

        public class AutoClosingMessageBox
        {
            Timer _timeoutTimer;
            string _caption;
            AutoClosingMessageBox(string text, string caption, int timeout)
            {
                _caption = caption;
                _timeoutTimer = new Timer(OnTimerElapsed, null, timeout, Timeout.Infinite);
                MessageBox.Show(text, caption);
            }
            public static void Show(string text, string caption, int timeout)
            {
                new AutoClosingMessageBox(text, caption, timeout);
            }
            void OnTimerElapsed(object state)
            {
                IntPtr mbWnd = FindWindow(null, _caption);
                if (mbWnd != IntPtr.Zero)
                    SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _timeoutTimer.Dispose();
            }
            const int WM_CLOSE = 0x0010;
            [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
            static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
            [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
            static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
        }
    }
}
