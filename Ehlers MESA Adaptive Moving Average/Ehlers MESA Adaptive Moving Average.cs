using System;
using cAlgo.API;

namespace cAlgo
{
    [Indicator(IsOverlay = true, TimeZone = TimeZones.UTC, AccessRights = AccessRights.None), Cloud("MAMA", "FAMA")]
    public class EhlersMesaAdaptiveMovingAverage : Indicator
    {
        private IndicatorDataSeries _sp, _dt, _p, _q1, _i2, _q2, _re, _im, _p1, _p3, _spp, _phase;

        [Parameter("Source")]
        public DataSeries Source { get; set; }

        [Parameter("Fast Limit", DefaultValue = 0.5)]
        public double FastLimit { get; set; }

        [Parameter("Slow Limit", DefaultValue = 0.05)]
        public double SlowLimit { get; set; }

        [Output("MAMA", LineColor = "Green", PlotType = PlotType.Line)]
        public IndicatorDataSeries Mama { get; set; }

        [Output("FAMA", LineColor = "Red", PlotType = PlotType.Line)]
        public IndicatorDataSeries Fama { get; set; }

        protected override void Initialize()
        {
            _sp = CreateDataSeries();
            _dt = CreateDataSeries();
            _p = CreateDataSeries();
            _q1 = CreateDataSeries();
            _i2 = CreateDataSeries();
            _q2 = CreateDataSeries();
            _re = CreateDataSeries();
            _im = CreateDataSeries();
            _p1 = CreateDataSeries();
            _p3 = CreateDataSeries();
            _spp = CreateDataSeries();
            _phase = CreateDataSeries();
        }

        public override void Calculate(int index)
        {
            _sp[index] = (4 * Source[index] + 3 * Source[index - 1] + 2 * Source[index - 2] + Source[index - 3]) / 10.0;
            _dt[index] = (.0962 * _sp[index] + .5769 * GetValueOrDefault(_sp, index - 2) - .5769 * GetValueOrDefault(_sp, index - 4) - .0962 * GetValueOrDefault(_sp, index - 6)) * (.075 * GetValueOrDefault(_p, index - 1) + .54);
            _q1[index] = (.0962 * _dt[index] + .5769 * GetValueOrDefault(_dt, index - 2) - .5769 * GetValueOrDefault(_dt, index - 4) - .0962 * GetValueOrDefault(_dt, index - 6)) * (.075 * GetValueOrDefault(_p, index - 1) + .54);

            var jq = (.0962 * _q1[index] + .5769 * GetValueOrDefault(_q1, index - 2) - .5769 * GetValueOrDefault(_q1, index - 4) - .0962 * GetValueOrDefault(_q1, index - 6)) * (.075 * GetValueOrDefault(_p, index - 1) + .54);

            var i2 = _dt[index] - jq;
            var q2 = _q1[index] + _dt[index];

            _i2[index] = .2 * i2 + .8 * GetValueOrDefault(_i2, index - 1);
            _q2[index] = .2 * q2 + .8 * GetValueOrDefault(_q2, index - 1);

            var re = i2 * GetValueOrDefault(_i2, index - 1) + q2 * GetValueOrDefault(_q2, index - 1);

            _re[index] = .2 * re + .8 * GetValueOrDefault(_re, index - 1);

            var im = i2 * GetValueOrDefault(_q2, index - 1) - q2 * GetValueOrDefault(_i2, index - 1);

            _im[index] = .2 * im + .8 * GetValueOrDefault(_im, index - 1);

            _p1[index] = _im[index] != 0 && _re[index] != 0 ? 360 / Math.Atan(_im[index] / _re[index]) : GetValueOrDefault(_p, index - 1);

            double p2;

            if (_p1[index] > 1.5 * GetValueOrDefault(_p1, index - 1))
            {
                p2 = 1.5 * GetValueOrDefault(_p1, index - 1);
            }
            else
            {
                p2 = _p1[index] < 0.67 * GetValueOrDefault(_p1, index - 1) ? 0.67 * GetValueOrDefault(_p1, index - 1) : _p1[index];
            }

            if (p2 < 6)
            {
                _p3[index] = 6;
            }
            else
            {
                _p3[index] = p2 > 50 ? 50 : p2;
            }

            var p = .2 * _p3[index] + .8 * GetValueOrDefault(_p3, index - 1);

            _spp[index] = .33 * p + .67 * GetValueOrDefault(_spp, index - 1);

            _phase[index] = Math.Atan(_q1[index] / _q1[index]);

            var dphase = GetValueOrDefault(_phase, index - 1) - _phase[index];
            var dphaseValue = dphase < 1 ? 1 : dphase;

            var alpha = FastLimit / dphaseValue;
            double alphaValue;

            if (alpha < SlowLimit)
            {
                alphaValue = SlowLimit;
            }
            else
            {
                alphaValue = alpha > FastLimit ? FastLimit : alpha;
            }

            Mama[index] = alphaValue * Source[index] + (1 - alphaValue) * GetValueOrDefault(Mama, index - 1);
            Fama[index] = .5 * alphaValue * Mama[index] + (1 - .5 * alphaValue) * GetValueOrDefault(Fama, index - 1);
        }

        private double GetValueOrDefault(DataSeries series, int index, double defaultValue = 0)
        {
            var value = series[index];

            return double.IsNaN(value) ? defaultValue : value;
        }
    }
}