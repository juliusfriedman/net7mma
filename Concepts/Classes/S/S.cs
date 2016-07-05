using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Media.Concepts.Classes.S
{
    /// <summary>
    ///  | = Real
    ///  - = Real
    ///  + = Real / Imaginary
    ///  / = Imaginary
    ///  \ = Imaginary
    ///    = Imaginary (is unused)
    ///    = Real / Imaginary
    ///  
    ///  X, Y (z) are `axis - lines` and thus cannot move.
    ///  Points are defined on a `axis - line`.
    ///  Points can move anywhere relative to the line.
    ///  Points can only be in one place at one time.
    ///  A point can expand into a wave but it must collapse into a point.
    ///  When a point expands into a wave it can represent multiple points => (Ψψ)
    ///  When a wave collapses into a point it must choose a definite form => Ψ, ψ, |, or ` ` 
    /// 
    ///  Points can move but cannot share space except with Waves
    ///  Waves can move anywhere within the extent of their form and can share space
    /// 
    ///  When a Point is moving the `axis - lines` do not move and thus create relative hidden dimensions when can only be observed at a specific frequency.
    /// 
    /// Looks kind of like this..
    /// 
    ///._________.
    ///|   |     |    
    ///|   |     |
    ///|___|_____| 
    ///|   |     |    
    ///|---+-----+
    ///|_\_|_____| 
    ///   \|/Ψ|*ψ|
    ///|---+-----+
    ///|__/|\*|*\|
    ///|     \|/ |
    ///+------+--+
    ///|     /|\ |      
    ///|  \|/*|*\|
    ///|---+-----+
    ///|__/|\_|/\|
    ///
    /// Let Real = N (1)
    /// 
    /// Let Imaginary = N / 3 (0.3333333333333333) (...Uncertain Wave Area)
    /// 
    /// Let Half = Imaginary / 2 (0.1666666666666667) (Half of Uncertain Area...)
    /// 
    /// Let Whole = (Half * 4 = (0.6666666666666667)) / 2 = (0.3333333333333333)
    /// 
    /// Let One = Whole - Imaginary
    /// 
    /// </summary>
    public class S /*.  uch, pace, ell   .*/ : Classes.C.IC
    {
        public S(Number x, Number y, Number z, Number α, Number β, Number γ)
        {
            Real = new System.Numerics.Vector3(x.ToSingle(), y.ToSingle(), z.ToSingle());

            Imaginary = new System.Numerics.Vector3(α.ToSingle(), β.ToSingle(), γ.ToSingle());
        }

        public S(Number x, Number y, Number z, Number α, Number β, Number γ, Energies.IEnergy e)
            : this(x, y, z, α, β, γ)
        {
            Energy = e;
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <remarks>Should be properties or methods</remarks>
        System.Numerics.Vector3 Real, Imaginary;

        /// <summary>
        /// `e`
        /// </summary>
        public readonly Energies.IEnergy Energy = Energies.Energy.One;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="f"></param>
        /// <param name="imaginary"></param>
        /// <returns></returns>
        static public System.Numerics.Vector3 Measure(S s, Frequencies.IFrequency f, out System.Numerics.Vector3 imaginary)
        {
            System.Numerics.Vector3 measure = new System.Numerics.Vector3(f.TotalMegahertz * s.Energy.TotalJoules);

            imaginary = s.Imaginary - measure;

            return s.Real - measure;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="s"></param>
        /// <param name="f"></param>
        /// <param name="imaginary"></param>
        /// <returns></returns>
        static public S Entangle(S s, Frequencies.IFrequency f, out System.Numerics.Vector3 imaginary)
        {
            imaginary = s.Real;

            s.Real = s.Imaginary * f.TotalMegahertz;

            s.Imaginary = imaginary * f.TotalMegahertz;

            return s;
        }

    }
}
