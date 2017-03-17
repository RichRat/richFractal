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
using System.Windows.Threading;

namespace richMandel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MandelCanvas m_mandelCanavas = new MandelCanvas();
        bool m_updatingText = false;

        public MainWindow()
        {
            InitializeComponent();
            Grid.SetRow(m_mandelCanavas, 1);
            //m_mandelCanavas.Width = 1400 / 2;
            //m_mandelCanavas.Height = 1000 / 2;
            m_mandelCanavas.SuperSample = 1;
            m_mandelCanavas.ViewChanged += onMandelCanvasViewChanged;
            w_myGrid.Children.Add(m_mandelCanavas);
            Dispatcher.BeginInvoke(new Action(() => m_mandelCanavas.render()), DispatcherPriority.ContextIdle, null);
            onMandelCanvasViewChanged(m_mandelCanavas.View, m_mandelCanavas.Position);
        }

        void onMandelCanvasViewChanged(Rect view, Point pos)
        {
            m_updatingText = true;
            w_viewWidth.Text = view.Width.ToString();
            w_viewXPosition.Text = pos.X.ToString();
            w_viewYPosition.Text = pos.Y.ToString();
            m_updatingText = false;
        }

        private void onGoButtonClick(object sender, RoutedEventArgs e)
        {
            m_mandelCanavas.render();
        }

        private void onRenderTxtChanged(object sender, TextChangedEventArgs e)
        {
            int depth = 0;
            if (!m_updatingText && int.TryParse(w_renderDepth.Text, out depth))
                m_mandelCanavas.Renderer.Depth = depth;
        }

        private void onXPositionTxtChanged(object sender, TextChangedEventArgs e)
        {
            double x = 0;
            if (!m_updatingText && double.TryParse(w_viewXPosition.Text, out x))
                m_mandelCanavas.PositionX = x;
        }

        private void onYPositionTxtChanged(object sender, TextChangedEventArgs e)
        {
            double y = 0;
            if (!m_updatingText && double.TryParse(w_viewXPosition.Text, out y))
                m_mandelCanavas.PositionY = y;
        }

        private void onViewWidthTxtChanged(object sender, TextChangedEventArgs e)
        {
            double w = 0;
            if (!m_updatingText && double.TryParse(w_viewWidth.Text, out w))
                m_mandelCanavas.ViewWidth = w;
        }
    }
}
