using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace DiscordBot.SemanticKernel.Plugins.Math
{
    public sealed class MathPlugin
    {
        [KernelFunction, Description("Take the square root of a number")]
        public static double Sqrt(
            [Description("The number to take a square root of")] object number1
        )
        {
            return System.Math.Sqrt(double.Parse((string)number1));
        }

        [KernelFunction, Description("Add two numbers")]
        public static double Add(
            [Description("The first number to add")] object number1,
            [Description("The second number to add")] object number2
        )
        {
            return double.Parse((string)number1) + double.Parse((string)number2);
        }

        [KernelFunction, Description("Subtract two numbers")]
        public static double Subtract(
            [Description("The first number to subtract from")] object number1,
            [Description("The second number to subtract away")] object number2
        )
        {
            return double.Parse((string)number1) - double.Parse((string)number2);
        }

        [KernelFunction, Description("Multiply two numbers. When increasing by a percentage, don't forget to add 1 to the percentage.")]
        public static double Multiply(
            [Description("The first number to multiply")] object number1,
            [Description("The second number to multiply")] object number2
        )
        {
            return double.Parse((string)number1) * double.Parse((string)number2);
        }

        [KernelFunction, Description("Divide two numbers")]
        public static double Divide(
            [Description("The first number to divide from")] object number1,
            [Description("The second number to divide by")] object number2
        )
        {
            return double.Parse((string)number1) / double.Parse((string)number2);
        }

        [KernelFunction, Description("Raise a number to a power")]
        public static double Power(
            [Description("The number to raise")] object number1,
            [Description("The power to raise the number to")] object number2
        )
        {
            return System.Math.Pow(double.Parse((string)number1), double.Parse((string)number2));
        }

        [KernelFunction, Description("Take the log of a number")]
        public static double Log(
            [Description("The number to take the log of")] object number1,
            [Description("The base of the log")] object number2
        )
        {
            return System.Math.Log(double.Parse((string)number1), double.Parse((string)number2));
        }

        [KernelFunction, Description("Round a number to the target number of decimal places")]
        public static double Round(
            [Description("The number to round")] object number1,
            [Description("The number of decimal places to round to")] object number2
        )
        {
            return System.Math.Round(double.Parse((string)number1), (int)double.Parse((string)number2));
        }

        [KernelFunction, Description("Take the absolute value of a number")]
        public static double Abs(
            [Description("The number to take the absolute value of")] object number1
        )
        {
            return System.Math.Abs(double.Parse((string)number1));
        }

        [KernelFunction, Description("Take the floor of a number")]
        public static double Floor(
            [Description("The number to take the floor of")] object number1
        )
        {
            return System.Math.Floor(double.Parse((string)number1));
        }

        [KernelFunction, Description("Take the ceiling of a number")]
        public static double Ceiling(
            [Description("The number to take the ceiling of")] object number1
        )
        {
            return System.Math.Ceiling(double.Parse((string)number1));
        }

        [KernelFunction, Description("Take the sine of a number")]
        public static double Sin(
            [Description("The number to take the sine of")] object number1
        )
        {
            return System.Math.Sin(double.Parse((string)number1));
        }

        [KernelFunction, Description("Take the cosine of a number")]
        public static double Cos(
            [Description("The number to take the cosine of")] object number1
        )
        {
            return System.Math.Cos(double.Parse((string)number1));
        }

        [KernelFunction, Description("Take the tangent of a number")]
        public static double Tan(
            [Description("The number to take the tangent of")] object number1
        )
        {
            return System.Math.Tan(double.Parse((string)number1));
        }

        [KernelFunction, Description("Take the arcsine of a number")]
        public static double Asin(
            [Description("The number to take the arcsine of")] object number1
        )
        {
            return System.Math.Asin(double.Parse((string)number1));
        }

        [KernelFunction, Description("Take the arccosine of a number")]
        public static double Acos(
            [Description("The number to take the arccosine of")] object number1
        )
        {
            return System.Math.Acos(double.Parse((string)number1));
        }

        [KernelFunction, Description("Take the arctangent of a number")]
        public static double Atan(
            [Description("The number to take the arctangent of")] object number1
        )
        {
            return System.Math.Atan(double.Parse((string)number1));
        }
    }
}
