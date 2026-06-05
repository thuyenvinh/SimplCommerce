using FluentAssertions;
using FluentValidation.TestHelper;
using SimplCommerce.ApiService.Auth;
using Xunit;

namespace SimplCommerce.ApiService.UnitTests.Auth;

public class RegisterRequestValidatorTests
{
    private readonly RegisterRequestValidator _sut = new();

    [Fact]
    public void Valid_request_passes()
    {
        var result = _sut.TestValidate(new RegisterRequest("alice@example.com", "secret", "Alice Smith"));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("missing-at-sign.com")]
    public void Email_must_be_valid(string email)
    {
        _sut.TestValidate(new RegisterRequest(email, "secret", "Alice"))
            .ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    public void Password_must_be_4_chars_or_more(string password)
    {
        _sut.TestValidate(new RegisterRequest("a@b.com", password, "Alice"))
            .ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void FullName_is_required()
    {
        _sut.TestValidate(new RegisterRequest("a@b.com", "secret", ""))
            .ShouldHaveValidationErrorFor(x => x.FullName);
    }

    [Fact]
    public void FullName_capped_at_200_chars()
    {
        var tooLong = new string('x', 201);
        _sut.TestValidate(new RegisterRequest("a@b.com", "secret", tooLong))
            .ShouldHaveValidationErrorFor(x => x.FullName);
    }
}

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _sut = new();

    [Fact]
    public void Valid_login_passes() =>
        _sut.TestValidate(new LoginRequest("a@b.com", "pw")).IsValid.Should().BeTrue();

    [Fact]
    public void Missing_email_fails() =>
        _sut.TestValidate(new LoginRequest("", "pw"))
            .ShouldHaveValidationErrorFor(x => x.Email);

    [Fact]
    public void Missing_password_fails() =>
        _sut.TestValidate(new LoginRequest("a@b.com", ""))
            .ShouldHaveValidationErrorFor(x => x.Password);
}

public class ForgotPasswordRequestValidatorTests
{
    private readonly ForgotPasswordRequestValidator _sut = new();

    [Fact]
    public void Valid_email_passes() =>
        _sut.TestValidate(new ForgotPasswordRequest("a@b.com")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Bad_email_fails(string email) =>
        _sut.TestValidate(new ForgotPasswordRequest(email))
            .ShouldHaveValidationErrorFor(x => x.Email);
}

public class ResetPasswordRequestValidatorTests
{
    private readonly ResetPasswordRequestValidator _sut = new();

    [Fact]
    public void Valid_reset_passes() =>
        _sut.TestValidate(new ResetPasswordRequest("a@b.com", "token", "newpw"))
            .IsValid.Should().BeTrue();

    [Fact]
    public void Empty_token_fails() =>
        _sut.TestValidate(new ResetPasswordRequest("a@b.com", "", "newpw"))
            .ShouldHaveValidationErrorFor(x => x.Token);

    [Fact]
    public void Short_password_fails() =>
        _sut.TestValidate(new ResetPasswordRequest("a@b.com", "token", "ab"))
            .ShouldHaveValidationErrorFor(x => x.NewPassword);
}
