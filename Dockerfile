# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy file sln và csproj để restore dependencies trước
COPY *.sln .
COPY BookingTour/*.csproj ./BookingTour/
RUN dotnet restore

# Copy toàn bộ source code và build
COPY . .
WORKDIR /app/BookingTour
RUN dotnet publish -c Release -o /app/out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "BookingTour.dll"]