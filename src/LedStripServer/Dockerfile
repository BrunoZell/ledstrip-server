FROM microsoft/dotnet:2.1-sdk-stretch AS build
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.config .
COPY *.csproj .
RUN dotnet restore

# copy and build app
COPY . .
RUN dotnet publish -c release -o out


# Build runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime-stretch-slim-arm32v7 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Make system devices available for System.Device.Gpio
#VOLUME /sys # For PWM access
#VOLUME /dev # For GPIO access

# Use --device /dev/gpiomem instead

ENTRYPOINT ["dotnet", "LedStripServer.dll"]
