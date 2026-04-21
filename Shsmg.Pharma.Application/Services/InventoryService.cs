using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Shsmg.Pharma.Application.Common;
using Shsmg.Pharma.Application.DTOs;
using Shsmg.Pharma.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Shsmg.Pharma.Application.Services;

public sealed class InventoryService(IPharmacyDbContext context, IValidator<InventoryItemDto> validator, ILogger<InventoryService> logger) : IInventoryService
{
    private readonly IPharmacyDbContext _context = context;
    private readonly IValidator<InventoryItemDto> _validator = validator;
    private readonly ILogger<InventoryService> _logger = logger;

    public async Task<IEnumerable<InventoryItemSummaryDto>> GetInventoryItemsAsync(string? searchQuery = null)
    {
        _logger.LogInformation("Attempting to retrieve inventory items.");
        var query = _context.InventoryItems
            .AsNoTracking()
            .Where(item => !item.IsDeleted);

        var normalized = searchQuery?.Trim();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            var pattern = $"%{normalized.Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(item =>
                EF.Functions.Like(item.HsnCode, pattern) ||
                EF.Functions.Like(item.Batch, pattern));
        }

        return await query
            .OrderBy(item => item.Description)
            .ThenBy(item => item.Batch)
            .Select(item => new InventoryItemSummaryDto
            {
                Id = item.Id,
                Description = item.Description,
                HsnCode = item.HsnCode,
                Batch = item.Batch,
                ExpiryDate = item.ExpiryDate,
                QuantityInStock = item.QuantityInStock,
                ReorderLevel = item.ReorderLevel,
                Rate = item.Rate,
                GstPercentage = item.GstPercentage
            })
            .ToListAsync();
    }

    public async Task<InventoryItemDto?> GetInventoryItemByIdAsync(Guid id)
    {
        _logger.LogInformation("Attempting to retrieve inventory item");
        return await _context.InventoryItems
            .AsNoTracking()
            .Where(item => item.Id == id && !item.IsDeleted)
            .Select(item => ToDto(item))
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<InventoryItemDto>> SearchInventoryItemsAsync(string query, int take = 10)
    {
        _logger.LogInformation("Attempting to search inventory items.");
        var normalized = query.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            _logger.LogInformation("Search query is empty or whitespace.");
            return [];
        }

        var pattern = $"%{normalized.Replace("%", "\\%").Replace("_", "\\_")}%";

        return await _context.InventoryItems
            .AsNoTracking()
            .Where(item => !item.IsDeleted &&
                (EF.Functions.Like(item.Description, pattern) ||
                 EF.Functions.Like(item.Batch, pattern) ||
                 EF.Functions.Like(item.HsnCode, pattern) ||
                 EF.Functions.Like(item.Mfg, pattern)))
            .OrderBy(item => item.Description)
            .ThenBy(item => item.Batch)
            .Take(Math.Max(1, take))
            .Select(item => ToDto(item))
            .ToListAsync();
    }

    public async Task<Guid> SaveInventoryItemAsync(InventoryItemDto dto)
    {
        _logger.LogInformation("Attempting to save inventory item.");
        await _validator.ValidateAndThrowAsync(dto);

        var now = DateTime.UtcNow;
        var isNew = dto.Id == Guid.Empty;

        InventoryItem entity;

        if (isNew)
        {
            entity = new InventoryItem
            {
                Id = Guid.NewGuid(),
                CreatedAt = now
            };

            _context.InventoryItems.Add(entity);
        }
        else
        {
            entity = await _context.InventoryItems.FirstOrDefaultAsync(item => item.Id == dto.Id)
                ?? throw new Exception("Inventory item not found.");
        }

        entity.Description = dto.Description.Trim();
        entity.HsnCode = dto.HsnCode.Trim();
        entity.Package = dto.Package.Trim();
        entity.Mfg = dto.Mfg.Trim();
        entity.Batch = dto.Batch.Trim();
        entity.ExpiryDate = dto.ExpiryDate.Trim();
        entity.QuantityInStock = dto.QuantityInStock;
        entity.ReorderLevel = dto.ReorderLevel;
        entity.Rate = dto.Rate;
        entity.GstPercentage = dto.GstPercentage;
        entity.LastModified = now;
        entity.IsDeleted = false;

        _logger.LogInformation("Saving inventory item.");

        await _context.SaveChangesAsync();
        return entity.Id;
    }

    public async Task DeleteInventoryItemAsync(Guid id)
    {
        _logger.LogInformation("Attempting to delete inventory item.");
        var entity = await _context.InventoryItems.FirstOrDefaultAsync(item => item.Id == id)
            ?? throw new Exception("Inventory item not found.");

        entity.IsDeleted = true;
        entity.LastModified = DateTime.UtcNow;
        _logger.LogInformation("Deleting inventory item.");

        await _context.SaveChangesAsync();
        _logger.LogInformation("Inventory item marked as deleted successfully.");
    }

    private static InventoryItemDto ToDto(InventoryItem item)
    {
        return new InventoryItemDto
        {
            Id = item.Id,
            Description = item.Description,
            HsnCode = item.HsnCode,
            Package = item.Package,
            Mfg = item.Mfg,
            Batch = item.Batch,
            ExpiryDate = item.ExpiryDate,
            QuantityInStock = item.QuantityInStock,
            ReorderLevel = item.ReorderLevel,
            Rate = item.Rate,
            GstPercentage = item.GstPercentage
        };
    }
}
