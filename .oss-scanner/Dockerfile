# Build environment for Anthropic's OSS Scanner (https://github.com/anthropics/oss-scanner).
# The scanner builds this with the repository as the build context, with network access; the scan that follows runs
# offline, so everything the build and the tests need is restored here.
FROM mcr.microsoft.com/dotnet/sdk:10.0

# The test project targets net8.0, so add the 8.0 runtime next to the 10.0 SDK.
COPY --from=mcr.microsoft.com/dotnet/runtime:8.0 /usr/share/dotnet/shared/Microsoft.NETCore.App /usr/share/dotnet/shared/Microsoft.NETCore.App

ENV DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

# scanner contract: the checkout lives inside the image, at /src
COPY . /src
WORKDIR /src

# Restore every package now; nothing can be fetched once the network is gone.
RUN dotnet restore BACnet.csproj \
    && dotnet restore Ethernet/BACnet.Ethernet.csproj \
    && dotnet restore Serial/BACnet.Serial.csproj \
    && dotnet restore Tests/BACnet.Tests.csproj

# net48 needs the .NET Framework targeting pack (Windows only), so build the .NET targets with debug info.
RUN for tfm in net8.0 net10.0; do \
      for proj in BACnet.csproj Ethernet/BACnet.Ethernet.csproj Serial/BACnet.Serial.csproj; do \
        dotnet build "$proj" --no-restore -c Debug -f "$tfm" || exit 1; \
      done; \
    done \
    && dotnet build Tests/BACnet.Tests.csproj --no-restore -c Debug

# Run the unit tests so the image is known to work, without letting a failing test abort the build.
RUN dotnet test --project Tests/BACnet.Tests.csproj --no-build -c Debug \
    || echo "WARNING: dotnet test reported failures"
