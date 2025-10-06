pipeline {
    agent any

    environment {
        IMAGE_NAME = "livability-api"
        APP_NAME   = "livability_api"
        DB_PASS    = credentials('DB_PASS')   // Jenkins Secret ID
    }

    stages {
        stage('Checkout') {
            steps {
                git branch: 'main', credentialsId: 'github-token', url: 'https://github.com/thomassite/Livability.Api.git'
            }
        }

        stage('Build .NET Project') {
            steps {
                sh '''
                    echo "=== Building .NET Project ==="
                    docker run --rm -v $PWD:/src -w /src mcr.microsoft.com/dotnet/sdk:8.0 \
                    dotnet publish -c Release -o out /p:UseAppHost=false
                '''
            }
        }

        stage('Build Docker Image') {
            steps {
                sh '''
                    echo "=== Building Docker Image ==="
                    docker build -t ${IMAGE_NAME}:latest .
                '''
            }
        }

        stage('Deploy Container') {
            steps {
                sh '''
                    echo "=== Restarting Container ==="
                    docker rm -f ${APP_NAME} 2>/dev/null || true

                    docker run -d \
                      --name ${APP_NAME} \
                      --restart unless-stopped \
                      -p 5000:5000 \
                      --add-host=host.docker.internal:host-gateway \
                      -e ASPNETCORE_ENVIRONMENT=Production \
                      -e ASPNETCORE_URLS=http://0.0.0.0:5000 \
                      -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Port=3306;Database=appdb;User=appuser;Password=${DB_PASS};SslMode=None;" \
                      ${IMAGE_NAME}:latest

                    docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
                '''
            }
        }
    }

    post {
        failure {
            echo "❌ Build or deployment failed"
        }
        success {
            echo "✅ API is running at port 5000"
        }
    }
}
