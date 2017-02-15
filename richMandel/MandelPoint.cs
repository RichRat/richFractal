using System.Numerics;

namespace richMandel
{
    class MandelPoint
    {
        /// <summary>
        /// result of the iterations so far
        /// </summary>
        public Complex z = 0;

        /// <summary>
        /// starting value
        /// </summary>
        public Complex c;

        /// <summary>
        /// how many iterations where done on this point
        /// </summary>
        public int depth = 0;

        /// <summary>
        /// creates a mandelPoint between toplet and topleft + botright
        /// </summary>
        /// <param name="topleft">top left corner of view</param>
        /// <param name="botright">size of view</param>
        /// <param name="x">0 - 1 relative x position</param>
        /// <param name="y">0 - 1 relative y position</param>
        public MandelPoint(Complex topleft, Complex botright, double x, double y)
        {
            c = new Complex(botright.Real * x, botright.Imaginary * y) - topleft;
        }

        public override string ToString()
        {
            return " z=" + z.ToString() + " c=" + c.ToString();
        }
    }
}
