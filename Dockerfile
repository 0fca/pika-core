FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

COPY *.csproj ./
RUN dotnet restore

RUN apt update & apt install git
RUN git clone https://github.com/0fca/Pika.Domain
RUN cd Pika.Domain && dotnet restore
WORKDIR /app
COPY . ./
RUN dotnet publish PikaCore.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
EXPOSE 80
ENTRYPOINT ["dotnet", "PikaCore.dll"]
