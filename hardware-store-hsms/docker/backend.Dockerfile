FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/HSMS.sln backend/HSMS.sln
COPY backend/HSMS.API/HSMS.API.csproj backend/HSMS.API/
COPY backend/HSMS.Application/HSMS.Application.csproj backend/HSMS.Application/
COPY backend/HSMS.Domain/HSMS.Domain.csproj backend/HSMS.Domain/
COPY backend/HSMS.Infrastructure/HSMS.Infrastructure.csproj backend/HSMS.Infrastructure/
RUN dotnet restore backend/HSMS.API/HSMS.API.csproj

COPY backend/ backend/
RUN dotnet publish backend/HSMS.API/HSMS.API.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HSMS.API.dll"]
