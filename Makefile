run_debug:
	dotnet run --project src/BomApp.UI/BomApp.UI.csproj --configuration Debug
build:
	dotnet build src/BomApp.UI/BomApp.UI.csproj --configuration Debug

publish_win:
	dotnet publish src/BomApp.UI/BomApp.UI.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true --output artifacts/publish/BomApp.UI/win-x64

publish_cli:
	dotnet publish src/BomApp.Cli/BomApp.Cli.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=false -p:PublishReadyToRun=true --output artifacts/publish/BomApp.Cli/win-x64

msi:
	dotnet build installer/BomApp.Installer/BomApp.Installer.wixproj --configuration Release

migrate_db:
	dotnet ef database update --project src/BomApp.Infrastructure/BomApp.Infrastructure.csproj --startup-project src/BomApp.Infrastructure/BomApp.Infrastructure.csproj --context BomDbContext 
cli_run:
	dotnet run --project src/BomApp.Cli/BomApp.Cli.csproj -- calculate --from 2026-05-01 --to 2026-05-29 --mode per-document