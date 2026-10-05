# Builds both .NET images from the repo root:
#   docker build -f deploy/dotnet.Dockerfile --target api .
#   docker build -f deploy/dotnet.Dockerfile --target migrator .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /repo

# Restore first, from project files only, so source edits don't bust the package cache layer.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/SkillCert.Domain/SkillCert.Domain.csproj src/SkillCert.Domain/
COPY src/SkillCert.Infrastructure/SkillCert.Infrastructure.csproj src/SkillCert.Infrastructure/
COPY src/SkillCert.ServiceDefaults/SkillCert.ServiceDefaults.csproj src/SkillCert.ServiceDefaults/
COPY src/SkillCert.Api/SkillCert.Api.csproj src/SkillCert.Api/
COPY src/SkillCert.Migrator/SkillCert.Migrator.csproj src/SkillCert.Migrator/
RUN dotnet restore src/SkillCert.Api/SkillCert.Api.csproj \
 && dotnet restore src/SkillCert.Migrator/SkillCert.Migrator.csproj

COPY src/ src/
# The OpenAPI document is written at dev build time for the web client; images don't need it.
RUN dotnet publish src/SkillCert.Api/SkillCert.Api.csproj -c Release --no-restore -o /out/api \
      -p:OpenApiGenerateDocuments=false \
 && dotnet publish src/SkillCert.Migrator/SkillCert.Migrator.csproj -c Release --no-restore -o /out/migrator

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS api
WORKDIR /app
COPY --from=build /out/api .
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "SkillCert.Api.dll"]

# Applies migrations, seeds demo data when Seed__DemoData=true, then exits.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS migrator
WORKDIR /app
COPY --from=build /out/migrator .
USER $APP_UID
ENTRYPOINT ["dotnet", "SkillCert.Migrator.dll"]
