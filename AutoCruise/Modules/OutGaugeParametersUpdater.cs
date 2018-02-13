using InSimDotNet.Out;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoCruise.Modules
{
    public class OutGaugeParametersUpdater : IDisposable
    {
        private OutGauge _outGauge;
        private Parameters _parameters;

        public OutGaugeParametersUpdater(OutGauge outGauge, Parameters parameters)
        {
            _outGauge = outGauge;
            _parameters = parameters;
        }

        public void StartUpdatingParameters()
        {
            _outGauge.PacketReceived += OutGauge_PacketReceived;
        }

        private void OutGauge_PacketReceived(object sender, OutGaugeEventArgs e)
        {
            _parameters.Speed = e.Speed;
            _parameters.Gear = e.Gear;
            _parameters.Rpm = e.RPM;
        }

        public void Dispose()
        {
            _outGauge.PacketReceived -= OutGauge_PacketReceived;
        }
    }
}
