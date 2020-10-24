#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0.0-rc.2-buster-slim-amd64 AS base
WORKDIR /app
#EXPOSE 80
#EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0.100-rc.2-buster-slim-amd64 AS build
WORKDIR /src
COPY ["FeederBot.csproj", "./"]
RUN dotnet restore "FeederBot.csproj"
COPY . .
#WORKDIR "/src/FeederBot"
RUN ls -la
RUN dotnet build "FeederBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FeederBot.csproj" -c Release -o /app/publish 

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FeederBot.dll"]