FROM mcr.microsoft.com/dotnet/sdk:7.0.404-1-alpine3.18-amd64 AS build-env
RUN apk add xmlstarlet bash --update --repository http://dl-4.alpinelinux.org/alpine/edge/testing \
	&& rm -rf /var/cache/apk/*
RUN apk add git xmlstarlet
ADD "https://api.github.com/repos/0fca/Pika.Domain/commits?per_page=1" latest_commit
RUN git clone https://github.com/0fca/Pika.Domain
RUN cd Pika.Domain && dotnet restore
WORKDIR /Pika.Core
COPY . .
RUN xmlstarlet edit \
           --update "//Project/PropertyGroup/Version" \
           --value "${MAJOR}.${MINOR}.${REV}" PikaCore.csproj > PikaCore.csproj.new
RUN mv PikaCore.csproj.new PikaCore.csproj
RUN dotnet publish PikaCore.csproj -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /Pika.Core
COPY --from=build-env /Pika.Core/out .
EXPOSE 5000
ENTRYPOINT ["dotnet", "PikaCore.dll"]
