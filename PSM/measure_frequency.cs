using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace PSM
{
    public class Frequency : IDisposable
    {

        public enum RateUnit : uint
        {
            NANOSECONDS = 1 / (1000 * 1000),
            MICROSECONDS = 1 / 1000,
            MILLISECONDS = 1,
            SECONDS = 1000 * MILLISECONDS,
            MINUTES = 60 * SECONDS,
            HOURS = 60 * MINUTES,
            DAYS = 24 * HOURS
        }


        public const RateUnit TICK_INTERVAL = RateUnit.SECONDS;
        public const RateUnit RATE_UNIT = RateUnit.SECONDS;

        private double _alpha;
        private double _count;
        private double _rate;

        private RateUnit _timePeriod;
        private RateUnit _tickInterval;

        private Timer _timer;

        public Frequency(RateUnit? timePeriod = null, RateUnit? tickInterval = null)
        {

            _timePeriod = timePeriod.HasValue ? timePeriod.Value : RateUnit.MINUTES;
            _tickInterval = tickInterval.HasValue ? tickInterval.Value : TICK_INTERVAL;

            _alpha = 1 - Math.Exp(-(double)_tickInterval / (double)_timePeriod);

            _timer = new Timer((double)TICK_INTERVAL);
            _timer.Elapsed += (a, b) => Tick();
        }

        public void Mark(double n)
        {
            _count += n;

            if (!_timer.Enabled)
                _timer.Start();
        }

        private void Tick()
        {

            double instantRate = _count / (double)_tickInterval;

            _count = 0D;
            _rate += (_alpha * (instantRate - _rate));
        }

        public double Rate(RateUnit? rateUnit = null)
        {
            return _rate * (rateUnit.HasValue ? (double)rateUnit.Value : (double)RATE_UNIT);
        }

        public void Dispose()
        {
            _timer.Stop();
        }
    }

}
