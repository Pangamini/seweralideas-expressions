using System;
using System.Collections.Generic;
using System.Globalization;
using SeweralIdeas.Pooling;
using UnityEngine;
namespace SeweralIdeas.Expressions
{
    public static class ExpressionFunctions
    {
        public static IExpression ResolveMethodInvocation(string name, List<IExpression> arguments)
        {
            if(name == "str")
            {
                if(arguments.Count == 1)
                {
                    if(arguments[0] is IExpression<float> arg0float)
                        return new AnonymousFunctionExpression<float, string>(name, MakeString, true, arg0float);
                    if(arguments[0] is IExpression<int> arg0int)
                        return new AnonymousFunctionExpression<int, string>(name, MakeString, true, arg0int);
                    if(arguments[0] is IExpression<bool> arg0bool)
                        return new AnonymousFunctionExpression<bool, string>(name, MakeString, true, arg0bool);
                }
                else if(arguments.Count == 2 && arguments[1] is IExpression<string> formatExp)
                {
                    if(arguments[0] is IExpression<float> arg0float)
                        return new AnonymousFunctionExpression<float,string, string>(name, MakeString, true, arg0float, formatExp);
                    if(arguments[0] is IExpression<int> arg0int)
                        return new AnonymousFunctionExpression<int, string, string>(name, MakeString, true, arg0int, formatExp);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "sin")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, Mathf.Sin, true, arg0);
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "cos")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, Mathf.Cos, true, arg0);
                ThrowNoOverloads(name, arguments);
            }

            if(name == "tan")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, Mathf.Tan, true, arg0);
                ThrowNoOverloads(name, arguments);
            }

            if(name == "asin")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, Mathf.Asin, true, arg0);
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "acos")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, Mathf.Acos, true, arg0);
                ThrowNoOverloads(name, arguments);
            }

            if(name == "atan")
            {
                if(arguments.Count == 1 && arguments[0] is IExpression<float> arg0)
                    return new AnonymousFunctionExpression<float, float>(name, Mathf.Atan, true, arg0);
                ThrowNoOverloads(name, arguments);
            }

            if(name == "atan2")
            {
                if(arguments.Count == 2 && arguments[0] is IExpression<float> arg0 && arguments[1] is IExpression<float> arg1)
                    return new AnonymousFunctionExpression<float, float, float>(name, Mathf.Atan2, true, arg0, arg1);
                ThrowNoOverloads(name, arguments);
            }

            if(name == "abs")
            {
                if(arguments.Count == 1)
                {
                    if(arguments[0] is IExpression<int> arg0i)
                        return new AnonymousFunctionExpression<int, int>(name, Mathf.Abs, true, arg0i);

                    if(arguments[0] is IExpression<float> arg0f)
                        return new AnonymousFunctionExpression<float, float>(name, Mathf.Abs, true, arg0f);
                }
            
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "max")
            {
                if(arguments.Count == 2)
                {
                    if(arguments[0] is IExpression<int> arg0i && arguments[1] is IExpression<int> arg1i)
                        return new AnonymousFunctionExpression<int, int, int>(name, Mathf.Max, true, arg0i, arg1i);
                    if(arguments[0] is IExpression<float> arg0f && arguments[1] is IExpression<float> arg1f)
                        return new AnonymousFunctionExpression<float, float, float>(name, Mathf.Max, true, arg0f, arg1f);
                }
                
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "min")
            {
                if(arguments.Count == 2)
                {
                    if(arguments[0] is IExpression<int> arg0i && arguments[1] is IExpression<int> arg1i)
                        return new AnonymousFunctionExpression<int, int, int>(name, Mathf.Min, true, arg0i, arg1i);
                    if(arguments[0] is IExpression<float> arg0f && arguments[1] is IExpression<float> arg1f)
                        return new AnonymousFunctionExpression<float, float, float>(name, Mathf.Min, true, arg0f, arg1f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "saturate")
            {
                if(arguments.Count == 1)
                {
                    if(arguments[0] is IExpression<float> arg0f)
                        return new AnonymousFunctionExpression<float, float>(name, Mathf.Clamp01, true, arg0f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "lerp")
            {
                if(arguments.Count == 3)
                {
                    if(arguments[0] is IExpression<float> arg0f && arguments[1] is IExpression<float> arg1f && arguments[2] is IExpression<float> arg2f)
                        return new AnonymousFunctionExpression<float, float, float, float>(name, Mathf.LerpUnclamped, true, arg0f, arg1f, arg2f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "unlerp")
            {
                if((arguments.Count == 3))
                {
                    if(arguments[0] is IExpression<float> arg0f && arguments[1] is IExpression<float> arg1f && arguments[2] is IExpression<float> arg2f)
                        return new AnonymousFunctionExpression<float, float, float, float>(name, InverseLerpUnclamped, true, arg0f, arg1f, arg2f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "clamp")
            {
                if((arguments.Count == 3))
                {
                    if(arguments[0] is IExpression<int> arg0i && arguments[1] is IExpression<int> arg1i && arguments[2] is IExpression<int> arg2i)
                        return new AnonymousFunctionExpression<int, int, int, int>(name, Mathf.Clamp, true, arg1i, arg2i, arg0i);
                    
                    if(arguments[0] is IExpression<float> arg0f && arguments[1] is IExpression<float> arg1f && arguments[2] is IExpression<float> arg2f)
                        return new AnonymousFunctionExpression<float, float, float, float>(name, Mathf.Clamp, true, arg1f, arg2f, arg0f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "floor")
            {
                if((arguments.Count == 1))
                {
                    if(arguments[0] is IExpression<float> arg0f)
                        return new AnonymousFunctionExpression<float, int>(name, Mathf.FloorToInt, true, arg0f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "round")
            {
                if((arguments.Count == 1))
                {
                    if(arguments[0] is IExpression<float> arg0f)
                        return new AnonymousFunctionExpression<float, int>(name, Mathf.RoundToInt, true, arg0f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "ceil")
            {
                if((arguments.Count == 1))
                {
                    if(arguments[0] is IExpression<float> arg0f)
                        return new AnonymousFunctionExpression<float, int>(name, Mathf.CeilToInt, true, arg0f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "repeat")
            {
                if((arguments.Count == 2))
                {
                    if(arguments[0] is IExpression<float> arg0f && arguments[1] is IExpression<float> arg1f)
                        return new AnonymousFunctionExpression<float, float, float>(name, Mathf.Repeat, true, arg0f, arg1f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "sign")
            {
                if((arguments.Count == 1))
                {
                    if(arguments[0] is IExpression<float> arg0f)
                        return new AnonymousFunctionExpression<float, float>(name, Mathf.Sign, true, arg0f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "sqrt")
            {
                if((arguments.Count == 1))
                {
                    if(arguments[0] is IExpression<float> arg0f)
                        return new AnonymousFunctionExpression<float, float>(name, Mathf.Sqrt, true, arg0f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "pow")
            {
                if((arguments.Count == 2))
                {
                    if(arguments[0] is IExpression<float> arg0f && arguments[1] is IExpression<float> arg1f)
                        return new AnonymousFunctionExpression<float, float, float>(name, Mathf.Pow, true, arg0f, arg1f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            if(name == "log")
            {
                if((arguments.Count == 2))
                {
                    if(arguments[0] is IExpression<float> arg0f && arguments[1] is IExpression<float> arg1f)
                        return new AnonymousFunctionExpression<float, float, float>(name, Mathf.Log, true, arg0f, arg1f);
                }
                ThrowNoOverloads(name, arguments);
            }
            
            return null;
        }

        private static string MakeString(float arg) => arg.ToString(CultureInfo.InvariantCulture);
        private static string MakeString(int arg) => arg.ToString(CultureInfo.InvariantCulture);
        private static string MakeString(float arg, string format) => arg.ToString(format, CultureInfo.InvariantCulture);
        private static string MakeString(int arg, string format) => arg.ToString(format, CultureInfo.InvariantCulture);
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
        
        public static IExpression ResolveConstant(string name)
        {
            switch (name)
            {
                case "pi": return new ConstantFloatExpression() { Value = Mathf.PI };
                case "deg2rad": return new ConstantFloatExpression() { Value = Mathf.Deg2Rad };
                case "rad2deg": return new ConstantFloatExpression() { Value = Mathf.Rad2Deg };
                case "inf": return new ConstantFloatExpression() { Value = Mathf.Infinity };
                case "epsilon": return new ConstantFloatExpression() { Value = Mathf.Epsilon };
            }
            return null;
        }
    }
}
