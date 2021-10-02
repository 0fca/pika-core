FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build-env
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore PikaCore.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o out PikaCore.csproj

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:3.1.10-buster-slim
RUN apt install -y apt-transport-https
RUN apt update
RUN apt install -y ffmpeg
EXPOSE 80
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "PikaCore.dll"]
