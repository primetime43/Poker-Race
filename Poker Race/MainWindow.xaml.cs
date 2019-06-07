using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Poker_Race
{
    public partial class GameMainWindow : Window
    {
        protected static Random rnd = new Random();
        public static ArrayList backUpListOfCards, listOfCards;
        public static string[] cards = new string[5];
        public static Dictionary<string, Dictionary<string, int>> cardMap;
        public GameMainWindow()
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
            createNewHand();
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
            addCardToHandAsync(image1.Source.ToString().Split('/').Last());
            image1.Source = generateRandomCard();
        }

        private void Image2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHandAsync(image2.Source.ToString().Split('/').Last());
            image2.Source = generateRandomCard();
        }

        private void Image3_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHandAsync(image3.Source.ToString().Split('/').Last());
            image3.Source = generateRandomCard();
        }

        private void Image4_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHandAsync(image4.Source.ToString().Split('/').Last());
            image4.Source = generateRandomCard();
        }

        private void Image5_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHandAsync(image5.Source.ToString().Split('/').Last());
            image5.Source = generateRandomCard();
        }

        private async Task addCardToHandAsync(string selectedCard)
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
                label1_Copy.Content = PokerHand.determinePokerHand(int.Parse(label1_Copy.Content.ToString()), label1_Copy2);//determine the user's hand
                await Task.Run(() => stopWorker());//don't continue until method returns
                startNewRound();
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
            listOfCards = new ArrayList(backUpListOfCards);//reset cards to full deck after round played
            await Task.Delay(1500);//wait 1.5 seconds to show new card hands
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
                Console.WriteLine("Value: " + counter);
                if(!worker.CancellationPending)
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
                Console.WriteLine("Out of time");
            }
        }

        protected void createNewHand()
        {
            startTimerBar();
            //check();
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

        class AutoClosingMessageBox
        {
            Timer _timeoutTimer;
            string _caption;
            AutoClosingMessageBox(string text, string caption, int timeout)
            {
                _caption = caption;
                _timeoutTimer = new Timer(OnTimerElapsed,
                    null, timeout, Timeout.Infinite);
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
