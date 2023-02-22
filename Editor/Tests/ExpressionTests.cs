using NUnit.Framework;
using SeweralIdeas.Expressions;

public class ExpressionTests
{
    [Test]
    public void Test1()
    {
        TestValue("5", 5);
        TestValue("-5", -5);
        TestValue("5.0", 5.0f);
        TestValue("\"Hello\"", "Hello");
        TestValue("\"Hello\"+\"World\"", "Hello"+"World");
        TestValue("2+3*4", 2+3*4);
        TestValue("(2+3)*4", (2+3)*4);
        TestValue("5*-4", 5*-4);
        TestValue("5>4", 5>4);
        TestValue("5>=4", 5>=4);
        TestValue("5<4", 5<4);
        TestValue("5<=4", 5<=4);
        TestValue("5==4", 5==4);
        TestValue("5!=4", 5!=4);
        TestValue("\"Hello\"+\"World\"==\"HelloWorld\"", "Hello"+"World"=="HelloWorld");
        TestValue("5>4? 1:2", 5>4? 1:2);
        TestValue("5>4 & 4 < 5", 5>4 && 4 < 5);
    }

    public static void TestValue<T>(string expression, T expectedValue)
    {
        IExpression<T> compiledNotOptimized = ExpressionCompiler.Parse<T>(expression, ExpressionCompiler.Options.None);
        IExpression<T> compiledOptimized = ExpressionCompiler.Parse<T>(expression, ExpressionCompiler.Options.OptimizeConstants);
        IExpression<T> compiledFromCompiled = ExpressionCompiler.Parse<T>(compiledNotOptimized.AsText(), ExpressionCompiler.Options.None);
        
        Assert.AreEqual(compiledNotOptimized.Evaluate(null), expectedValue, expression);
        Assert.AreEqual(compiledOptimized.Evaluate(null), expectedValue, expression);
        Assert.AreEqual(compiledFromCompiled.Evaluate(null), expectedValue, expression);
        Assert.AreEqual(compiledNotOptimized.AsText(), compiledFromCompiled.AsText());
    }

    public static void TestInvalid(string expression)
    {
        ExpressionCompiler.Parse(expression);
    }
}
