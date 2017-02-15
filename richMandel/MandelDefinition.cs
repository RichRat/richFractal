using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace richMandel
{
    class MandelDefinition : IFractalDefinition
    {
        ///applies mandelbrot function to point
        public void applyFunction(MandelPoint p)
        {
            p.z = p.z * p.z + p.c;
        }

        
        //check if point is in the mandelbrot set
        public bool isInSet(MandelPoint p)
        {
            return p.z.Real * p.z.Real + p.z.Imaginary * p.z.Imaginary < 500;
        }
    }
}
