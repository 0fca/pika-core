FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env

RUN apt update & apt install git
ADD "https://api.github.com/repos/0fca/Pika.Domain/commits?per_page=1" latest_commit
RUN git clone https://github.com/0fca/Pika.Domain
RUN cd Pika.Domain && dotnet restore
WORKDIR /Pika.Core
COPY . .
RUN dotnet publish PikaCore.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /Pika.Core
COPY --from=build-env /Pika.Core/out .
EXPOSE 5000
ENTRYPOINT ["dotnet", "PikaCore.dll"]
