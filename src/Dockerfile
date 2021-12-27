FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

ENV RABBIT_HOST "localhost"
ENV RABBIT_PORT "5672"

COPY ./bin/Release/net5.0/publish/ /app

ENTRYPOINT ["dotnet", "Akka.Streams.RabbitDemo.dll"]
