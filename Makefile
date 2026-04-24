run_debug:
	dotnet run --project src/BomApp.UI/BomApp.UI.csproj --configuration Debug
build:
	dotnet build src/BomApp.UI/BomApp.UI.csproj --configuration Debug

migrate_db:
	dotnet ef database update --project src/BomApp.Infrastructure/BomApp.Infrastructure.csproj --startup-project src/BomApp.Infrastructure/BomApp.Infrastructure.csproj --context BomDbContext 