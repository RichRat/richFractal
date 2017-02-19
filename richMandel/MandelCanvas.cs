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
        private static int MIN_SELECT_SIZE = 5;

        public event Action<double, double, double, double> ViewChanged;

        Rectangle m_dragRect = new Rectangle();
        Line m_dragLine = new Line();
        Image m_image = new Image();
        WriteableBitmap m_bitmap;
        int m_supersample = 1;

        //TODO set function
        Rect m_view = new Rect(2, 1, 3, 2);
        MandelRenderer m_render;
        
        public enum DragModes
        {
            Selection,
            Move
        }
        DragModes m_dragMode = DragModes.Selection;
        public DragModes DragMode { get; set; }

        //mouse info
        Point m_dragFrom;
        Point m_mousePos;
        bool m_dragging = false;

        public MandelCanvas()
        {
            this.MouseMove += onMouseMove;
            m_render = new MandelRenderer(new MandelDefinition());
            this.MouseDown += onMouseDown;
            this.MouseUp += onMouseUp;
            m_image.MouseWheel += onImageMouseWheel;
            

            m_dragRect.Fill = new SolidColorBrush(Colors.Transparent);
            m_dragRect.Stroke = new SolidColorBrush(Colors.White);
            m_dragRect.StrokeThickness = 2;
            m_dragRect.Visibility = Visibility.Collapsed;
            //todo
            var dashes = new DoubleCollection();
            dashes.Add(5);
            dashes.Add(2);
            m_dragRect.StrokeDashArray = dashes;

            m_dragLine.Stroke = new SolidColorBrush(Colors.White);
            m_dragLine.StrokeThickness = 7;
            m_dragLine.StrokeStartLineCap = PenLineCap.Round;
            m_dragLine.StrokeEndLineCap = PenLineCap.Triangle;
            m_dragLine.Visibility = Visibility.Collapsed;

            this.Background = new SolidColorBrush(Colors.Black);

            this.Children.Add(m_image);
            this.Children.Add(m_dragRect);
            this.Children.Add(m_dragLine);
        }

        public void render()
        {
            initBitmap();
            m_render.startRender(m_bitmap, m_view);
        }

        private void initBitmap()
        {
            int pxWidth = (int)this.ActualWidth * m_supersample;
            int pxHeight = (int)this.ActualHeight * m_supersample;
            m_bitmap = new WriteableBitmap(pxWidth, pxHeight, 96 * SuperSample, 96 * m_supersample, PixelFormats.Bgr24, null);  
            m_image.Source = m_bitmap;
        }

        //multiplies the size of the underlying image
        public int SuperSample
        {
            get { return m_supersample; }
            set
            {
                if (value >= 1 && value < 5)
                    m_supersample = value;
            }
        }

        //sets fractal view and starts rendering
        public Rect View
        {
            get { return m_view;  }
            set
            {
                if (!m_view.Equals(value))
                {
                    m_view = value;
                    render();
                }
            }
        }

        private void onMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            m_dragging = true;
            m_dragFrom = e.GetPosition(m_image);
            initDragIndicator();
        }


        void onMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (m_dragging)
            {
                m_mousePos = e.GetPosition(m_image);
                drawDragIndicator();
            }
        }

        private void onMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (m_dragging)
            {
                m_dragging = false;
                m_mousePos = e.GetPosition(m_image);
                applyDragToView();
                render();
            }
        }

        void onImageMouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point mp = e.GetPosition(m_image);
            bool up = e.Delta > 0;
            zoomToPoint(mp , up ? 2 : 0.5);
        }


        private Point imageToFractalPoint(Point p)
        {
            double x = m_view.X - m_view.Width * (p.X / m_image.ActualWidth);
            double y = m_view.Y - m_view.Height * (p.Y / m_image.ActualHeight);
            return new Point(x, y);
        }

        private Size imageToFractaqlSize(Size s)
        {
            Console.WriteLine(s.Width + " " + s.Width);
            s.Width = m_view.Width / m_image.ActualWidth * s.Width;
            s.Height = m_view.Height / m_image.ActualHeight * s.Height;
            Console.WriteLine(s.Width + " " + s.Width);
            return s;
        }

        private Point imageToFractalPoint(double x, double y)
        {
            return imageToFractalPoint(new Point(x, y));
        }

        private void zoomToPoint(Point mp, double factor)
        {
            Point fp = imageToFractalPoint(mp);
            double xmpos = m_image.ActualWidth / mp.X;
            double ympos = m_image.ActualHeight / mp.Y;


            m_view.Width /= factor;
            m_view.Height /= factor;
            m_view.X = fp.X + m_view.Width / xmpos;
            m_view.Y = fp.Y + m_view.Height / ympos;
            render();
        }

        private void initDragIndicator()
        {
            if (m_dragMode == DragModes.Move)
            {
                m_dragLine.Visibility = Visibility.Visible;
                m_dragLine.X1 = m_dragFrom.X;
                m_dragLine.Y1 = m_dragFrom.Y;
            }
            else if (m_dragMode == DragModes.Selection)
            {
                m_dragRect.Visibility = Visibility.Visible;
                m_mousePos = m_dragFrom;
            }

            drawDragIndicator();
        }

        private void drawDragIndicator()
        {
            if (m_dragMode == DragModes.Move)
            {
                m_dragLine.X2 = m_mousePos.X;
                m_dragLine.Y2 = m_mousePos.Y;
            }
            else if (m_dragMode == DragModes.Selection)
            {
                Rect selection = calcSelectionRect();
                Canvas.SetLeft(m_dragRect, selection.X);
                Canvas.SetTop(m_dragRect, selection.Y);
                m_dragRect.Width = selection.Width;
                m_dragRect.Height = selection.Height;
            }
        }

        private void applyDragToView()
        {
            Point from = imageToFractalPoint(m_dragFrom);
            Point to = imageToFractalPoint(m_mousePos);
            Vector delta = to - from;
            if (m_dragMode == DragModes.Move)
            {
                
                m_view.X -= delta.X;
                m_view.Y -= delta.Y;
            }
            else if (m_dragMode == DragModes.Selection)
            {
                Vector screenDelta = m_mousePos - m_dragFrom;
                if (Math.Abs(screenDelta.X) > MIN_SELECT_SIZE && Math.Abs(screenDelta.Y) > MIN_SELECT_SIZE)
                {
                    Rect selection = calcSelectionRect();
                    Console.WriteLine(selection.ToString());
                    m_view.Location = imageToFractalPoint(selection.Location); 
                    m_view.Size = imageToFractaqlSize(selection.Size);
                }
            }

            m_dragLine.Visibility = Visibility.Collapsed;
            m_dragRect.Visibility = Visibility.Collapsed;
        }

        private Rect calcSelectionRect()
        {    
            double x1 = Math.Min(m_dragFrom.X, m_mousePos.X);
            double y1 = Math.Min(m_dragFrom.Y, m_mousePos.Y);
            double x2 = Math.Max(m_dragFrom.X, m_mousePos.X);
            double y2 = Math.Max(m_dragFrom.Y, m_mousePos.Y);

            double width = Math.Abs(x1 - x2);
            double height = Math.Abs(y1 - y2);

            double ratio = m_image.ActualWidth / m_image.ActualHeight;
            height = width / ratio;



            return new Rect(x1, y1, width, height);
        }

        public MandelRenderer Renderer 
        {
            get { return m_render; }
        }
    }
}
