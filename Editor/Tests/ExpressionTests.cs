#if UNITY_5_3_OR_NEWER
#define UNITY
#endif

#if UNITY
using NUnit.Framework;
using SeweralIdeas.Expressions;

public class ExpressionTests
{
    [Test]
    public void Integers()
    {
        TestValue("5", 5);
        TestValue("-5", -5);
        TestValue("2+3*4", 2+3*4);
        TestValue("(2+3)*4", (2+3)*4);
        TestValue("5*-4", 5*-4);
        TestValue("5>4", 5>4);
        TestValue("5>=4", 5>=4);
        TestValue("5<4", 5<4);
        TestValue("5<=4", 5<=4);
        TestValue("5==4", 5==4);
        TestValue("5!=4", 5!=4);
        TestValue("5>4? 1:2", 5>4? 1:2);
        TestValue("5<4? 1:2", 5<4? 1:2);
        TestValue("5>4 & 4 < 5", 5>4 && 4 < 5);
    }

    [Test]
    public void Strings()
    {
        TestValue("\"\"", "");
        // TestValue("\"\\\"\"", "\"");
        TestValue("\"Hello\"", "Hello");
        TestValue("\"Hello\"+\"World\"", "Hello"+"World");
        TestValue("\"Hello\"+\"World\"==\"HelloWorld\"", "Hello"+"World"=="HelloWorld");
    }

    [Test]
    public void Floats()
    {
        TestValue("5.1", 5.1f);
        TestValue("5.1 + 1.2", 5.1f + 1.2f);
        TestValue("5.1 * 1.2", 5.1f * 1.2f);
        TestValue("5.1 / 1.2", 5.1f / 1.2f);
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
#endif