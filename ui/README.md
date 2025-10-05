Stellar Ascendance – Media Upload, Thumbnailing & Library

End-to-end stack for uploading media, auto-generating thumbnails via Azure Functions (Event Grid trigger + FFmpeg), and managing a user’s media library through an ASP.NET Core Web API with an Angular (Tailwind) UI.

/api        → ASP.NET Core Web API (JWT-secured + API key endpoint)
/functions  → Azure Functions (Event Grid trigger → FFmpeg → thumbnail upload → API notify)
/ui         → Angular app (Tailwind) for upload & browsing

Overview

Flow

[UI] --(multipart/form-data)--> [API] POST /api/UploadEncodeAndStreamFiles/upload-file
   ↳ API streams file to Azure Blob Storage: input-{userId}/<filename>

[Azure Storage] --(Event Grid: BlobCreated)--> [Azure Function] (EventGridTrigger)
   ↳ downloads the new blob
   ↳ (videos) runs FFmpeg (frame @ 00:00:02, size 120x80)
   ↳ uploads thumbnail → thumbnails/<name>_thumbnail.png (same container)
   ↳ POSTs { videoAzureUrl, thumbnailAzureUrl } to
      /api/MediaItems/add-media-item-thumbnail  (header: X-Api-Key)

[API]
   ↳ stores/updates the DB row under the **uploading user** with:
      - a reference to the original asset (video/image) `azureUrl`
      - (videos) a reference to the generated `ThumbnailUrl`

Client access pattern (mobile app & Angular UI)

After upload, the API owns the references for both the asset and (for videos) the generated thumbnail under the user who uploaded it.

Clients (mobile or Angular UI) only need the ThumbnailUrl to render fast previews—saving compute and time by avoiding full video loads or client-side thumbnail generation. Use the original azureUrl only when the user opens/plays the asset.

Components
🔹 Azure Function (/functions)

Trigger: EventGridTrigger on Storage BlobCreated (and future: DeleteBlob).

Entry: Function1.RunAsync(...).

Helpers: DeserializeBlobUrl(...), IsVideoFile(...), CleanPathSegment(...).

FFmpeg path (App Service): ${HOME}/site/wwwroot/ffmpeg/ffmpeg.exe.

Local test trigger URL:

http://localhost:7071/runtime/webhooks/EventGrid?functionName=Function1


Note: The function generates & uploads the thumbnail first (for videos). The HTTP POST to the API is a notification/metadata update so the UI/mobile can rely on DB records (instead of listing blobs) and directly use ThumbnailUrl.

🔹 Web API (/api)

Controllers

MediaItemsController

POST /api/MediaItems/create-media-item (JWT) — create DB entry for file/folder.

POST /api/MediaItems/add-media-item-thumbnail (API key) — link thumbnail to video by azureUrl.

GET /api/MediaItems/get-media-items?id=<folderId|null> (JWT) — returns { folders, files, path } for current user.

GET /api/MediaItems/get-media-item-by-id?Id=<id> (JWT)

PATCH /api/MediaItems/patch-media-item (JWT) — rename/update description; validates duplicate names.

DELETE /api/MediaItems/delete-media-item?id=<id> (JWT) — deletes blob and its thumbnail, then DB row.

UploadEncodeAndStreamFilesController

POST /api/UploadEncodeAndStreamFiles/upload-file (JWT) — accepts multipart/form-data; streams to input-{userId}/<filename>; returns Azure URL.

Storage layout

input-{userId}/<filename>
input-{userId}/thumbnails/<name>_thumbnail.png

🔹 UI (/ui)

Angular + Tailwind.

Authenticated upload to API, list folders/files, show thumbnails when available.

Prerequisites

.NET 8/9 SDK (API & Functions)

Node.js 18+ (Angular CLI recommended)

Azure Functions Core Tools

Azure Storage (or Azurite locally)

FFmpeg (locally on PATH, or adjust function path)

Configuration (local)

Do not commit secrets. Use environment variables, dotnet user-secrets, or Azure Key Vault in prod.

Functions — functions/local.settings.json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "WebApiBaseUrl": "https://localhost:7270",
    "WebApiKey": "dev-api-key"
  }
}

API — api/appsettings.Development.json (example)
{
  "Authentication": {
    "ApiKey": "dev-api-key"
  },
  "JWT": {
    "ValidAudiences": ["http://localhost:4200/", "http://localhost:19006/"],
    "ValidIssuers": ["https://localhost:7270/"],
    "Secret": "dev-jwt-secret"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=FitnessDemo;Trusted_Connection=True;Encrypt=False",
    "AzureStorageConnection": "UseDevelopmentStorage=true"
  }
}

Run locally
API
cd api
dotnet restore
dotnet run
# Serves at https://localhost:7270 (per launchSettings.json)

Functions
cd functions
dotnet restore
func start
# http://localhost:7071

UI
cd ui
npm install
npm start   # or: ng serve
# http://localhost:4200

Testing
1) Upload from UI

Sign in, upload a video/image → API stores to input-{userId}/<filename> and returns URL.

2) Simulate Event Grid (manual)
curl -X POST "http://localhost:7071/runtime/webhooks/EventGrid?functionName=Function1" \
  -H "aeg-event-type: Notification" \
  -H "Content-Type: application/json" \
  -d '[
    {
      "id":"1",
      "eventType":"Microsoft.Storage.BlobCreated",
      "subject":"/blobServices/default/containers/input-USERID/blobs/sample.mp4",
      "eventTime":"2023-01-01T00:00:00Z",
      "data":{
        "api":"PutBlob",
        "url":"http://127.0.0.1:10000/devstoreaccount1/input-USERID/sample.mp4"
      },
      "dataVersion":"1",
      "metadataVersion":"1",
      "topic":"/subscriptions/xxx/resourceGroups/xxx/providers/Microsoft.Storage/storageAccounts/xxx"
    }
  ]'

3) API notification (what the Function does for videos)
POST {WebApiBaseUrl}/api/MediaItems/add-media-item-thumbnail
X-Api-Key: {WebApiKey}
Content-Type: application/json

{
  "videoAzureUrl": "https://.../input-USERID/sample.mp4",
  "thumbnailAzureUrl": "https://.../input-USERID/thumbnails/sample_thumbnail.png"
}

Azure setup (Event Grid → Function)

Deploy the Function App (func azure functionapp publish <function-app-name>).

In Storage Account → Events → + Event Subscription.

Event type: BlobCreated.
Endpoint: your Function’s Event Grid trigger (select the Function destination).

Save.

FFmpeg on Azure Functions

Default expected at:
${HOME}/site/wwwroot/ffmpeg/ffmpeg.exe

Options:

Include FFmpeg binary in functions/ffmpeg/ and deploy (mind licensing).

Or change the path in code.

Quote arguments to handle spaces:

psi.Arguments = $"-i \"{tempVideoFilePath}\" -ss 00:00:02 -vframes 1 -s 120x80 \"{tempThumbnailFilePath}\"";

Security & Secrets

Never commit keys/passwords/tokens.

Store secrets in env vars, user-secrets, or Key Vault.

If any secret was ever committed, rotate it immediately (AAD app secret, JWT secret, SendGrid key, SQL password, Storage keys).

Troubleshooting

UI folder appears as submodule on GitHub
If ui/ shows an arrow and git add . fails with “does not have a commit checked out”, remove submodule remnants:

git rm -f --cached ui
if (Test-Path .gitmodules) {
  git config -f .gitmodules --remove-section submodule.ui 2>$null
  git add .gitmodules
}
if (Test-Path .git\modules\ui) { Remove-Item -Recurse -Force .git\modules\ui }
if (Test-Path ui\.git)        { Remove-Item -Recurse -Force ui\.git }
git add ui
git commit -m "Track ui as regular directory"
git push


Function runs but thumbnail not uploaded

Ensure AzureWebJobsStorage is valid (or Azurite running).

Verify container exists (input-{userId}) and userId matches.

Confirm FFmpeg path and that the video is a supported extension.

Function logs show FFmpeg output but file missing

Use File.Exists(tempThumbnailFilePath) (not string length) to validate output.

Ensure temp directories exist and you don’t delete the directory with File.Delete(...). Use:

if (Directory.Exists(tempVideoDirectory)) Directory.Delete(tempVideoDirectory, true);
if (File.Exists(tempThumbnailFilePath)) File.Delete(tempThumbnailFilePath);


Why it “works” without the POST?
Because the thumbnail exists in storage already. The POST only updates your DB so the UI/mobile can query once and use ThumbnailUrl for previews.
