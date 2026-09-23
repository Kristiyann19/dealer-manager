# dealer-manager

## Architecture

- `DealerManager.WebAPI/`: WebAPI host and controllers.
- `DealerManager.Domain/`: entities, enums, and shared domain types. No project dependencies.
- `DealerManager.Application/`: DTOs, filters, repository and service interfaces. References Domain.
- `DealerManager.Infrastructure/`: mapping profiles, extensions, helpers, repository and service implementations. References Application.

WebAPI references Application and Infrastructure and acts as the composition root for dependency injection. Keep domain logic independent of infrastructure and HTTP concerns. Empty folders contain `.gitkeep` files so the structure is preserved in Git.

Build from the repository root:

```sh
dotnet build DealerManager.slnx
```

Build the Windows container from the repository root:

```sh
docker build -f DealerManager.WebAPI/Dockerfile .
```
