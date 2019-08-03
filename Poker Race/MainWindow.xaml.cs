using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace Poker_Race
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //frame.NavigationService.Navigate(new GameMainPage());
            frame.NavigationService.Navigate(new HomePage());
        }
    }
}
