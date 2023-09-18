namespace SeweralIdeas.Expressions
{

    public abstract class AnonymousFunctionExpressionBase<TRet> : Expression<TRet>
    {

        public override sealed bool IsPureSelf { get; }

        public string Name { get; }

        public AnonymousFunctionExpressionBase(string name, bool pure)
        {
            
            Name = name;
            IsPureSelf = pure;
        }
    }
    
    public class AnonymousFunctionExpression<TRet> : AnonymousFunctionExpressionBase<TRet>
    {
        public AnonymousFunctionExpression(string name, Func<TRet> func, bool pure) : base(name, pure)
        {
            Func = func;
        }

        public Func<TRet> Func { get; }

        public override TRet Evaluate(IEvalContext? context) => Func();
        public override string AsText() => $"{Name}()";

        public override void VisitTopLevelChildren(IExpression.ChildReplacementVisitor visitor)
        {
        }
    }
    
    public class AnonymousFunctionExpression<TArg0, TRet> : AnonymousFunctionExpressionBase<TRet>
    {
        public AnonymousFunctionExpression(string name, Func<TArg0?, TRet?> func, bool pure, IExpression<TArg0> arg0) : base(name, pure)
        {
            Func = func ?? throw new ArgumentNullException(nameof(func));
            Arg0 = arg0 ?? throw new ArgumentNullException(nameof(arg0));
        }

        public IExpression<TArg0> Arg0 { get; private set; }
        public Func<TArg0?, TRet?> Func { get; }

        public override TRet? Evaluate(IEvalContext? context) => Func(Arg0.Evaluate(context));
        public override string AsText() => $"{Name}({Arg0.AsText()})";
        
        public override void VisitTopLevelChildren(IExpression.ChildReplacementVisitor visitor)
        {
            Arg0 = (IExpression<TArg0>)visitor(Arg0);
        }
    }

    public class AnonymousFunctionExpression<TArg0, TArg1, TRet> : AnonymousFunctionExpressionBase<TRet>
    {
        public AnonymousFunctionExpression(string name, Func<TArg0?, TArg1?, TRet?> func, bool pure, IExpression<TArg0> arg0, IExpression<TArg1> arg1) : base(name, pure)
        {
            Arg0 = arg0;
            Arg1 = arg1;
            Func = func;
        }

        public IExpression<TArg0> Arg0 { get; private set; }
        public IExpression<TArg1> Arg1 { get; private set; }
        public Func<TArg0?, TArg1?, TRet?> Func { get; }

        public override TRet? Evaluate(IEvalContext? context) => Func(Arg0.Evaluate(context), Arg1.Evaluate(context));
        public override string AsText() => $"{Name}({Arg0.AsText()}, {Arg1.AsText()})";
        
        public override void VisitTopLevelChildren(IExpression.ChildReplacementVisitor visitor)
        {
            Arg0 = (IExpression<TArg0>)visitor(Arg0);
            Arg1 = (IExpression<TArg1>)visitor(Arg1);
        }
    }

    public class AnonymousFunctionExpression<TArg0, TArg1, TArg2, TRet> : AnonymousFunctionExpressionBase<TRet>
    {
        public AnonymousFunctionExpression(string name, Func<TArg0?, TArg1?, TArg2?, TRet?> func, bool pure, IExpression<TArg0> arg0, IExpression<TArg1> arg1, IExpression<TArg2> arg2) : base(name, pure)
        {
            Arg0 = arg0;
            Arg1 = arg1;
            Arg2 = arg2;
            Func = func;
        }

        public IExpression<TArg0?> Arg0 { get; private set; }
        public IExpression<TArg1?> Arg1 { get; private set; }
        public IExpression<TArg2?> Arg2 { get; private set; }
        public Func<TArg0?, TArg1?, TArg2?, TRet?> Func { get; }

        public override TRet? Evaluate(IEvalContext? context) => Func(Arg0.Evaluate(context), Arg1.Evaluate(context), Arg2.Evaluate(context));
        public override string AsText() => $"{Name}({Arg0.AsText()}, {Arg1.AsText()})";
        
        public override void VisitTopLevelChildren(IExpression.ChildReplacementVisitor visitor)
        {
            Arg0 = (IExpression<TArg0>)visitor(Arg0);
            Arg1 = (IExpression<TArg1>)visitor(Arg1);
            Arg2 = (IExpression<TArg2>)visitor(Arg2);
        }
    }
}
