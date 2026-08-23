using SistemaAlmacen.Shared.Common;

namespace SistemaAlmacen.Business.Tests.Services;

public class ResultTests
{
    [Fact]
    public void Success_ReturnsSuccessfulResult()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.FieldErrors);
        Assert.False(result.HasFieldErrors);
    }

    [Fact]
    public void Failure_WithMessage_ReturnsFailedResult()
    {
        var result = Result.Failure("Algo salió mal");

        Assert.False(result.IsSuccess);
        Assert.Equal("Algo salió mal", result.ErrorMessage);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.FieldErrors);
    }

    [Fact]
    public void Failure_WithMessageAndCode_ReturnsFailedResultWithCode()
    {
        var result = Result.Failure("Stock insuficiente", "STOCK_INSUFICIENTE");

        Assert.False(result.IsSuccess);
        Assert.Equal("Stock insuficiente", result.ErrorMessage);
        Assert.Equal("STOCK_INSUFICIENTE", result.ErrorCode);
    }

    [Fact]
    public void ValidationFailure_WithFieldErrors_ReturnsFailedResult()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Nombre"] = ["El nombre es requerido."],
            ["Email"] = ["El formato del email es inválido.", "El email ya existe."]
        };

        var result = Result.ValidationFailure(errors);

        Assert.False(result.IsSuccess);
        Assert.True(result.HasFieldErrors);
        Assert.NotNull(result.FieldErrors);
        Assert.Equal(2, result.FieldErrors.Count);
        Assert.Single(result.FieldErrors["Nombre"]);
        Assert.Equal(2, result.FieldErrors["Email"].Length);
    }

    [Fact]
    public void GenericSuccess_ReturnsSuccessfulResultWithValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.ErrorMessage);
        Assert.Null(result.ErrorCode);
    }

    [Fact]
    public void GenericSuccess_WithReferenceType_ReturnsValue()
    {
        var dto = new TestDto { Id = 1, Name = "Test" };
        var result = Result<TestDto>.Success(dto);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Id);
        Assert.Equal("Test", result.Value.Name);
    }

    [Fact]
    public void GenericFailure_ReturnsFailedResultWithDefaultValue()
    {
        var result = Result<int>.Failure("Error", "ERR_001");

        Assert.False(result.IsSuccess);
        Assert.Equal(default, result.Value);
        Assert.Equal("Error", result.ErrorMessage);
        Assert.Equal("ERR_001", result.ErrorCode);
    }

    [Fact]
    public void GenericValidationFailure_ReturnsFieldErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Precio"] = ["El precio debe ser mayor a 0.01."]
        };

        var result = Result<string>.ValidationFailure(errors);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.True(result.HasFieldErrors);
        Assert.Single(result.FieldErrors!);
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccessResult()
    {
        Result<int> result = 99;

        Assert.True(result.IsSuccess);
        Assert.Equal(99, result.Value);
    }

    [Fact]
    public void ImplicitConversion_FromReferenceType_CreatesSuccessResult()
    {
        var dto = new TestDto { Id = 5, Name = "Producto" };
        Result<TestDto> result = dto;

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value!.Id);
    }

    [Fact]
    public void GenericResult_InheritsFromResult()
    {
        Result<string> genericResult = Result<string>.Success("test");

        // Can be assigned to base Result reference
        Result baseResult = genericResult;

        Assert.True(baseResult.IsSuccess);
    }

    [Fact]
    public void HasFieldErrors_ReturnsFalse_WhenFieldErrorsIsNull()
    {
        var result = Result.Failure("simple error");

        Assert.False(result.HasFieldErrors);
    }

    [Fact]
    public void HasFieldErrors_ReturnsFalse_WhenFieldErrorsIsEmpty()
    {
        var result = Result.ValidationFailure(new Dictionary<string, string[]>());

        Assert.False(result.HasFieldErrors);
    }

    private class TestDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
