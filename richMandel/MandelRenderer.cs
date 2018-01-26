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
using System.Diagnostics;


// NOTES
//  * Supersample might have some mouse issues mandelpoint was null!!
//  * scrolling in when calculating sometimes crashes because of non locked bitmap ?!?!?!
//      * probably when aborting current render

namespace richMandel
{
    /// <summary>
    /// Class that renders an image to the given Fractal definition
    /// </summary>
    class MandelRenderer
    {
        //TODO make all of those configurable
        private static int colorPeriod = 70;
        private static Color scol = Colors.Orange;
        private static Color ecol = Colors.RoyalBlue;

        public event Action<long> Finished;
        public event Action<double> Progress;
        
        //store values for animated calcualtion
        MandelPoint[,] m_values = null;
        int m_curDepth;
        int m_drawInterval = 5; //TODO make configurable
        int m_fpsMax = 120;        //TODO make configurable

        List<Color> m_colorLookup = new List<Color>();

        private int m_maxDepth = 2000;
        private int m_threadCount = 1;

        int m_pxHeight;
        int m_pxWidth;
        IntPtr m_pBackbuffer;
        Complex m_topLeft;
        Complex m_size;

        WriteableBitmap m_bitmap;
        int m_bytesPerPixel;

        private int m_curRow;
        bool rendering = false;

        IFractalDefinition m_fractDef;
        Object m_threadLock = new Object();

        Thread m_renderThread = null;

        Stopwatch m_fpsSw = new Stopwatch();

        public MandelRenderer(IFractalDefinition fd)
        {
            m_fractDef = fd;
            initColorLookup();
        }

        public int Depth
        {
            get { return m_maxDepth; }
            set 
            { 
                if (value >= 0) 
                    m_maxDepth = value; 
            }
        }

        public int ThreadCount
        {
            get { return m_threadCount; }
            set 
            { 
                if (value >= 0)
                    m_threadCount = value; 
            }
        }

        private void initColorLookup()
        {
            for (int i = 0; i < colorPeriod; i++)
            {
                double s = Math.Sin((i / (double)colorPeriod) * Math.PI);
                //s *= s;
                double e = 1 - s;

                var c = new Color();
                c.R = (byte)(s * scol.R + e * ecol.R);
                c.G = (byte)(s * scol.G + e * ecol.G);
                c.B = (byte)(s * scol.B + e * ecol.B);
                m_colorLookup.Add(c);
            }
        }

        public void startRender()
        {
            startRender(m_bitmap, Rect.Empty, false);
        }

        public void startRender(WriteableBitmap bitmap, Rect view)
        {
            startRender(bitmap, view, true);
        }

        private void startRender(WriteableBitmap bitmap, Rect view, bool init)
        {
            bool joinPrevThread = rendering;
            rendering = false; //this will stop current render threads

            new Thread(() =>
            {
                //wait untill currently readering threads have finished to avoid collisiions
                if (joinPrevThread && m_renderThread != null)
                    m_renderThread.Join();

                if (init)
                    initRender(bitmap, view);
                
                m_renderThread = Thread.CurrentThread;
                rendering = true;
                calcPoints();   //do actual calulation
                if (rendering)  //dont do those things when aborting
                {
                    drawToBitmap(); 
                    invokeFinished();
                }

                rendering = false;
                m_renderThread = null;
            }).Start();
        }

        public void continueRender()
        {
            if (!rendering && m_curDepth < m_maxDepth)
                startRender();
        }

        private void initRender(WriteableBitmap bitmap, Rect view)
        {
            m_bitmap = bitmap;
            m_bitmap.Dispatcher.Invoke(new Action(() => 
            {
                m_pxWidth = m_bitmap.PixelWidth;
                if (m_pxWidth % 2 == 1) m_pxWidth++;
                m_pxHeight = m_bitmap.PixelHeight;
                m_bytesPerPixel = m_bitmap.Format.BitsPerPixel / 8;
                m_pBackbuffer = m_bitmap.BackBuffer;
            }));

            m_topLeft = new Complex(view.Left, view.Top);
            m_size = new Complex(view.Width, view.Height);

            //init point array
            m_curDepth = 0;
            m_values = new MandelPoint[m_pxWidth, m_pxHeight];
            for (int x = 0; x < m_pxWidth; x++)
                for (int y = 0; y < m_pxHeight; y++)
                    m_values[x, y] = new MandelPoint(m_topLeft, m_size, x / (double)m_pxWidth, y / (double)m_pxHeight);
        }

        private void drawToBitmap()
        {
            //Console.WriteLine(m_curDepth);
            m_bitmap.Dispatcher.BeginInvoke(new Action(() =>
            {
                //move changes to frontbuffer
                lock (this)
                {
                    m_bitmap.Lock();
                    m_bitmap.AddDirtyRect(new Int32Rect(0, 0, m_bitmap.PixelWidth, m_pxHeight));
                    m_bitmap.Unlock();
                }
            }));
        }

        private void invokeFinished()
        {
            invokeProgress(1);
            if (Finished != null)
                Finished.Invoke(-1);
        }

        private void invokeProgress(double progress)
        {
            if (Progress != null)
                Progress.Invoke(progress);
        }

        private void calcPoints()
        {
            int waitingThreads = 0;
            List<Thread> tlist = new List<Thread>();
            m_curRow = 0;
            for (int i = 0; i < m_threadCount; i++)
            {
                tlist.Add(new Thread(() =>
                {
                    while (rendering && m_curDepth <= m_maxDepth)
                    {
                        //calculate
                        int y = getNextLine();
                        while (rendering && y != -1)
                        {
                            for (int x = 0; x < m_pxWidth; x++)
                                calcPoint(x, y);

                            y = getNextLine();
                        }
                        
                        //threading...
                        lock (m_threadLock)
                        {
                            //last thread has to trigger drawing and continues rendering of the next frame
                            if (waitingThreads == m_threadCount - 1)
                            {
                                calcOn();
                                Monitor.PulseAll(m_threadLock);
                            }
                            else
                            {
                                waitingThreads++;
                                Monitor.Wait(m_threadLock);
                                waitingThreads--;
                            }
                        }
                    }
                }));
            }

            foreach (Thread t in tlist) t.Start();
            foreach (Thread t in tlist) t.Join();

        }

        private void calcOn()
        {
            m_curDepth += m_drawInterval;

            m_curRow = 0;
            drawToBitmap();
            invokeProgress((double)m_curDepth / m_maxDepth);

            m_fpsSw.Stop();
            //limit fps to m_fpsMax
            if (m_fpsSw.ElapsedMilliseconds < 1000 / m_fpsMax)
                Thread.Sleep(1000 / m_fpsMax - (int)m_fpsSw.ElapsedMilliseconds);

            m_fpsSw.Restart();
        }

        private int getNextLine()
        {
            int row = -1;
            lock (m_threadLock)
            {
                if (m_curRow >= m_pxHeight)
                    return -1;

                row = m_curRow++;
            }

            return row;
        }

        private void calcPoint(int x, int y)
        {
            MandelPoint mp = m_values[x, y];
            if (mp.notInSet)
                return;

            do
            {
                m_fractDef.applyFunction(mp);
                if (!m_fractDef.isInSet(mp))
                {
                    setPixel(x, y, m_colorLookup[mp.depth % colorPeriod]);
                    mp.notInSet = true;
                    return;
                }

                mp.depth++;
            }
            while (rendering && mp.depth <= m_maxDepth && mp.depth % m_drawInterval != 0);

            setPixel(x, y, Colors.Black);
        }

        private void setPixel(int x, int y, Color c)
        {
            //Console.WriteLine("set pixel:" + x + " " + y);
            unsafe
            {
                byte* pPixels = (byte*)m_pBackbuffer;

                pPixels += (x + y * m_pxWidth) * m_bytesPerPixel;
                *(pPixels++) = c.B;
                *(pPixels++) = c.G;
                *pPixels     = c.R;
            }
        }
    }
}
