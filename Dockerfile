# Set the major version of dotnet
ARG DOTNET_VERSION=10.0

# ==============================================
# .NET SDK: Build
# ==============================================
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}-azurelinux3.0 AS build
WORKDIR /build
ARG CI
ENV CI=${CI}

# Mount GitHub Token as a Docker secret so that NuGet Feed can be accessed
#RUN --mount=type=secret,id=github_token dotnet nuget add source --username USERNAME --password $(cat /run/secrets/github_token) --store-password-in-clear-text --name github "https://nuget.pkg.github.com/DFE-Digital/index.json"

# Copy the application code
COPY ./src/ ./src/

# Build and publish the dotnet solution
RUN dotnet restore ./src/Dfe.Mcp.Server.slnx && \
    dotnet build ./src/Dfe.Mcp.Server.Web --no-restore -c Release && \
    dotnet publish ./src/Dfe.Mcp.Server.Web --no-build -o /app

# ==============================================
# .NET Runtime: Publish
# ==============================================
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_VERSION}-azurelinux3.0 AS final
WORKDIR /app
LABEL org.opencontainers.image.source="https://github.com/DFE-Digital/rsd-mcp-server"
LABEL org.opencontainers.image.description="Dfe.Mcp.Server"

COPY --from=build /app /app
COPY ./scripts/docker-entrypoint.sh /app/docker-entrypoint.sh
RUN sed -i 's/\r//' ./docker-entrypoint.sh && \
    chmod +x ./docker-entrypoint.sh

USER $APP_UID

ENTRYPOINT ["./docker-entrypoint.sh"]
CMD ["dotnet", "Dfe.Mcp.Server.Web.dll"]