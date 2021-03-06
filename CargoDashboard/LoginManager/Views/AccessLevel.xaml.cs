﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LoginManager
{
    /// <summary>
    /// Interaction logic for AccessLevel.xaml
    /// </summary>
    public partial class AccessLevel : Page
    {
        public AccessLevel()
        {
            InitializeComponent();
            CultureResources.registerDataProvider(this);
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = Window.GetWindow(this).DataContext;
        }
    }
}
