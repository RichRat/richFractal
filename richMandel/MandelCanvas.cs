using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;   
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Input;

namespace richMandel
{
    class MandelCanvas : Canvas
    {
        public event Action<double, double, double, double> ViewChanged;

        Rectangle m_selectRect = new Rectangle();
        Image m_image = new Image();
        WriteableBitmap m_bitmap;
        int m_supersample = 1;
        int m_pxWidth;
        int m_pxHeight;

        //TODO set function
        Rect m_view = new Rect(1.5, 0.5, 1.5, 1.0);
        MandelRenderer m_render;
        

        public MandelCanvas()
        {
            this.MouseMove += onMouseMove;
            m_render = new MandelRenderer(new MandelDefinition());
            m_image.MouseDown += onImageMouseDown;
            m_image.MouseUp += onImageMouseUp;
            this.Children.Add(m_image);

            m_selectRect.Fill = new SolidColorBrush(Colors.Transparent);
            m_selectRect.Stroke = new SolidColorBrush(Colors.White);
            m_selectRect.StrokeThickness = 2;
            m_selectRect.Visibility = System.Windows.Visibility.Collapsed;
            //todo
            //m_selectRect.StrokeDashArray = 

            this.Background = new SolidColorBrush(Colors.Black);
        }


        public void render()
        {
            initBitmap();
            m_render.startRender(m_bitmap, m_view);
        }

        private void initBitmap()
        {
            m_pxWidth = (int)this.ActualWidth * m_supersample;
            m_pxHeight = (int)this.ActualHeight * m_supersample;
            m_bitmap = new WriteableBitmap(m_pxWidth, m_pxHeight, 96 * SuperSample, 96 * m_supersample, PixelFormats.Bgr24, null);  
            m_image.Source = m_bitmap;
        }

        Rect ViewSize 
        {
            set 
            {
                if (!value.Equals(m_view))
                {
                    m_view = value;
                    initBitmap();
                }
            }
            get { return m_view; }
        }

        //multiplies the size of the underlying image
        int SuperSample
        {
            get { return m_supersample;  }
            set
            {
                if (value >= 1 && value < 5)
                    m_supersample = value;
            }
        }

        //sets fractal view and starts rendering
        Rect View
        {
            get { return m_view; }
            set 
            {
                if (!m_view.Equals(value))
                {
                    m_view = value;
                    render();
                }
            }
        }

        static double minRectSize;
        Point m_mousePosOld;
        Point m_mousePos;
        private void onImageMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Point mp = e.GetPosition(m_image);
            
            zoomToPoint(imageToFractalPoint(mp), e.ChangedButton == MouseButton.Left ? 2 : 0.5);
        }

        void onMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point curMPos = e.GetPosition(m_image);
            if (Math.Abs((m_mousePosOld - curMPos).Length) > minRectSize)
                ;
        }

        private void onImageMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            m_mousePosOld = e.GetPosition(m_image);
        }

        private Point imageToFractalPoint(Point p)
        {
            double x = m_view.X - m_view.Width * (p.X / m_image.ActualWidth);
            double y = m_view.Y - m_view.Height * (p.Y / m_image.ActualHeight);
            return new Point(x, y);
        }

        private void zoomToPoint(Point fp, double factor)
        {
            m_view.Width /= factor;
            m_view.Height /= factor;
            m_view.X = fp.X + m_view.Width / 2;
            m_view.Y = fp.Y + m_view.Height / 2;
            render();
        }
    }
}
