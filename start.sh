#!/bin/bash

echo "🚀 Starting GameStore API with Docker Compose..."

# Stop and remove old containers
echo "🧹 Cleaning up old containers..."
docker-compose down 2>/dev/null || true

# Build and start all services
echo "📦 Building and starting services..."
docker-compose up --build -d

echo "✅ All services started!"
echo "🌐 App: http://localhost:5000"
echo "📊 pgAdmin: http://localhost:5050"
echo "🐘 PostgreSQL: localhost:5432"

