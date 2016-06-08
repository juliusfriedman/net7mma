using System;
using System.Collections.Generic;
using System.Linq;
/*
Copyright (c) 2013 juliusfriedman@gmail.com
  
 SR. Software Engineer ASTI Transportation Inc.

Permission is hereby granted, free of charge, 
 * to any person obtaining a copy of this software and associated documentation files (the "Software"), 
 * to deal in the Software without restriction, 
 * including without limitation the rights to :
 * use, 
 * copy, 
 * modify, 
 * merge, 
 * publish, 
 * distribute, 
 * sublicense, 
 * and/or sell copies of the Software, 
 * and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * 
 * JuliusFriedman@gmail.com should be contacted for further details.

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. 
 * 
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, 
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, 
 * TORT OR OTHERWISE, 
 * ARISING FROM, 
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 * v//
 */
///All types besides UnitBase could eventually be struct
namespace Media.Concepts.Classes
{
    public interface IUnit
    {
        IEnumerable<string> Symbols { get; } //Used with formatting

        /// <summary>
        /// A Number which represents a value from which a scalar value can be calculated from the TotalUnits member
        /// </summary>
        Number Constant { get; }

        /// <summary>
        /// A Number which represents the total amount of integral units of this instance
        /// </summary>
        Number TotalUnits { get; }
    }

    /// <summary>
    /// The base class of all units or magnitudes.
    /// </summary>
    /// <remarks>
    /// Other implementations...
    /// https://github.com/dotnet/corefx/issues/6831
    /// https://github.com/JohanLarsson/Gu.Units/blob/master/Gu.Units/ElectricCharge.generated.cs
    /// </remarks>
    public abstract class UnitBase : IUnit, IFormattable
    {
        /// <summary>
        /// The <see cref="System.Globalization.RegionInfo"/> as retrieved from <see cref="System.Globalization.RegionInfo.CurrentRegion"/>
        /// </summary>
        public static readonly System.Globalization.RegionInfo CurrentRegion = System.Globalization.RegionInfo.CurrentRegion;

        /// <summary>
        /// Indicates if the <see cref="CurrentRegion"/> utilizes the Metric system of units.
        /// </summary>
        public static bool IsMetricSystem
        {
            get { return CurrentRegion.IsMetric; }
        }

        abstract protected List<string> m_Symbols { get; }

        /// <summary>
        /// Defines the number used to scale other distances to this number.
        /// </summary>
        public Number Constant { get; internal protected set; }

        public IEnumerable<string> Symbols { get { return m_Symbols.AsReadOnly(); } }

        public Number Units { get; protected set; }

        IEnumerable<string> IUnit.Symbols
        {
            get { return Symbols; }
        }

        Number IUnit.Constant
        {
            get { return Constant; }
        }

        public Number TotalUnits
        {
            get
            {
                //More Flexible
                //return Constant.ToDouble() > 1D ? Units.ToDouble() * Constant.ToDouble() : Units.ToDouble() / Constant.ToDouble();
                return new Number(Units.ToDouble() * Constant.ToDouble());
            }
        }

        /// <summary>
        /// Constructs a new UnitBase with the given constant
        /// </summary>
        /// <param name="constant">The constant which when multiplied by the Units property represents a quantity</param>
        public UnitBase(Number constant)
        {
            Constant = constant;
        }

        /// <summary>
        /// Constructs a new UnitBase from another.
        /// If the Constants of the two Units are the same the Units property is assigned, otherwise the Units is obtained by division of the other UnitBase's Units by this instances Constant.
        /// </summary>
        /// <param name="constant">The constant which when multiplied by the Units property represents a quantity</param>
        /// <param name="other">Another Unit base</param>
        public UnitBase(Number constant, UnitBase other)
            : this(constant)
        {
            if (other.Constant != Constant)
                Units = Constant.ToDouble() / other.Units.ToDouble();
            else
                Units = other.Units;
        }


        public virtual string ToString(string join = " ")
        {
            return Units.ToString() + join + m_Symbols.FirstOrDefault() ?? string.Empty;
        }

        public override string ToString()
        {
            return ToString(null);
        }

        string IFormattable.ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, format, ToString());
        }
    }

    public static class Distances
    {
        public interface IDistance : IUnit
        {
            Number TotalMeters { get; }
        }

        public class Distance : UnitBase, IDistance
        {

            public static readonly double PlankLengthsPerMeter = 6.1873559 * Math.Pow(10, 34);

            public static readonly double MilsPerMeter = 2.54 * Math.Pow(10, -5);

            public const double InchesPerMeter = 0.0254;

            public const double FeetPerMeter = 0.3048;

            public const double YardsPerMeter = 0.9144;

            public const double MilesPerMeter = 1609.344;

            public static readonly double AttometersPerMeter = Math.Pow(10, 18);

            //1 yoctometer = 0,001 zeptometer
            //1 attometer = 1000 zeptometer
            //1 000 yoctometer
            //0,001 attometer
            //10−21 meter
            public static readonly double ZeptometersPerMeter = Math.Pow(10, -21);

            public static readonly double YoctometersPerMeter = Math.Pow(10, -24);

            public const double NanometersPerMeter = 1000000000;

            public const double MicronsPerMeter = 1000000;

            public const double MillimetersPerMeter = 1000;

            public const double CentimetersPerMeter = 100;

            public const double DecimetersPerMeter = 10;

            public const double M = 1;

            public const double KilometersPerMeter = 0.001;

            /// <summary>
            /// The minimum distance in Meters = The Plank Length
            /// </summary>
            public static readonly Distance MinValue = Physics.ℓP;

            public static readonly Distance PositiveInfinity = new Distance(Number.PositiveInfinty);

            public static readonly Distance NegitiveInfinity = new Distance(Number.NegitiveInfinity);

            public static readonly Distance Zero = new Distance(Number.ComplexZero);

            static List<string> DistanceSymbols = new List<string>()
            {
                "ℓP",
                "mil",
                "in",
                "ft",
                "yd",
                "mi",
                "n",
                "µ",
                "mm",
                "cm",
                "m",
                "km"
            };

            public Distance()
                : base(M)
            {
                Constant = MinValue.Constant;
                Units = MinValue.Units;
            }

            public Distance(Number meters)
                : base(M)
            {
                Units = meters;
            }

            public Distance(Distance other) : base(M, other) { }

            public Distance(Number value, Distance other) : base(M, other) { Units = value; }

            protected override List<string> m_Symbols
            {
                get
                {
                    return DistanceSymbols;
                }
            }

            public virtual Number TotalMeters
            {
                get { return Units; }
            }

            public virtual Number TotalInches
            {
                get { return TotalMeters / InchesPerMeter; }
            }

            public virtual Number TotalFeet
            {
                get { return TotalMeters / FeetPerMeter; }
            }

            public virtual Number TotalYards
            {
                get { return TotalMeters / YardsPerMeter; }
            }

            public virtual Number TotalKilometers
            {
                get { return TotalMeters / KilometersPerMeter; }
            }

            public static Distance FromInches(Number value)
            {
                return new Distance(value.ToDouble() * InchesPerMeter);
            }

            public static Distance FromFeet(Number value)
            {
                return new Distance(value.ToDouble() * FeetPerMeter);
            }

            public static Distance FromYards(Number value)
            {
                return new Distance(value.ToDouble() * YardsPerMeter);
            }

            public static Distance operator +(Distance a, int amount)
            {
                return new Distance(a.Units.ToDouble() + amount);
            }

            public static Distance operator -(Distance a, int amount)
            {
                return new Distance(a.Units.ToDouble() - amount);
            }

            public static Distance operator *(Distance a, int amount)
            {
                return new Distance(a.Units.ToDouble() * amount);
            }

            public static Distance operator /(Distance a, int amount)
            {
                return new Distance(a.Units.ToDouble() / amount);
            }

            public static Distance operator %(Distance a, int amount)
            {
                return new Distance(a.Units.ToDouble() % amount);
            }

            public static bool operator >(Distance a, IDistance b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant > b.TotalMeters;
                return a.Units > b.TotalMeters;
            }

            public static bool operator <(Distance a, IDistance b)
            {
                return !(a > b);
            }

            public static bool operator ==(Distance a, IDistance b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant == b.TotalMeters;
                return a.Units == b.TotalMeters;
            }

            public static bool operator !=(Distance a, IDistance b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (obj is IDistance) return obj as IDistance == this;
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Constant.GetHashCode() << 16 | Units.GetHashCode() >> 16;
            }
        }
    }

    //Angles?

    public static class Frequencies
    {
        ////    public enum FrequencyKind
        ////    {
        ////        Local,
        ////        Universal
        ////    }

        ////    public static class Clock
        ////    {
        ////    }


        public interface IFrequency
        {
            Number TotalMegahertz { get; }
        }


        //http://en.wikipedia.org/wiki/Frequency
        /* Frequencies not expressed in hertz:
         * 
         * Even higher frequencies are believed to occur naturally, 
         * in the frequencies of the quantum-mechanical wave functions of high-energy
         * (or, equivalently, massive) particles, although these are not directly observable, 
         * and must be inferred from their interactions with other phenomena. 
         * For practical reasons, these are typically not expressed in hertz, 
         * but in terms of the equivalent quantum energy, which is proportional to the frequency by the factor of Planck's constant.
         */
        public class Frequency : UnitBase, IFrequency
        {

            public static implicit operator double(Frequency t) { return t.Units.ToDouble(); }

            public static implicit operator Frequency(double t) { return new Frequency(t); }

            public static readonly Frequency Zero = new Frequency(Number.ComplexZero);

            public static readonly Frequency One = new Frequency(new Number(Hz)); //Hz

            public const double Hz = 1;

            public const double KHz = 1000D;

            public const double MHz = 1000000D;

            public const double GHz = 1000000000D;

            public const double THz = 1000000000000D;


            //http://en.wikipedia.org/wiki/Visible_spectrum - Audible?
            public static bool IsVisible(Frequency f, double min = 430, double max = 790)
            {
                double F = f.Terahertz.ToDouble();
                return F >= min && F <= max;
            }

            static List<string> FrequencySymbols = new List<string>()
        {
            "Hz",
            "KHz",
            "MHz",
            "GHz",
            "THz"
        };

            public Frequency()
                : base(Hz)
            {
                //Constant = MinValue.Constant;
                //Units = MinValue.Units;
            }

            public Frequency(double MHz)
                : base(Hz)
            {
                Units = MHz;
            }

            public Frequency(Frequency other) : base(Hz, other) { }

            public Frequency(Number value, Frequency other) : base(Hz, other) { Units = value; }

            protected override List<string> m_Symbols
            {
                get
                {
                    return FrequencySymbols;
                }
            }

            public TimeSpan Period
            {
                get
                {
                    return TimeSpan.FromSeconds(TotalHertz);
                }
            }

            public virtual Number TotalHertz
            {
                get { return Units; }
            }

            public virtual Number TotalKilohertz
            {
                get { return TotalHertz * KHz; }
            }

            public virtual Number TotalMegahertz
            {
                get { return TotalHertz * MHz; }
            }

            public virtual Number TotalGigahertz
            {
                get { return TotalHertz * GHz; }
            }

            public virtual Number Terahertz
            {
                get { return TotalHertz * THz; }
            }

            public static Frequency FromKilohertz(Number value)
            {
                return new Frequency(value.ToDouble() * KHz);
            }

            public static Frequency FromMegahertz(Number value)
            {
                return new Frequency(value.ToDouble() * MHz);
            }

            public static Frequency FromGigahertz(Number value)
            {
                return new Frequency(value.ToDouble() * GHz);
            }

            public static Frequency FromTerahertz(Number value)
            {
                return new Frequency(value.ToDouble() * THz);
            }

            public static Frequency operator +(Frequency a, int amount)
            {
                return new Frequency(a.Units.ToDouble() + amount);
            }

            public static Frequency operator -(Frequency a, int amount)
            {
                return new Frequency(a.Units.ToDouble() - amount);
            }

            public static Frequency operator *(Frequency a, int amount)
            {
                return new Frequency(a.Units.ToDouble() * amount);
            }

            public static Frequency operator /(Frequency a, int amount)
            {
                return new Frequency(a.Units.ToDouble() / amount);
            }

            public static Frequency operator %(Frequency a, int amount)
            {
                return new Frequency(a.Units.ToDouble() % amount);
            }

            public static bool operator >(Frequency a, Frequency b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant > b.TotalUnits;
                return a.Units > b.TotalUnits;
            }

            public static bool operator <(Frequency a, Frequency b)
            {
                return !(a > b);
            }

            public static bool operator ==(Frequency a, Frequency b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant == b.TotalUnits;
                return a.Units == b.TotalUnits;
            }

            public static bool operator !=(Frequency a, Frequency b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (obj is Frequency) return true;
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Constant.GetHashCode() << 16 | Units.GetHashCode() >> 16;
            }


            //Add methods for conversion to time
            //http://www.hellspark.com/dm/ebench/tools/Analog_Oscilliscope/tutorials/scope_notes_from_irc.html        

        }

        ////    public struct Date
        ////    {
        ////        pulic DateTime ToDateTime(Frequency? time = null);
        ////    }
    }

    public static class Temperatures
    {

        public interface ITemperature : IUnit
        {
            Number TotalCelcius { get; }
        }

        public class Temperature : UnitBase, ITemperature
        {

            public static implicit operator double(Temperature t) { return t.Units.ToDouble(); }

            public static implicit operator Temperature(double t) { return new Temperature(t); }

            public static readonly Temperature MinValue = 0D;

            public static readonly Temperature One = 1D; //Celcius

            const double FahrenheitMultiplier = 1.8;

            public const double Fahrenheit = 32D;

            public const double Kelvin = 273.15D;

            public const char Degrees = '°';

            static List<string> TempratureSymbols = new List<string>()
        {
            "C",
            "F",
            "K",
        };

            public Temperature()
                : base(One.Units)
            {
                //Constant = MinValue.Constant;
                //Units = MinValue.Units;
            }

            public Temperature(double celcius)
                : base(One.Units)
            {
                Units = celcius;
            }

            public Temperature(Temperature other) : base(One.Units, other) { }

            public Temperature(Number value, Temperature other) : base(One.Units, other) { Units = value; }

            protected override List<string> m_Symbols
            {
                get
                {
                    return TempratureSymbols;
                }
            }

            public virtual Number TotalCelcius
            {
                get { return Units; }
            }

            public virtual Number TotalKelvin
            {
                get { return TotalCelcius + Kelvin; }
            }

            public virtual Number TotalFahrenheit
            {
                get { return TotalCelcius * FahrenheitMultiplier + Fahrenheit; }
            }

            public static Temperature FromFahrenheit(Number value)
            {
                return new Temperature(value.ToDouble() * Fahrenheit);
            }

            public static Temperature FromKelvin(Number value)
            {
                return new Temperature(value.ToDouble() - Kelvin);
            }

            public static Temperature operator +(Temperature a, int amount)
            {
                return new Temperature(a.Units.ToDouble() + amount);
            }

            public static Temperature operator -(Temperature a, int amount)
            {
                return new Temperature(a.Units.ToDouble() - amount);
            }

            public static Temperature operator *(Temperature a, int amount)
            {
                return new Temperature(a.Units.ToDouble() * amount);
            }

            public static Temperature operator /(Temperature a, int amount)
            {
                return new Temperature(a.Units.ToDouble() / amount);
            }

            public static Temperature operator %(Temperature a, int amount)
            {
                return new Temperature(a.Units.ToDouble() % amount);
            }

            public static bool operator >(Temperature a, ITemperature b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant > b.TotalUnits;
                return a.Units > b.TotalUnits;
            }

            public static bool operator <(Temperature a, ITemperature b)
            {
                return !(a > b);
            }

            public static bool operator ==(Temperature a, ITemperature b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant == b.TotalUnits;
                return a.Units == b.TotalUnits;
            }

            public static bool operator !=(Temperature a, ITemperature b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (obj is ITemperature) return true;
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Constant.GetHashCode() << 16 | Units.GetHashCode() >> 16;
            }

            public override string ToString()
            {
                return ToString(" " + Degrees);
            }
        }

    }

    public static class Masses
    {
        public interface IMass : IUnit
        {
            Number TotalKilograms { get; }
        }

        public class Mass : UnitBase, IMass
        {

            public const double AtomicMassesPerKilogram = 6.022136652e+26;

            public const double OuncesPerKilogram = 35.274;

            public const double PoundsPerKilogram = 2.20462;

            public const double Kg = 1;

            public const double GramsPerKilogram = 1000;

            static List<string> MassSymbols = new List<string>()
        {
            "u",
            "o",
            "lb",
            "kg",
            "g",
        };

            public Mass()
                : base(Kg)
            {
                //Constant = MinValue.Constant;
                //Units = MinValue.Units;
            }

            public Mass(Number kiloGrams)
                : base(Kg)
            {
                Units = kiloGrams;
            }

            public Mass(Mass other) : base(Kg, other) { }

            public Mass(Number value, Mass other) : base(Kg, other) { Units = value; }

            protected override List<string> m_Symbols
            {
                get
                {
                    return MassSymbols;
                }
            }

            public virtual Number TotalKilograms
            {
                get { return Units; }
            }

            public virtual Number TotalAtomicMasses
            {
                get { return TotalKilograms * AtomicMassesPerKilogram; }
            }

            public virtual Number TotalGrams
            {
                get { return TotalKilograms * GramsPerKilogram; }
            }
            public virtual Number TotalOunces
            {
                get { return TotalKilograms * OuncesPerKilogram; }
            }

            public virtual Number TotalPounds
            {
                get { return TotalKilograms * PoundsPerKilogram; }
            }

            public static Mass FromGrams(Number value)
            {
                return new Mass(value.ToDouble() * GramsPerKilogram);
            }

            public static Mass FromPounds(Number value)
            {
                return new Mass(value.ToDouble() * PoundsPerKilogram);
            }

            public static Mass FromOunces(Number value)
            {
                return new Mass(value.ToDouble() * OuncesPerKilogram);
            }

            public static Mass FromAtomicMasses(Number value)
            {
                return new Mass(value.ToDouble() * AtomicMassesPerKilogram);
            }

            public static Mass operator +(Mass a, int amount)
            {
                return new Mass(a.Units.ToDouble() + amount);
            }

            public static Mass operator -(Mass a, int amount)
            {
                return new Mass(a.Units.ToDouble() - amount);
            }

            public static Mass operator *(Mass a, int amount)
            {
                return new Mass(a.Units.ToDouble() * amount);
            }

            public static Mass operator /(Mass a, int amount)
            {
                return new Mass(a.Units.ToDouble() / amount);
            }

            public static Mass operator %(Mass a, int amount)
            {
                return new Mass(a.Units.ToDouble() % amount);
            }

            public static bool operator >(Mass a, IMass b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant > b.TotalUnits;
                return a.Units > b.TotalUnits;
            }

            public static bool operator <(Mass a, IMass b)
            {
                return !(a > b);
            }

            public static bool operator ==(Mass a, IMass b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant == b.TotalUnits;
                return a.Units == b.TotalUnits;
            }

            public static bool operator !=(Mass a, IMass b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (obj is IMass) return true;
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Constant.GetHashCode() << 16 | Units.GetHashCode() >> 16;
            }
        }
    }

    public static class Energies
    {

        public interface IEnergy : IUnit
        {
            Number TotalJoules { get; }
        }

        public class Energy : UnitBase, IEnergy
        {

            public static implicit operator double(Energy t) { return t.Units.ToDouble(); }

            public static implicit operator Energy(double t) { return new Energy(t); }

            public static readonly Energy MinValue = 0D;

            public static readonly Energy One = Joule;

            public static readonly Energy Zero = 0D;

            public const double ITUCaloriesPerJoule = 0.23884589663;

            public const double BtusPerJoule = 0.00094781707775;

            public const double ThermochemicalBtusPerJoule = 0.00094845138281;

            public const double DekajoulesPerJoule = 0.1;

            public const double Joule = 1;

            public const double ExajoulesPerJoule = 1.0e-18;

            public const double TerajoulesPerJoule = 1.0e-12;

            public const double DecijoulesPerJoule = 10;

            public const double CentijoulesPerJoule = 100;

            public const double TeraelectronvoltsPerJoule = 6241506.48;

            public const double FemtojoulesPerJoule = 1000000000000000;

            public const double AuttojoulePerJoule = 1000000000000000000;

            static List<string> EnergySymbols = new List<string>()
        {
            "J",
            //"Btu",
        };


            public Energy(double joules)
                : this(new Number(joules))
            {
            }

            public Energy()
                : base(Joule) { }

            public Energy(Energy other) : base(Joule, other) { }

            public Energy(Number joules)
                : base(Joule)
            {
                Units = joules;
            }

            public Energy(Masses.IMass m) :
                this(Math.Pow(m.TotalKilograms.ToDouble() * Velocities.Velocity.MaxValue.TotalMetersPerSecond.ToDouble(), 2))
            {

            }

            protected override List<string> m_Symbols
            {
                get
                {
                    return EnergySymbols;
                }
            }

            public virtual Number TotalJoules
            {
                get { return Units; }
            }

            public virtual Number Decijoules
            {
                get { return TotalJoules / DecijoulesPerJoule; }
            }

            public virtual Number Dekajoules
            {
                get { return TotalJoules / DekajoulesPerJoule; }
            }

            public virtual Number TotalITUCalories
            {
                get { return TotalJoules / ITUCaloriesPerJoule; }
            }

            public static Energy FromITUCaloriesPerJoule(Number value)
            {
                return new Energy(value.ToDouble() * ITUCaloriesPerJoule);
            }

            public static Energy FromDekajoules(Number value)
            {
                return new Energy(value.ToDouble() * DekajoulesPerJoule);
            }

            public static Energy operator +(Energy a, int amount)
            {
                return new Energy(a.Units.ToDouble() + amount);
            }

            public static Energy operator -(Energy a, int amount)
            {
                return new Energy(a.Units.ToDouble() - amount);
            }

            public static Energy operator *(Energy a, int amount)
            {
                return new Energy(a.Units.ToDouble() * amount);
            }

            public static Energy operator /(Energy a, int amount)
            {
                return new Energy(a.Units.ToDouble() / amount);
            }

            public static Energy operator %(Energy a, int amount)
            {
                return new Energy(a.Units.ToDouble() % amount);
            }

            public static bool operator >(Energy a, IEnergy b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant > b.TotalUnits;
                return a.Units > b.TotalUnits;
            }

            public static bool operator <(Energy a, IEnergy b)
            {
                return !(a > b);
            }

            public static bool operator ==(Energy a, IEnergy b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant == b.TotalUnits;
                return a.Units == b.TotalUnits;
            }

            public static bool operator !=(Energy a, IEnergy b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (obj is IEnergy) return true;
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Constant.GetHashCode() << 16 | Units.GetHashCode() >> 16;
            }

        }

    }

    public static class Velocities
    {
        public interface IVelocity : IUnit
        {
            Number TotalMetersPerSecond { get; }
        }

        public class Velocity : UnitBase, IVelocity
        {
            public const double FeetPerSecond = 3.28084;

            public const double MilesPerHour = 2.23694;

            public const double KilometersPerHour = 3.6;

            public const double Knots = 1.94384;

            public const double MetersPerSecond = 1;

            public static readonly Velocity MaxValue = new Velocity(Physics.c);//the speed of light = 299 792 458 meters per second

            static List<string> VelocitySymbols = new List<string>()
            {
                "mph",
                "fps",
                "kph",
                "mps",
            };

            public Velocity()
                : base(MetersPerSecond) { }

            public Velocity(Number metersPerSecond)
                : base(MetersPerSecond)
            {
                Units = metersPerSecond;
            }

            public Velocity(Velocity other) : base(MetersPerSecond, other) { }

            public Velocity(Number value, Velocity other) : base(MetersPerSecond, other) { Units = value; }

            protected override List<string> m_Symbols
            {
                get
                {
                    return VelocitySymbols;
                }
            }

            public virtual Number TotalMetersPerSecond
            {
                get { return Units; }
            }

            public virtual Number TotalMilesPerHour
            {
                get { return TotalMetersPerSecond * MilesPerHour; }
            }

            public virtual Number TotalFeetPerSecond
            {
                get { return TotalMetersPerSecond * FeetPerSecond; }
            }

            public virtual Number TotalKilometersPerHour
            {
                get { return TotalMetersPerSecond * KilometersPerHour; }
            }

            public static Velocity FromKnots(Number value)
            {
                return new Velocity(value.ToDouble() * Knots);
            }

            public static Velocity operator +(Velocity a, int amount)
            {
                return new Velocity(a.Units.ToDouble() + amount);
            }

            public static Velocity operator -(Velocity a, int amount)
            {
                return new Velocity(a.Units.ToDouble() - amount);
            }

            public static Velocity operator *(Velocity a, int amount)
            {
                return new Velocity(a.Units.ToDouble() * amount);
            }

            public static Velocity operator /(Velocity a, int amount)
            {
                return new Velocity(a.Units.ToDouble() / amount);
            }

            public static Velocity operator %(Velocity a, int amount)
            {
                return new Velocity(a.Units.ToDouble() % amount);
            }

            public static bool operator >(Velocity a, IVelocity b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant > b.TotalUnits;
                return a.Units > b.TotalUnits;
            }

            public static bool operator <(Velocity a, IVelocity b)
            {
                return !(a > b);
            }

            public static bool operator ==(Velocity a, IVelocity b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant == b.TotalUnits;
                return a.Units == b.TotalUnits;
            }

            public static bool operator !=(Velocity a, IVelocity b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (obj is IVelocity) return true;
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Constant.GetHashCode() << 16 | Units.GetHashCode() >> 16;
            }
        }
    }

    public static class Forces
    {
        public interface IForce : IUnit
        {
            Number TotalNewtons { get; }
        }

        /*
        newton is the unit for force
        joules is the unit for work done
        by definition, work done = force X distance
        so multiply newton by metre to get joules
        1 newton = 1 joule/meter
         */
        public class Force : UnitBase, IForce
        {

            public static Energies.Energy ToEnergy(Distances.IDistance d)
            {
                return new Energies.Energy(d.TotalMeters.ToDouble());
            }

            public static implicit operator double(Force t) { return t.Units.ToDouble(); }

            public static implicit operator Force(double t) { return new Force(t); }

            public const double Newton = 1D;

            static List<string> ForceSymbols = new List<string>()
        {
            "N"
        };

            public Force()
                : base(Newton)
            {
            }

            public Force(double celcius)
                : base(Newton)
            {
                Units = celcius;
            }

            public Force(Force other) : base(Newton, other) { }

            public Force(Number value, Force other) : base(Newton, other) { Units = value; }

            protected override List<string> m_Symbols
            {
                get
                {
                    return ForceSymbols;
                }
            }

            public virtual Number TotalNewtons
            {
                get { return Units; }
            }

            public static Force operator +(Force a, int amount)
            {
                return new Force(a.Units.ToDouble() + amount);
            }

            public static Force operator -(Force a, int amount)
            {
                return new Force(a.Units.ToDouble() - amount);
            }

            public static Force operator *(Force a, int amount)
            {
                return new Force(a.Units.ToDouble() * amount);
            }

            public static Force operator /(Force a, int amount)
            {
                return new Force(a.Units.ToDouble() / amount);
            }

            public static Force operator %(Force a, int amount)
            {
                return new Force(a.Units.ToDouble() % amount);
            }

            public static bool operator >(Force a, IForce b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant > b.TotalUnits;
                return a.Units > b.TotalUnits;
            }

            public static bool operator <(Force a, IForce b)
            {
                return !(a > b);
            }

            public static bool operator ==(Force a, IForce b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant == b.TotalUnits;
                return a.Units == b.TotalUnits;
            }

            public static bool operator !=(Force a, IForce b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (obj is IForce) return true;
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Constant.GetHashCode() << 16 | Units.GetHashCode() >> 16;
            }
        }
    }

    public static class Wavelengths
    {
        public interface IWavelength : IUnit
        {
            Distances.IDistance TotalMeters { get; }

            Frequencies.IFrequency TotalHz { get; }

            Energies.IEnergy TotalJoules { get; }

            Velocities.IVelocity TotalVelocity { get; }
        }

        /*
        newton is the unit for Wavelength
        joules is the unit for work done
        by definition, work done = Wavelength X distance
        so multiply newton by metre to get joules
        1 newton = 1 joule/meter
         */
        public class Wavelength : UnitBase, IWavelength
        {

            public static implicit operator double(Wavelength t) { return t.Units.ToDouble(); }

            public static implicit operator Wavelength(double t) { return new Wavelength(t); }

            static List<string> WavelengthSymbols = new List<string>()
            {
                "nm",
                "μm",
                "m"
            };

            public const double Nm = 1D;

            public Wavelength()
                : base(Nm)
            {
            }

            public Wavelength(Distances.Distance meters)
                : base(Nm)
            {
                Units = meters.TotalMeters * Distances.Distance.NanometersPerMeter;
            }

            public Wavelength(double nanometers)
                : base(Nm)
            {
                Units = nanometers;
            }

            public Wavelength(Frequencies.Frequency hZ)
                : base(Nm)
            {
                Units = Velocities.Velocity.MaxValue.Units.ToComplex() * hZ.TotalHertz.ToComplex();
            }

            public Wavelength(Wavelength other) : base(Nm, other) { }

            public Wavelength(Number value, Wavelength other) : base(Nm, other) { Units = value; }

            protected override List<string> m_Symbols
            {
                get
                {
                    return WavelengthSymbols;
                }
            }

            public virtual Distances.IDistance TotalMeters
            {
                get { return new Distances.Distance(Units.ToComplex() * Distances.Distance.NanometersPerMeter); }
            }

            public virtual Velocities.IVelocity TotalVelocity
            {
                get { return new Velocities.Velocity(Velocities.Velocity.MaxValue.Units.ToDouble() / Units.ToDouble()); }
            }

            public virtual Frequencies.IFrequency TotalHz
            {
                get { return new Frequencies.Frequency(TotalVelocity.TotalMetersPerSecond.ToDouble() * TotalMeters.TotalUnits.ToDouble()); }
            }

            public virtual Energies.IEnergy TotalJoules
            {
                get { return new Energies.Energy(new Number(Physics.hc / TotalMeters.TotalUnits.ToDouble())); }
            }

            public static Wavelength operator +(Wavelength a, int amount)
            {
                return new Wavelength(a.Units.ToDouble() + amount);
            }

            public static Wavelength operator -(Wavelength a, int amount)
            {
                return new Wavelength(a.Units.ToDouble() - amount);
            }

            public static Wavelength operator *(Wavelength a, int amount)
            {
                return new Wavelength(a.Units.ToDouble() * amount);
            }

            public static Wavelength operator /(Wavelength a, int amount)
            {
                return new Wavelength(a.Units.ToDouble() / amount);
            }

            public static Wavelength operator %(Wavelength a, int amount)
            {
                return new Wavelength(a.Units.ToDouble() % amount);
            }

            public static bool operator >(Wavelength a, IWavelength b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant > b.TotalUnits;
                return a.Units > b.TotalUnits;
            }

            public static bool operator <(Wavelength a, IWavelength b)
            {
                return !(a > b);
            }

            public static bool operator ==(Wavelength a, IWavelength b)
            {
                if (a.Constant != b.Constant)
                    return a.Units * b.Constant == b.TotalUnits;
                return a.Units == b.TotalUnits;
            }

            public static bool operator !=(Wavelength a, IWavelength b)
            {
                return !(a == b);
            }

            public override bool Equals(object obj)
            {
                if (obj is IWavelength) return true;
                return base.Equals(obj);
            }

            public override int GetHashCode()
            {
                return Constant.GetHashCode() << 16 | Units.GetHashCode() >> 16;
            }
        }
    }

    //Current ->     //http://en.wikipedia.org/wiki/Coulomb
}
