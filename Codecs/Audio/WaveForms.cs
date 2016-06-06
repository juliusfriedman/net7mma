namespace Media.Codec
{
    //Todo combine with WaveForm.

    /// <summary>
    /// A Specific kind of Wave with possibly different amplitude for left and right channels.
    /// </summary>
    internal class SineWave
    {
        #region Fields

        private double m_frequency;
        private double m_dataSlice;
        private double m_leftAmplitude;
        private double m_rightAmplitude;

        #endregion

        #region Properties

        public double RightAmplitude
        {
            get { return m_rightAmplitude; }
            set { m_rightAmplitude = value; }
        }

        public double LeftAmplitude
        {
            get { return m_leftAmplitude; }
            set { m_leftAmplitude = value; }
        }

        public double DataSlice
        {
            get { return m_dataSlice; }
            set { m_dataSlice = value; }
        }

        public double Frequency
        {
            get { return m_frequency; }
            set { m_frequency = value; }
        }

        #endregion

        #region Constructors

        internal SineWave(double p_frequency, double p_leftAmplitude, double p_rightAmplitude)
        {
            this.m_frequency = p_frequency;
            this.m_leftAmplitude = p_leftAmplitude;
            this.m_rightAmplitude = p_rightAmplitude;

        }

        #endregion

    }

    /// <summary>
    /// Base class of all wave forms
    /// </summary>
    public abstract class Waveform
    {
        public double Amplitude { get; set; }

        public double Frequency { get; set; }

        public double Phase { get; set; }

        public double Damping { get; set; }

        protected Waveform(double amplitude, double frequency, double phase, double damping)
        {
            this.Amplitude = amplitude;
            this.Frequency = frequency;
            this.Phase = phase;
            this.Damping = damping;
        }

        public abstract double GetValue(double t);

        public virtual double GetDerivative(double t, double dt)
        {
            double y_prev = GetValue(t - dt);
            double y_now = GetValue(t);
            return (y_now - y_prev) / dt;
        }

        public double GetDampingMultiplier(double t)
        {
            return System.Math.Pow(System.Math.E, -Damping * t);
        }
    }

    public class Sine : Waveform
    {
        public Sine(double amplitude, double frequency, double phase = 0, double damping = 0)
            : base(amplitude, frequency, phase, damping)
        {
        }

        public override double GetValue(double t)
        {
            return Amplitude * GetDampingMultiplier(t) * System.Math.Sin(2 * System.Math.PI * Frequency * t + Phase);
        }

        public override double GetDerivative(double t, double dt)
        {
            return Amplitude * GetDampingMultiplier(t) * (2 * System.Math.PI * Frequency) * System.Math.Cos(2 * System.Math.PI * Frequency * t + Phase);
        }
    }

    public class Saw : Waveform
    {
        public Saw(double amplitude, double frequency, double phase = 0, double damping = 0)
            : base(amplitude, frequency, phase, damping)
        {
        }

        public override double GetValue(double t)
        {
            double ft = Frequency * t;
            return Amplitude * GetDampingMultiplier(t) * System.Math.Sin(2 * (ft - System.Math.Floor(ft)) - 1 + Phase);
        }
    }

    public class Triangle : Waveform
    {
        public Triangle(double amplitude, double frequency, double phase = 0, double damping = 0)
            : base(amplitude, frequency, phase, damping)
        {
        }

        public override double GetValue(double t)
        {
            double ft = Frequency * t;
            return Amplitude * GetDampingMultiplier(t) * System.Math.Sin((ft - 2 * System.Math.Floor((ft + 1) / 2)) * System.Math.Pow(-1, System.Math.Floor((ft + 1) / 2)) + Phase);
        }
    }

    public class Square : Waveform
    {
        public Square(double amplitude, double frequency, double phase = 0, double damping = 0)
            : base(amplitude, frequency, phase, damping)
        {
        }

        public override double GetValue(double t)
        {
            if (t == 0) return 0;
            return Amplitude * GetDampingMultiplier(t) * System.Math.Sign(System.Math.Sin(2 * System.Math.PI * Frequency * t + Phase));
        }
    }

    //Tone, DTMF etc
}
