# ---------- build ----------
# Node is needed to compile the Angular client into wwwroot during dotnet publish.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN apt-get update \
 && apt-get install -y --no-install-recommends curl ca-certificates gnupg \
 && curl -fsSL https://deb.nodesource.com/setup_22.x | bash - \
 && apt-get install -y --no-install-recommends nodejs \
 && rm -rf /var/lib/apt/lists/*

COPY ["Directory.Build.props", "./"]
COPY ["src/DigiRAG.Domain/DigiRAG.Domain.csproj", "src/DigiRAG.Domain/"]
COPY ["src/DigiRAG.Application/DigiRAG.Application.csproj", "src/DigiRAG.Application/"]
COPY ["src/DigiRAG.Infrastructure/DigiRAG.Infrastructure.csproj", "src/DigiRAG.Infrastructure/"]
COPY ["src/DigiRAG.Api/DigiRAG.Api.csproj", "src/DigiRAG.Api/"]

RUN dotnet restore "src/DigiRAG.Api/DigiRAG.Api.csproj"

# Cache the npm install layer separately from the application sources.
COPY ["src/DigiRAG.Client/package.json", "src/DigiRAG.Client/"]
RUN cd src/DigiRAG.Client && npm install --no-audit --no-fund

COPY . .
RUN dotnet publish "src/DigiRAG.Api/DigiRAG.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Uploaded documents live here; mount a volume at this path in production.
RUN mkdir -p /app/App_Data/Storage
ENV Storage__LocalPath=/app/App_Data/Storage

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "DigiRAG.Api.dll"]
