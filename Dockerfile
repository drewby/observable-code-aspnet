FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
ARG REVISION

WORKDIR /app

# Copy project
COPY *.csproj ./
# Restore as distinct layers
RUN dotnet restore

# Copy everything else
COPY . ./

# Build and publish a release
RUN dotnet publish -c Release -o out /p:VersionSuffix=$REVISION-$(date +'%m%d%H%M')

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0

RUN apt-get update; apt-get install curl -y

WORKDIR /app

RUN echo 'APPINFO=$(dotnet testapi.dll --version) && \
    echo -e "Application\\t: $(sed -n 1p <<< "$APPINFO")" && \
    echo -e "Version\\t\\t: $(sed -n 2p <<< "$APPINFO")" && \
    echo -e "Revision\\t: $(sed -n 3p <<< "$APPINFO")" && \
    echo -e "Build time\\t: $(sed -n 4p <<< "$APPINFO")"' >> /root/.bashrc


ENV ASPNETCORE_URLS "http://*:5000"

RUN echo '#!/bin/bash' > run.sh; \
    echo 'APPINFO=$(dotnet testapi.dll --version)' >> run.sh; \
    echo 'export OTEL_RESOURCE_ATTRIBUTES=$OTEL_RESOURCE_ATTRIBUTES,service.version=$(sed -n 2p <<< "$APPINFO"),service.revision=$(sed -n 3p <<< "$APPINFO"),service.buildtime=$(sed -n 4p <<< "$APPINFO")' >> run.sh; \
    echo 'dotnet testapi.dll' >> run.sh; \
    chmod +x run.sh

COPY --from=build-env /app/out .

ENTRYPOINT ["/app/run.sh"]
