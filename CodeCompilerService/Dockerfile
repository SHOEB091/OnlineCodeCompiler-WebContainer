FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["CodeCompilerService.csproj", "./"]
RUN dotnet restore "CodeCompilerService.csproj"
COPY . .
RUN dotnet build "CodeCompilerService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodeCompilerService.csproj" -c Release -o /app/publish

FROM ubuntu:22.04 AS final
WORKDIR /app

# Install necessary compilers and runtimes
RUN apt-get update && apt-get install -y \
    software-properties-common \
    && add-apt-repository ppa:deadsnakes/ppa \
    && apt-get update \
    && apt-get install -y \
    python3.10 \
    python3.10-distutils \
    python3-pip \
    openjdk-17-jdk \
    gcc \
    g++ \
    nodejs \
    npm \
    dotnet-sdk-8.0 \
    && rm -rf /var/lib/apt/lists/*

# Set up Python environment
RUN python3 -m pip install --upgrade pip

# Set up Java environment
ENV JAVA_HOME=/usr/lib/jvm/java-17-openjdk-amd64
ENV PATH=$PATH:$JAVA_HOME/bin

# Set up Node.js environment
ENV NODE_PATH=/usr/lib/nodejs

COPY --from=publish /app/publish .

# Create a directory for temporary files
RUN mkdir -p /tmp/CodeCompiler && \
    chmod -R 777 /tmp/CodeCompiler

# Expose the port the app runs on
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "CodeCompilerService.dll", "--urls", "http://0.0.0.0:8080"] 