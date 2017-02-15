using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace richMandel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MandelCanvas m_mandelCanavas = new MandelCanvas();

        public MainWindow()
        {
            InitializeComponent();
            Grid.SetRow(m_mandelCanavas, 1);
            m_mandelCanavas.Width = 1400 / 2;
            m_mandelCanavas.Height = 1000 / 2;
            w_myGrid.Children.Add(m_mandelCanavas);
        }

        private void onGoButtonClick(object sender, RoutedEventArgs e)
        {
            m_mandelCanavas.render();
        }
    }
}
