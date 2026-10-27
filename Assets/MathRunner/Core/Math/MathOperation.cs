using System;

namespace MathRunner.Core.Math
{
    [Flags]
    public enum MathOperation
    {
        None = 0,
        Add = 1,
        Subtract = 2,
        Multiply = 4,
        Divide = 8,
    }
}
