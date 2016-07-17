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
namespace Media.Concepts.Classes
{
    /// <summary>
    /// http://physics.nist.gov/cuu/Constants/Table/allascii.txt
    /// </summary>
    public sealed class Physics
    {
        public sealed class Uncertainties
        {

            //Todo, should be Number to avoid readonly ValueType

            public const double Exact = 0D;

            public static readonly double StandardUncertainty = 0.000097 * System.Math.Pow(10, -35);

            public const double NewtonUncertainty = 0.0010e-11;

            //Newtonian constant of gravitation over h-bar c
            public const double NewtonUncertaintyOverHbarC = 0.0010e-39;

            //Aka RelationshipUncertainty
            public const double EquivelanceUncertainty = 0.00000026e-10;

            public const double MassUncertainty = 0.0000029;

            public const double AlphaParticleMassUncertainty = 0.00000029e-27;

        }

        //Maths...

        public class Probability { }

        public sealed class Probabilities
        {
            public const Probability Default = null;
        }

        //Angles?

        //To define useful classed with names which can encapsulte the endless list of possibilities for values to ensure intellisense overload does not happen to us.

        //Todo, should be Number to avoid readonly ValueType

        /// <summary>
        /// The speed of light in mps
        /// </summary>
        public const double c = 299792458D; //Need ToMilesPerSecond on Velocities? 
        //Maybe a ToUnitPerPeriod(Distance d, TimeSpan t)?
        //Could also do a static Distance.FromMilesPerSeconds as once the instance is in the form of Units it can be changed to other formats using the propeties.

        //Todo should be Current Or Electric Charge, Columbs

        /*
         
         In terms of SI base units, the coulomb is the equivalent of one ampere-second. 
         Conversely, an electric current of A represents 1 C of unit electric charge carriers flowing past a specific point in 1 s. 
         The unit electric charge is the amount of charge contained in a single electron.
         
         */

        //Planck constant in J·m
        public static double hc = 1.98644568 * System.Math.Pow(10, -25);



        /// <summary>
        /// The Planck Length in?? (meters)
        /// </summary>
        public static Distances.Distance ℓP = new Distances.Distance(new System.Numerics.Complex(1.616199 * System.Math.Pow(10, -35), Uncertainties.StandardUncertainty));

        //Need Weights

        /// <summary>
        /// {220} lattice spacing of silicon                            192.015 5714 e-12        0.000 0032 e-12          m
        /// </summary>
        public static Distances.Distance LatticeSpacingOfSilicon = new Distances.Distance(new System.Numerics.Complex(192.0155714e-12, Uncertainties.EquivelanceUncertainty));

        //alpha particle-electron mass ratio                          7294.299 5361            0.000 0029               

        //alpha particle-electron mass ratio                          7294.299 5361            0.000 0029

        //alpha particle mass in kk                                       6.644 656 75 e-27        0.000 000 29 e-27        kg
        public static Masses.Mass AlphaParticleMass = new Masses.Mass(new System.Numerics.Complex(16.64465675e-27, Uncertainties.AlphaParticleMassUncertainty));

        //Needs Atomic Mass
        //alpha particle mass in u                                    4.001 506 179 125        0.000 000 000 062        u
        //public static Masses.Mass AlphaParticleMass  = Masses.Mass.FromAtomicMasses(new System.Numerics.Complex(4.001506179125, 0.000000000062));

        /// <summary>
        /// alpha particle mass energy equivalent                       5.971 919 67 e-10        0.000 000 26 e-10        J
        /// </summary>
        public static Energies.Energy AlphaParticleMassEnergyEquivalent = new Energies.Energy(new System.Numerics.Complex(5.97191967e-10, Uncertainties.EquivelanceUncertainty));


        //alpha particle mass energy equivalent in MeV                3727.379 240             0.000 082                MeV
        //Needs MeV
        //public static Number MeV = new Number(new System.Numerics.Complex(3727.379240, 0.000082));



        //alpha particle molar mass                                   4.001 506 179 125 e-3    0.000 000 000 062 e-3    kg mol^-1
        //alpha particle-proton mass ratio                            3.972 599 689 33         0.000 000 000 36         

        /*
       Angstrom star                                               1.000 014 95 e-10        0.000 000 90 e-10        m
       atomic mass constant                                        1.660 538 921 e-27       0.000 000 073 e-27       kg
       atomic mass constant energy equivalent                      1.492 417 954 e-10       0.000 000 066 e-10       J
       atomic mass unit-electron volt relationship                 931.494 061 e6           0.000 021 e6             eV
       atomic mass unit-hartree relationship                       3.423 177 6845 e7        0.000 000 0024 e7        E_h
       atomic mass unit-hertz relationship                         2.252 342 7168 e23       0.000 000 0016 e23       Hz
       atomic mass unit-inverse meter relationship                 7.513 006 6042 e14       0.000 000 0053 e14       m^-1
       atomic mass unit-joule relationship                         1.492 417 954 e-10       0.000 000 066 e-10       J
       atomic mass unit-kelvin relationship                        1.080 954 08 e13         0.000 000 98 e13         K
       atomic mass unit-kilogram relationship                      1.660 538 921 e-27       0.000 000 073 e-27       kg
       atomic unit of 1st hyperpolarizability                      3.206 361 449 e-53       0.000 000 071 e-53       C^3 m^3 J^-2
       atomic unit of 2nd hyperpolarizability                      6.235 380 54 e-65        0.000 000 28 e-65        C^4 m^4 J^-3
       atomic unit of action                                       1.054 571 726 e-34       0.000 000 047 e-34       J s
       atomic unit of charge                                       1.602 176 565 e-19       0.000 000 035 e-19       C
       atomic unit of charge density                               1.081 202 338 e12        0.000 000 024 e12        C m^-3
       atomic unit of current                                      6.623 617 95 e-3         0.000 000 15 e-3         A
       atomic unit of electric dipole mom.                         8.478 353 26 e-30        0.000 000 19 e-30        C m
       atomic unit of electric field                               5.142 206 52 e11         0.000 000 11 e11         V m^-1
       atomic unit of electric field gradient                      9.717 362 00 e21         0.000 000 21 e21         V m^-2
       atomic unit of electric polarizability                      1.648 777 2754 e-41      0.000 000 0016 e-41      C^2 m^2 J^-1
       atomic unit of electric potential                           27.211 385 05            0.000 000 60             V
       atomic unit of electric quadrupole mom.                     4.486 551 331 e-40       0.000 000 099 e-40       C m^2
       atomic unit of energy                                       4.359 744 34 e-18        0.000 000 19 e-18        J
       atomic unit of force                                        8.238 722 78 e-8         0.000 000 36 e-8         N
       atomic unit of length                                       0.529 177 210 92 e-10    0.000 000 000 17 e-10    m
       atomic unit of mag. dipole mom.                             1.854 801 936 e-23       0.000 000 041 e-23       J T^-1
       atomic unit of mag. flux density                            2.350 517 464 e5         0.000 000 052 e5         T
       atomic unit of magnetizability                              7.891 036 607 e-29       0.000 000 013 e-29       J T^-2
       atomic unit of mass                                         9.109 382 91 e-31        0.000 000 40 e-31        kg
       atomic unit of mom.um                                       1.992 851 740 e-24       0.000 000 088 e-24       kg m s^-1
       atomic unit of permittivity                                 1.112 650 056... e-10    (exact)                  F m^-1
       atomic unit of time                                         2.418 884 326 502 e-17   0.000 000 000 012 e-17   s
       atomic unit of velocity                                     2.187 691 263 79 e6      0.000 000 000 71 e6      m s^-1
       */


        //Hartree energy                                              4.359 744 34 e-18        0.000 000 19 e-18        J

        //hartree-hertz relationship                                  6.579 683 920 729 e15    0.000 000 000 033 e15    Hz

    }
}
