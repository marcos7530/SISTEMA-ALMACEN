using SistemaAlmacen.Business.Interfaces;
using SistemaAlmacen.Data.Entities;
using SistemaAlmacen.Data.Repositories;
using SistemaAlmacen.Shared.Common;
using SistemaAlmacen.Shared.DTOs;
using SistemaAlmacen.Shared.DTOs.Clientes;

namespace SistemaAlmacen.Business.Services;

/// <summary>
/// Implementación del servicio de clientes con validaciones de negocio.
/// </summary>
public class ClienteService : IClienteService
{
    private readonly IUnitOfWork _unitOfWork;

    public ClienteService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    /// <inheritdoc />
    public async Task<PaginatedResult<ClienteDto>> GetClientesAsync(ClienteFilter filter)
    {
        var result = await _unitOfWork.Clientes.GetActivosAsync(filter);

        var items = new List<ClienteDto>();
        foreach (var cliente in result.Items)
        {
            var saldo = cliente.CuentaCorrienteHabilitada
                ? await _unitOfWork.Clientes.GetSaldoAsync(cliente.Id)
                : 0m;
            items.Add(MapToDto(cliente, saldo));
        }

        return new PaginatedResult<ClienteDto>
        {
            Items = items,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };
    }

    /// <inheritdoc />
    public async Task<ClienteDto?> GetByIdAsync(int id)
    {
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(id);

        if (cliente is null || !cliente.Activo)
            return null;

        var saldo = cliente.CuentaCorrienteHabilitada
            ? await _unitOfWork.Clientes.GetSaldoAsync(cliente.Id)
            : 0m;

        return MapToDto(cliente, saldo);
    }

    /// <inheritdoc />
    public async Task<Result<ClienteDto>> CreateAsync(CreateClienteRequest request)
    {
        var validationResult = ValidateClienteRequest(request.Nombre, request.Email, request.LimiteCredito);
        if (validationResult is not null)
            return validationResult;

        var documento = string.IsNullOrWhiteSpace(request.Documento) ? null : request.Documento.Trim();

        // Validar unicidad de documento entre clientes activos
        if (documento is not null && await _unitOfWork.Clientes.ExisteDocumentoActivoAsync(documento))
            return Result<ClienteDto>.Failure("Ya existe un cliente activo con ese documento.", "DOCUMENTO_DUPLICADO");

        var now = DateTime.UtcNow;
        var cliente = new Cliente
        {
            Nombre = request.Nombre.Trim(),
            Documento = documento,
            CondicionIva = request.CondicionIva,
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim(),
            Direccion = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim(),
            CuentaCorrienteHabilitada = request.CuentaCorrienteHabilitada,
            LimiteCredito = request.LimiteCredito,
            Activo = true,
            FechaCreacion = now,
            FechaModificacion = now
        };

        await _unitOfWork.Clientes.AddAsync(cliente);
        await _unitOfWork.SaveChangesAsync();

        return Result<ClienteDto>.Success(MapToDto(cliente, 0m));
    }

    /// <inheritdoc />
    public async Task<Result<ClienteDto>> UpdateAsync(int id, UpdateClienteRequest request)
    {
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(id);

        if (cliente is null || !cliente.Activo)
            return Result<ClienteDto>.Failure("El cliente no fue encontrado.", "NOT_FOUND");

        var validationResult = ValidateClienteRequest(request.Nombre, request.Email, request.LimiteCredito);
        if (validationResult is not null)
            return validationResult;

        var documento = string.IsNullOrWhiteSpace(request.Documento) ? null : request.Documento.Trim();

        if (documento is not null && await _unitOfWork.Clientes.ExisteDocumentoActivoAsync(documento, id))
            return Result<ClienteDto>.Failure("Ya existe un cliente activo con ese documento.", "DOCUMENTO_DUPLICADO");

        cliente.Nombre = request.Nombre.Trim();
        cliente.Documento = documento;
        cliente.CondicionIva = request.CondicionIva;
        cliente.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        cliente.Telefono = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
        cliente.Direccion = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim();
        cliente.CuentaCorrienteHabilitada = request.CuentaCorrienteHabilitada;
        cliente.LimiteCredito = request.LimiteCredito;
        cliente.FechaModificacion = DateTime.UtcNow;

        _unitOfWork.Clientes.Update(cliente);
        await _unitOfWork.SaveChangesAsync();

        var saldo = cliente.CuentaCorrienteHabilitada
            ? await _unitOfWork.Clientes.GetSaldoAsync(cliente.Id)
            : 0m;

        return Result<ClienteDto>.Success(MapToDto(cliente, saldo));
    }

    /// <inheritdoc />
    public async Task<Result> DeactivateAsync(int id)
    {
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(id);

        if (cliente is null || !cliente.Activo)
            return Result.Failure("El cliente no fue encontrado.", "NOT_FOUND");

        // No permitir baja con saldo deudor pendiente
        if (cliente.CuentaCorrienteHabilitada)
        {
            var saldo = await _unitOfWork.Clientes.GetSaldoAsync(cliente.Id);
            if (saldo > 0)
                return Result.Failure(
                    $"No se puede dar de baja un cliente con saldo deudor pendiente ({saldo:C}).",
                    "SALDO_PENDIENTE");
        }

        cliente.Activo = false;
        cliente.FechaModificacion = DateTime.UtcNow;

        _unitOfWork.Clientes.Update(cliente);
        await _unitOfWork.SaveChangesAsync();

        return Result.Success();
    }

    private static Result<ClienteDto>? ValidateClienteRequest(string nombre, string? email, decimal limiteCredito)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(nombre))
            errors["Nombre"] = new[] { "El nombre es requerido." };
        else if (nombre.Trim().Length > 150)
            errors["Nombre"] = new[] { "El nombre no puede exceder 150 caracteres." };

        if (!string.IsNullOrWhiteSpace(email) && email.Trim().Length > 254)
            errors["Email"] = new[] { "El correo no puede exceder 254 caracteres." };

        if (limiteCredito < 0 || limiteCredito > 999_999_999.99m)
            errors["LimiteCredito"] = new[] { "El límite de crédito debe estar entre 0 y 999,999,999.99." };

        if (errors.Count > 0)
            return Result<ClienteDto>.ValidationFailure(errors);

        return null;
    }

    private static ClienteDto MapToDto(Cliente cliente, decimal saldo)
    {
        return new ClienteDto
        {
            Id = cliente.Id,
            Nombre = cliente.Nombre,
            Documento = cliente.Documento,
            CondicionIva = cliente.CondicionIva,
            Email = cliente.Email,
            Telefono = cliente.Telefono,
            Direccion = cliente.Direccion,
            CuentaCorrienteHabilitada = cliente.CuentaCorrienteHabilitada,
            LimiteCredito = cliente.LimiteCredito,
            Activo = cliente.Activo,
            SaldoCuentaCorriente = saldo
        };
    }
}
