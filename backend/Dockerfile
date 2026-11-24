FROM mcr.microsoft.com/dotnet/sdk:10.0.404 AS build
WORKDIR /src

COPY rules.md api.md ./
COPY backend/TutorFlow.Api/TutorFlow.Api.csproj backend/TutorFlow.Api/
RUN dotnet restore backend/TutorFlow.Api/TutorFlow.Api.csproj

COPY backend/TutorFlow.Api/ backend/TutorFlow.Api/
RUN dotnet publish backend/TutorFlow.Api/TutorFlow.Api.csproj -c Release -o /out --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /out .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "TutorFlow.Api.dll"]
