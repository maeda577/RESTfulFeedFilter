ARG DOTNET_IMAGE_VERSION=6.0

FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_IMAGE_VERSION} AS builder
WORKDIR /build
COPY . .
RUN dotnet publish --configuration Release --output /rff

FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_IMAGE_VERSION}
COPY --from=builder /rff /rff
CMD ["dotnet", "/rff/Rff.dll"]
