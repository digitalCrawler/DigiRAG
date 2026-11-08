FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/DigiRAG.Domain/DigiRAG.Domain.csproj", "src/DigiRAG.Domain/"]
COPY ["src/DigiRAG.Application/DigiRAG.Application.csproj", "src/DigiRAG.Application/"]
COPY ["src/DigiRAG.Infrastructure/DigiRAG.Infrastructure.csproj", "src/DigiRAG.Infrastructure/"]
COPY ["src/DigiRAG.Worker/DigiRAG.Worker.csproj", "src/DigiRAG.Worker/"]

RUN dotnet restore "src/DigiRAG.Worker/DigiRAG.Worker.csproj"

COPY . .
WORKDIR "/src/src/DigiRAG.Worker"
RUN dotnet build "DigiRAG.Worker.csproj" -c Release -o /app/build
RUN dotnet publish "DigiRAG.Worker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DigiRAG.Worker.dll"]
