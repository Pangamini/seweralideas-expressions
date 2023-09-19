using System.Globalization;
using SeweralIdeas.Pooling;

namespace SeweralIdeas.Expressions
{
    public static class ExpressionFunctions
    {
        public static IExpression? ResolveMethodInvocation(string name, List<IExpression> arguments)
        {
            if(name == "str")
            {
                if(arguments.Count == 1)
                {
                    if(arguments[0] is IExpression<float> arg0Float)
                        return new AnonymousFunctionExpression<float, string>(name, MakeString, true, arg0Float);
                    if(arguments[0] is IExpression<int> arg0Int)
                        return new AnonymousFunctionExpression<int, string>(name, MakeString, true, arg0Int);
                    if(arguments[0] is IExpression<bool> arg0Bool)
                        return new AnonymousFunctionExpression<bool, string>(name, MakeString, true, arg0Bool);
                }
                else if(arguments.Count == 2 && arguments[1] is IExpression<string> formatExp)
                {
                    if(arguments[0] is IExpression<float> arg0Float)
                        return new AnonymousFunctionExpression<float,string, string>(name, MakeString, true, arg0Float, formatExp);
                    if(arguments[0] is IExpression<int> arg0Int)
                        return new AnonymousFunctionExpression<int, string, string>(name, MakeString, true, arg0Int, formatExp);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "sin")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, MathF.Sin, true, arg0);
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "cos")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, MathF.Cos, true, arg0);
                ThrowNoOverloads(name, arguments);
            }

            if(name == "tan")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, MathF.Tan, true, arg0);
                ThrowNoOverloads(name, arguments);
            }

            if(name == "asin")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, MathF.Asin, true, arg0);
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "acos")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, MathF.Acos, true, arg0);
                ThrowNoOverloads(name, arguments);
            }

            if(name == "atan")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, MathF.Atan, true, arg0);
                ThrowNoOverloads(name, arguments);
            }

            if(name == "atan2")
            {
                if(arguments.Count == 2 && arguments[0] is IExpression<float> arg0 && arguments[1] is IExpression<float> arg1)
                    return new AnonymousFunctionExpression<float, float, float>(name, MathF.Atan2, true, arg0, arg1);
                ThrowNoOverloads(name, arguments);
            }

            if(name == "abs")
            {
                if(arguments.Count == 1)
                {
                    if(arguments[0] is IExpression<int> arg0I)
                        return new AnonymousFunctionExpression<int, int>(name, Math.Abs, true, arg0I);

                    if(arguments[0] is IExpression<float> arg0F)
                        return new AnonymousFunctionExpression<float, float>(name, MathF.Abs, true, arg0F);
                }
            
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "max")
            {
                if(arguments.Count == 2)
                {
                    if(arguments[0] is IExpression<int> arg0I && arguments[1] is IExpression<int> arg1I)
                        return new AnonymousFunctionExpression<int, int, int>(name, Math.Max, true, arg0I, arg1I);
                    if(arguments[0] is IExpression<float> arg0F && arguments[1] is IExpression<float> arg1F)
                        return new AnonymousFunctionExpression<float, float, float>(name, MathF.Max, true, arg0F, arg1F);
                }
                
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "min")
            {
                if(arguments.Count == 2)
                {
                    if(arguments[0] is IExpression<int> arg0I && arguments[1] is IExpression<int> arg1I)
                        return new AnonymousFunctionExpression<int, int, int>(name, Math.Min, true, arg0I, arg1I);
                    if(arguments[0] is IExpression<float> arg0F && arguments[1] is IExpression<float> arg1F)
                        return new AnonymousFunctionExpression<float, float, float>(name, MathF.Min, true, arg0F, arg1F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "saturate")
            {
                if(arguments.Count == 1)
                {
                    if(arguments[0] is IExpression<float> arg0F)
                        return new AnonymousFunctionExpression<float, float>(name, Clamp01, true, arg0F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "lerp")
            {
                if(arguments.Count == 3)
                {
                    if(arguments[0] is IExpression<float> arg0F && arguments[1] is IExpression<float> arg1F && arguments[2] is IExpression<float> arg2F)
                        return new AnonymousFunctionExpression<float, float, float, float>(name, LerpUnclamped, true, arg0F, arg1F, arg2F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "unlerp")
            {
                if((arguments.Count == 3))
                {
                    if(arguments[0] is IExpression<float> arg0F && arguments[1] is IExpression<float> arg1F && arguments[2] is IExpression<float> arg2F)
                        return new AnonymousFunctionExpression<float, float, float, float>(name, InverseLerpUnclamped, true, arg0F, arg1F, arg2F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "clamp")
            {
                if((arguments.Count == 3))
                {
                    if(arguments[0] is IExpression<int> arg0I && arguments[1] is IExpression<int> arg1I && arguments[2] is IExpression<int> arg2I)
                        return new AnonymousFunctionExpression<int, int, int, int>(name, Math.Clamp, true, arg1I, arg2I, arg0I);
                    
                    if(arguments[0] is IExpression<float> arg0F && arguments[1] is IExpression<float> arg1F && arguments[2] is IExpression<float> arg2F)
                        return new AnonymousFunctionExpression<float, float, float, float>(name, Math.Clamp, true, arg1F, arg2F, arg0F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "floor")
            {
                if((arguments.Count == 1))
                {
                    if(arguments[0] is IExpression<float> arg0F)
                        return new AnonymousFunctionExpression<float, int>(name, FloorToInt, true, arg0F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "round")
            {
                if((arguments.Count == 1))
                {
                    if(arguments[0] is IExpression<float> arg0F)
                        return new AnonymousFunctionExpression<float, int>(name, RoundToInt, true, arg0F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "ceil")
            {
                if((arguments.Count == 1))
                {
                    if(arguments[0] is IExpression<float> arg0F)
                        return new AnonymousFunctionExpression<float, int>(name, CeilToInt, true, arg0F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "repeat")
            {
                if((arguments.Count == 2))
                {
                    if(arguments[0] is IExpression<float> arg0F && arguments[1] is IExpression<float> arg1F)
                        return new AnonymousFunctionExpression<float, float, float>(name, Repeat, true, arg0F, arg1F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "sign")
            {
                if((arguments.Count == 1))
                {
                    if(arguments[0] is IExpression<float> arg0F)
                        return new AnonymousFunctionExpression<float, int>(name, Math.Sign, true, arg0F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "sqrt")
            {
                if((arguments.Count == 1))
                {
                    if(arguments[0] is IExpression<float> arg0F)
                        return new AnonymousFunctionExpression<float, float>(name, MathF.Sqrt, true, arg0F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "pow")
            {
                if((arguments.Count == 2))
                {
                    if(arguments[0] is IExpression<float> arg0F && arguments[1] is IExpression<float> arg1F)
                        return new AnonymousFunctionExpression<float, float, float>(name, MathF.Pow, true, arg0F, arg1F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "log")
            {
                if((arguments.Count == 2))
                {
                    if(arguments[0] is IExpression<float> arg0F && arguments[1] is IExpression<float> arg1F)
                        return new AnonymousFunctionExpression<float, float, float>(name, MathF.Log, true, arg0F, arg1F);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            return null;
        }

        private static string MakeString(float arg) => arg.ToString(CultureInfo.InvariantCulture);
        private static string MakeString(int arg) => arg.ToString(CultureInfo.InvariantCulture);
        private static string MakeString(float arg, string? format) => arg.ToString(format, CultureInfo.InvariantCulture);
        private static string MakeString(int arg, string? format) => arg.ToString(format, CultureInfo.InvariantCulture);
        private static string MakeString(bool arg) => arg.ToString(CultureInfo.InvariantCulture);

        private static void ThrowNoOverloads(string name, List<IExpression> arguments)
        {
            using (StringBuilderPool.Get(out var sb))
            {
                sb.Append("No overloads for ");
                sb.Append(name);
                sb.Append(" (");
                if(arguments.Count > 0)
                {
                    sb.Append(arguments[0].ReturnType.Name);
                    for( int i = 1; i < arguments.Count; ++i )
                    {
                        sb.Append(", ");
                        sb.Append(arguments[i].ReturnType.Name);
                    }
                }
                sb.Append(")");
                throw new ExpressionCompiler.ParseException(sb.ToString());
            }
        }
        
        private static float InverseLerpUnclamped(float a, float b, float value)
        {
            if (Math.Abs(a - b) > float.Epsilon)
                return ((value - a) / (b - a));
            else
                return 0.0f;
        }
        
        public static IExpression? ResolveConstant(string name)
        {
            switch (name)
            {
                case "pi": return new ConstantExpression<float>() { Value = MathF.PI };
                case "deg2rad": return new ConstantExpression<float>() { Value = Deg2Rad };
                case "rad2deg": return new ConstantExpression<float>() { Value = Rad2Deg };
                case "inf": return new ConstantExpression<float>() { Value = float.PositiveInfinity };
                case "epsilon": return new ConstantExpression<float>() { Value = float.Epsilon };
            }
            return null;
        }
        
        private const float Deg2Rad = (MathF.PI * 2) / 360.0f;
        private const float Rad2Deg = 1f / Deg2Rad;
        
        public static float LerpUnclamped(float a, float b, float t) => a + (b - a) * t;
        
        public static float Clamp01(float value)
        {
            return value switch
            {
                < 0F => 0F,
                > 1F => 1F,
                _ => value
            };
        }

        public static float Repeat(float t, float length)
        {
            return Clamp(t - MathF.Floor(t / length) * length, 0.0f, length);
        }
        
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
                value = min;
            else if (value > max)
                value = max;
            return value;
        }
        
        public static int CeilToInt(float f) => (int)Math.Ceiling(f);

        public static int FloorToInt(float f) => (int)Math.Floor(f);

        public static int RoundToInt(float f) => (int)Math.Round(f);
    }
}
