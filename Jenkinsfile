pipeline {
  agent any
  environment {
    APP_NAME    = 'livability.api'
    IMAGE_LOCAL = 'livability.api:latest'          // 本機 image 名
    IMAGE_TAG   = "livability.api:${env.BUILD_NUMBER}"
    CONTAINER   = 'livability.api'
    PORT_HOST   = '5000'
    PORT_APP    = '5000'

    // 用 host.docker.internal 連原生 MySQL
    DB_CONN     = 'Server=host.docker.internal;Port=3306;Database=appdb;User=root;Password=${DB_PASS};SslMode=None;'
  }

  options { timestamps() }

  stages {

    stage('Checkout') {
      steps { checkout scm }
    }

    stage('Build .NET') {
      steps {
        sh 'dotnet --version'
        sh 'dotnet restore'
        sh 'dotnet build -c Release'
      }
    }

    stage('Build Docker Image') {
      steps {
        sh """
          docker build -t ${IMAGE_LOCAL} -t ${IMAGE_TAG} .
          docker images | head -n 10
        """
      }
    }

    // === 可選：推到 Docker Hub（先在 Jenkins 新增「使用者/密碼」型 Credentials，ID: DOCKERHUB）===
    // stage('Push to Docker Hub') {
    //   environment { REG_IMAGE = 'yourdockerhub/your-repo' } // ex: myuser/myapp-api
    //   steps {
    //     withCredentials([usernamePassword(credentialsId: 'DOCKERHUB', passwordVariable: 'DOCKER_PASS', usernameVariable: 'DOCKER_USER')]) {
    //       sh """
    //         echo "${DOCKER_PASS}" | docker login -u "${DOCKER_USER}" --password-stdin
    //         docker tag ${IMAGE_TAG} ${REG_IMAGE}:${env.BUILD_NUMBER}
    //         docker tag ${IMAGE_LOCAL} ${REG_IMAGE}:latest
    //         docker push ${REG_IMAGE}:${env.BUILD_NUMBER}
    //         docker push ${REG_IMAGE}:latest
    //         docker logout
    //       """
    //     }
    //   }
    // }

    stage('Deploy (restart container on this EC2)') {
      steps {
        // 建議把 DB 密碼存在 Jenkins Credentials（Secret text，ID: DB_PASS）
        withCredentials([string(credentialsId: 'DB_PASS', variable: 'DB_PASS')]) {
          sh """
            # 停舊容器（如果存在）
            docker rm -f ${CONTAINER} || true

            # 跑新容器：加 host-gateway，讓容器能用 host.docker.internal 找到主機 MySQL
            docker run -d --name ${CONTAINER} \\
              --restart unless-stopped \\
              -p ${PORT_HOST}:${PORT_APP} \\
              --add-host=host.docker.internal:host-gateway \\
              -e ASPNETCORE_ENVIRONMENT=Production \\
              -e ConnectionStrings__DefaultConnection="${DB_CONN}" \\
              ${IMAGE_TAG}

            # 簡單健康檢查
            sleep 5
            docker ps --format "table {{.Names}}\\t{{.Status}}\\t{{.Ports}}"
          """
        }
      }
    }
  }

  post {
    failure {
      sh 'docker logs --tail=200 ${CONTAINER} || true'
    }
    always {
      sh 'docker system df || true'
    }
  }
}
