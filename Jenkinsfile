pipeline {
    agent any

    environment {
        DOTNET_CLI_HOME = "C:\\Program Files\\dotnet"
        AZURE_SUBSCRIPTION_ID=credentials('subscription-id')
        AZURE_CLIENT_ID=credentials('client-id')
        AZURE_CLIENT_SECRET=credentials('client_secret')
        AZURE_TENANT_ID=credentials('Tenant-id')
        AZURE_RESOURCE_GROUP='manishadotnetapp'
        AZURE_VM_NAME='fromjenkinstovm'
        AZURE_VM_IP='20.192.28.184'
        AZURE_VM_USER='Manishasachdeva'
        AZURE_VM_PASSWORD=credentials('vmpassword-id')
        AZURE_STORAGE_ACCOUNT='stdotnetapp'
        AZURE_STORAGE_KEY=credentials('storage-key-id')
        AZURE_CONTAINER_NAME='condotnetapp'
        APPLICATION_ZIP='dotnetcore-sms.zip'
    }

    stages {
        stage('Checkout') {
            steps {
                checkout scm
            }
        }

        stage('Build') {
            steps {
                script {
                    // Restoring dependencies
                    //bat "cd ${DOTNET_CLI_HOME} && dotnet restore"
                    bat "dotnet restore"

                    // Building the application
                    bat "dotnet build --configuration Release"
                }
            }
        }

        stage('Test') {
            steps {
                script {
                    // Running tests
                    bat "dotnet test --no-restore --configuration Release"
                }
            }
        }

        stage('Publish') {
            steps {
                script {
                    // Publishing the application
                    bat "dotnet publish --no-restore --configuration Release --output .\\publish"
                    //compress the published output into a zip file
                    bat="powershell compress-Archive -Path .\\publish* -DestinationPath .\\${APPLICATION_ZIP} -Force"
                }
            }
        }
        stage('Azure Login'){
            steps {
                script {
                    //login to azure
                    bat 'az login --service-principal -u %AZURE_CLIENT_ID%' -p %AZURE_CLIENT_SECRET% --tenant %AZURE_TENANT_ID%
                    bat 'az account set --subscription %AZURE_SUBSCRIPTION_ID%'
                }
            }
        }
        stage('Upload to Azure Storage'){
            steps {
                script {
                    //uploading the ziped file to azure blob storage
                    bat 'az storage blob upload --account-name %AZURE_STORAGE_ACCOUNT% --account-key %AZURE_STORAGE_KEY% --container-name %AZURE_CONTAINER_NAME% --file %APPLICATION_ZIP% --name %APPLICATION_ZIP% --overwrite'
                }
            }
        }
        stage('Generate SAS Token'){
            steps {
                script {
                    //Generate SAS Token for the uploaded azure blob storage
                    def expiryDate = new Date() + 1
                    def expiryFormatted = expiryDate.format("yyyy-MM-dd'T'HH:mm:ss'Z'")
                    def sasTokenCommand = "az storage blob generate-sas --accoumt-name ${AZURE_STORAGE_ACCOUNT} --account-key ${AZURE_STORAGE_KEY} --container-name ${AZURE_CONTAINER_NAME} --name ${APPLICATION_ZIP} --permissions r --expiry ${expiryFormatted} -o tsv > sas_token.txt"
                    //this bat will execute this sas command
                    bat(script: sasTokenCommand, returnStatus:true)
                    //now read the sas token from the file
                    def sasTokenFile = readFile('sas_token.txt').trim()
                    echo "Generated SAS Token: ${sasTokenFile}"
                    //now push that sas token in an environment veriable
                    env.SAS_TOKEN = sasTokenFile
                }
            }
        }
        stage('Deploy to azure VM'){
            steps {
                script {
                    //create the blob storage url
                    def blobUrl = "https://${AZURE_STORAGE_ACCOUNT}.blob.core.windows.net/${AZURE_CONTAINER_NAME}/${APPLICATION_ZIP}?@{evn.SAS_TOKEN}"
                    echo "BLOB URL:${blobUrl}"
                    echo "SAS: ${evn.SAS_TOKEN}"
                    //construct powershell script with proper escaping
                    def powershellScript = """
                    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
                    \$storageUrl = '${blobUrl}'
                    \$destinationPath = 'D:\\dotnetapp\\${APPLICATION_ZIP}'
                    if (Test-Path \$destinationPath) { Remove-Item \$destinationPath -Force }
                    Write-Output "Downloading file from \$storageUrl to \$destinationPath"
                    Invoke-WebRequest -Uri \$storageUrl -OutFile \$destinationPath"
                    Write-Output "Extracting files to D:\\dotnetapp"
                    Expand Archive -Path \$destinationPath -DestinationPath 'D:\\dotnetapp' -Force
                    Write-Output "Deployment Completed"
                    """
                    //save the powershell script to the file
                    writeFile file: 'deploy-ps1', text: powershellScript
                    //run powershell script on vm
                    bat """
                    az vm run-command invoke -g ${AZURE_RESOURCE_GROUP} -n {AZURE_VM_NAME} --command-id RunPowerShellScript --scripts @deploy.ps1
                    """
                }
            }
        }
    }

    post {
        success {
            echo 'Build, test, and publish successful!'
        }
    }
}
