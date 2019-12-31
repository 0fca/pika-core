FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY PikaCore.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN ls -lah
RUN dotnet publish PikaCore.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:2.2
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "PikaCore.dll"]
