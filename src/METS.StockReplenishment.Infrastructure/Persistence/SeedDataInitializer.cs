public static class SeedDataInitializer
{
    public static async Task SeedAsync(StockReplenishmentDbContext dbContext)
    {
        if (dbContext.Locations.Any() || dbContext.ReplenishmentRequests.Any())
        {
            return;
        }

        var locations = new List<Location>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Code = "HITACHI-Ludvika",
                Name = "HITACHI-Ludvika"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "HITACHI-Västerås",
                Name = "HITACHI-Västerås"
            },
            new()
            {
                Id = Guid.NewGuid(),
                Code = "HITACHI-Gothenburg",
                Name = "HITACHI-Gothenburg"
            }
        };

        var draftRequest = new ReplenishmentRequest
        {
            Id = Guid.NewGuid(),
            LocationCode = "HITACHI-Ludvika",
            Priority = Priority.Normal,
            Status = RequestStatus.Draft,
            ValidationStatus = ValidationStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddHours(-6),
            Items =
            [
                new RequestItem
                {
                    Id = Guid.NewGuid(),
                    ArticleNumber = "ART-1001",
                    Description = "Bearing",
                    RequestedQuantity = 12,
                    FulfilledQuantity = 0
                },
                new RequestItem
                {
                    Id = Guid.NewGuid(),
                    ArticleNumber = "ART-1002",
                    Description = "Seal Kit",
                    RequestedQuantity = 4,
                    FulfilledQuantity = 0
                }
            ]
        };

        var approvedRequest = new ReplenishmentRequest
        {
            Id = Guid.NewGuid(),
            LocationCode = "HITACHI-Västerås",
            Priority = Priority.Urgent,
            Status = RequestStatus.Approved,
            ValidationStatus = ValidationStatus.Valid,
            CreatedAt = DateTime.UtcNow.AddHours(-10),
            SubmittedAt = DateTime.UtcNow.AddHours(-9),
            ReviewedAt = DateTime.UtcNow.AddHours(-8),
            Items =
            [
                new RequestItem
                {
                    Id = Guid.NewGuid(),
                    ArticleNumber = "ART-2001",
                    Description = "Hydraulic Hose",
                    RequestedQuantity = 2,
                    FulfilledQuantity = 0
                }
            ]
        };

        var fulfilledRequest = new ReplenishmentRequest
        {
            Id = Guid.NewGuid(),
            LocationCode = "HITACHI-Gothenburg",
            Priority = Priority.Low,
            Status = RequestStatus.Fulfilled,
            ValidationStatus = ValidationStatus.Valid,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            SubmittedAt = DateTime.UtcNow.AddHours(-20),
            ReviewedAt = DateTime.UtcNow.AddHours(-18),
            FulfilledAt = DateTime.UtcNow.AddHours(-12),
            Items =
            [
                new RequestItem
                {
                    Id = Guid.NewGuid(),
                    ArticleNumber = "ART-3001",
                    Description = "Fastener Pack",
                    RequestedQuantity = 50,
                    FulfilledQuantity = 50
                }
            ]
        };

        // Added submitted and rejected requests for testing purposes

        var submittedRequest = new ReplenishmentRequest
        {
            Id = Guid.NewGuid(),
            LocationCode = "HITACHI-Ludvika",
            Priority = Priority.Normal,
            Status = RequestStatus.Submitted,
            ValidationStatus = ValidationStatus.Valid,
            CreatedAt = DateTime.UtcNow.AddHours(-5),
            SubmittedAt = DateTime.UtcNow.AddHours(-4),
            Items =
            [
                new RequestItem
                {
                    Id = Guid.NewGuid(),
                    ArticleNumber = "ART-4001",
                    Description = "Filter Element",
                    RequestedQuantity = 10,
                    FulfilledQuantity = 0
                }
            ]
        };

        var rejectedRequest = new ReplenishmentRequest
        {
            Id = Guid.NewGuid(),
            LocationCode = "HITACHI-Västerås",
            Priority = Priority.Urgent,
            Status = RequestStatus.Rejected,
            ValidationStatus = ValidationStatus.Invalid,
            RejectionReason = "Insufficient stock available.",
            CreatedAt = DateTime.UtcNow.AddHours(-8),
            SubmittedAt = DateTime.UtcNow.AddHours(-7),
            ReviewedAt = DateTime.UtcNow.AddHours(-6),
            Items =
            [
                new RequestItem
                {
                    Id = Guid.NewGuid(),
                    ArticleNumber = "ART-5001",
                    Description = "Control Valve",
                    RequestedQuantity = 5,
                    FulfilledQuantity = 0
                }
            ]
        };

        await dbContext.Locations.AddRangeAsync(locations);
        await dbContext.ReplenishmentRequests.AddRangeAsync(draftRequest, approvedRequest, fulfilledRequest, submittedRequest, rejectedRequest);
        await dbContext.SaveChangesAsync();
    }
}