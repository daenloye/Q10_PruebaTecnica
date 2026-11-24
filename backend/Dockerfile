ARG DOTNET_RUNTIME_IMAGE=mcr.microsoft.com/dotnet/aspnet:8.0
FROM mcr.microsoft.com/dotnet/sdk:10.0.404 AS build
WORKDIR /src

COPY rules.md api.md ./
COPY backend/TutorFlow.Api/TutorFlow.Api.csproj backend/TutorFlow.Api/
RUN --mount=type=secret,id=docker-ca,target=/tmp/docker-ca.crt,required=false \
    if [ -s /tmp/docker-ca.crt ] && grep -q "BEGIN CERTIFICATE" /tmp/docker-ca.crt; then \
      cp /tmp/docker-ca.crt /usr/local/share/ca-certificates/tutorflow-local-ca.crt && update-ca-certificates; \
    fi && dotnet restore backend/TutorFlow.Api/TutorFlow.Api.csproj

COPY backend/TutorFlow.Api/ backend/TutorFlow.Api/
RUN dotnet publish backend/TutorFlow.Api/TutorFlow.Api.csproj -c Release -o /out --no-restore

FROM ${DOTNET_RUNTIME_IMAGE} AS runtime
RUN command -v curl >/dev/null 2>&1 || (apt-get update \
    && apt-get install -y --no-install-recommends ca-certificates curl \
    && rm -rf /var/lib/apt/lists/*)
RUN --mount=type=secret,id=docker-ca,target=/tmp/docker-ca.crt,required=false \
    if [ -s /tmp/docker-ca.crt ] && grep -q "BEGIN CERTIFICATE" /tmp/docker-ca.crt; then \
      cp /tmp/docker-ca.crt /usr/local/share/ca-certificates/tutorflow-local-ca.crt && update-ca-certificates; \
    fi
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TutorFlow.Api.dll"]
