using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Diagnostics.CodeAnalysis;
using SeweralIdeas.Pooling;

namespace SeweralIdeas.Expressions
{
    public static class ExpressionCompiler
    {
        public static readonly IResolver DefaultResolver = new Resolver();

        [Flags]
        public enum Options
        {
            None = 0,
            OptimizeConstants = 1 << 0
        }

        public const Options DefaultOptions = Options.OptimizeConstants;
        
        public interface IResolver
        {
            IExpression ResolveMethodInvocation(string invocationName, List<IExpression> arguments);
            IExpression ResolveVariable(string namedObjName);
        }

        private struct State
        {
            public int pos;
            public string expText;
            public IResolver resolver;
        }

        public class ParseException : Exception
        {
            public ParseException(string message) : base(message)
            {
            }
        }


        public static IExpression<T> Parse<T>([NotNull] string expText, [NotNull] IResolver resolver, Options options = DefaultOptions)
        {
            IExpression exp = Parse(expText, resolver, options);

            if(exp is IExpression<T> tExp)
                return tExp;

            throw new ParseException($"Invalid return type '{exp.ReturnType.Name}', expected '{typeof( T ).Name}'");
        }

        public static IExpression<T> Parse<T>([NotNull] string expText, Options options = DefaultOptions) => Parse<T>(expText, DefaultResolver, options);

        public static IExpression Parse([NotNull] string expText, Options options = DefaultOptions) => Parse(expText, DefaultResolver, options);

        public static IExpression Parse([NotNull] string expText, [NotNull] IResolver resolver, Options options = DefaultOptions)
        {
            if(expText == null)
                throw new ArgumentNullException(nameof(expText));

            int pos = 0;

            var state = new State()
            {
                pos = 0,
                expText = expText,
                resolver = resolver
            };

            if(!ParseSubExpression(ref state, false, out IExpression expression, out bool endedWithComma) || endedWithComma)
            {
                if(endedWithComma)
                    throw new ParseException("Unexpected comma");
                else
                    throw new ParseException("Parsing error");
            }

            if(HasOption(options,Options.OptimizeConstants))
            {
                expression = expression.SimplifyIfPure(out var pure);
            }

            return expression;
        }

        private static bool HasOption(Options options, Options option)
        {
            return (options & option) == option;
        }

        private static bool ParseArguments(ref State state, List<IExpression> outArgs)
        {

            var startState = state;
            bool endedWithComma = false;
            while(true)
            {
                if(!ParseToken(ref state, out var token))
                    throw new ParseException("Expected argument or ')'");

                if(token.type == Token.Type.BracketClose)
                {
                    break;
                }

                bool end = ParseSubExpression(ref startState, true, out var expression, out endedWithComma);
                outArgs.Add(expression);
                state.pos = startState.pos;
                if(end)
                {
                    break;
                }

            }

            if(endedWithComma)
                throw new ParseException("Expected argument");

            return true;
        }

        private static bool ParseSubExpression(ref State state, bool endsWithBracket, out IExpression output, out bool endedWithComma)
        {
            endedWithComma = false;
            output = default;
            bool endedWithBracket = false;

            using (UnityEngine.Pool.ListPool<OperatorOrOperand>.Get(out var elements))
            {
                if(!ReadTokens(ref state, ref endedWithComma, elements, ref endedWithBracket))
                    return false;

                ReplaceOperatorsWithExpressions(ref state, elements);

                foreach (var element in elements)
                {
                    if(element.IsOperator)
                        throw new ParseException("Failed to process the expression. There are some unprocessed operators left");
                }

                if(elements.Count != 1)
                {
                    throw new ParseException("Parsing error");
                }
                output = elements[0].Operand;

                return endsWithBracket == endedWithBracket;
            }
        }
        private static bool ReadTokens(ref State state, ref bool endedWithComma, List<OperatorOrOperand> elements, ref bool endedWithBracket)
        {
            while(ParseToken(ref state, out Token token))
            {
                if(token.type == Token.Type.Comma)
                {
                    endedWithComma = true;
                    break;
                }

                if(token.type == Token.Type.BracketOpen)
                {
                    if(elements.Count > 0 && elements[^1].IsOperand && elements[^1].Operand is NamedObjectExpression invocation)
                    {
                        using (UnityEngine.Pool.ListPool<IExpression>.Get(out var arguments))
                        {
                            if(ParseArguments(ref state, arguments))
                            {
                                var methodInvocation = state.resolver.ResolveMethodInvocation(invocation.Name, arguments);
                                if(methodInvocation == null)
                                    throw new ParseException($"Cannot resolve method name '{invocation.Name}'");

                                elements[^1] = new OperatorOrOperand(methodInvocation);
                                continue;
                            }
                            else
                            {
                                return false;
                            }
                        }

                        continue;
                    }
                    else
                    {
                        if(ParseSubExpression(ref state, true, out var subExpression, out bool comma))
                        {
                            if(comma)
                                throw new ParseException("Unexpected comma");
                            elements.Add(new OperatorOrOperand(subExpression));
                        }
                        else
                            return false;
                    }
                }

                if(token.type == Token.Type.BracketClose)
                {
                    endedWithBracket = true;
                    break;
                }

                if(token.type == Token.Type.Operator)
                {
                    elements.Add(new OperatorOrOperand(token.op));
                    continue;
                }

                if(token.type == Token.Type.StringLiteral)
                {
                    if(elements.Count > 0 && elements[^1].IsOperand)
                        throw new ParseException("Expected operator");
                    elements.Add(new OperatorOrOperand(new ConstantStringExpression() { Value = token.text }));
                    continue;
                }

                if(token.type == Token.Type.Operand)
                {
                    if(elements.Count > 0 && elements[^1].IsOperand)
                        throw new ParseException("Expected operator");
                    elements.Add(new OperatorOrOperand(TextToOperand(token.text)));
                    continue;
                }
            }
            return true;
        }
        private static void ReplaceOperatorsWithExpressions(ref State state, List<OperatorOrOperand> elements)
        {

            // variables
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperand)
                    continue;

                var operand = elements[i].Operand;

                if(operand is NamedObjectExpression namedObj)
                {
                    var newOperand = state.resolver.ResolveVariable(namedObj.Name);
                    if(newOperand == null)
                    {
                        throw new ParseException($"Unrecognized symbol '{namedObj.Name}'");
                    }

                    elements[i] = new(newOperand);
                }
            }

            // not operator
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperator)
                    continue;

                Operator op = elements[i].Operator;
                if(op is Operator.Not)
                {
                    var operand = elements.GetExpectedOperand(i + 1);
                    if(!(operand is IExpression<bool> boolOperand))
                    {
                        throw new ParseException($"Operator '!' can not be used with operand of type '{operand.ReturnType.Name}'");
                    }
                    elements.RemoveRange(i, 2);
                    elements.Insert(i, new(new NotExpression() { Operand = boolOperand }));
                }
            }

            // unary plus/minus operator
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperator)
                    continue;

                var op = elements[i].Operator;
                if(i - 1 >= 0 && elements[i - 1].IsOperand)
                    continue;

                if(op is Operator.Minus)
                {
                    var operand = elements.GetExpectedOperand(i + 1);
                    IExpression newExpression;
                    if(operand is IExpression<int> intExp)
                        newExpression = new NegateIntExpression { Operand = intExp };
                    else if(operand is IExpression<float> floatExp)
                        newExpression = new NegateFloatExpression { Operand = floatExp };
                    else
                        throw new ParseException($"Operator '-' cannot be used with operand of type '{operand.ReturnType.Name}'");

                    elements.RemoveRange(i, 2);
                    elements.Insert(i, new(newExpression));
                }
                else if(op is Operator.Plus)
                {
                    var operand = elements.GetExpectedOperand(i + 1);
                    IExpression newExpression;
                    if(operand is IExpression<int> intExp)
                        newExpression = intExp;
                    else if(operand is IExpression<float> floatExp)
                        newExpression = floatExp;
                    else
                        throw new ParseException($"Operator '+' cannot be used with operand of type '{operand.ReturnType.Name}'");

                    elements.RemoveRange(i, 2);
                    elements.Insert(i, new(newExpression));
                }

            }

            // multiplicative operators
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperator)
                    continue;

                Operator op = elements[i].Operator;
                if(op is Operator.Mul or Operator.Div or Operator.Mod)
                {
                    MakeArithmeticOperatorExpression(elements, ref i);
                }
            }


            // additive operators
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperator)
                    continue;

                Operator op = elements[i].Operator;
                if(op is Operator.Plus or Operator.Minus)
                {
                    MakeArithmeticOperatorExpression(elements, ref i);
                }
            }


            // relational operators
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperator)
                    continue;

                Operator op = elements[i].Operator;
                if(op is Operator.Greater or Operator.GEquals or Operator.Less or Operator.LEquals)
                {
                    MakeRelationalOperatorExpression(elements, ref i);
                }
            }

            // equality operators
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperator)
                    continue;

                Operator op = elements[i].Operator;
                if(op is Operator.Equals or Operator.NEquals)
                {
                    MakeRelationalOperatorExpression(elements, ref i);
                }
            }


            // and operator
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperator)
                    continue;

                Operator op = elements[i].Operator;
                if(op is Operator.And)
                {
                    MakeLogicalOperatorExpression(elements, ref i);
                }
            }

            // or operator
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperator)
                    continue;

                Operator op = elements[i].Operator;
                if(op is Operator.Or)
                {
                    MakeLogicalOperatorExpression(elements, ref i);
                }
            }

            // xor operator
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperator)
                    continue;

                Operator op = elements[i].Operator;
                if(op is Operator.Xor)
                {
                    MakeLogicalOperatorExpression(elements, ref i);
                }
            }


            // condition operatop
            for( int i = 0; i < elements.Count; ++i )
            {
                if(!elements[i].IsOperator)
                    continue;

                Operator op = elements[i].Operator;
                if(op is Operator.Condition)
                {
                    MakeConditionalOperatorExpression(elements, ref i);
                }
            }
        }

        private static void MakeConditionalOperatorExpression(List<OperatorOrOperand> elements, ref int i)
        {
            Operator op = elements.GetExpectedOperator(i, Operator.Condition);
            Operator op2 = elements.GetExpectedOperator(i + 2, Operator.Branch);

            var condition = elements.GetExpectedOperand(i - 1);
            var trueBranch = elements.GetExpectedOperand(i + 1);
            var falseBranch = elements.GetExpectedOperand(i + 3);

            IExpression operatorExpression = null;

            if(!(condition is IExpression<bool> conditionBool))
            {
                throw new ParseException($"Conditional operator '?' can not be used with operand of type '{condition.ReturnType.Name}'");
            }

            if(trueBranch is IExpression<bool> trueBool && falseBranch is IExpression<bool> falseBool)
            {
                operatorExpression = new SelectExpression<bool>()
                {
                    Condition = conditionBool,
                    IfTrue = trueBool,
                    IfFalse = falseBool
                };
            }

            else if(trueBranch is IExpression<int> trueInt && falseBranch is IExpression<int> falseInt)
            {
                operatorExpression = new SelectIntExpression()
                {
                    Condition = conditionBool,
                    IfTrue = trueInt,
                    IfFalse = falseInt
                };
            }

            else if(trueBranch is IExpression<float> trueFloat && falseBranch is IExpression<float> falseFloat)
            {
                operatorExpression = new SelectExpression<float>()
                {
                    Condition = conditionBool,
                    IfTrue = trueFloat,
                    IfFalse = falseFloat
                };
            }

            if(operatorExpression == null)
            {
                throw new ParseException($"Cannot determine the type of conditional expression for '{trueBranch.ReturnType.Name}' and '{falseBranch.ReturnType.Name}'");
            }

            elements.RemoveRange(i - 1, 5);
            elements.Insert(i - 1, new(operatorExpression));
            --i;
        }

        private static void MakeLogicalOperatorExpression(List<OperatorOrOperand> elements, ref int i)
        {
            Operator op = elements.GetExpectedOperator(i);
            IExpression operatorExpression = null;
            IExpression lhs = elements.GetExpectedOperand(i - 1);
            IExpression rhs = elements.GetExpectedOperand(i + 1);

            var logicalOp = op switch
            {
                Operator.And => LogicalExpression.Operation.And,
                Operator.Or => LogicalExpression.Operation.Or,
                Operator.Xor => LogicalExpression.Operation.Xor,
                _ => throw new IndexOutOfRangeException()
            };

            if(lhs is IExpression<bool> lhsBool && rhs is IExpression<bool> rhsBool)
            {
                operatorExpression = new LogicalExpression()
                {
                    Operands = { lhsBool, rhsBool },
                    Op = logicalOp
                };
            }

            if(operatorExpression == null)
            {
                throw new ParseException($"Operator '{op}' cannot be applied to operands of type '{lhs.ReturnType.Name}' and '{rhs.ReturnType.Name}'");
            }

            elements.RemoveRange(i - 1, 3);
            elements.Insert(i - 1, new(operatorExpression));
            --i;
        }

        private static void MakeRelationalOperatorExpression(List<OperatorOrOperand> elements, ref int i)
        {
            IExpression operatorExpression = null;
            var lhs = elements.GetExpectedOperand(i - 1);
            var rhs = elements.GetExpectedOperand(i + 1);
            var op = elements.GetExpectedOperator(i);

            var relationalOp = op switch
            {
                Operator.Greater => RelationOperation.Greater,
                Operator.GEquals => RelationOperation.GreaterEqual,
                Operator.Less => RelationOperation.Less,
                Operator.LEquals => RelationOperation.LessEqual,
                Operator.Equals => RelationOperation.Equal,
                Operator.NEquals => RelationOperation.NotEqual,
                _ => throw new IndexOutOfRangeException()
            };

            if(lhs is IExpression<int> lhsInt && rhs is IExpression<int> rhsInt)
            {
                operatorExpression = new CompareExpression<int>()
                {
                    Lhs = lhsInt,
                    Rhs = rhsInt,
                    Op = relationalOp
                };
            }

            else if(lhs is IExpression<float> lhsFloat && rhs is IExpression<float> rhsFloat)
            {
                operatorExpression = new CompareExpression<float>()
                {
                    Lhs = lhsFloat,
                    Rhs = rhsFloat,
                    Op = relationalOp
                };
            }

            else if(lhs is IExpression<bool> lhsBool && rhs is IExpression<bool> rhsBool)
            {
                operatorExpression = new CompareExpression<bool>()
                {
                    Lhs = lhsBool,
                    Rhs = rhsBool,
                    Op = relationalOp
                };
            }

            else if(lhs is IExpression<string> lhsString && rhs is IExpression<string> rhsString)
            {
                operatorExpression = new CompareExpression<string>()
                {
                    Lhs = lhsString,
                    Rhs = rhsString,
                    Op = relationalOp
                };
            }

            if(operatorExpression == null)
            {
                throw new ParseException($"Operator '{op}' cannot be applied to operands of type '{lhs.ReturnType.Name}' and '{rhs.ReturnType.Name}'");
            }

            elements.RemoveRange(i - 1, 3);
            elements.Insert(i - 1, new(operatorExpression));
            --i;
        }

        public static bool AllOperandsAre<T>(IReadOnlyList<IExpression> operands)
        {
            for( int i = 0; i < operands.Count; ++i )
            {
                if(operands[i] is not IExpression<T>)
                    return false;
            }
            return true;
        }

        private static void MakeArithmeticOperatorExpression(List<OperatorOrOperand> elements, ref int i)
        {
            IExpression operatorExpression = null;

            using (UnityEngine.Pool.ListPool<IExpression>.Get(out var myOperands))
            {
                myOperands.Add(elements.GetExpectedOperand(i - 1));
                myOperands.Add(elements.GetExpectedOperand(i + 1));

                Operator op = elements.GetExpectedOperator(i);
                int j;
                for( j = i + 2; j < elements.Count; ++j )
                {
                    if(!elements[j].IsOperator || elements[j].Operator != op)
                        break;

                    ++j;
                    myOperands.Add(elements.GetExpectedOperand(j));
                }

                var arithmeticOp = op switch
                {
                    Operator.Mul => ArithmeticOperation.Multiply,
                    Operator.Div => ArithmeticOperation.Divide,
                    Operator.Plus => ArithmeticOperation.Add,
                    Operator.Minus => ArithmeticOperation.Subtract,
                    Operator.Mod => ArithmeticOperation.Mod,
                    _ => throw new IndexOutOfRangeException()
                };

                if(AllOperandsAre<int>(myOperands))
                {
                    var arithmetic = new ArithmeticIntExpression() { Op = arithmeticOp };
                    foreach (var myOp in myOperands)
                        arithmetic.Operands.Add((IExpression<int>)myOp);
                    operatorExpression = arithmetic;
                }

                else if(AllOperandsAre<float>(myOperands))
                {
                    var arithmetic = new ArithmeticFloatExpression() { Op = arithmeticOp };
                    foreach (var myOp in myOperands)
                        arithmetic.Operands.Add((IExpression<float>)myOp);
                    operatorExpression = arithmetic;
                }

                else if(arithmeticOp == ArithmeticOperation.Add && AllOperandsAre<string>(myOperands))
                {
                    var arithmetic = new StringConcatExpression();
                    foreach (var myOp in myOperands)
                        arithmetic.Operands.Add((IExpression<string>)myOp);
                    operatorExpression = arithmetic;
                }

                if(operatorExpression == null)
                {
                    throw new ParseException($"Operator '{op}' cannot be applied to operands");
                }

                elements.RemoveRange(i - 1, j - i + 1);
                elements.Insert(i - 1, new(operatorExpression));
                --i;
            }
        }


        private static IExpression TextToOperand(string text)
        {
            if(char.IsDigit(text[0]))
            {
                if(int.TryParse(text, out int integer))
                {
                    return new ConstantIntExpression()
                    {
                        Value = integer
                    };
                }
                if(float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float floating))
                {
                    return new ConstantFloatExpression()
                    {
                        Value = floating
                    };
                }
                throw new ParseException($"Cannot parse '{text}' as number");
            }
            else
            {
                return GetNamedExpression(text);
            }
        }

        private static IExpression GetNamedExpression(string text)
        {
            if(bool.TryParse(text, out var boolean))
            {
                return new ConstantExpression<bool>() { Value = boolean };
            }

            return new NamedObjectExpression()
            {
                Name = text
            };
        }


        private struct Token
        {
            public enum Type
            {
                Operator,
                Operand,
                BracketOpen,
                BracketClose,
                Comma,
                StringLiteral
            }

            public Type type { get; private set; }
            public string text { get; private set; }

            public Operator op { get; private set; }

            public static Token Operand(string name)
            {
                return new Token()
                {
                    type = Type.Operand,
                    text = name
                };
            }

            public static Token StringLiteral(string name)
            {
                return new Token()
                {
                    type = Type.StringLiteral,
                    text = name
                };
            }

            public static Token Operator(Operator op)
            {
                return new Token()
                {
                    op = op,
                    type = Type.Operator,
                    text = null
                };
            }

            public static Token BracketOpen() => new Token()
            {
                type = Type.BracketOpen,
                text = null
            };

            public static Token BracketClose() => new Token()
            {
                type = Type.BracketClose,
                text = null
            };

            public static Token Comma() => new Token()
            {
                type = Type.Comma,
                text = null
            };

            public override string ToString()
            {
                switch (type)
                {
                    case Type.Operand: return text;
                    case Type.Operator: return op.ToString();
                    case Type.BracketClose: return ")";
                    case Type.BracketOpen: return "(";
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        private static bool ParseToken(ref State state, out Token token)
        {
            using (StringBuilderPool.Get(out var nextOperand))
            {
                if(ParseOperand(ref state, nextOperand))
                {
                    token = Token.Operand(nextOperand.ToString());
                    return true;
                }
            }

            if(ParseOperator(ref state, out var op))
            {
                token = Token.Operator(op);
                return true;
            }

            if(state.pos >= state.expText.Length)
            {
                token = default;
                return false;
            }

            char nextChar = state.expText[state.pos];

            if(nextChar == '"')
            {
                state.pos++;
                if(ParseString(ref state, out string str))
                {
                    token = Token.StringLiteral(str);
                    return true;
                }
            }


            if(nextChar == '(')
            {
                state.pos++;
                token = Token.BracketOpen();
                return true;
            }

            if(nextChar == ')')
            {
                state.pos++;
                token = Token.BracketClose();
                return true;
            }

            if(nextChar == ',')
            {
                state.pos++;
                token = Token.Comma();
                return true;
            }

            throw new ParseException($"Unexpected character '{nextChar}'");
            token = default;
            return false;
        }

        private static bool ParseString(ref State state, out string str)
        {
            int start = state.pos;
            using (StringBuilderPool.Get(out var sb))
            {
                for( ; state.pos < state.expText.Length; ++state.pos )
                {
                    char ch = state.expText[state.pos];

                    if(ch == '\\')
                    {
                        state.pos++;
                        char escaped = state.expText[state.pos];
                        var replace = escaped switch
                        {
                            '"' => '"',
                            'n' => '\n',
                            't' => '\t',
                            '\\' => '\\',
                            _ => throw new ParseException($"Unrecognized escape character '{escaped}'")
                        };
                        sb.Append(replace);
                    }

                    else if(ch == '"')
                    {
                        str = sb.ToString();
                        state.pos++;
                        return true;
                    }

                    else
                    {
                        sb.Append(ch);
                    }
                }
                str = default;
                state.pos = start;
                return false;
            }
        }

        private static bool CheckOperator(ref State state, string operatorString)
        {
            if(string.Compare(state.expText, state.pos, operatorString, 0, operatorString.Length, StringComparison.InvariantCulture) != 0)
                return false;

            state.pos += operatorString.Length;
            return true;
        }

        private static bool CheckSubstring(ref State state, string substring)
        {
            return string.Compare(state.expText, state.pos, substring, 0, substring.Length, StringComparison.InvariantCulture) == 0;
        }

        private static bool ParseOperand(ref State state, StringBuilder sb)
        {
            sb.Clear();
            SkipWhitespace(ref state);
            int opStart = state.pos;

            while(GetChar(ref state, out var ch) && IsObjectNameChar(ch))
            {
                ++state.pos;
            }

            int length = state.pos - opStart;
            if(length <= 0)
            {
                state.pos = opStart;
                return false;
            }

            sb.Append(state.expText, opStart, length);
            return true;
        }

        private static bool GetChar(ref State state, out char ch)
        {
            if(state.pos < state.expText.Length)
            {
                ch = state.expText[state.pos];
                return true;
            }
            ch = default;
            return false;
        }

        private static bool IsObjectNameChar(char ch) => char.IsLetter(ch) || char.IsNumber(ch) || ch is '_' or '.';

        private static void SkipWhitespace(ref State state)
        {
            while(GetChar(ref state, out var ch) && char.IsWhiteSpace(ch))
            {
                ++state.pos;
            }
        }

        public enum Operator
        {
            None,
            Plus, //  +
            Minus, //  -
            Mul, //  *
            Div, //  /
            Mod, //  /
            And, //  &
            Or, //  |
            Xor, //  ^
            Equals, //  ==
            NEquals, //  !=
            Greater, //  >
            GEquals, //  >=
            Less, //  <
            LEquals, //  <=
            Condition, //  ?
            Branch, //  :
            Not //  !
        }

        private static bool ParseOperator(ref State state, out Operator op)
        {
            int start = state.pos;
            SkipWhitespace(ref state);
            if(CheckOperator(ref state, "=="))
            {
                op = Operator.Equals;
                return true;
            }

            if(CheckOperator(ref state, "!="))
            {
                op = Operator.NEquals;
                return true;
            }

            if(CheckOperator(ref state, ">="))
            {
                op = Operator.GEquals;
                return true;
            }

            if(CheckOperator(ref state, "<="))
            {
                op = Operator.LEquals;
                return true;
            }

            if(CheckOperator(ref state, "+"))
            {
                op = Operator.Plus;
                return true;
            }

            if(CheckOperator(ref state, "-"))
            {
                op = Operator.Minus;
                return true;
            }

            if(CheckOperator(ref state, "*"))
            {
                op = Operator.Mul;
                return true;
            }

            if(CheckOperator(ref state, "/"))
            {
                op = Operator.Div;
                return true;
            }

            if(CheckOperator(ref state, "%"))
            {
                op = Operator.Mod;
                return true;
            }

            if(CheckOperator(ref state, "&"))
            {
                op = Operator.And;
                return true;
            }

            if(CheckOperator(ref state, "|"))
            {
                op = Operator.Or;
                return true;
            }

            if(CheckOperator(ref state, "^"))
            {
                op = Operator.Xor;
                return true;
            }

            if(CheckOperator(ref state, ">"))
            {
                op = Operator.Greater;
                return true;
            }

            if(CheckOperator(ref state, "<"))
            {
                op = Operator.Less;
                return true;
            }

            if(CheckOperator(ref state, "?"))
            {
                op = Operator.Condition;
                return true;
            }

            if(CheckOperator(ref state, ":"))
            {
                op = Operator.Branch;
                return true;
            }

            if(CheckOperator(ref state, "!"))
            {
                op = Operator.Not;
                return true;
            }

            op = default;
            state.pos = start;
            return false;
        }

        private class NamedObjectExpression : IExpression
        {
            public bool IsPureSelf => false;
            public string Name { get; set; }
            public string AsText() => Name;

            object IExpression.Evaluate(IEvalContext context) => throw new NotImplementedException();
            Type IExpression.ReturnType => null;

            public void VisitChildren(IExpression.ChildReplacementVisitor visitor)
            {
            }
            public IExpression SimplifyIfPure(out bool pure)
            {
                pure = false;
                return this;
            }
        }

        public class Resolver : IResolver
        {
            public virtual IExpression ResolveMethodInvocation(string name, List<IExpression> arguments)
            {
                return ExpressionFunctions.ResolveMethodInvocation(name, arguments);
            }

            public virtual IExpression ResolveVariable(string name)
            {
                return ExpressionFunctions.ResolveConstant(name);
            }
        }
    }

    internal struct OperatorOrOperand
    {
        public OperatorOrOperand(IExpression expression)
        {
            m_expression = expression;
            m_operator = ExpressionCompiler.Operator.None;
        }

        public OperatorOrOperand(ExpressionCompiler.Operator op)
        {
            m_expression = null;
            m_operator = op;
        }

        private IExpression m_expression;
        private ExpressionCompiler.Operator m_operator;

        public bool IsOperand => m_expression != null;
        public bool IsOperator => m_expression == null;
        public IExpression Operand
        {
            get {
                if(!IsOperand)
                    throw new InvalidCastException();
                return m_expression;
            }
        }
        public ExpressionCompiler.Operator Operator
        {
            get {
                if(!IsOperator)
                    throw new InvalidCastException();
                return m_operator;
            }
        }

        public override string ToString()
        {
            if(IsOperator)
                return m_operator.ToString();
            else
                return m_expression.ToString();
        }
    }

    internal static class Extensions
    {
        public static ExpressionCompiler.Operator GetExpectedOperator(this List<OperatorOrOperand> elements, int index, ExpressionCompiler.Operator expectedOperator)
        {
            if(elements.Count <= index || !elements[index].IsOperator || elements[index].Operator != expectedOperator)
                throw new ExpressionCompiler.ParseException($"Expected operator '{expectedOperator}'");
            return expectedOperator;
        }

        public static ExpressionCompiler.Operator GetExpectedOperator(this List<OperatorOrOperand> elements, int index)
        {
            if(elements.Count <= index || !elements[index].IsOperator)
                throw new ExpressionCompiler.ParseException($"Expected operator");
            return elements[index].Operator;
        }

        public static IExpression GetExpectedOperand(this List<OperatorOrOperand> elements, int index)
        {
            if(elements.Count <= index || index < 0 || !elements[index].IsOperand)
                throw new ExpressionCompiler.ParseException($"Expected operand");
            return elements[index].Operand;
        }
    }
}
