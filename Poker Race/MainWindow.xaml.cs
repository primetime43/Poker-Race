using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace Poker_Race
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static Random rnd = new Random();
        private static ArrayList listOfCards;
        public MainWindow()
        {
            InitializeComponent();
            listOfCards = new ArrayList();
            DirectoryInfo d = new DirectoryInfo(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\Poker Cards");//Assuming Test is your Folder
            FileInfo[] Files = d.GetFiles("*.png"); //Getting Text files
            foreach (FileInfo file in Files)
            {
                listOfCards.Add(file.Name);
            }
            image1.Source = randomGenerateCard();
            image2.Source = randomGenerateCard();
            image3.Source = randomGenerateCard();
            image4.Source = randomGenerateCard();
            image5.Source = randomGenerateCard();
            handCard1.Source = null;
            handCard2.Source = null;
            handCard3.Source = null;
            handCard4.Source = null;
            handCard5.Source = null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //var uriSource = new Uri("Poker Cards/queen_of_spades2.png", UriKind.Relative);
            image1.Source = new BitmapImage(uriSource);
            progressBar1.Value = progressBar1.Value + 1;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            image1.Source = null;
        }

        private BitmapImage randomGenerateCard()
        {
            uriSource = new Uri("Poker Cards/" + listOfCards[rnd.Next(listOfCards.Count)].ToString(), UriKind.Relative);//random number generated between 0 - 51
            return new BitmapImage(uriSource);
        }

        private static Uri uriSource;
        //Button onClick actions
        private void Image1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHand(image1.Source.ToString().Split('/').Last());
            image1.Source = randomGenerateCard();
        }

        private void Image2_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHand(image2.Source.ToString().Split('/').Last());
            image2.Source = randomGenerateCard();
        }

        private void Image3_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHand(image3.Source.ToString().Split('/').Last());
            image3.Source = randomGenerateCard();
        }

        private void Image4_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHand(image4.Source.ToString().Split('/').Last());
            image4.Source = randomGenerateCard();
        }

        private void Image5_MouseDown(object sender, MouseButtonEventArgs e)
        {
            addCardToHand(image5.Source.ToString().Split('/').Last());
            image5.Source = randomGenerateCard();
        }

        private void addCardToHand(string selectedCard)
        {
            uriSource = new Uri("Poker Cards/" + selectedCard, UriKind.Relative);
            if (handCard1.Source == null)
            {
                handCard1.Source = new BitmapImage(uriSource);
            }
            else if(handCard2.Source == null)
            {
                handCard2.Source = new BitmapImage(uriSource);
            }
            else if(handCard3.Source == null)
            {
                handCard3.Source = new BitmapImage(uriSource);
            }
            else if(handCard4.Source == null)
            {
                handCard4.Source = new BitmapImage(uriSource);
            }
            else if(handCard5.Source == null)
            {
                handCard5.Source = new BitmapImage(uriSource);
            }
        }
        //
    }
}
