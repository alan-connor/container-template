# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

ARG BUILD_CONTAINER=mcr.microsoft.com/dotnet/sdk:10.0
ARG BASE_CONTAINER=mcr.microsoft.com/dotnet/aspnet:10.0

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM ${BASE_CONTAINER} AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080

# This stage restores the service project (cached until the .csproj changes)
FROM ${BUILD_CONTAINER} AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Container.Template.Api/Container.Template.Api.csproj", "src/Container.Template.Api/"]
RUN dotnet restore "src/Container.Template.Api/Container.Template.Api.csproj"
COPY src/ src/

# This stage builds and publishes the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/src/Container.Template.Api"
RUN dotnet publish "Container.Template.Api.csproj" -c ${BUILD_CONFIGURATION} -o /app/publish /p:UseAppHost=false --no-restore

# This stage is used in production or when running from VS in regular mode
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Container.Template.Api.dll"]
