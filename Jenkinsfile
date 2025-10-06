pipeline {
  agent any
  options { disableConcurrentBuilds() } // 避免小機器被拖掛
  environment {
    APP_NAME   = 'livability_api'
    IMAGE_NAME = 'livability-api'
    PORT_HOST  = '5000'   // 外部埠
    PORT_APP   = '5000'   // 容器內埠（配合你的 Dockerfile）
  }
  stages {
    stage('Checkout') {
      steps {
        // 防止 safe.directory 問題
        sh '''
          git config --global --add safe.directory `pwd` || true
        '''
        checkout scm
      }
    }

    stage('Build .NET (optional)') {
      steps {
        sh '''
          set -eux
          dotnet --info
          dotnet restore
          dotnet build -c Release
        '''
      }
    }

    stage('Build Docker image') {
      steps {
        sh '''
          set -eux
          docker build -t ${IMAGE_NAME}:${BUILD_NUMBER} -t ${IMAGE_NAME}:latest .
          docker images | head -n 10
        '''
      }
    }

    stage('Run/Restart container') {
      steps {
        withCredentials([string(credentialsId: 'DB_PASS', variable: 'DB_PASS')]) {
          sh '''
            set -eux
            # 先停同名容器（若存在）
            docker rm -f ${APP_NAME} 2>/dev/null || true

            # 跑起來（自動重啟、限制資源、加 host 映射連主機 MySQL）
            docker run -d --name ${APP_NAME} \
              --restart unless-stopped \
              -p ${PORT_HOST}:${PORT_APP} \
              --add-host=host.docker.internal:host-gateway \
              --memory=512m --memory-swap=1g --cpus="0.8" \
              -e ASPNETCORE_ENVIRONMENT=Production \
              -e ASPNETCORE_URLS=http://0.0.0.0:${PORT_APP} \
              -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Port=3306;Database=appdb;User=appuser;Password=${DB_PASS};SslMode=None;" \
              ${IMAGE_NAME}:${BUILD_NUMBER}

            # 簡單等待與健康檢查
            for i in {1..30}; do
              sleep 2
              if curl -fsS http://127.0.0.1:${PORT_HOST}/health || curl -fsS http://127.0.0.1:${PORT_HOST}; then
                echo "App is responding"
                break
              fi
              echo "waiting app..."
            done

            docker ps --format "table {{.Names}}\\t{{.Status}}\\t{{.Ports}}"
          '''
        }
      }
    }
  }

  post {
    failure {
      sh '''
        echo "==== recent container logs ===="
        docker logs --tail=200 ${APP_NAME} || true
      '''
    }
    always {
      sh 'docker system df || true'
    }
  }
}
