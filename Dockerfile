FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore PikaCore.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out PikaCore.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:5.0
EXPOSE 80
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "PikaCore.dll"]
