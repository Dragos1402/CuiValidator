using CuiValidator.Api.Services;

namespace CuiValidator.Tests;

public class CuiFormatTests
{
    [Theory]
    [InlineData("14080700")]      // ANAF
    [InlineData("RO14080700")]
    [InlineData("ro 14080700")]
    [InlineData("RO-14080700")]   // format seen on scanned invoices
    public void Valid_cui_passes(string cui) => Assert.True(CuiFormat.IsValid(cui));

    [Theory]
    [InlineData("14080701")]      // wrong control digit
    [InlineData("1")]             // too short
    [InlineData("12345678901")]   // too long
    [InlineData("14O80700")]      // letter O
    [InlineData("")]
    [InlineData(null)]
    public void Invalid_cui_fails(string? cui) => Assert.False(CuiFormat.IsValid(cui));
}
