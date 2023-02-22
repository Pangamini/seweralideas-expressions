using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace SeweralIdeas.Expressions
{
    public interface IExpression
    {
        public delegate IExpression Visitor(IExpression expression);
        string AsText();
        object Evaluate(IEvalContext context);
        Type ReturnType { get; }
        bool IsPureSelf { get; }

        void VisitChildren(Visitor visitor);
    }


    public interface IExpression<T> : IExpression
    {
        new T Evaluate(IEvalContext context);
    }

    [Serializable]
    public abstract class Expression<T> : IExpression<T>
    {
        public abstract T Evaluate(IEvalContext context);
        public abstract string AsText();
        public abstract bool IsPureSelf { get; }
        public abstract void VisitChildren(IExpression.Visitor visitor);
        public Type ReturnType => typeof( T );
        object IExpression.Evaluate(IEvalContext context) => Evaluate(context);
        public override string ToString() => $"<{typeof(T).Name}>{AsText()}";
    }

    [Serializable]
    public class ConstantExpression<T> : Expression<T>
    {
        [SerializeField] private T m_value;

        public T Value
        {
            get => m_value;
            set => m_value = value;
        }

        public override string AsText() => Value.ToString();

        public override sealed T Evaluate(IEvalContext context) => Value;
        public override sealed bool IsPureSelf => true;
        public override sealed void VisitChildren(IExpression.Visitor visitor)
        {
        }
    }

    public class LogicalExpression : Expression<bool>
    {
        private Operation m_op;
        private readonly List<IExpression<bool>> m_operands = new();

        public override void VisitChildren(IExpression.Visitor visitor)
        {
            for( int i = 0; i < m_operands.Count; ++i )
            {
                m_operands[i] = (IExpression<bool>)visitor(m_operands[i]);
            }
        }

        public override bool IsPureSelf => true;

        public List<IExpression<bool>> Operands => m_operands;

        public override string AsText()
        {
            if(m_operands.Count == 0)
            {
                return Evaluate(null).ToString();
            }

            var sb = new StringBuilder();
            sb.Append("(");
            var separator = m_op switch
            {
                Operation.And => " && ",
                Operation.Or => " || ",
                Operation.Xor => " ^ "
            };

            sb.Append(m_operands[0].GetAsText());

            for( int i = 1; i < m_operands.Count; ++i )
            {
                sb.Append(separator);
                sb.Append(m_operands[i].GetAsText());
            }

            sb.Append(")");
            return sb.ToString();
        }

        public enum Operation
        {
            And,
            Or,
            Xor
        }

        public Operation Op
        {
            get => m_op;
            set => m_op = value;
        }

        public override bool Evaluate(IEvalContext context)
        {
            switch (m_op)
            {
                case Operation.And:
                    foreach (var expression in m_operands)
                    {
                        if(!expression.Evaluate(context))
                            return false;
                    }

                    return true;

                case Operation.Or:
                    foreach (var expression in m_operands)
                    {
                        if(expression.Evaluate(context))
                            return true;
                    }

                    return false;

                case Operation.Xor:
                    bool ret = false;
                    foreach (var expression in m_operands)
                    {
                        ret ^= expression.Evaluate(context);
                    }

                    return ret;

                default: throw new IndexOutOfRangeException();
            }
        }
    }

    public enum ArithmeticOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Mod
    }

    public static class Helpers
    {
        public static string GetAsText(this IExpression expression)
        {
            if(expression == null)
                return "<?>";
            return expression.AsText();
        }

        public static string ArithmeticOperationAsText(IReadOnlyList<IExpression> m_expressions, ArithmeticOperation operation)
        {
            var sb = new StringBuilder();
            sb.Append("(");

            var separator = operation switch
            {
                ArithmeticOperation.Add => " + ",
                ArithmeticOperation.Multiply => " * ",
                ArithmeticOperation.Divide => " / ",
                ArithmeticOperation.Subtract => " - ",
                ArithmeticOperation.Mod => " % "
            };

            sb.Append(m_expressions[0].GetAsText());

            for( int i = 1; i < m_expressions.Count; ++i )
            {
                sb.Append(separator);
                sb.Append(m_expressions[i].GetAsText());
            }
            sb.Append(")");
            return sb.ToString();
        }
    }

    public class StringConcatExpression : Expression<string>
    {
        private ArithmeticOperation m_op;
        private readonly List<IExpression<string>> m_operands = new();
        public override bool IsPureSelf => true;
        public List<IExpression<string>> Operands => m_operands;

        public override void VisitChildren(IExpression.Visitor visitor)
        {
            for( int i = 0; i < m_operands.Count; ++i )
            {
                m_operands[i] = (IExpression<string>)visitor(m_operands[i]);
            }
        }

        public override string AsText()
        {
            if(m_operands.Count == 0)
                return "\"\"";

            return Helpers.ArithmeticOperationAsText(m_operands, ArithmeticOperation.Add);
        }

        public override string Evaluate(IEvalContext context)
        {
            if(m_operands.Count == 0)
            {
                return "";
            }

            string value = m_operands[0].Evaluate(context);

            for( var index = 1; index < m_operands.Count; index++ )
            {
                var expression = m_operands[index];
                value += expression.Evaluate(context);
            }

            return value;

        }
    }

    public class ArithmeticFloatExpression : Expression<float>
    {
        private ArithmeticOperation m_op;
        private readonly List<IExpression<float>> m_operands = new();
        public override bool IsPureSelf => true;
        public List<IExpression<float>> Operands => m_operands;

        public override void VisitChildren(IExpression.Visitor visitor)
        {
            for( int i = 0; i < m_operands.Count; ++i )
            {
                m_operands[i] = (IExpression<float>)visitor(m_operands[i]);
            }
        }
        
        public ArithmeticOperation Op
        {
            get => m_op;
            set => m_op = value;
        }

        public override string AsText()
        {
            if (m_operands.Count == 0)
                return Evaluate(null).ToString();
            
            return Helpers.ArithmeticOperationAsText(m_operands, Op);
        }

        public override float Evaluate(IEvalContext context)
        {
            if (m_operands.Count == 0)
            {
                return 0;
            }
            
            float value = m_operands[0].Evaluate(context);
            
            switch(m_op)
            {
                case ArithmeticOperation.Add:
                    for (var index = 1; index < m_operands.Count; index++)
                    {
                        var expression = m_operands[index];
                        value += expression.Evaluate(context);
                    }

                    return value;

                case ArithmeticOperation.Subtract:
                    for (var index = 1; index < m_operands.Count; index++)
                    {
                        var expression = m_operands[index];
                        value -= expression.Evaluate(context);
                    }

                    return value;
                
                case ArithmeticOperation.Multiply:
                    for (var index = 1; index < m_operands.Count; index++)
                    {
                        var expression = m_operands[index];
                        value *= expression.Evaluate(context);
                    }

                    return value;
                
                case ArithmeticOperation.Divide:
                    for (var index = 1; index < m_operands.Count; index++)
                    {
                        var expression = m_operands[index];
                        value /= expression.Evaluate(context);
                    }

                    return value;
                
                case ArithmeticOperation.Mod:
                    for (var index = 1; index < m_operands.Count; index++)
                    {
                        var expression = m_operands[index];
                        value %= expression.Evaluate(context);
                    }

                    return value;
                
                default: throw new IndexOutOfRangeException();
            }
        }
    }

    public class NegateIntExpression : Expression<int>, IExpression<float>
    {
        private IExpression<int> m_operand;
        
        public override int Evaluate(IEvalContext context) => -m_operand.Evaluate(context);
        public override string AsText() => $"-{m_operand.AsText()}";

        float IExpression<float>.Evaluate(IEvalContext context) => Evaluate(context);
        
        public override bool IsPureSelf => true;
        
        public IExpression<int> Operand
        {
            get => m_operand;
            set => m_operand = value;
        }
        public override void VisitChildren(IExpression.Visitor visitor)
        {
            m_operand = (IExpression<int>)visitor(m_operand);
        }
    }

    public class NotExpression : Expression<bool>
    {
        private IExpression<bool> m_operand;
        
        public override bool Evaluate(IEvalContext context) => !m_operand.Evaluate(context);
        public override string AsText() => $"!{m_operand.AsText()}";
        
        public override bool IsPureSelf => true;
        
        public IExpression<bool> Operand
        {
            get => m_operand;
            set => m_operand = value;
        }
        public override void VisitChildren(IExpression.Visitor visitor)
        {
            m_operand = (IExpression<bool>)visitor(m_operand);
        }
    }
    
    public class NegateFloatExpression : Expression<float>
    {
        private IExpression<float> m_operand;
        
        public override float Evaluate(IEvalContext context) => -m_operand.Evaluate(context);
        public override string AsText() => $"-{m_operand.AsText()}";
        
        public override bool IsPureSelf => true;
        
        public IExpression<float> Operand
        {
            get => m_operand;
            set => m_operand = value;
        }
        public override void VisitChildren(IExpression.Visitor visitor)
        {
            m_operand = (IExpression<float>)visitor(m_operand);
        }
    }

    
    public class ArithmeticIntExpression : Expression<int>, IExpression<float>
    {
        private ArithmeticOperation m_op;
        private List<IExpression<int>> m_operands = new();
        public override bool IsPureSelf => true;
        public List<IExpression<int>> Operands => m_operands;

        public override void VisitChildren(IExpression.Visitor visitor)
        {
            for( int i = 0; i < m_operands.Count; ++i )
            {
                m_operands[i] = (IExpression<int>)visitor(m_operands[i]);
            }
        }
        
        public ArithmeticOperation Op
        {
            get => m_op;
            set => m_op = value;
        }
        
        public override string AsText()
        {
            if (m_operands.Count == 0)
                return Evaluate(null).ToString();

            return Helpers.ArithmeticOperationAsText(m_operands, Op);
        }
        
        float IExpression<float>.Evaluate(IEvalContext context) => Evaluate(context);
        
        public override int Evaluate(IEvalContext context)
        {
            if (m_operands.Count == 0)
            {
                return 0;
            }
            
            int value = m_operands[0].Evaluate(context);
            
            switch(m_op)
            {
                case ArithmeticOperation.Add:
                    for (var index = 1; index < m_operands.Count; index++)
                    {
                        var expression = m_operands[index];
                        value += expression.Evaluate(context);
                    }

                    return value;

                case ArithmeticOperation.Subtract:
                    for (var index = 1; index < m_operands.Count; index++)
                    {
                        var expression = m_operands[index];
                        value -= expression.Evaluate(context);
                    }

                    return value;
                
                case ArithmeticOperation.Multiply:
                    for (var index = 1; index < m_operands.Count; index++)
                    {
                        var expression = m_operands[index];
                        value *= expression.Evaluate(context);
                    }

                    return value;
                
                case ArithmeticOperation.Divide:
                    for (var index = 1; index < m_operands.Count; index++)
                    {
                        var expression = m_operands[index];
                        value /= expression.Evaluate(context);
                    }

                    return value;
                
                default: throw new IndexOutOfRangeException();
            }
        }
    }
    
    public enum RelationOperation
    {
        Equal,
        NotEqual,
        Less,
        Greater,
        LessEqual,
        GreaterEqual
    }

    public class CompareExpression<T> : Expression<bool> where T:IComparable<T>
    {
        private RelationOperation m_op;
        private IExpression<T> m_lhs;
        private IExpression<T> m_rhs;
        public override bool IsPureSelf => true;
        
        
        public override void VisitChildren(IExpression.Visitor visitor)
        {
            m_lhs = (IExpression<T>) visitor(m_lhs);
            m_rhs = (IExpression<T>) visitor(m_rhs);
        }
        
        public IExpression<T> Lhs
        {
            get => m_lhs;
            set => m_lhs = value;
        }

        public IExpression<T> Rhs
        {
            get => m_rhs;
            set => m_rhs = value;
        }

        public RelationOperation Op
        {
            get => m_op;
            set => m_op = value;
        }

        public override string AsText()
        {
            var separator = m_op switch
            {
                RelationOperation.Equal => "==",
                RelationOperation.Greater => ">",
                RelationOperation.Less => "<",
                RelationOperation.GreaterEqual => ">=",
                RelationOperation.LessEqual => "<=",
                RelationOperation.NotEqual => "!=",
                _=> throw new IndexOutOfRangeException()
            };
            return $"({m_lhs.GetAsText()} {separator} {m_rhs.GetAsText()})";
        }

        public override bool Evaluate(IEvalContext context)
        {
            T leftVal = Lhs.Evaluate(context);
            T rightVal = Rhs.Evaluate(context);
            int comparison = leftVal.CompareTo(rightVal);

            return Op switch
            {
                RelationOperation.Equal => comparison == 0,
                RelationOperation.NotEqual => comparison != 0,
                RelationOperation.Less => comparison < 0,
                RelationOperation.Greater => comparison > 0,
                RelationOperation.LessEqual => comparison <= 0,
                RelationOperation.GreaterEqual => comparison >= 0,
                _ => throw new IndexOutOfRangeException()
            };
        }
    }

    public class SelectExpression<T> : Expression<T>
    {
        private IExpression<bool> m_condition;
        private IExpression<T> m_ifTrue;
        private IExpression<T> m_ifFalse;
        public override bool IsPureSelf => true;
        
        public override void VisitChildren(IExpression.Visitor visitor)
        {
            m_condition = (IExpression<bool>) visitor(m_condition);
            m_ifTrue = (IExpression<T>) visitor(m_ifTrue);
            m_ifFalse = (IExpression<T>) visitor(m_ifFalse);
        }
        
        public IExpression<bool> Condition
        {
            get => m_condition;
            set => m_condition = value;
        }
        public IExpression<T> IfTrue
        {
            get => m_ifTrue;
            set => m_ifTrue = value;
        }
        public IExpression<T> IfFalse
        {
            get => m_ifFalse;
            set => m_ifFalse = value;
        }

        public override T Evaluate(IEvalContext context)
        {
            if (Condition.Evaluate(context))
            {
                return IfTrue.Evaluate(context);
            }
            else
            {
                return IfFalse.Evaluate(context);
            }
        }

        public override string AsText()
        {
            return $"({Condition.GetAsText()}?{IfTrue.GetAsText()}:{IfFalse.GetAsText()})";
        }
    }

    public class SelectIntExpression : SelectExpression<int>, IExpression<float>
    {
        float IExpression<float>.Evaluate(IEvalContext context) => Evaluate(context);
    }

    [Serializable]
    public class ConstantIntExpression : ConstantExpression<int>, IExpression<float>
    {
        float IExpression<float>.Evaluate(IEvalContext context) => Evaluate(context);
    }
    
    [Serializable]
    public class ConstantStringExpression : ConstantExpression<string>
    {
        public override string AsText() => $"\"{Value}\"";
    }
    
    [Serializable]
    public class ConstantFloatExpression : ConstantExpression<float>
    {
        public override string AsText()
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if(Mathf.Floor(Value) == Value)
                return Value.ToString(".0", CultureInfo.InvariantCulture);
            else
                return Value.ToString(CultureInfo.InvariantCulture);
        }
    }
    
    public interface IEvalContext
    {
        T GetValue<T>(object variable);
    }
}
