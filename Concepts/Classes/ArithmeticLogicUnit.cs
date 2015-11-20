//http://www.codeproject.com/script/Articles/ViewDownloads.aspx?aid=141650

namespace Media.Concepts.Classes
{
    /// <summary>
    /// Defines functionality for Arithmetic conventions
    /// </summary>
    [System.CLSCompliant(false)]
    public static class ArithmeticLogicUnit
    {
        const int Mask = 1 << 30;

        public static uint Adder(uint Addend_One, uint Addend_Two)
        {
            uint Carry;

            while (Addend_Two != 0)
            {
                Carry = Addend_One & Addend_Two;
                Addend_One ^= Addend_Two;
                Addend_Two = Carry << 1;
            }
            return Addend_One;
        }

        public static uint Subtractor(uint Subtrahend, uint Minuend)
        {
            Minuend = Adder(~Minuend, 1);
            return Adder(Subtrahend, Minuend);
        }

        public static uint Multiplier(uint Factor_One, uint Factor_Two)
        {
            uint Ans;
            Ans = 0;
            while (Factor_Two != 0)
            {
                if ((Factor_Two & 1) == 1)
                {
                    Ans = Adder(Ans, Factor_One);
                }
                Factor_Two >>= 1;
                Factor_One <<= 1;
            }
            return Ans;
        }

        public static uint Divider(uint Dividend, uint Divisor)
        {
            uint Quotient, Hold, Temp;

            Hold = Divisor;
            Quotient = 0;
            while (Hold < Mask && Hold < Dividend)  // Should always be 1 << (BitsPerInteger - 2);
            {
                Hold <<= 1;
            }
            while (Hold >= Divisor)
            {
                Quotient <<= 1;
                if (Dividend >= Hold)
                {
                    Temp = Adder(~Hold, 1);
                    Dividend = Adder(Dividend, Temp);
                    Quotient |= 1;
                }
                Hold >>= 1;
            }
            return Quotient;
        }

        public static uint Modulus(uint Dividend, uint Divisor)
        {
            uint Hold, Temp;

            Hold = Divisor;
            while (Hold < Mask && Hold < Dividend)   // Should always be 1 << (BitsPerInteger - 2);  
            {
                Hold <<= 1;
            }
            while (Hold >= Divisor)
            {
                if (Dividend >= Hold)
                {
                    Temp = Adder(~Hold, 1);
                    Dividend = Adder(Dividend, Temp);
                }
                Hold >>= 1;
            }
            return Dividend;
        }

        public static uint SquareRoot(uint Square)
        {
            uint Root, Temp, One;
            Root = 0;
            One = Mask;  // 1 << (BitSize - 2);
            while (One > Square)
            {
                One >>= 2;
            }
            while (One != 0)
            {
                Temp = Adder(Root, One);
                Root >>= 1;
                if (Square >= Temp)
                {
                    Square = Subtractor(Square, Temp);
                    Root = Adder(Root, One);
                }
                One >>= 2;
            }
            return Root;
        }

        public static uint Logarithm(uint Num, uint Base)
        {
            uint Exp;

            Exp = 0;
            while (Num >= Base)
            {
                Num = Divider(Num, Base);
                Exp = Adder(Exp, 1);
            }
            return Exp;
        }

        public static uint BinaryLogarithm(uint Num)
        {
            uint Exp;

            Exp = 0;
            while (Num >= 2)
            {
                Num >>= 1;
                Exp = Adder(Exp, 1);
            }
            return Exp;
        }


    }
}
