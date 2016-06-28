using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nVideo.Common
{
    public class RationalLarge {

    public static  Rational ONE = new Rational(1, 1);
    public static  Rational HALF = new Rational(1, 2);
    public static  RationalLarge ZERO = new RationalLarge(0, 1);

    internal long num;
     internal long den;

    public RationalLarge(long num, long den) {
        this.num = num;
        this.den = den;
    }

    public long getNum() {
        return num;
    }

    public long getDen() {
        return den;
    }

    public static Rational parse(String sstring) {
        String[] split = sstring.Split(':'); //split(sstring, ":");
        return new Rational(int.Parse(split[0]), int.Parse(split[1]));
    }

    public override int GetHashCode() {
         int prime = 31;
        int result = 1;
        result = prime * result + (int) (den ^ (den >> 32));
        result = prime * result + (int) (num ^ (num >> 32));
        return result;
    }

    public override bool Equals(Object obj) {
        if (this == obj)
            return true;
        if (obj == null)
            return false;
        //if (getClass() != obj.getClass())
            //return false;
        RationalLarge other = (RationalLarge) obj;
        if (den != other.den)
            return false;
        if (num != other.num)
            return false;
        return true;
    }

    public long multiplyS(long scalar) {
        return (num * scalar) / den;
    }

    public long divideS(long scalar) {
        return (den * scalar) / num;
    }

    public long divideByS(long scalar) {
        return num / (den * scalar);
    }

    public RationalLarge flip() {
        return new RationalLarge(den, num);
    }

    public static RationalLarge R(long num, long den) {
        return new RationalLarge(num, den);
    }

    public static RationalLarge R(long num) {
        return R(num, 1);
    }

    public bool lessThen(RationalLarge sec) {
        return num * sec.den < sec.num * den;
    }

    public bool greaterThen(RationalLarge sec) {
        return num * sec.den > sec.num * den;
    }

    public bool smallerOrEqualTo(RationalLarge sec) {
        return num * sec.den <= sec.num * den;
    }

    public bool greaterOrEqualTo(RationalLarge sec) {
        return num * sec.den >= sec.num * den;
    }

    public bool equals(RationalLarge other) {
        return num * other.den == other.num * den;
    }


    public static Rational reduce(Rational r)
    {
        return reduce(r.getNum(), r.getDen());
    }

    public static int gcd(int a, int b)
    {
        if (b != 0)
            return gcd(b, a % b);
        else
            return a;
    }

    public static long gcd(long a, long b)
    {
        if (b != 0)
            return gcd(b, a % b);
        else
            return a;
    }

    public static Rational reduce(int num, int den)
    {
        int gcds = gcd(num, den);
        return new Rational(num / gcds, den / gcds);
    }

    public static RationalLarge reduce(long num, long den)
    {
        long gcds = gcd(num, den);
        return new RationalLarge(num / gcds, den / gcds);
    }


    public RationalLarge plus(RationalLarge other) {
        return reduce(num * other.den + other.num * den, den * other.den);
    }

    public RationalLarge plus(Rational other) {
        return reduce(num * other.den + other.num * den, den * other.den);
    }

    public RationalLarge minus(RationalLarge other) {
        return reduce(num * other.den - other.num * den, den * other.den);
    }

    public RationalLarge minus(Rational other) {
        return reduce(num * other.den - other.num * den, den * other.den);
    }

    public RationalLarge plus(long scalar) {
        return new RationalLarge(num + scalar * den, den);
    }

    public RationalLarge minus(long scalar) {
        return new RationalLarge(num - scalar * den, den);
    }

    public RationalLarge multiply(long scalar) {
        return new RationalLarge(num * scalar, den);
    }

    public RationalLarge divide(long scalar) {
        return new RationalLarge(den * scalar, num);
    }

    public RationalLarge divideBy(long scalar) {
        return new RationalLarge(num, den * scalar);
    }

    public RationalLarge multiply(RationalLarge other) {
        return reduce(num * other.num, den * other.den);
    }

    public RationalLarge multiply(Rational other) {
        return reduce(num * other.num, den * other.den);
    }

    public RationalLarge divide(RationalLarge other) {
        return reduce(other.num * den, other.den * num);
    }

    public RationalLarge divide(Rational other) {
        return reduce(other.num * den, other.den * num);
    }

    public RationalLarge divideBy(RationalLarge other) {
        return reduce(num * other.den, den * other.num);
    }

    public RationalLarge divideBy(Rational other) {
        return reduce(num * other.den, den * other.num);
    }

    public double scalar() {
        return ((double) num) / den;
    }

    public long scalarClip() {
        return num / den;
    }
}
}
