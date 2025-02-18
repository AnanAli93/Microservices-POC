# Base image for the build stage
FROM alpine:latest AS builder

# Create a directory to hold initialization scripts
WORKDIR /app

# Copy initialization scripts from the host to the container
COPY sql-init /app

# Install required packages (adjust based on your script needs)
RUN apk add --no-cache openssl

# Base image for the final SQL Server image
FROM mcr.microsoft.com/mssql/server:2019-latest

# Set environment variables for SQL Server
ENV ACCEPT_EULA=Y
ENV SA_PASSWORD=181995Am@
ENV SQL_HOST=sql-container
ENV SQL_PORT=1433
ENV SQL_USER=sa
ENV SQL_PASSWORD=181995Am@
ENV SQL_DATABASE=UsersDB

# Expose the SQL Server port
EXPOSE 1433

# Copy initialization scripts from the build stage
COPY --from=builder /app /docker-entrypoint-initdb.d

# Run SQL Server
CMD /opt/mssql/bin/sqlservr
