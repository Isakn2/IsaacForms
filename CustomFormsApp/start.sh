#!/bin/bash

echo "Starting database migration script..."

# Apply database migrations
echo "Applying database migrations..."
dotnet ef database update

echo "Database migrations completed successfully."

# Start the application
echo "Starting the application..."
dotnet CustomFormsApp.dll