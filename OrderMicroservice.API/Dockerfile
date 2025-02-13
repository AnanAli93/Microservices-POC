#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["OrderMicroservice.API/OrderMicroservice.API.csproj", "OrderMicroservice.API/"]
RUN dotnet restore "./OrderMicroservice.API/OrderMicroservice.API.csproj"
COPY . .
WORKDIR "/src/OrderMicroservice.API"
RUN dotnet build "./OrderMicroservice.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./OrderMicroservice.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV MONGODB_HOST=localhost
ENV MONGODB_PORT=27017
Env MONGODB_DATABASE=OrdersDatabase
ENV UserMicroserviceName=localhost
ENV UserMicroservicePort=9090
ENV ProductsMicroserviceName=localhost
ENV ProductsMicroservicePort=8080
ENV SQL_HOST=sql-container,1433
ENV SQL_USER=sa
ENV SQL_PASSWORD=181995Am@
ENV SQL_DATABASE=UsersDB
ENTRYPOINT ["dotnet", "OrderMicroservice.API.dll"]