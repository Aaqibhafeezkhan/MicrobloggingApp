# Microblogging Application

MicrobloggingApp is a simple microblogging platform that allows users to create short posts (up to 140 characters), upload images, and view a timeline of posts. The application includes real-time updates using SignalR, search and filter functionality, and robust backend API services. It is designed using .NET 8, Razor Pages for the frontend, and Azure Blob Storage for image storage.

---

## **Features**
- **Post Creation:** Create short text posts with optional image attachments.
- **Timeline Display:** View all posts in chronological order with optimized image rendering.
- **Real-Time Updates:** See new posts instantly without refreshing, powered by SignalR.
- **Search and Filters:** Search posts by keywords or filter them by date.
- **Post Management:** Edit or delete posts directly from the timeline.

---

## **Technologies Used**
- **Backend:** .NET 8 (ASP.NET Core Web API)
- **Frontend:** Razor Pages
- **Database:** SQL Server
- **Image Processing:** ImageSharp
- **Real-Time Updates:** SignalR
- **Cloud Storage:** Azure Blob Storage
- **Hosting:** Azure App Service
- **CI/CD:** Azure DevOps Pipelines / GitHub Actions

---

## **Deployment Steps**

### **Backend Deployment**

1. **Publish the Backend API:**
   - Navigate to the API project directory:
     ```bash
     cd MicrobloggingApp.API
     ```
   - Publish the project:
     ```bash
     dotnet publish -c Release -o ./publish
     ```

2. **Create an Azure App Service:**
   - Log in to the Azure Portal and create a new App Service for the backend.
   - Select the runtime stack as `.NET 8`.

3. **Deploy to Azure:**
   - Use Azure CLI to deploy:
     ```bash
     az webapp deploy --resource-group <YourResourceGroup> --name <YourBackendAppName> --src-path ./publish
     ```

4. **Set Configuration in Azure:**
   - Go to your App Service in Azure → **Configuration** → **Application Settings**.
   - Add the following settings:
     - `AzureBlobStorage:ConnectionString`: Your Azure Blob Storage connection string.
     - `AzureBlobStorage:ContainerName`: Your container name.

5. **Test the API:**
   - Open the App Service URL in your browser and access the API endpoints (e.g., `/swagger`).

---

### **Frontend Deployment**

1. **Publish the Frontend:**
   - Navigate to the frontend project directory:
     ```bash
     cd MicrobloggingApp.Frontend
     ```
   - Publish the project:
     ```bash
     dotnet publish -c Release -o ./publish
     ```

2. **Create an Azure App Service:**
   - Log in to the Azure Portal and create a new App Service for the frontend.
   - Select the runtime stack as `.NET 8`.

3. **Deploy to Azure:**
   - Use Azure CLI to deploy:
     ```bash
     az webapp deploy --resource-group <YourResourceGroup> --name <YourFrontendAppName> --src-path ./publish
     ```

4. **Update API Base URL in the Frontend:**
   - Ensure the frontend is configured to communicate with the deployed backend by updating the base URL in `Program.cs`:
     ```csharp
     builder.Services.AddHttpClient("MicrobloggingAPI", client =>
     {
         client.BaseAddress = new Uri("https://<YourBackendAppName>.azurewebsites.net/");
     });
     ```

5. **Test the Frontend:**
   - Open the frontend App Service URL and test all features (e.g., post creation, timeline updates).

---

## **Setting Up CI/CD for Automatic Deployment**

### **Using Azure DevOps Pipelines**

1. **Create a New Pipeline:**
   - In Azure DevOps, create a new pipeline linked to your Git repository.

2. **Add the Following YAML Configuration:**
   - This pipeline will build and deploy both the API and frontend projects:
     ```yaml
     trigger:
       branches:
         include:
           - main

     pool:
       vmImage: 'windows-latest'

     stages:
       - stage: Build
         jobs:
           - job: Build
             steps:
               - task: UseDotNet@2
                 inputs:
                   packageType: 'sdk'
                   version: '8.0.x'

               - script: dotnet build MicrobloggingApp.sln --configuration Release
                 displayName: 'Build Solution'

       - stage: Deploy_Backend
         dependsOn: Build
         jobs:
           - job: DeployBackend
             steps:
               - script: dotnet publish MicrobloggingApp.API/MicrobloggingApp.API.csproj --configuration Release --output ./publish
                 displayName: 'Publish Backend API'

               - task: AzureWebApp@1
                 inputs:
                   azureSubscription: '<YourAzureSubscription>'
                   appType: 'webApp'
                   appName: '<YourBackendAppName>'
                   package: './publish'

       - stage: Deploy_Frontend
         dependsOn: Build
         jobs:
           - job: DeployFrontend
             steps:
               - script: dotnet publish MicrobloggingApp.Frontend/MicrobloggingApp.Frontend.csproj --configuration Release --output ./publish
                 displayName: 'Publish Frontend'

               - task: AzureWebApp@1
                 inputs:
                   azureSubscription: '<YourAzureSubscription>'
                   appType: 'webApp'
                   appName: '<YourFrontendAppName>'
                   package: './publish'
     ```

3. **Run the Pipeline:**
   - Trigger the pipeline and monitor the deployment logs in Azure DevOps.

---

### **Using GitHub Actions**

1. **Create a GitHub Actions Workflow:**
   - Add a file named `.github/workflows/deploy.yml` to your repository.

2. **Add the Following Configuration:**
   ```yaml
   name: Deploy to Azure

   on:
     push:
       branches:
         - main

   jobs:
     build:
       runs-on: ubuntu-latest

       steps:
       - name: Checkout Code
         uses: actions/checkout@v3

       - name: Set Up .NET
         uses: actions/setup-dotnet@v3
         with:
           dotnet-version: '8.0.x'

       - name: Build Solution
         run: dotnet build MicrobloggingApp.sln --configuration Release

     deploy_backend:
       needs: build
       runs-on: ubuntu-latest

       steps:
       - name: Publish Backend
         run: dotnet publish MicrobloggingApp.API/MicrobloggingApp.API.csproj --configuration Release --output ./publish

       - name: Deploy Backend to Azure
         uses: azure/webapps-deploy@v2
         with:
           app-name: '<YourBackendAppName>'
           slot-name: 'production'
           publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}

     deploy_frontend:
       needs: build
       runs-on: ubuntu-latest

       steps:
       - name: Publish Frontend
         run: dotnet publish MicrobloggingApp.Frontend/MicrobloggingApp.Frontend.csproj --configuration Release --output ./publish

       - name: Deploy Frontend to Azure
         uses: azure/webapps-deploy@v2
         with:
           app-name: '<YourFrontendAppName>'
           slot-name: 'production'
           publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
