# Build image for ShareX-slim. Windows-targeted (WinForms) assemblies compile
# fine on Linux via EnableWindowsTargeting; only running them needs Windows.
FROM mcr.microsoft.com/dotnet/sdk:9.0

# These paths are backed by named volumes. A named volume takes its ownership
# from the image contents at that path, so create them world-writable to let an
# arbitrary (host-matching) uid write into them.
RUN mkdir -p /nuget /dotnet-home && chmod 1777 /nuget /dotnet-home

ENV HOME=/dotnet-home \
    NUGET_PACKAGES=/nuget \
    DOTNET_CLI_HOME=/dotnet-home \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1

WORKDIR /src
