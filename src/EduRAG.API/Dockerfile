FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EduRAG.API/EduRAG.API.csproj", "EduRAG.API/"]
COPY ["../EduRAG.Infrastructure/EduRAG.Infrastructure.csproj", "EduRAG.Infrastructure/"]
COPY ["../EduRAG.Application/EduRAG.Application.csproj", "EduRAG.Application/"]
COPY ["../EduRAG.Domain/EduRAG.Domain.csproj", "EduRAG.Domain/"]
RUN dotnet restore "EduRAG.API/EduRAG.API.csproj"
COPY . .
RUN dotnet build "EduRAG.API/EduRAG.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EduRAG.API/EduRAG.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "EduRAG.API.dll"]
