using System.Reflection;
using System.Runtime.CompilerServices;
using AwesomeAssertions;

namespace GuildSaber.UnitTests.Utils;

/// <summary>
/// A collection of esoteric utilities for running tests reflectively.
/// </summary>
public static class ReflectiveTestUtils
{
    /// <summary>
    /// Runs all public instance methods from the class of the caller instance that are marked with either the TestAttribute or
    /// TestCaseAttribute.
    /// Each method is invoked and expected to throw an exception with a message that matches the provided wildcard pattern.
    /// </summary>
    /// <param name="callerInstance">The instance of the class from which the methods will be invoked.</param>
    /// <param name="expectedWildCardPattern">The wildcard pattern that the exception message is expected to match.</param>
    /// <param name="ignoredMethodNames">An optional list of method names to ignore. These methods will not be invoked.</param>
    /// <param name="callerPath">The file path of the caller. This is automatically filled in by the CallerFilePath attribute.</param>
    /// <param name="callerName">
    /// The name of the method that is calling this method. This is automatically filled in by the
    /// CallerMemberName attribute.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the caller class type cannot be found. This can occur if the
    /// file is not named after the caller class name.
    /// </exception>
    /// <returns>A Task representing the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task RunAllTestMethodsFromCallerClassInstanceWithWildcardThrowCondition(
        object callerInstance,
        string expectedWildCardPattern,
        IEnumerable<string>? ignoredMethodNames = null,
        [CallerFilePath] string callerPath = "",
        [CallerMemberName] string callerName = "")
    {
        var callerFilePath = Path.GetFileNameWithoutExtension(callerPath);
        var callerClassType = Array.Find(Assembly.GetExecutingAssembly().GetTypes(), t => t.Name == callerFilePath);
        if (callerClassType is null)
            throw new InvalidOperationException(
                "Caller class type not found, make sure that the file is named after the caller class name"
            );

        var testMethods = callerClassType
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(m =>
                m.Name != callerName &&
                (!ignoredMethodNames?.Contains(m.Name) ?? true) &&
                (m.GetCustomAttributes<FactAttribute>().Any() ||
                 m.GetCustomAttributes<TheoryAttribute>().Any()));

        foreach (var method in testMethods)
        {
            var task = method.Invoke(callerInstance, null) as Task;

            await FluentActions.Invoking(async () => await task!)
                .Should().ThrowAsync<Exception>()
                .WithMessage(expectedWildCardPattern,
                    "Method {0} should throw with \"{1}\" wildcard error message",
                    method.Name,
                    expectedWildCardPattern);
        }
    }
}