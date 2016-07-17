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
    /// Looks kind of like this.. (When you fold X dimensional space time onto itself uniformly 1 'dimension` at a time).
    /// 
    /// The following example is a sample 1 x horizontal reduction on 3 x 5, dimension space where the 5th dimension is expressed both in coodinates in the plane and at a given (T)ime.
    /// The 5th phase shows a basic deconstruction and is not elaborate.
    /// 
    /// The graph assumes two points, one point on a space plane which can move on the graph and another on a time plane which can move in time.
    /// 
    /// Thus the following graph attempts to depict the `inside` of the point in a 3 dimensional space and the true surface of the graph exists at angle not visible from that exposed by the graph. (outside and to the left)
    /// 
    /// As the Space collapsed to 0 there remains 1 point bound only in time of reference to the graph and only as a wave which then in a different dimension of space and time.
    /// 
    /// 012S01234S
    ///._________.  S
    ///|   |     |  1  
    ///|   |     |  2
    ///|___|_____|  3 T
    ///|   |     |  1 T 
    ///|---+-----+  2 T
    ///|_\_|_____|  3 T
    ///   \|/Ψ|*ψ|  1
    ///|---+-----+  2 T
    ///|__/|\*|*\|  3
    ///|     \|/ |  1
    ///+------+--+  2 T
    ///|     /|\ |  3    
    ///|  \|/*|*\|  1
    ///|---+-----+  2 T
    ///|__/|\_|/\|  3
    ///\   |  |  /  C
    /// *--+--+-*
    ///   \/\/
    ///    \/
    ///    
    /// Let Real = N (1)
    /// 
    /// Let Imaginary = N / 3 (0.3333333333333333) (...Uncertain Wave Area)
    /// 
    /// Let Half = Imaginary / 2 (0.1666666666666667) (Half of Uncertain Area...)
    /// 
    /// Let Whole = (Half * 4 = (0.6666666666666667)) / 2 = (0.3333333333333333)
    /// 
    /// Let One = Whole - Imaginary = (1) = Real.
    /// 
    /// </summary>
    public class S /*.  uch, pace, ell   .*/ : Media.Concepts.Classes.I.ICore
    {
        public S(Number x, Number y, Number z, 
            Number α, Number β, Number γ)
        {
            Real = new System.Numerics.Vector3(x.ToSingle(), y.ToSingle(), z.ToSingle());

            Imaginary = new System.Numerics.Vector3(α.ToSingle(), β.ToSingle(), γ.ToSingle());
        }

        public S(Number x, Number y, Number z, 
            Number α, Number β, Number γ, 
            Energies.IEnergy e)
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
        public readonly Energies.IEnergy Energy = (Energies.IEnergy)((Number)(-double.Epsilon));

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
