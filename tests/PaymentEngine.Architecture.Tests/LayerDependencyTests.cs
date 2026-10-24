using System.Reflection;
using NetArchTest.Rules;

namespace PaymentEngine.Architecture.Tests;

public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = Assembly.Load("PaymentEngine.Domain");
    private static readonly Assembly ApplicationAssembly = Assembly.Load("PaymentEngine.Application");

    [Theory]
    [InlineData("PaymentEngine.Application")]
    [InlineData("PaymentEngine.Infrastructure")]
    [InlineData("PaymentEngine.Api")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Npgsql")]
    public void Domain_ShouldNotDependOn(string forbidden)
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful, $"O Domain não pode depender de {forbidden}.");
    }

    [Theory]
    [InlineData("PaymentEngine.Infrastructure")]
    [InlineData("PaymentEngine.Api")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Npgsql")]
    public void Application_ShouldNotDependOn(string forbidden)
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbidden)
            .GetResult();

        Assert.True(result.IsSuccessful, $"A Application não pode depender de {forbidden}.");
    }
}