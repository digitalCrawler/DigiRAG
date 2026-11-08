FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/DigiRAG.Domain/DigiRAG.Domain.csproj", "src/DigiRAG.Domain/"]
COPY ["src/DigiRAG.Application/DigiRAG.Application.csproj", "src/DigiRAG.Application/"]
COPY ["src/DigiRAG.Infrastructure/DigiRAG.Infrastructure.csproj", "src/DigiRAG.Infrastructure/"]
COPY ["src/DigiRAG.Api/DigiRAG.Api.csproj", "src/DigiRAG.Api/"]

RUN dotnet restore "src/DigiRAG.Api/DigiRAG.Api.csproj"

COPY . .
WORKDIR "/src/src/DigiRAG.Api"
RUN dotnet build "DigiRAG.Api.csproj" -c Release -o /app/build
RUN dotnet publish "DigiRAG.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENTRYPOINT ["dotnet", "DigiRAG.Api.dll"]
