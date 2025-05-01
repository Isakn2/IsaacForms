#!/bin/bash

echo "Starting database migration script..."

# Debug: Check environment variables (redacting sensitive parts)
echo "Checking environment variables..."
echo "DEFAULT_CONNECTION exists: $(if [ -n "$DEFAULT_CONNECTION" ]; then echo "YES"; else echo "NO"; fi)"
if [ -n "$DEFAULT_CONNECTION" ]; then
    # Print only the beginning of the connection string to avoid exposing credentials
    CONNECTION_PREVIEW=$(echo $DEFAULT_CONNECTION | cut -c 1-30)
    echo "Connection string preview: ${CONNECTION_PREVIEW}..."
fi

# Format the connection string if necessary
if [[ $DEFAULT_CONNECTION == postgres://* ]]; then
    echo "Converting Render-style PostgreSQL URL to EF Core format..."
    
    # Parse components from the URL
    USER=$(echo $DEFAULT_CONNECTION | sed -n 's/postgres:\/\/\([^:]*\):.*/\1/p')
    PASSWORD=$(echo $DEFAULT_CONNECTION | sed -n 's/postgres:\/\/[^:]*:\([^@]*\).*/\1/p')
    HOST=$(echo $DEFAULT_CONNECTION | sed -n 's/postgres:\/\/[^@]*@\([^:]*\).*/\1/p')
    PORT=$(echo $DEFAULT_CONNECTION | sed -n 's/postgres:\/\/[^:]*:[^@]*@[^:]*:\([0-9]*\).*/\1/p')
    DB=$(echo $DEFAULT_CONNECTION | sed -n 's/postgres:\/\/[^:]*:[^@]*@[^:]*:[0-9]*\/\([^?]*\).*/\1/p')
    
    # Format the connection string for EF Core
    export ConnectionStrings__DefaultConnection="Host=${HOST};Port=${PORT};Database=${DB};Username=${USER};Password=${PASSWORD};SSL Mode=Require;Trust Server Certificate=true;"
    
    echo "Formatted connection string (preview): Host=${HOST};Port=${PORT};Database=${DB};Username=${USER};Password=****;"
else
    # If the connection string is already in the correct format, use it directly
    export ConnectionStrings__DefaultConnection="$DEFAULT_CONNECTION"
    echo "Using provided connection string format"
fi

# Apply database migrations
echo "Applying database migrations..."
dotnet ef database update --verbose

echo "Database migrations completed successfully."

# Start the application
echo "Starting the application..."
dotnet CustomFormsApp.dll