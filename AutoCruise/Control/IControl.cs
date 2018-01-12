using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCruise.Control
{
    public interface IControl : IDisposable
    {
        void Reset();
        void SetLongitudal(float longitudal);
        void SetLateral(float lateral);
    }
}
