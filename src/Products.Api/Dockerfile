# Se construye desde la raíz del repo: docker build -f src/Products.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Products.Api/Products.Api.csproj src/Products.Api/
RUN dotnet restore src/Products.Api/Products.Api.csproj
COPY src/Products.Api/ src/Products.Api/
RUN dotnet publish src/Products.Api/Products.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Products.Api.dll"]
