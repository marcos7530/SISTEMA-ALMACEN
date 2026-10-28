using FluentAssertions;
using SistemaAlmacen.Shared.Common;

namespace SistemaAlmacen.Data.Tests.Common;

public class ArgentinaTimeZoneTests
{
    [Fact]
    public void Instance_ResolvesArgentinaOffset_MinusThreeHours()
    {
        // Argentina no observa horario de verano: el offset es -3 horas de forma constante.
        var offset = ArgentinaTimeZone.Instance.GetUtcOffset(new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc));

        offset.Should().Be(TimeSpan.FromHours(-3));
    }

    [Fact]
    public void ToLocal_KnownUtcInstant_EqualsUtcMinusThreeHours()
    {
        var utc = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        var local = ArgentinaTimeZone.ToLocal(utc);

        local.Should().Be(new DateTime(2024, 6, 15, 9, 0, 0));
    }

    [Fact]
    public void ToLocal_EfUnspecifiedKind_TreatedAsUtc()
    {
        // EF devuelve Kind=Unspecified pero los valores fueron escritos como UTC.
        var utcAsUnspecified = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Unspecified);

        var local = ArgentinaTimeZone.ToLocal(utcAsUnspecified);

        local.Should().Be(new DateTime(2024, 6, 15, 9, 0, 0));
    }

    [Fact]
    public void ToUtcRange_SaleOnNightOfDayD_FallsInsideRange()
    {
        // Escenario del bug reportado: una venta realizada el día D a las 22:00 hora local
        // se guarda como UTC D+1 01:00. Debe entrar en el rango ToUtcRange(D-7, D).
        var d = new DateTime(2024, 6, 15);
        var saleUtc = new DateTime(2024, 6, 16, 1, 0, 0, DateTimeKind.Utc); // D 22:00 local

        var (inicioUtc, finUtc) = ArgentinaTimeZone.ToUtcRange(d.AddDays(-7), d);

        saleUtc.Should().BeOnOrAfter(inicioUtc);
        saleUtc.Should().BeOnOrBefore(finUtc);
    }

    [Fact]
    public void ToUtcRange_SaleEarlyMorningOfDayD_IsOnOrAfterLowerBound()
    {
        // Venta el día D a las 00:30 hora local = UTC D 03:30. Con fechaInicio=D debe ser >= inicioUtc.
        var d = new DateTime(2024, 6, 15);
        var saleUtc = new DateTime(2024, 6, 15, 3, 30, 0, DateTimeKind.Utc); // D 00:30 local

        var (inicioUtc, _) = ArgentinaTimeZone.ToUtcRange(d, d);

        saleUtc.Should().BeOnOrAfter(inicioUtc);
    }

    [Fact]
    public void ToUtcRange_NullBounds_YieldUtcMinAndMaxValue()
    {
        var (inicioUtc, finUtc) = ArgentinaTimeZone.ToUtcRange(null, null);

        inicioUtc.Should().Be(DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));
        finUtc.Should().Be(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc));
    }

    [Fact]
    public void ToUtcRange_LowerBound_IsLocalMidnightConvertedToUtc()
    {
        var d = new DateTime(2024, 6, 15);

        var (inicioUtc, _) = ArgentinaTimeZone.ToUtcRange(d, null);

        // Medianoche local D 00:00 = UTC D 03:00.
        inicioUtc.Should().Be(new DateTime(2024, 6, 15, 3, 0, 0));
    }

    [Fact]
    public void ToUtcRange_UpperBound_IsLocalEndOfDayConvertedToUtc()
    {
        var d = new DateTime(2024, 6, 15);

        var (_, finUtc) = ArgentinaTimeZone.ToUtcRange(null, d);

        // Fin del día local D 23:59:59.9999999 = UTC D+1 02:59:59.9999999.
        finUtc.Should().Be(new DateTime(2024, 6, 16, 0, 0, 0).AddTicks(-1).AddHours(3));
    }
}
