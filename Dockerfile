# Use the official ASP.NET Core SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["HV-Travel.Web/HV-Travel.Web.csproj", "HV-Travel.Web/"]
COPY ["HV-Travel.Domain/HV-Travel.Domain.csproj", "HV-Travel.Domain/"]
COPY ["HV-Travel.Application/HV-Travel.Application.csproj", "HV-Travel.Application/"]
COPY ["HV-Travel.Infrastructure/HV-Travel.Infrastructure.csproj", "HV-Travel.Infrastructure/"]
RUN dotnet restore "HV-Travel.Web/HV-Travel.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/HV-Travel.Web"
RUN dotnet build "HV-Travel.Web.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "HV-Travel.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HV-Travel.Web.dll"]
