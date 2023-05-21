#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Stonks/Stonks.csproj", "Stonks/"]
RUN dotnet restore "Stonks/Stonks.csproj"
COPY . .
WORKDIR "/src/Stonks"
RUN dotnet build "Stonks.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Stonks.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Stonks.dll"]