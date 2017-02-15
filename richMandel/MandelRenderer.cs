using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Numerics;
using System.Threading;
using System.Timers;

namespace richMandel
{
    /// <summary>
    /// Class that renders an image to the given Fractal definition
    /// </summary>
    class MandelRenderer
    {
        //TODO make all of those configurable
        private static int THREADCOUNT = 1;
        private static int MAXDEPTH = 1000;
        private static double colorPeriod = 42;
        private static Color scol = Colors.Red;
        private static Color ecol = Colors.Yellow;

        event Action<long> Finished;
        
        //store values for animated calcualtion
        //MandelPoint[][] m_values = null;

        //dont waste time calculating colors over and over
        List<Color> m_colorLookup = new List<Color>();

        int m_pxHeight;
        int m_pxWidth;
        IntPtr m_pBackbuffer;
        Complex m_topLeft;
        Complex m_size;

        WriteableBitmap m_bitmap;
        int m_bytesPerPixel;

        private int m_currentRow;
        bool rendering = false;

        IFractalDefinition m_fractDef;
        Object m_semaph1 = new Object();
        Object m_semaph2 = new Object();

        public MandelRenderer(IFractalDefinition fd)
        {
            if (rendering)
                rendering = false;
            m_fractDef = fd;
            initColorLookup();
        }

        private void initColorLookup()
        {
            for (int i = 0; i < MAXDEPTH; i++)
            {
                double s = Math.Sin(i / colorPeriod);
                s *= s;
                double e = 1 - s;

                var c = new Color();
                c.R = (byte)(s * scol.R + e * ecol.R);
                c.G = (byte)(s * scol.G + e * ecol.G);
                c.B = (byte)(s * scol.B + e * ecol.B);
                m_colorLookup.Add(c);
            }
        }

        public void startRender(WriteableBitmap bitmap, Rect view)
        {
            m_bitmap = bitmap;
            m_pxWidth = m_bitmap.PixelWidth;
            m_pxHeight = m_bitmap.PixelHeight;
            m_bytesPerPixel = m_bitmap.Format.BitsPerPixel / 8;
            m_pBackbuffer = m_bitmap.BackBuffer;

            m_topLeft = new Complex(view.Left, view.Top);
            m_size = new Complex(view.Width, view.Height);

            //m_values = new MandelPoint[m_pxWidth][];
            //for (int i = 0; i < m_pxWidth; i++)
            //    m_values[i] = new MandelPoint[m_pxHeight];

            new Thread(() => 
            {
                rendering = true;
                calcPoints();
                if (rendering)
                {
                    m_bitmap.Dispatcher.Invoke(() =>
                    {
                        //move changes to frontbuffer
                        m_bitmap.Lock();
                        m_bitmap.AddDirtyRect(new Int32Rect(0, 0, m_pxWidth, m_pxHeight));
                        m_bitmap.Unlock();
                        if (Finished != null)
                            Finished.Invoke(-1);
                    });
                }
                rendering = false;
            }).Start();
        }

        private void calcPoints()
        {
            //todo encapsulate in thread and use dispatcher to call back
            List<Thread> tlist = new List<Thread>();
            m_currentRow = 0;
            for (int i = 0; i < THREADCOUNT; i++)
            {
                Thread t = new Thread(() =>
                {
                    int y = getNextLine();;
                    while (rendering && y != -1)
                    {
                        for (int x = 0; x < m_pxWidth; x++)
                            calcPoint(x, y);
                        y = getNextLine();
                    }
                });
                t.Start();
                tlist.Add(t);
            }

            foreach (Thread t in tlist)
                t.Join();
        }

        private int getNextLine()
        {
            lock (m_semaph1)
            {
                if (m_currentRow >= m_pxHeight)
                    return -1;
                return m_currentRow++;
            }
        }

        private void calcPoint(int x, int y)
        {
            
            //MandelPoint mp = m_values[x][y];
            var mp = new MandelPoint(m_topLeft, m_size, x / (double)m_pxWidth, y / (double)m_pxHeight);
            for (int i = 0; i < MAXDEPTH; i++)
            {
                m_fractDef.applyFunction(mp);

                if (!m_fractDef.isInSet(mp))
                {
                    setPixel(x, y, m_colorLookup[i]);
                    return;
                }
            }

            setPixel(x, y, Colors.Black);
        }

        private void setPixel(int x, int y, Color c)
        {
            lock (m_semaph2)
            {
                unsafe
                { //fuck yeah pointers
                    byte* pPixels = (byte*)m_pBackbuffer;

                    pPixels += (x + y * m_pxWidth) * m_bytesPerPixel;
                    *pPixels++ = c.B;
                    *pPixels++ = c.G;
                    *pPixels = c.R;
                }
            }
        }
    }
}
